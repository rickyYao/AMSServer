﻿--为当前相对日订单生成罚息科目================================================================================
INSERT INTO BillItem (BillID, [Subject], Amount, DueAmt, ReceivedAmt, 
CreateTime, SubjectType, OperatorID, IsCurrent, IsShelve, BusinessID)
SELECT bl.BillID, 23, 0, 0, 0, getdate(), 0, 0, bl.IsCurrent, bl.IsShelve, bl.BusinessID
FROM Bill bl
JOIN Business bs ON bl.BusinessID = bs.BusinessID
WHERE bl.BillType IN (1, 2)
AND bl.IsCurrent = 1 
AND bs.BusinessStatus IN (1, 2)
AND bs.ServiceSideID = {0}
AND bs.LendingSideID = {1}
AND bs.ServiceSideKey = 'COMPANY/WX_CDWS_LENDING'
AND bs.PeriodType = 32
AND bs.IsRepayment = 1
AND bs.FrozenNo = ''
AND EXISTS (
	SELECT *
	FROM BillItem xbi
	JOIN Bill xbl ON xbi.BillID = xbl.BillID
	WHERE xbi.Subject IN (1, 2, 3) AND xbl.BusinessID = bs.BusinessID 
	AND xbi.DueAmt > xbi.ReceivedAmt AND xbl.BillType IN (1, 2) AND xbl.IsShelve = 0
)
AND NOT EXISTS (
	SELECT *
	FROM BillItem xbi
	WHERE xbi.Subject = 23 
	AND xbi.BillID = bl.BillID
)

--为逾期帐单参与罚息生成的科目分别生成罚息（本金、利息、管理费）
INSERT INTO PenaltyInt (BusinessID, ReasonID, ReasonItemID, ToBillID, Amount, IsShelve, CreateTime)
SELECT bl.BusinessID, bl.BillID, bi.BillItemID, cbl.BillID,
	bi.DueAmt * bs.PenaltyRate * 
		(DATEDIFF(DAY, (CASE WHEN bl.LimitTime >= '{3}' THEN bl.LimitTime ELSE '{3}' END),
			'{2}') + 1) - bi.PenaltyIntAmt,
0, getdate()
FROM Bill bl
JOIN BillItem bi ON bl.BillID = bi.BillID
JOIN Business bs ON bl.BusinessID = bs.BusinessID
JOIN Bill cbl ON cbl.BusinessID = bs.BusinessID AND cbl.IsCurrent = 1 AND cbl.BillType IN (1, 2)
WHERE bl.BillType IN (1, 2)
AND bl.BillStatus <> 3
AND bi.Subject IN (1, 2, 3)
AND bs.CLoanStatus = 1
AND bs.BusinessStatus IN (1, 2)
AND bs.ServiceSideID = {0}
AND bs.LendingSideID = {1}
AND bs.ServiceSideKey = 'COMPANY/WX_CDWS_LENDING'
AND bs.ProductKind = 'PRODUCTKIND/WUDIYADAIKUAN'
AND bs.IsRepayment = 1
AND bi.DueAmt > bi.ReceivedAmt
AND bs.FrozenNo = ''
ORDER BY bl.BusinessID DESC

INSERT INTO PenaltyInt (BusinessID, ReasonID, ReasonItemID, ToBillID, Amount, IsShelve, CreateTime)
SELECT bl.BusinessID, bl.BillID, bi.BillItemID, cbl.BillID,
	bi.DueAmt * bs.PenaltyRate * 
		(DATEDIFF(DAY, (CASE WHEN bl.LimitTime >= '2013/7/21' THEN bl.LimitTime ELSE '2013/7/21' END),
			'{2}') + 1) - bi.PenaltyIntAmt,
