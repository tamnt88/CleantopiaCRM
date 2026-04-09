USE CleantopiaCRM;
GO

IF COL_LENGTH('dbo.Customers', 'CustomerSource') IS NULL
BEGIN
    ALTER TABLE dbo.Customers
    ADD CustomerSource NVARCHAR(100) NOT NULL CONSTRAINT DF_Customers_CustomerSource DEFAULT N'Trực tiếp';
END
GO

IF COL_LENGTH('dbo.Customers', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Customers
    ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSDATETIME();
END
GO

UPDATE dbo.Customers
SET CustomerSource = N'Trực tiếp'
WHERE CustomerSource IS NULL OR LTRIM(RTRIM(CustomerSource)) = N'';
GO
