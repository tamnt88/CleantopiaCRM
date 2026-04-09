USE CleantopiaCRM;
GO

/* Upgrade dich vu:
   1) Tach don vi sang bang ServiceUnits
   2) Them ServicePricePolicies de luu don vi + chu ky tai cham soc cho tung gia dich vu
*/

IF OBJECT_ID('dbo.ServiceUnits', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServiceUnits (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ServiceUnits_IsActive DEFAULT 1,
        SortOrder INT NOT NULL CONSTRAINT DF_ServiceUnits_SortOrder DEFAULT 0,
        CONSTRAINT UQ_ServiceUnits_Name UNIQUE(Name)
    );
END
GO

IF OBJECT_ID('dbo.ServicePricePolicies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ServicePricePolicies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ServicePriceId INT NOT NULL,
        UnitId INT NOT NULL,
        RecareCycleDays INT NOT NULL CONSTRAINT DF_ServicePricePolicies_RecareCycleDays DEFAULT 180,
        CONSTRAINT FK_ServicePricePolicies_ServicePrices FOREIGN KEY (ServicePriceId) REFERENCES dbo.ServicePrices(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ServicePricePolicies_ServiceUnits FOREIGN KEY (UnitId) REFERENCES dbo.ServiceUnits(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ServicePricePolicies_ServicePriceId' AND object_id = OBJECT_ID('dbo.ServicePricePolicies'))
    CREATE UNIQUE INDEX IX_ServicePricePolicies_ServicePriceId ON dbo.ServicePricePolicies(ServicePriceId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ServicePricePolicies_UnitId' AND object_id = OBJECT_ID('dbo.ServicePricePolicies'))
    CREATE INDEX IX_ServicePricePolicies_UnitId ON dbo.ServicePricePolicies(UnitId);
GO

/* Seed don vi mac dinh */
MERGE dbo.ServiceUnits AS t
USING (VALUES
    (N'Gói', 1, 10),
    (N'Bộ', 1, 20),
    (N'm2', 1, 30),
    (N'Cái', 1, 40),
    (N'Lần', 1, 50)
) AS s(Name, IsActive, SortOrder)
ON t.Name = s.Name
WHEN MATCHED THEN UPDATE SET t.IsActive = s.IsActive, t.SortOrder = s.SortOrder
WHEN NOT MATCHED THEN INSERT(Name, IsActive, SortOrder) VALUES(s.Name, s.IsActive, s.SortOrder);
GO

/* Migrate policy cho cac ServicePrice cu:
   - map Unit text -> ServiceUnits
   - neu khong map duoc thi gan mac dinh 'Gói'
   - chu ky mac dinh 180 ngay
*/
DECLARE @DefaultUnitId INT = (
    SELECT TOP 1 Id
    FROM dbo.ServiceUnits
    WHERE Name = N'Gói'
    ORDER BY Id
);

;WITH Src AS (
    SELECT
        sp.Id AS ServicePriceId,
        sp.Unit,
        su.Id AS UnitId
    FROM dbo.ServicePrices sp
    OUTER APPLY (
        SELECT TOP 1 u.Id
        FROM dbo.ServiceUnits u
        WHERE LOWER(LTRIM(RTRIM(u.Name))) = LOWER(LTRIM(RTRIM(ISNULL(sp.Unit, N''))))
    ) su
)
INSERT INTO dbo.ServicePricePolicies(ServicePriceId, UnitId, RecareCycleDays)
SELECT
    s.ServicePriceId,
    COALESCE(s.UnitId, @DefaultUnitId),
    180
FROM Src s
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.ServicePricePolicies p
    WHERE p.ServicePriceId = s.ServicePriceId
);
GO
