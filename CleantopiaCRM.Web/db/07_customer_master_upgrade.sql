USE CleantopiaCRM;
GO

/* Upgrade khach hang: nguon khach + loai khach + nhieu dia chi dich vu + thong tin hoa don */

IF OBJECT_ID('dbo.CustomerSources', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerSources (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CustomerSources_IsActive DEFAULT 1,
        SortOrder INT NOT NULL CONSTRAINT DF_CustomerSources_SortOrder DEFAULT 0,
        CONSTRAINT UQ_CustomerSources_Name UNIQUE(Name)
    );
END
GO

IF OBJECT_ID('dbo.CustomerTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerTypes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        IsBusiness BIT NOT NULL CONSTRAINT DF_CustomerTypes_IsBusiness DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_CustomerTypes_IsActive DEFAULT 1,
        SortOrder INT NOT NULL CONSTRAINT DF_CustomerTypes_SortOrder DEFAULT 0,
        CONSTRAINT UQ_CustomerTypes_Name UNIQUE(Name)
    );
END
GO

IF COL_LENGTH('dbo.Customers', 'CustomerSourceId') IS NULL
    ALTER TABLE dbo.Customers ADD CustomerSourceId INT NULL;
IF COL_LENGTH('dbo.Customers', 'CustomerTypeId') IS NULL
    ALTER TABLE dbo.Customers ADD CustomerTypeId INT NULL;
IF COL_LENGTH('dbo.Customers', 'IsBusiness') IS NULL
    ALTER TABLE dbo.Customers ADD IsBusiness BIT NOT NULL CONSTRAINT DF_Customers_IsBusiness DEFAULT 0;
IF COL_LENGTH('dbo.Customers', 'CompanyName') IS NULL
    ALTER TABLE dbo.Customers ADD CompanyName NVARCHAR(250) NULL;
IF COL_LENGTH('dbo.Customers', 'TaxCode') IS NULL
    ALTER TABLE dbo.Customers ADD TaxCode NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Customers', 'BillingAddress') IS NULL
    ALTER TABLE dbo.Customers ADD BillingAddress NVARCHAR(500) NULL;
IF COL_LENGTH('dbo.Customers', 'BillingEmail') IS NULL
    ALTER TABLE dbo.Customers ADD BillingEmail NVARCHAR(250) NULL;
IF COL_LENGTH('dbo.Customers', 'BillingPhone') IS NULL
    ALTER TABLE dbo.Customers ADD BillingPhone NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.Customers', 'BillingReceiver') IS NULL
    ALTER TABLE dbo.Customers ADD BillingReceiver NVARCHAR(250) NULL;
GO

IF OBJECT_ID('dbo.CustomerServiceAddresses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerServiceAddresses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId INT NOT NULL,
        AddressId INT NOT NULL,
        ContactName NVARCHAR(250) NOT NULL,
        ContactPhone NVARCHAR(20) NULL,
        ContactEmail NVARCHAR(250) NULL,
        SiteName NVARCHAR(250) NULL,
        IsDefault BIT NOT NULL CONSTRAINT DF_CustomerServiceAddresses_IsDefault DEFAULT 0,
        HasOwnInvoiceInfo BIT NOT NULL CONSTRAINT DF_CustomerServiceAddresses_HasOwnInvoiceInfo DEFAULT 0,
        InvoiceCompanyName NVARCHAR(250) NULL,
        InvoiceTaxCode NVARCHAR(50) NULL,
        InvoiceAddress NVARCHAR(500) NULL,
        InvoiceEmail NVARCHAR(250) NULL,
        InvoicePhone NVARCHAR(20) NULL,
        InvoiceReceiver NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_CustomerServiceAddresses_IsActive DEFAULT 1,
        CONSTRAINT FK_CustomerServiceAddresses_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT FK_CustomerServiceAddresses_Addresses FOREIGN KEY (AddressId) REFERENCES dbo.Addresses(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Source' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE INDEX IX_Customers_Source ON dbo.Customers(CustomerSourceId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Type' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE INDEX IX_Customers_Type ON dbo.Customers(CustomerTypeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CustomerServiceAddresses_Customer' AND object_id = OBJECT_ID('dbo.CustomerServiceAddresses'))
    CREATE INDEX IX_CustomerServiceAddresses_Customer ON dbo.CustomerServiceAddresses(CustomerId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Customers_CustomerSources')
    ALTER TABLE dbo.Customers WITH CHECK ADD CONSTRAINT FK_Customers_CustomerSources FOREIGN KEY (CustomerSourceId) REFERENCES dbo.CustomerSources(Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Customers_CustomerTypes')
    ALTER TABLE dbo.Customers WITH CHECK ADD CONSTRAINT FK_Customers_CustomerTypes FOREIGN KEY (CustomerTypeId) REFERENCES dbo.CustomerTypes(Id);
GO

MERGE dbo.CustomerSources AS t
USING (VALUES
    (N'Trực tiếp', 1, 10),
    (N'Website', 1, 20),
    (N'Facebook', 1, 30),
    (N'Zalo', 1, 40),
    (N'Giới thiệu', 1, 50),
    (N'Telesales', 1, 60)
) AS s(Name, IsActive, SortOrder)
ON t.Name = s.Name
WHEN MATCHED THEN UPDATE SET t.IsActive = s.IsActive, t.SortOrder = s.SortOrder
WHEN NOT MATCHED THEN INSERT(Name, IsActive, SortOrder) VALUES(s.Name, s.IsActive, s.SortOrder);
GO

MERGE dbo.CustomerTypes AS t
USING (VALUES
    (N'Cá nhân', 0, 1, 10),
    (N'Hộ gia đình', 0, 1, 20),
    (N'Doanh nghiệp', 1, 1, 30),
    (N'Đại lý', 1, 1, 40)
) AS s(Name, IsBusiness, IsActive, SortOrder)
ON t.Name = s.Name
WHEN MATCHED THEN UPDATE SET t.IsBusiness = s.IsBusiness, t.IsActive = s.IsActive, t.SortOrder = s.SortOrder
WHEN NOT MATCHED THEN INSERT(Name, IsBusiness, IsActive, SortOrder) VALUES(s.Name, s.IsBusiness, s.IsActive, s.SortOrder);
GO

/* Map source cu (chuoi) -> source moi */
IF COL_LENGTH('dbo.Customers', 'CustomerSource') IS NOT NULL
BEGIN
    UPDATE c
    SET c.CustomerSourceId = s.Id
    FROM dbo.Customers c
    JOIN dbo.CustomerSources s ON s.Name = c.CustomerSource
    WHERE c.CustomerSourceId IS NULL
      AND c.CustomerSource IS NOT NULL
      AND LTRIM(RTRIM(c.CustomerSource)) <> N'';
END
GO

/* Default loai khach */
UPDATE c
SET CustomerTypeId = t.Id
FROM dbo.Customers c
JOIN dbo.CustomerTypes t ON t.Name = CASE WHEN c.IsBusiness = 1 THEN N'Doanh nghiệp' ELSE N'Cá nhân' END
WHERE c.CustomerTypeId IS NULL;
GO

/* Migrate dia chi cu sang bang nhieu dia chi */
IF COL_LENGTH('dbo.Customers', 'AddressId') IS NOT NULL
BEGIN
    INSERT INTO dbo.CustomerServiceAddresses(CustomerId, AddressId, ContactName, ContactPhone, ContactEmail, SiteName, IsDefault, IsActive)
    SELECT c.Id, c.AddressId, c.Name, c.Phone, c.Email, N'Địa chỉ chính', 1, 1
    FROM dbo.Customers c
    WHERE c.AddressId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.CustomerServiceAddresses a WHERE a.CustomerId = c.Id);
END
GO

/* Menu CRUD cho Nguon khach + Loai khach trong He thong */
IF OBJECT_ID('dbo.MenuItems','U') IS NOT NULL AND OBJECT_ID('dbo.RoleMenus','U') IS NOT NULL
BEGIN
    DECLARE @SystemMenuId INT = (SELECT TOP 1 Id FROM dbo.MenuItems WHERE Code IN ('system','hethong') ORDER BY Id);

    IF @SystemMenuId IS NOT NULL
    BEGIN
        MERGE dbo.MenuItems AS t
        USING (VALUES
            ('system.customer-source', N'Nguồn khách', '/CustomerSources', 'fa-solid fa-bullhorn', @SystemMenuId, 63, 1),
            ('system.customer-type', N'Loại khách', '/CustomerTypes', 'fa-solid fa-tags', @SystemMenuId, 64, 1)
        ) AS s(Code, Title, Url, IconCss, ParentId, SortOrder, IsActive)
        ON t.Code = s.Code
        WHEN MATCHED THEN UPDATE SET t.Title = s.Title, t.Url = s.Url, t.IconCss = s.IconCss, t.ParentId = s.ParentId, t.SortOrder = s.SortOrder, t.IsActive = s.IsActive
        WHEN NOT MATCHED THEN INSERT(Code, Title, Url, IconCss, ParentId, SortOrder, IsActive) VALUES(s.Code, s.Title, s.Url, s.IconCss, s.ParentId, s.SortOrder, s.IsActive);

        INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
        SELECT 'Admin', m.Id FROM dbo.MenuItems m
        WHERE m.Code IN ('system.customer-source','system.customer-type')
          AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus r WHERE r.RoleName = 'Admin' AND r.MenuItemId = m.Id);

        INSERT INTO dbo.RoleMenus(RoleName, MenuItemId)
        SELECT 'DieuPhoi', m.Id FROM dbo.MenuItems m
        WHERE m.Code IN ('system.customer-source','system.customer-type')
          AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus r WHERE r.RoleName = 'DieuPhoi' AND r.MenuItemId = m.Id);
    END
END
GO
