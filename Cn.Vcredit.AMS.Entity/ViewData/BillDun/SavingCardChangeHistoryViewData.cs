﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cn.Vcredit.AMS.Entity.ViewData.BillDun
{
    /// <summary>
    /// Author:姚海凡
    /// CreateTime:2014年8月21日
    /// Description:历史修改信息实体类
    /// </summary>
    public class SavingCardChangeHistoryViewData
    {
        /// <summary>
        /// 总件数
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 业务号
        /// </summary>
        public int BusinessID { get; set; }
        /// <summary>
        /// 银行卡号(原)
        /// </summary>
        public string OriginalSavingCard { get; set; }
        /// <summary>
        /// 银行户名(原)
        /// </summary>
        public string OriginalSavingUser { get; set; }
        /// <summary>
        /// 银行(原)
        /// </summary>
        public string OriginalBankKey { get; set; }
        /// <summary>
        /// 银行开户支行(原)
        /// </summary>
        public string OriginalSubBranch { get; set; }
        /// <summary>
        /// 修改后的银行卡号
        /// </summary>
        public string ChangeSavingCard { get; set; }
        /// <summary>
        /// 修改后的银行户名
        /// </summary>
        public string ChangeSavingUser { get; set; }
        /// <summary>
        /// 修改后的银行
        /// </summary>
        public string ChangeBankKey { get; set; }
        /// <summary>
        /// 修改后的银行开户支行
        /// </summary>
        public string ChangeSubBranch { get; set; }
        /// <summary>
        /// 凭证名称
        /// </summary>
        public string MeanName { get; set; }
        /// <summary>
        /// 凭证路径
        /// </summary>
        public string MeanPath { get; set; }
        /// <summary>
        /// 地区
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// 门店
        /// </summary>
        public string BranchKey { get; set; }
        /// <summary>
        /// 门店名称
        /// </summary>
        public string BranckName { get; set; }
    }
}
