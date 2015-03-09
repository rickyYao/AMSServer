﻿using System;
using System.Collections.Generic;
using System.Linq;
using Cn.Vcredit.Common.Patterns;
using Cn.Vcredit.Common.TypeConvert;
using Cn.Vcredit.Common.Constants;
using Cn.Vcredit.AMS.Data.DB.Data;
using Cn.Vcredit.AMS.Common.Enums;
using Cn.Vcredit.AMS.BaseService.DAL;
using Cn.Vcredit.AMS.BaseService.Common;
using Cn.Vcredit.AMS.Common.Dictionary;

namespace Cn.Vcredit.AMS.BaseService.BLL.Products
{
    /// <summary>
    /// 静安小贷抵押
    /// </summary>
    public class JingAnMortgageLoan : VcreditProduct
    {

         #region- 字段属性 -
        /// <summary>
        /// 科目优先级
        /// </summary>
        private List<List<EnumCostSubject>> _Subjects;
        public List<List<EnumCostSubject>> Subjects
        {
            get
            {
                if (_Subjects == null)
                {
                    _Subjects = new List<List<EnumCostSubject>> { 
                    //手续费、保证金
                    new List<EnumCostSubject>{EnumCostSubject.Procedures,EnumCostSubject.Earnest},
                    //服务费
				    new List<EnumCostSubject>{EnumCostSubject.ServiceFee},
                    //本息扣失
				    new List<EnumCostSubject>{EnumCostSubject.InterestBuckleFail},
                    //罚息
				    new List<EnumCostSubject>{EnumCostSubject.PunitiveInterest},
                    //本息
				    new List<EnumCostSubject>{EnumCostSubject.Capital,EnumCostSubject.Interest}
                    };
                }
                return _Subjects;
            }
        }
        #endregion

        #region- 构造函数 -
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="business"></param>
        public JingAnMortgageLoan(Business business) : base(business) { }
        #endregion

        #region- 功能函数 -
        /// <summary>
        /// 创建基本款项（包括本金、利息、服务费、担保费）
        /// </summary>
        /// <param name="business">业务对象</param>
        /// <returns>返回款项集合</returns>
        public override List<BillItem> CreateBaseItems(bool isLast = false)
        {
            return GetBillItem(isLast);
        }

        private List<BillItem> GetBillItem(bool isLast = false)
        {
            List<BillItem> billItem = new List<BillItem>();

            // 添加信托方扣款项目
            if (Business.LendingSideID > 0)
			{
				decimal capital = 0.0M;

				// 判断当期帐单属于第几期帐单,最后一期普通帐单（本金按余额计）
				if (isLast)
					capital = SubjectFormula.CalcuateCapitalOver(Business.LoanCapital
						, Business.CapitalRate.HasValue ? Business.CapitalRate.Value : 0, Business.LoanPeriod);
				else
					capital = SubjectFormula.CalcuLateCapitalByRate(Business.LoanCapital
						, Business.CapitalRate.HasValue ? Business.CapitalRate.Value : 0);

				var interest = SubjectFormula.CalculatInterest(Business.LoanCapital, Business.InterestRate);

				billItem.Add(CreateBillItem(EnumCostSubject.Capital, capital));
				billItem.Add(CreateBillItem(EnumCostSubject.Interest, interest));
            }
            
            if (Business.ServiceRate > 0)
            {
                var service = SubjectFormula.CalculatServiceFee(Business.LoanCapital, Business.ServiceRate, false);
                billItem.Add(CreateBillItem(EnumCostSubject.ServiceFee, service));
            }
            return billItem;
        }

        /// <summary>
        /// 获取信用贷款每期应还基本科目
        /// </summary>
        /// <param name="business">业务对象</param>
        /// <returns>返回款项集合</returns>
        public override List<BillItem> GetBaseItemsByMonth()
        {
            return GetBillItem();
        }

        /// <summary>
        /// 获取信用贷款拥有的所有款项
        /// </summary>
        /// <returns>款项字典</returns>
        public override Dictionary<byte, string> GetProductItems()
        {
            return Singleton<SubjectDictionary>.Instance.JingAnMortgageLoanDic;
        }

