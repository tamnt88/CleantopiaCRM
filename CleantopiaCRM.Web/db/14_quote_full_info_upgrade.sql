USE CleantopiaCRM;
GO

IF COL_LENGTH(N'dbo.Quotes', N'ServiceProvinceId') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceProvinceId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'ServiceWardId') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceWardId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'HasVatInvoice') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD HasVatInvoice BIT NOT NULL CONSTRAINT DF_Quotes_HasVatInvoice DEFAULT(0);
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'InvoiceCompanyName') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceCompanyName NVARCHAR(250) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'InvoiceTaxCode') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceTaxCode NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'InvoiceAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceAddress NVARCHAR(500) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'InvoiceEmail') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceEmail NVARCHAR(250) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'InvoiceReceiver') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceReceiver NVARCHAR(250) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'SubtotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD SubtotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_SubtotalAmount DEFAULT(0);
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'VatAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_VatAmount DEFAULT(0);
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'TotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_TotalAmount DEFAULT(0);
END
GO
