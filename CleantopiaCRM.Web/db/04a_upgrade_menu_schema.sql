USE CleantopiaCRM;
GO

IF OBJECT_ID('dbo.MenuItems','U') IS NULL
BEGIN
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
END
GO

IF OBJECT_ID('dbo.RoleMenus','U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMenus (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RoleName NVARCHAR(50) NOT NULL,
        MenuItemId INT NOT NULL,
        CONSTRAINT FK_RoleMenus_MenuItems FOREIGN KEY (MenuItemId) REFERENCES dbo.MenuItems(Id),
        CONSTRAINT UQ_RoleMenus UNIQUE(RoleName, MenuItemId)
    );
END
GO