        /// <summary>
        /// 创建扣失科目
        /// </summary>
        public override List<BillItem> CreateBuckleFail(DateTime limitTime)
        {
            List<BillItem> items = new List<BillItem>();

            // 获取当期帐单
            var curBill = BillDAL.GetNormalBills(Business.Bills).FirstOrDefault(p => p.BillMonth == _CurMonth);

            // 无当期帐单 或者 有全额支付时间且全额支付时间是在扣失日之前（包括当天）的则不生成扣失
            if (curBill == null || (limitTime >= curBill.FullPaidTime && curBill.FullPaidTime != null))
                return items;

            // 判断当期帐单是否应生成本息扣失
            List<byte> subjects = new List<byte> { (byte)EnumCostSubject.Capital, (byte)EnumCostSubject.Interest };
            if (IsCreateBuckleFail(subjects, curBill))
                items.Add(CreateBillItem(EnumCostSubject.InterestBuckleFail,
                    SubjectFormula.CalculatBuckleFail(Business.ContractNo), true, curBill.BillID));

            // 判断当期帐单是否应生成服务费担保费扣失
            subjects = new List<byte> { (byte)EnumCostSubject.ServiceFee, (byte)EnumCostSubject.GuaranteeFee };
            if (IsCreateBuckleFail(subjects, curBill))
                items.Add(CreateBillItem(EnumCostSubject.ServiceBuckleFail,
                    SubjectFormula.CalculatBuckleFail(Business.ContractNo), true, curBill.BillID));

            return items;
        }

        /// <summary>
        /// 创建罚息科目
        /// </summary>
        public override BillItem CreatePunitiveInterest(DateTime limitTime, List<PenaltyInt> penaltyInts)
        {
            // 获取当期帐单，为NULL或已在第三次扣失日前全额付清则继续循环
            var nbills = BillDAL.GetNormalBills(Business.Bills);
            var prevBill = nbills.FirstOrDefault(p => p.BillMonth == _PrevMonth);
            var curBill = nbills.FirstOrDefault(p => p.BillMonth == _CurMonth);

            // 判断是否存在原因帐单或当期帐单，不存在则返回NULL
            if (prevBill == null || curBill == null || Business.Bills.Count == 1
                || (limitTime >= prevBill.FullPaidTime && prevBill.FullPaidTime != null))
                return null;

            // 获取本金、利息或租金等参与科目，计算上期所欠本息或租金
            var subjects = new List<byte>{ (byte)EnumCostSubject.Capital, 
							(byte)EnumCostSubject.Interest, (byte)EnumCostSubject.Rent};

            // 是否为渤海订单, 渤海订单一次性生成一个月的罚息， 其他合同都按日罚息千分之一计算
            bool isBoHai = Business.LendingSideKey.EndsWith(EnumCompanyKey.BHXT_LENDING.ToEnumName());
            decimal debtsPrincipal = 0;
            if (isBoHai)
            {
                // 获取普通帐单的所欠本息
                var bills = BillDAL.GetArrearsBills(Business.Bills, true);
                foreach (var bill in bills)
                {
                    debtsPrincipal += bill.BillItems.Where(p => subjects.Contains(p.Subject) && !p.IsCurrent)
                        .Sum(p => p.DueAmt - p.ReceivedAmt);
                }

                // 判断欠费帐单是否存在未归还所欠本息或租金，无则不生成罚息
                if (debtsPrincipal <= 0)
                    return null;

                debtsPrincipal = SubjectFormula.CalculatPunitiveInterest(debtsPrincipal, 0.03m);
            }
            else
            {
                // 获取上期帐单的本息欠费
                debtsPrincipal = prevBill.BillItems.Where(p => subjects.Contains(p.Subject))
                    .Sum(s => s.DueAmt - s.ReceivedAmt);

                // 判断上期帐单是否存在未归还所欠本息或租金，无则不生成罚息
                if (debtsPrincipal <= 0)
                    return null;

                var beginDay = DateTime.Now.AddMonths(-1).SetDay(21);
                // 在罚息表中计算罚息金额
                CalculatPenaltyInt(penaltyInts, debtsPrincipal, prevBill.BillID, curBill.BillID, beginDay);

                // 当所欠本息大于零时，计算从上月21日至今天数的罚息
                var totalDays = DateTime.Now - beginDay;
                debtsPrincipal = SubjectFormula.CalculatPunitiveInterest(debtsPrincipal,
                    Business.PenaltyRate, totalDays.Days);
            }

            // 创建罚息科目（科目名称、金额）
            return CreateBillItem(EnumCostSubject.PunitiveInterest, debtsPrincipal, false, curBill.BillID);
        }

