CREATE TABLE [dbo].[EquityHistory] (
    [ID]                               BIGINT        IDENTITY (1, 1) NOT NULL,
    [SimFinId]                         INT           NOT NULL,
    [Ticker]                           NVARCHAR (50) NOT NULL,
    [Date]                             DATETIME      NOT NULL,
    [Open]                             FLOAT (53)    NOT NULL,
    [High]                             FLOAT (53)    NOT NULL,
    [Low]                              FLOAT (53)    NOT NULL,
    [Close]                            FLOAT (53)    NOT NULL,
    [AdjClose]                         FLOAT (53)    NOT NULL,
    [Volume]                           BIGINT        NOT NULL,
    [Dividend]                         FLOAT (53)    NULL,
    [SharesOutstanding]                FLOAT (53)    NULL,
    [MarketCap]                        FLOAT (53)    NULL,
    [PriceToEarningsQuarterly]         FLOAT (53)    NULL,
    [PriceToEarningsTTM]               FLOAT (53)    NULL,
    [PriceToSalesQuarterly]            FLOAT (53)    NULL,
    [PriceToSalesTTM]                  FLOAT (53)    NULL,
    [PriceToBookValue]                 FLOAT (53)    NULL,
    [PriceToFreeCashFlowQuarterly]     FLOAT (53)    NULL,
    [PriceToFreeCashFlowTTM]           FLOAT (53)    NULL,
    [EnterpriseValue]                  FLOAT (53)    NULL,
    [EnterpriseValueToEBITDA]          FLOAT (53)    NULL,
    [EnterpriseValueToSales]           FLOAT (53)    NULL,
    [EnterpriseValueToFreeCashFlow]    FLOAT (53)    NULL,
    [BookToMarketValue]                FLOAT (53)    NULL,
    [OperatingIncomeToEnterpriseValue] FLOAT (53)    NULL,
    [AltmanZScore]                     FLOAT (53)    NULL,
    [DividendYield]                    FLOAT (53)    NULL,
    [PriceToEarningsAdjusted]          FLOAT (53)    NULL,
    [Y1Date]                           DATE          NULL,
    [Y1AdjClose]                       FLOAT (53)    NULL,
    [Y1Cagr]                           FLOAT (53)    NULL,
    [Y1TotalReturn]                    FLOAT (53)    NULL,
    [Y2Date]                           DATE          NULL,
    [Y2AdjClose]                       FLOAT (53)    NULL,
    [Y2Cagr]                           FLOAT (53)    NULL,
    [Y2TotalReturn]                    FLOAT (53)    NULL,
    [Y3Date]                           DATE          NULL,
    [Y3AdjClose]                       FLOAT (53)    NULL,
    [Y3Cagr]                           FLOAT (53)    NULL,
    [Y3TotalReturn]                    FLOAT (53)    NULL,
    [Y5Date]                           DATE          NULL,
    [Y5AdjClose]                       FLOAT (53)    NULL,
    [Y5Cagr]                           FLOAT (53)    NULL,
    [Y5TotalReturn]                    FLOAT (53)    NULL,
    [YearlyReturnsTimestamp]           DATE          NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_EquityHistory_SimFinId_Date]
    ON [dbo].[EquityHistory]([SimFinId] ASC, [Date] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_EquityHistory_Ticker_Date]
    ON [dbo].[EquityHistory]([Ticker] ASC, [Date] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_EquityHistory_Date_SimFinId]
    ON [dbo].[EquityHistory]([Date] ASC, [SimFinId] ASC);

