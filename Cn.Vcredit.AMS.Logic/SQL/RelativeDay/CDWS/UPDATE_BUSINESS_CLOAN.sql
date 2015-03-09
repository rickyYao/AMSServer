﻿--订单提前清贷3, 坏帐清贷4，满约清贷2
UPDATE Business SET CLoanStatus =
	CASE WHEN EXISTS(
		SELECT bl.BusinessID
		FROM Bill bl 
		WHERE bl.BusinessID = bs.BusinessID
		AND bl.BillType = 4 
		AND bl.BillStatus = 3
	) THEN 4 WHEN ( 
		SELECT COUNT(*)
		FROM Bill bl 
		WHERE bl.BusinessID = bs.BusinessID
		AND bl.BillType IN (1, 2) 
		AND bl.BillStatus = 3
	) >=bs.LoanPeriod THEN 2
	  ELSE 3 END,
	ClearLoanTime = (
		SELECT MAX(xr.ReceivedTime) 
		FROM Received xr
		JOIN Bill xbl ON xr.BillID = xbl.BillID
		WHERE xbl.BusinessID = bs.BusinessID
	), IsRepayment = 0
FROM Business bs
WHERE bs.ResidualCapital < 1 AND bs.ResidualCapital > -1
AND bs.OverAmount = 0 
AND bs.CurrentOverAmount = 0 AND bs.OtherAmount = 0
AND bs.CLoanStatus <> 8 AND IsRepayment = 1
AND bs.BusinessID IN ({0})

--删除提前清贷成功订单的搁置科目
DELETE BillItem
FROM Business bs
JOIN Bill bl ON bs.BusinessID = bl.BusinessID
JOIN BillItem bi ON bl.BillID = bi.BillID
WHERE bs.ResidualCapital < 1 AND bs.ResidualCapital >= -1 
AND bs.OverAmount = 0 
AND bs.CurrentOverAmount = 0 AND bs.OtherAmount = 0
AND bs.CLoanStatus <> 8 AND IsRepayment = 1
AND bs.PeriodType = 32 
AND bi.IsShelve = 1
AND bs.BusinessID IN ({0})

--删除提前清贷成功订单的搁置帐单
DELETE Bill
FROM Business bs
JOIN Bill bl ON bs.BusinessID = bl.BusinessID
WHERE bs.ResidualCapital < 1 AND bs.ResidualCapital >= -1 
AND bs.OverAmount = 0 
AND bs.CurrentOverAmount = 0 AND bs.OtherAmount = 0
AND bs.CLoanStatus <> 8 AND IsRepayment = 1
AND bs.PeriodType = 32
AND bl.IsShelve = 1
AND bs.BusinessID IN ({0})

--删除不存在帐单的应收科目
DELETE BillItem
FROM BillItem bi WHERE NOT EXISTS
(
	SELECT * FROM Bill bl WHERE bl.BillID = bi.BillID
) AND bi.BillID <> 0

--设置清贷订单催收单的CancelTime为下一账单日日期
UPDATE
    dun
SET 
    dun.IsCurrent = 0 ,
    dun.CancelTime = bus.NextBillDate
FROM
    dbo.Business bus
    JOIN dbo.Dun dun ON dun.BusinessID = bus.BusinessID
WHERE
    bus.IsRepayment = 0
    AND dun.CancelTime IS NULL




