using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Services;

public static class DbInitializer
{
    public static async Task EnsureServicePricingSchemaAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_DiscountAmount DEFAULT(0);
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'ServiceAddressId') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceAddressId INT NULL;
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'ServiceAddressText') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceAddressText NVARCHAR(1000) NULL;
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'ContactName') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ContactName NVARCHAR(250) NULL;
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'ContactPhone') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ContactPhone NVARCHAR(20) NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'ServiceProvinceId') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceProvinceId INT NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'ServiceWardId') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD ServiceWardId INT NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'HasVatInvoice') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD HasVatInvoice BIT NOT NULL CONSTRAINT DF_Quotes_HasVatInvoice DEFAULT(0);
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'InvoiceCompanyName') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceCompanyName NVARCHAR(250) NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'InvoiceTaxCode') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceTaxCode NVARCHAR(50) NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'InvoiceAddress') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceAddress NVARCHAR(500) NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'InvoiceEmail') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceEmail NVARCHAR(250) NULL;
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'InvoiceReceiver') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD InvoiceReceiver NVARCHAR(250) NULL;
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'VatRate') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD VatRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_Quotes_VatRate DEFAULT(8);
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'SubtotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD SubtotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_SubtotalAmount DEFAULT(0);
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'VatAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD VatAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_VatAmount DEFAULT(0);
END
""");
        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Quotes', N'TotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Quotes ADD TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Quotes_TotalAmount DEFAULT(0);
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.QuoteItems', N'DiscountAmount') IS NULL
BEGIN
    ALTER TABLE dbo.QuoteItems ADD DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_QuoteItems_DiscountAmount DEFAULT(0);
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.QuoteItems', N'Note') IS NULL
BEGIN
    ALTER TABLE dbo.QuoteItems ADD Note NVARCHAR(500) NULL;
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF COL_LENGTH(N'dbo.Customers', N'CustomerCode') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD CustomerCode NVARCHAR(30) NULL;
END
""");

        await db.Database.ExecuteSqlRawAsync("""
UPDATE c
SET c.CustomerCode = CONCAT('KH-', RIGHT(CONCAT('00000', CAST(c.Id AS VARCHAR(20))), 5))
FROM dbo.Customers c
WHERE c.CustomerCode IS NULL OR LTRIM(RTRIM(c.CustomerCode)) = '';
""");

        await db.Database.ExecuteSqlRawAsync("""
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
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.ServiceCategories', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ServiceCategories](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(150) NOT NULL,
        [ParentId] INT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_ServiceCategories_IsActive] DEFAULT(1),
        [SortOrder] INT NOT NULL CONSTRAINT [DF_ServiceCategories_SortOrder] DEFAULT(0),
        CONSTRAINT [FK_ServiceCategories_Parent] FOREIGN KEY([ParentId]) REFERENCES [dbo].[ServiceCategories]([Id])
    );
    CREATE UNIQUE INDEX [IX_ServiceCategories_ParentId_Name] ON [dbo].[ServiceCategories]([ParentId], [Name]);
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.ServiceUnits', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ServiceUnits](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [IsActive] BIT NOT NULL CONSTRAINT [DF_ServiceUnits_IsActive] DEFAULT(1),
        [SortOrder] INT NOT NULL CONSTRAINT [DF_ServiceUnits_SortOrder] DEFAULT(0)
    );
    CREATE UNIQUE INDEX [IX_ServiceUnits_Name] ON [dbo].[ServiceUnits]([Name]);
END
""");

        await db.Database.ExecuteSqlRawAsync("""
IF OBJECT_ID(N'dbo.ServicePricePolicies', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ServicePricePolicies](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ServicePriceId] INT NOT NULL,
        [UnitId] INT NOT NULL,
        [RecareCycleDays] INT NOT NULL CONSTRAINT [DF_ServicePricePolicies_RecareCycleDays] DEFAULT(180),
        CONSTRAINT [FK_ServicePricePolicies_ServicePrices] FOREIGN KEY ([ServicePriceId]) REFERENCES [dbo].[ServicePrices]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ServicePricePolicies_ServiceUnits] FOREIGN KEY ([UnitId]) REFERENCES [dbo].[ServiceUnits]([Id]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [IX_ServicePricePolicies_ServicePriceId] ON [dbo].[ServicePricePolicies]([ServicePriceId]);
    CREATE INDEX [IX_ServicePricePolicies_UnitId] ON [dbo].[ServicePricePolicies]([UnitId]);
END
""");

        if (!await db.ServiceUnits.AnyAsync())
        {
            db.ServiceUnits.AddRange(
                new ServiceUnit { Name = "Gói", SortOrder = 10, IsActive = true },
                new ServiceUnit { Name = "Bộ", SortOrder = 20, IsActive = true },
                new ServiceUnit { Name = "m2", SortOrder = 30, IsActive = true },
                new ServiceUnit { Name = "Cái", SortOrder = 40, IsActive = true },
                new ServiceUnit { Name = "Lần", SortOrder = 50, IsActive = true }
            );
            await db.SaveChangesAsync();
        }

        var defaultUnitId = await db.ServiceUnits.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync();
        var existingServiceIds = await db.ServicePrices.Select(x => x.Id).ToListAsync();
        var policyServiceIds = await db.ServicePricePolicies.Select(x => x.ServicePriceId).ToListAsync();
        var missingServiceIds = existingServiceIds.Except(policyServiceIds).ToList();
        if (missingServiceIds.Count > 0 && defaultUnitId > 0)
        {
            var serviceUnits = await db.ServicePrices
                .Where(x => missingServiceIds.Contains(x.Id))
                .Select(x => new { x.Id, x.Unit })
                .ToListAsync();

            var unitMap = await db.ServiceUnits.ToDictionaryAsync(x => x.Name.Trim().ToLower(), x => x.Id);
            foreach (var item in serviceUnits)
            {
                var key = (item.Unit ?? string.Empty).Trim().ToLower();
                var unitId = unitMap.TryGetValue(key, out var mappedId) ? mappedId : defaultUnitId;
                db.ServicePricePolicies.Add(new ServicePricePolicy
                {
                    ServicePriceId = item.Id,
                    UnitId = unitId,
                    RecareCycleDays = 180
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.ServiceCategories.AnyAsync())
        {
            var roots = await db.ServicePrices
                .Select(x => x.Category)
                .Where(x => x != null && x != "")
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            foreach (var root in roots)
            {
                db.ServiceCategories.Add(new ServiceCategory
                {
                    Name = root,
                    ParentId = null,
                    IsActive = true
                });
            }
            await db.SaveChangesAsync();

            var rootMap = await db.ServiceCategories
                .Where(x => x.ParentId == null)
                .ToDictionaryAsync(x => x.Name, x => x.Id);

            var children = await db.ServicePrices
                .Select(x => new { x.Category, x.ServiceName })
                .Where(x => x.Category != null && x.Category != "" && x.ServiceName != null && x.ServiceName != "")
                .Distinct()
                .ToListAsync();

            foreach (var c in children)
            {
                if (!rootMap.TryGetValue(c.Category, out var parentId)) continue;
                var exists = await db.ServiceCategories.AnyAsync(x => x.ParentId == parentId && x.Name == c.ServiceName);
                if (exists) continue;
                db.ServiceCategories.Add(new ServiceCategory
                {
                    Name = c.ServiceName,
                    ParentId = parentId,
                    IsActive = true
                });
            }

            await db.SaveChangesAsync();
        }
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.AppUsers.AnyAsync())
        {
            db.AppUsers.Add(new AppUser
            {
                Username = "admin",
                PasswordHash = PasswordHasher.Hash("admin123"),
                FullName = "Quan tri he thong",
                Role = "Admin"
            });
        }

        if (!await db.ServicePrices.AnyAsync())
        {
            db.ServicePrices.AddRange(
                new ServicePrice { ServiceName = "Ve sinh may lanh treo tuong", Unit = "Bo", Price = 180000, Description = "Bao duong co ban" },
                new ServicePrice { ServiceName = "Tong ve sinh nha pho", Unit = "m2", Price = 18000 },
                new ServicePrice { ServiceName = "Ve sinh sofa", Unit = "Ghe", Price = 150000 },
                new ServicePrice { ServiceName = "Ve sinh rem cua", Unit = "m2", Price = 25000 },
                new ServicePrice { ServiceName = "Tong ve sinh van phong", Unit = "m2", Price = 16000 }
            );
        }

        if (!await db.CustomerSources.AnyAsync())
        {
            db.CustomerSources.AddRange(
                new CustomerSource { Name = "Trực tiếp", SortOrder = 10, IsActive = true },
                new CustomerSource { Name = "Website", SortOrder = 20, IsActive = true },
                new CustomerSource { Name = "Facebook", SortOrder = 30, IsActive = true },
                new CustomerSource { Name = "Zalo", SortOrder = 40, IsActive = true }
            );
        }

        if (!await db.CustomerTypes.AnyAsync())
        {
            db.CustomerTypes.AddRange(
                new CustomerType { Name = "Cá nhân", IsBusiness = false, SortOrder = 10, IsActive = true },
                new CustomerType { Name = "Doanh nghiệp", IsBusiness = true, SortOrder = 20, IsActive = true }
            );
        }

        await db.SaveChangesAsync();
    }
}
