USE CleantopiaCRM;
GO

IF COL_LENGTH(N'dbo.Quotes', N'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes
    ADD DiscountAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_Quotes_DiscountAmount DEFAULT(0);
END
GO

IF COL_LENGTH(N'dbo.QuoteItems', N'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.QuoteItems
    ADD DiscountAmount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_QuoteItems_DiscountAmount DEFAULT(0);
END
GO

IF COL_LENGTH(N'dbo.QuoteItems', N'Note') IS NULL
BEGIN
    ALTER TABLE dbo.QuoteItems
    ADD Note NVARCHAR(500) NULL;
END
GO
