WITH YearReturns AS (
    SELECT
        y0.SimFinId,
        y0.Date AS CurrentDate,
        y0.AdjClose AS Y0AdjClose,
        y0.Date AS NextYearDate,
        y1.AdjClose AS Y1AdjClose,
        y2.AdjClose AS Y2AdjClose,
        y3.AdjClose AS Y3AdjClose,
        y5.AdjClose AS Y5AdjClose
    FROM
        dbo.EquityHistoryMSFT y0
    LEFT JOIN
        dbo.EquityHistoryMSFT y1
    ON
        y0.SimFinId = y1.SimFinId
        AND y1.Date = DATEADD(DAY, 364, y0.Date)
    LEFT JOIN
        dbo.EquityHistoryMSFT y2
    ON
        y0.SimFinId = y2.SimFinId
        AND y2.Date = DATEADD(DAY, 728, y0.Date)
    LEFT JOIN
        dbo.EquityHistoryMSFT y3
    ON
        y0.SimFinId = y3.SimFinId
        AND y3.Date = DATEADD(DAY, 1092, y0.Date)
    LEFT JOIN
        dbo.EquityHistoryMSFT y5
    ON
        y0.SimFinId = y5.SimFinId
        AND y5.Date = DATEADD(DAY, 1820, y0.Date)
)
SELECT
    SimFinId,
    CurrentDate,
    Y0AdjClose,
    NextYearDate,
    Y1AdjClose,
    Y2AdjClose,
    Y3AdjClose,
    Y5AdjClose,
    CASE
        WHEN Y1AdjClose IS NOT NULL AND Y0AdjClose IS NOT NULL AND Y0AdjClose > 0 THEN
            ((Y1AdjClose - Y0AdjClose) / Y0AdjClose) * 100
        ELSE
            NULL
    END AS Y1TotalReturn,
    CASE
        WHEN Y2AdjClose IS NOT NULL AND Y0AdjClose IS NOT NULL AND Y0AdjClose > 0 THEN
            ((Y2AdjClose - Y0AdjClose) / Y0AdjClose) * 100
        ELSE
            NULL
    END AS Y2TotalReturn,
    CASE
        WHEN Y3AdjClose IS NOT NULL AND Y0AdjClose IS NOT NULL AND Y0AdjClose > 0 THEN
            ((Y3AdjClose - Y0AdjClose) / Y0AdjClose) * 100
        ELSE
            NULL
    END AS Y3TotalReturn,
    CASE
        WHEN Y5AdjClose IS NOT NULL AND Y0AdjClose IS NOT NULL AND Y0AdjClose > 0 THEN
            ((Y5AdjClose - Y0AdjClose) / Y0AdjClose) * 100
        ELSE
            NULL
    END AS Y5TotalReturn
FROM
    YearReturns
ORDER BY
    SimFinId,
    CurrentDate DESC;