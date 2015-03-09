﻿using Cn.Vcredit.AMS.Data.DB.Data;
using Cn.Vcredit.Common.ExcelExport;
using Cn.Vcredit.Common.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cn.Vcredit.AMS.Logic.BLL.PayBank.Export
{
	/// <summary>
	/// Author:陈伟
	/// CreateTime:2012年6月8日
	/// 导出模版五 信托方明细
	/// 格式为：
	/// </summary>
	public class BankExportEight : ExportTemplate2
	{
		#region- 功能函数 -

		#region 返回字节数
        public override byte[] RetByte(List<string[]> list, int operateid, string title)
        {
            string[] titles = {"序号","合同号","	姓名","身份证号","名义金额","月本金",
                "月利息","扣款失败手续费","罚息","总合计","月利率","贷款期限","账号","开户省市","放贷月份","扣款月份","科目名称"};
            XlsDocument xls = new XlsDocument();
            xls.FileName = title;
            Worksheet wks = xls.Workbook.Worksheets.Add(title);
            Cells cell = wks.Cells;

            XF xf = ExportExtend.SetCommonStyle(xls);
            ExportExtend.SetColumnWidth(xls, wks, 1, 20);
            ExportExtend.SetColumnWidth(xls, wks, 2, 11);
            ExportExtend.SetColumnWidth(xls, wks, 3, 25);
            ExportExtend.SetColumnWidth(xls, wks, 4, 12);
            ExportExtend.SetColumnWidth(xls, wks, 5, 12);
            ExportExtend.SetColumnWidth(xls, wks, 6, 12);
            ExportExtend.SetColumnWidth(xls, wks, 7, 15);
            ExportExtend.SetColumnWidth(xls, wks, 8, 12);
            ExportExtend.SetColumnWidth(xls, wks, 9, 12);
            ExportExtend.SetColumnWidth(xls, wks, 10, 12);
            ExportExtend.SetColumnWidth(xls, wks, 11, 10);
            ExportExtend.SetColumnWidth(xls, wks, 12, 25);
            ExportExtend.SetColumnWidth(xls, wks, 14, 13);
            ExportExtend.SetColumnWidth(xls, wks, 15, 13);
            for (int i = 1; i <= list.Count; i++)
            {
                if (i == 1)//序号
                {
                    for (int j = 1; j <= list[i - 1].Length; j++)
                    {
                        Cell cel = cell.Add(i, j, titles[j - 1], xf);
                    }
                }
                for (int j = 1; j <= list[i - 1].Length; j++)
                {
                    if (j == 1)
                    {
                        Cell cel = cell.Add(i + 1, 1, i, xf);
                    }
                    else if (j >= 5 && j<=10)//金额格式
                    {
                        Cell cel = cell.Add(i + 1, j, list[i - 1][j - 1].Trim().ToDecimal(), xf);
                    }
                    else if (j == 12)
                    {
                        Cell cel = cell.Add(i + 1, j, list[i - 1][j - 1].Trim().ToInt(), xf);
                    }
                    else
                    {
                        Cell cel = cell.Add(i + 1, j, list[i - 1][j - 1].Trim(), xf);
                    }
                }
            }
            return xls.Bytes.ByteArray;
        }
		#endregion

		#region 导出内容转换
		/// <summary>
		/// 转换导出的内容
		/// </summary>
		/// <param name="list"></param>
		/// <param name="num"></param>
		/// <param name="customer"></param>
		/// <param name="enums"></param>
		/// <returns></returns>
        public override string[] GetImportExcelItem(PayBankExportItem item, decimal amount,
           List<BankAccount> bankaccounts, List<Enumeration> enums, BankAccount bka, bool isgurante = false)
        {
            string[] strlist = 
			{
                "",
				item.ContractNo.Trim(),
				item.SavingUser.Trim(),
                item.IdenNo.Trim(),
				item.LoanCapital.ToString(),
                item.BJ.ToString(),
				item.LX.ToString(),
                item.BXKS.ToString(),
                item.FX.ToString(),
                amount.ToString(),
				item.InterestRate.ToPercent(),
                item.LoanPeriod.ToString(),
                item.SavingCard.Trim(),
                bka.AreaName.Trim(),
                item.LoanTime.ToString("yyyy年MM月"),
                item.BillMonth.Trim().Replace("/","年")+"月",
                GetSpouse(item.DunLevel)
			};
            return strlist;
        }
		#endregion

		#endregion
	}
}
