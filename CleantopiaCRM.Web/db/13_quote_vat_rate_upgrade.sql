-- 13_quote_vat_rate_upgrade.sql
-- Bo sung VAT cho bao gia

IF COL_LENGTH(N'dbo.Quotes', N'VatRate') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes
    ADD VatRate DECIMAL(5,2) NOT NULL
        CONSTRAINT DF_Quotes_VatRate DEFAULT(8);
END
GO

UPDATE dbo.Quotes
SET VatRate = 8
WHERE VatRate IS NULL;
GO
