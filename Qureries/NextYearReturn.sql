WITH EquityHistoryWithNextYear AS (
    SELECT
        e1.SimFinId,
        e1.Date AS CurrentDate,
        e1.AdjClose AS CurrentAdjClose,
        e2.Date AS NextYearDate,
        e2.AdjClose AS NextYearAdjClose
    FROM
        dbo.EquityHistory e1
    LEFT JOIN
        dbo.EquityHistory e2
    ON
        e1.SimFinId = e2.SimFinId
        AND e2.Date = DATEADD(DAY, 365, e1.Date)
)
SELECT
    SimFinId,
    CurrentDate,
    CurrentAdjClose,
    NextYearDate,
    NextYearAdjClose,
    CASE
        WHEN NextYearAdjClose IS NOT NULL AND CurrentAdjClose IS NOT NULL THEN
            ((NextYearAdjClose - CurrentAdjClose) / CurrentAdjClose) * 100
        ELSE
            NULL
    END AS PercentIncrease
FROM
    EquityHistoryWithNextYear
ORDER BY
    SimFinId,
    CurrentDate;