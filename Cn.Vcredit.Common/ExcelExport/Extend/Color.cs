using System;
using System.Collections.Generic;
using System.Text;
using Cn.Vcredit.Common.ExcelExport.ByteUtil;

namespace Cn.Vcredit.Common.ExcelExport
{
    /// <summary>
    /// Excel导出扩展
    /// </summary>
    public static class ExportExtend 
    {
        /// <summary>
        /// 设置某一列值的宽度
        /// </summary>
        /// <param name="xls"></param>
        /// <param name="wks"></param>
        /// <param name="columnindex"></param>
        /// <param name="widthline"></param>
        public static void SetColumnWidth(XlsDocument xls, Worksheet wks, ushort columnindex, ushort widthline)
        {
            ColumnInfo colinfo = new ColumnInfo(xls, wks);
            colinfo.ColumnIndexStart = columnindex;
            colinfo.ColumnIndexEnd = columnindex;
            colinfo.Width = (ushort)(widthline * 256);
            wks.AddColumnInfo(colinfo);
        }
    }
}
