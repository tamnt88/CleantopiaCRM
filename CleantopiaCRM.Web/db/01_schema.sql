/*
CAO NHAT:
- File nay co tinh chat DESTRUCTIVE (drop/recreate tables).
- Mac dinh bi KHOA de tranh xoa du lieu ngoai y muon.
- Chi bat khi can khoi tao moi hoan toan DB.
*/
DECLARE @AllowDestructiveRebuild BIT = 0; -- DOI THANH 1 neu ban CHAC CHAN muon xoa toan bo du lieu

IF @AllowDestructiveRebuild = 0
BEGIN
    RAISERROR(N'01_schema.sql dang o safe mode. Khong thuc thi DROP TABLE. Neu can rebuild trang, dat @AllowDestructiveRebuild = 1.', 16, 1);
    RETURN;
END
GO

IF DB_ID('CleantopiaCRM') IS NULL
    CREATE DATABASE CleantopiaCRM;
GO
USE CleantopiaCRM;
GO

IF OBJECT_ID('dbo.Assignments','U') IS NOT NULL DROP TABLE dbo.Assignments;
IF OBJECT_ID('dbo.ServiceFeedbacks','U') IS NOT NULL DROP TABLE dbo.ServiceFeedbacks;
IF OBJECT_ID('dbo.MaintenanceReminders','U') IS NOT NULL DROP TABLE dbo.MaintenanceReminders;
IF OBJECT_ID('dbo.RoleMenus','U') IS NOT NULL DROP TABLE dbo.RoleMenus;
IF OBJECT_ID('dbo.MenuItems','U') IS NOT NULL DROP TABLE dbo.MenuItems;
IF OBJECT_ID('dbo.QuoteItems','U') IS NOT NULL DROP TABLE dbo.QuoteItems;
IF OBJECT_ID('dbo.Quotes','U') IS NOT NULL DROP TABLE dbo.Quotes;
IF OBJECT_ID('dbo.Appointments','U') IS NOT NULL DROP TABLE dbo.Appointments;
IF OBJECT_ID('dbo.AppUsers','U') IS NOT NULL DROP TABLE dbo.AppUsers;
IF OBJECT_ID('dbo.Employees','U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID('dbo.CustomerServiceAddresses','U') IS NOT NULL DROP TABLE dbo.CustomerServiceAddresses;
IF OBJECT_ID('dbo.Customers','U') IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.CustomerTypes','U') IS NOT NULL DROP TABLE dbo.CustomerTypes;
IF OBJECT_ID('dbo.CustomerSources','U') IS NOT NULL DROP TABLE dbo.CustomerSources;
IF OBJECT_ID('dbo.ServicePrices','U') IS NOT NULL DROP TABLE dbo.ServicePrices;
IF OBJECT_ID('dbo.Addresses','U') IS NOT NULL DROP TABLE dbo.Addresses;
IF OBJECT_ID('dbo.GhnWards','U') IS NOT NULL DROP TABLE dbo.GhnWards;
IF OBJECT_ID('dbo.GhnProvinces','U') IS NOT NULL DROP TABLE dbo.GhnProvinces;
IF OBJECT_ID('dbo.Countries','U') IS NOT NULL DROP TABLE dbo.Countries;
GO

CREATE TABLE dbo.Countries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(10) NOT NULL,
    Name NVARCHAR(250) NOT NULL,
    CONSTRAINT UQ_Countries_Code UNIQUE(Code)
);

CREATE TABLE dbo.CustomerSources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_CustomerSources_Name UNIQUE(Name)
);

CREATE TABLE dbo.CustomerTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsBusiness BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_CustomerTypes_Name UNIQUE(Name)
);

CREATE TABLE dbo.MenuItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(100) NOT NULL,
    Title NVARCHAR(250) NOT NULL,
    Url NVARCHAR(300) NULL,
    IconCss NVARCHAR(100) NULL,
    ParentId INT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_MenuItems_Code UNIQUE(Code),
    CONSTRAINT FK_MenuItems_Parent FOREIGN KEY (ParentId) REFERENCES dbo.MenuItems(Id)
);

