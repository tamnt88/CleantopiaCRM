USE CleantopiaCRM;
GO

IF OBJECT_ID('dbo.ServiceCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceCategories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        ParentId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ServiceCategories_IsActive DEFAULT 1,
        SortOrder INT NOT NULL CONSTRAINT DF_ServiceCategories_SortOrder DEFAULT 0,
        CONSTRAINT FK_ServiceCategories_Parent FOREIGN KEY (ParentId) REFERENCES dbo.ServiceCategories(Id)
    );
    CREATE UNIQUE INDEX IX_ServiceCategories_ParentId_Name ON dbo.ServiceCategories(ParentId, Name);
END
GO

/* seed root category từ dữ liệu cũ */
INSERT INTO dbo.ServiceCategories(Name, ParentId, IsActive, SortOrder)
SELECT s.Category, NULL, 1, 0
FROM (
    SELECT DISTINCT Category
    FROM dbo.ServicePrices
    WHERE Category IS NOT NULL AND LTRIM(RTRIM(Category)) <> N''
) s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ServiceCategories c
    WHERE c.ParentId IS NULL AND c.Name = s.Category
);
GO

/* seed child category (service name) từ dữ liệu cũ */
;WITH src AS (
    SELECT DISTINCT sp.Category, sp.ServiceName
    FROM dbo.ServicePrices sp
    WHERE sp.Category IS NOT NULL AND LTRIM(RTRIM(sp.Category)) <> N''
      AND sp.ServiceName IS NOT NULL AND LTRIM(RTRIM(sp.ServiceName)) <> N''
)
INSERT INTO dbo.ServiceCategories(Name, ParentId, IsActive, SortOrder)
SELECT src.ServiceName, p.Id, 1, 0
FROM src
JOIN dbo.ServiceCategories p ON p.ParentId IS NULL AND p.Name = src.Category
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.ServiceCategories c
    WHERE c.ParentId = p.Id AND c.Name = src.ServiceName
);
GO
