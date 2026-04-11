USE CleantopiaCRM;
GO

IF COL_LENGTH(N'dbo.Quotes', N'ServiceAddressId') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceAddressId INT NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'ServiceAddressText') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceAddressText NVARCHAR(1000) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'ContactName') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ContactName NVARCHAR(250) NULL;
END
GO

IF COL_LENGTH(N'dbo.Quotes', N'ContactPhone') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ContactPhone NVARCHAR(20) NULL;
END
GO