CREATE TABLE dbo.RoleMenus (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL,
    MenuItemId INT NOT NULL,
    CONSTRAINT FK_RoleMenus_MenuItems FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id),
    CONSTRAINT UQ_RoleMenus UNIQUE(RoleName, MenuItemId)
);

CREATE TABLE dbo.GhnProvinces (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProvinceId INT NOT NULL,
    ProvinceName NVARCHAR(250) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SyncedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_GhnProvinces_ProvinceId UNIQUE(ProvinceId)
);

CREATE TABLE dbo.GhnWards (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    WardCode NVARCHAR(20) NULL,
    WardIdV2 INT NOT NULL,
    WardName NVARCHAR(250) NOT NULL,
    ProvinceId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    SyncedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_GhnWards_WardIdV2 UNIQUE(WardIdV2),
    CONSTRAINT FK_GhnWards_GhnProvinces FOREIGN KEY (ProvinceId) REFERENCES dbo.GhnProvinces(Id)
);

CREATE TABLE dbo.Addresses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    HouseNumber NVARCHAR(100) NOT NULL,
    Street NVARCHAR(250) NOT NULL,
    ProvinceId INT NOT NULL,
    WardId INT NOT NULL,
    CONSTRAINT FK_Addresses_GhnProvinces FOREIGN KEY (ProvinceId) REFERENCES dbo.GhnProvinces(Id),
    CONSTRAINT FK_Addresses_GhnWards FOREIGN KEY (WardId) REFERENCES dbo.GhnWards(Id)
);

CREATE TABLE dbo.Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerCode NVARCHAR(30) NULL,
    Name NVARCHAR(250) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(250) NULL,
    CountryId INT NOT NULL,
    CustomerSourceId INT NULL,
    CustomerTypeId INT NULL,
    IsBusiness BIT NOT NULL DEFAULT 0,
    CompanyName NVARCHAR(250) NULL,
    TaxCode NVARCHAR(50) NULL,
    BillingAddress NVARCHAR(500) NULL,
    BillingEmail NVARCHAR(250) NULL,
    BillingPhone NVARCHAR(20) NULL,
    BillingReceiver NVARCHAR(250) NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Customers_Countries FOREIGN KEY (CountryId) REFERENCES dbo.Countries(Id),
    CONSTRAINT FK_Customers_CustomerSources FOREIGN KEY (CustomerSourceId) REFERENCES dbo.CustomerSources(Id),
    CONSTRAINT FK_Customers_CustomerTypes FOREIGN KEY (CustomerTypeId) REFERENCES dbo.CustomerTypes(Id)
);

CREATE TABLE dbo.CustomerServiceAddresses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    AddressId INT NOT NULL,
    ContactName NVARCHAR(250) NOT NULL,
    ContactPhone NVARCHAR(20) NULL,
    ContactEmail NVARCHAR(250) NULL,
    SiteName NVARCHAR(250) NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    HasOwnInvoiceInfo BIT NOT NULL DEFAULT 0,
    InvoiceCompanyName NVARCHAR(250) NULL,
    InvoiceTaxCode NVARCHAR(50) NULL,
    InvoiceAddress NVARCHAR(500) NULL,
    InvoiceEmail NVARCHAR(250) NULL,
    InvoicePhone NVARCHAR(20) NULL,
    InvoiceReceiver NVARCHAR(250) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_CustomerServiceAddresses_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_CustomerServiceAddresses_Addresses FOREIGN KEY (AddressId) REFERENCES dbo.Addresses(Id)
);

CREATE TABLE dbo.Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeCode NVARCHAR(30) NOT NULL,
    FullName NVARCHAR(250) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    Email NVARCHAR(250) NULL,
    AddressId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Employees_EmployeeCode UNIQUE(EmployeeCode),
    CONSTRAINT FK_Employees_Addresses FOREIGN KEY (AddressId) REFERENCES dbo.Addresses(Id)
);

