IF COL_LENGTH(N'dbo.Customers', N'CustomerCode') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD CustomerCode NVARCHAR(30) NULL;
END
GO

UPDATE c
SET c.CustomerCode = CONCAT('KH-', RIGHT(CONCAT('00000', CAST(c.Id AS VARCHAR(20))), 5))
FROM dbo.Customers c
WHERE c.CustomerCode IS NULL OR LTRIM(RTRIM(c.CustomerCode)) = '';
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Customers_CustomerCode'
      AND object_id = OBJECT_ID(N'dbo.Customers')
)
BEGIN
    CREATE UNIQUE INDEX IX_Customers_CustomerCode
        ON dbo.Customers(CustomerCode)
        WHERE CustomerCode IS NOT NULL;
END
GO