        /// <summary>
        /// 获取可足科填充科目（手工扣款规则）
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public override List<BillItem> GetFullPaymentItems(decimal amount)
        {
            List<Bill> bills = BillDAL.GetArrearsBills(Business.Bills).OrderBy(o => o.BillMonth).ToList();
            List<BillItem> items = new List<BillItem>();
            foreach (Bill bill in bills)
            {
                foreach (var subject in Subjects)
                {
                    var item = bill.BillItems.Where(p =>
                        subject.Contains(p.Subject.ValueToEnum<EnumCostSubject>()) && !p.IsShelve);
                    var amt = item.Sum(p => p.DueAmt - p.ReceivedAmt);
                    if (amt == 0)
                        continue;
                    if (amount >= amt)
                    {
                        amount -= amt;
                        items.AddRange(item);
                    }
                    else
                        return items;//手工扣款规则若发现无法填帐则直接停止遍历并返回
                    if (amount == 0)
                        break;
                }
                if (amount == 0)
                    break;
            }
            return items;
        }

        /// <summary>
        /// 获取可足科填充科目（自动填帐规则）
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public override List<BillItem> GetFillItems(decimal amount)
        {
            List<Bill> bills = BillDAL.GetArrearsBills(Business.Bills).OrderBy(o => o.CreateTime).ToList();
            List<BillItem> items = new List<BillItem>();
            foreach (Bill bill in bills)
            {
                foreach (var subject in Subjects)
                {
                    var item = bill.BillItems.Where(p =>
                        subject.Contains(p.Subject.ValueToEnum<EnumCostSubject>()) && !p.IsShelve);
                    var amt = item.Sum(p => p.DueAmt - p.ReceivedAmt);
                    if (amt <= 0)
                        continue;
                    if (amount >= amt)
                    {
                        amount -= amt;
                        items.AddRange(item);
                    }
                    if (amount == 0)
                        break;
                }
                if (amount == 0)
                    break;
            }
            return items;
        }

        /// <summary>
        /// 获取单笔扣款收款帐户编号
        /// 规则：
        /// 1、外贸未担保订单 => 收款归放贷方
        /// 2、渤海订单 => 收款归服务方
        /// 3、外贸担保订单 => 收款归服务方
        /// </summary>
        /// <returns></returns>
        public override int GetAccountPayeeID(EnumCostSubject subject)
        {
            return Business.LendingSideID;
        }

        #endregion

        public override List<byte> InterestBuckleFailSubjects
        {
            get
            {
                // 判断当期帐单是否应生成本息扣失
                List<byte> subjects = new List<byte> { (byte)EnumCostSubject.Capital, (byte)EnumCostSubject.Interest,
                            (byte)EnumCostSubject.ServiceFee
                };

                return subjects;
            }
        }

        public override List<byte> ServiceBuckleFailSubjects
        {
            get
            {
                // 判断当期帐单是否应生成本息扣失
                List<byte> subjects = new List<byte> { };

                return subjects;
            }
        }

        public override List<byte> PunitiveInterestSubjects
        {
            get
            {
                // 判断当期帐单是否应生成本息扣失
                List<byte> subjects = new List<byte> { (byte)EnumCostSubject.Capital, (byte)EnumCostSubject.Interest,
                                    (byte)EnumCostSubject.ServiceFee };

                return subjects;
            }
        }
    }
}
