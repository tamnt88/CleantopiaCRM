using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Services;

public static class DbInitializer
{
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

        await db.SaveChangesAsync();
    }
}