CREATE TABLE dbo.AppUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName NVARCHAR(250) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    EmployeeId INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_AppUsers_Username UNIQUE(Username),
    CONSTRAINT FK_AppUsers_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id)
);

CREATE TABLE dbo.ServicePrices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Category NVARCHAR(100) NOT NULL,
    ServiceName NVARCHAR(250) NOT NULL,
    VariantName NVARCHAR(100) NULL,
    Unit NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    DisplayOrder INT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Quotes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteNo NVARCHAR(50) NOT NULL,
    CustomerId INT NOT NULL,
    QuoteDate DATE NOT NULL,
    ValidUntil DATE NULL,
    Status NVARCHAR(50) NOT NULL,
    ServiceAddressId INT NULL,
    ServiceProvinceId INT NULL,
    ServiceWardId INT NULL,
    ServiceAddressText NVARCHAR(1000) NULL,
    ContactName NVARCHAR(250) NULL,
    ContactPhone NVARCHAR(20) NULL,
    HasVatInvoice BIT NOT NULL DEFAULT(0),
    InvoiceCompanyName NVARCHAR(250) NULL,
    InvoiceTaxCode NVARCHAR(50) NULL,
    InvoiceAddress NVARCHAR(500) NULL,
    InvoiceEmail NVARCHAR(250) NULL,
    InvoiceReceiver NVARCHAR(250) NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
    VatRate DECIMAL(5,2) NOT NULL DEFAULT(8),
    SubtotalAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
    VatAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Quotes_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);

CREATE TABLE dbo.QuoteItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteId INT NOT NULL,
    ServicePriceId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT(0),
    Note NVARCHAR(500) NULL,
    CONSTRAINT FK_QuoteItems_Quotes FOREIGN KEY (QuoteId) REFERENCES dbo.Quotes(Id),
    CONSTRAINT FK_QuoteItems_ServicePrices FOREIGN KEY (ServicePriceId) REFERENCES dbo.ServicePrices(Id)
);

CREATE TABLE dbo.Appointments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    ScheduledAt DATETIME2 NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Appointments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);

CREATE TABLE dbo.Assignments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL,
    EmployeeId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    SupervisionNote NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Assignments_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments(Id),
    CONSTRAINT FK_Assignments_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(Id)
);

CREATE TABLE dbo.ServiceFeedbacks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    AppointmentId INT NULL,
    Rating INT NOT NULL,
    Content NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_ServiceFeedbacks_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
    CONSTRAINT FK_ServiceFeedbacks_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments(Id)
);

CREATE TABLE dbo.MaintenanceReminders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    ServiceName NVARCHAR(250) NOT NULL,
    CycleDays INT NOT NULL,
    LastServiceDate DATE NOT NULL,
    NextReminderDate DATE NOT NULL,
    IsDone BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MaintenanceReminders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);
GO

CREATE INDEX IX_Customers_Name ON dbo.Customers(Name);
CREATE UNIQUE INDEX IX_Customers_CustomerCode ON dbo.Customers(CustomerCode) WHERE CustomerCode IS NOT NULL;
CREATE INDEX IX_Customers_Source ON dbo.Customers(CustomerSourceId);
CREATE INDEX IX_Customers_Type ON dbo.Customers(CustomerTypeId);
CREATE INDEX IX_CustomerServiceAddresses_Customer ON dbo.CustomerServiceAddresses(CustomerId);
CREATE INDEX IX_Employees_FullName ON dbo.Employees(FullName);
CREATE INDEX IX_Quotes_QuoteDate ON dbo.Quotes(QuoteDate);
CREATE INDEX IX_Appointments_ScheduledAt ON dbo.Appointments(ScheduledAt);
CREATE INDEX IX_MaintenanceReminders_NextReminderDate ON dbo.MaintenanceReminders(NextReminderDate);
GO
