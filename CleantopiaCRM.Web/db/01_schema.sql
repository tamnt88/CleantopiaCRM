IF DB_ID('CleantopiaCRM') IS NULL
    CREATE DATABASE CleantopiaCRM;
GO
USE CleantopiaCRM;
GO

IF OBJECT_ID('dbo.Assignments','U') IS NOT NULL DROP TABLE dbo.Assignments;
IF OBJECT_ID('dbo.ServiceFeedbacks','U') IS NOT NULL DROP TABLE dbo.ServiceFeedbacks;
IF OBJECT_ID('dbo.MaintenanceReminders','U') IS NOT NULL DROP TABLE dbo.MaintenanceReminders;
IF OBJECT_ID('dbo.QuoteItems','U') IS NOT NULL DROP TABLE dbo.QuoteItems;
IF OBJECT_ID('dbo.Quotes','U') IS NOT NULL DROP TABLE dbo.Quotes;
IF OBJECT_ID('dbo.Appointments','U') IS NOT NULL DROP TABLE dbo.Appointments;
IF OBJECT_ID('dbo.AppUsers','U') IS NOT NULL DROP TABLE dbo.AppUsers;
IF OBJECT_ID('dbo.Employees','U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID('dbo.Customers','U') IS NOT NULL DROP TABLE dbo.Customers;
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
    Name NVARCHAR(250) NOT NULL,
    Phone NVARCHAR(20) NULL,
    Email NVARCHAR(250) NULL,
    CountryId INT NOT NULL,
    AddressId INT NOT NULL,
    Notes NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Customers_Countries FOREIGN KEY (CountryId) REFERENCES dbo.Countries(Id),
    CONSTRAINT FK_Customers_Addresses FOREIGN KEY (AddressId) REFERENCES dbo.Addresses(Id)
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
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Quotes_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);

CREATE TABLE dbo.QuoteItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    QuoteId INT NOT NULL,
    ServicePriceId INT NOT NULL,
    Quantity DECIMAL(18,2) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
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
CREATE INDEX IX_Employees_FullName ON dbo.Employees(FullName);
CREATE INDEX IX_Quotes_QuoteDate ON dbo.Quotes(QuoteDate);
CREATE INDEX IX_Appointments_ScheduledAt ON dbo.Appointments(ScheduledAt);
CREATE INDEX IX_MaintenanceReminders_NextReminderDate ON dbo.MaintenanceReminders(NextReminderDate);
GO