0, getdate()
FROM Bill bl
JOIN BillItem bi ON bl.BillID = bi.BillID
JOIN Business bs ON bl.BusinessID = bs.BusinessID
JOIN Bill cbl ON cbl.BusinessID = bs.BusinessID AND cbl.IsCurrent = 1 AND cbl.BillType IN (1, 2)
WHERE bl.BillType IN (1, 2)
AND bl.BillStatus <> 3
AND bi.Subject IN (1, 2, 3)
AND bs.CLoanStatus = 1
AND bs.BusinessStatus IN (1, 2)
AND bs.ServiceSideID = {0}
AND bs.LendingSideID = {1}
AND bs.ServiceSideKey = 'COMPANY/WX_CDWS_LENDING'
AND bs.ProductKind = 'PRODUCTKIND/CHENGDUCHEDAI'
AND bs.IsRepayment = 1
AND bi.DueAmt > bi.ReceivedAmt
AND bs.FrozenNo = ''
ORDER BY bl.BusinessID DESC

--为今日增加过罚息的科目进行分别统计
UPDATE BillItem SET PenaltyIntAmt = ISNULL(
	(
		SELECT SUM(xp.Amount) FROM PenaltyInt xp WHERE xp.ReasonItemID = bi.BillItemID
	), 0)
FROM Bill bl
JOIN BillItem bi ON bl.BillID = bi.BillID
JOIN Business bs ON bl.BusinessID = bs.BusinessID
WHERE bl.BillType IN (1, 2)
AND bl.BillStatus <> 3
AND bi.Subject IN (1, 2, 3)
AND bs.PeriodType = 32 
AND bs.CLoanStatus = 1
AND bs.BusinessStatus IN (1, 2)
AND bs.ServiceSideID = {0}
AND bs.LendingSideID = {1}
AND bs.ServiceSideKey = 'COMPANY/WX_CDWS_LENDING'
AND bs.IsRepayment = 1
AND bs.FrozenNo = ''
AND bi.DueAmt > bi.ReceivedAmt

--更新当期帐单罚息科目
UPDATE BillItem SET Amount = ISNULL(
	(
		SELECT SUM(xp.Amount) FROM PenaltyInt xp WHERE xp.ToBillID = bl.BillID
	), 0)
FROM Bill bl
JOIN BillItem bi ON bl.BillID = bi.BillID
JOIN Business bs ON bl.BusinessID = bs.BusinessID
WHERE bl.BillType IN (1, 2)
AND bs.PeriodType = 32
AND bs.CLoanStatus = 1
AND bs.BusinessStatus IN (1, 2)
AND bs.ServiceSideID = {0}
AND bs.LendingSideID = {1}
AND bs.ServiceSideKey = 'COMPANY/WX_CDWS_LENDING'
AND bs.IsRepayment = 1
AND bs.FrozenNo = ''
AND bl.IsCurrent = 1
AND bi.Subject = 23
AND EXISTS (
	SELECT *
	FROM BillItem xbi
	JOIN Bill xbl ON xbi.BillID = xbl.BillID
	WHERE xbi.Subject IN (1, 2, 3) AND xbl.BusinessID = bs.BusinessID --如本金或利息未还清，则生成本息扣失科目
	AND xbi.DueAmt > xbi.ReceivedAmt
)

UPDATE BillItem SET DueAmt = Amount + ISNULL(
	(
		SELECT SUM(xr.Amount) FROM Received xr WHERE xr.BillItemID = bi.BillItemID
		AND xr.ReceivedType < 10
	), 0)
FROM Bill bl
JOIN BillItem bi ON bl.BillID = bi.BillID
JOIN Business bs ON bl.BusinessID = bs.BusinessID
WHERE bl.BillType IN (1, 2)
AND bs.PeriodType = 32
AND bs.CLoanStatus = 1
AND bs.BusinessStatus IN (1, 2)
AND bs.ServiceSideID = {0}
AND bs.LendingSideID = {1}
AND bs.ServiceSideKey = 'COMPANY/WX_CDWS_LENDING'
AND bs.IsRepayment = 1
AND bs.FrozenNo = ''
AND bl.IsCurrent = 1
AND bi.Subject = 23
AND EXISTS (
	SELECT *
	FROM BillItem xbi
	JOIN Bill xbl ON xbi.BillID = xbl.BillID
	WHERE xbi.Subject IN (1, 2, 3) AND xbl.BusinessID = bs.BusinessID --如本金或利息未还清，则生成本息扣失科目
	AND xbi.DueAmt > xbi.ReceivedAmt
)
