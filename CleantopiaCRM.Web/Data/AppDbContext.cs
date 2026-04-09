using CleantopiaCRM.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<CustomerSource> CustomerSources => Set<CustomerSource>();
    public DbSet<CustomerType> CustomerTypes => Set<CustomerType>();
    public DbSet<CustomerServiceAddress> CustomerServiceAddresses => Set<CustomerServiceAddress>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<GhnProvince> GhnProvinces => Set<GhnProvince>();
    public DbSet<GhnWard> GhnWards => Set<GhnWard>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
    public DbSet<ServiceCategory> ServiceCategories => Set<ServiceCategory>();
    public DbSet<ServiceUnit> ServiceUnits => Set<ServiceUnit>();
    public DbSet<ServicePricePolicy> ServicePricePolicies => Set<ServicePricePolicy>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<ServiceFeedback> ServiceFeedbacks => Set<ServiceFeedback>();
    public DbSet<MaintenanceReminder> MaintenanceReminders => Set<MaintenanceReminder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuoteItem>().Ignore(x => x.Amount);
        modelBuilder.Entity<GhnProvince>().HasIndex(x => x.ProvinceId).IsUnique();
        modelBuilder.Entity<GhnWard>().HasIndex(x => x.WardIdV2).IsUnique();
        modelBuilder.Entity<GhnWard>().HasIndex(x => x.WardCode);
        modelBuilder.Entity<MenuItem>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<RoleMenu>().HasIndex(x => new { x.RoleName, x.MenuItemId }).IsUnique();
        modelBuilder.Entity<CustomerSource>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<CustomerType>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<ServiceCategory>().HasIndex(x => new { x.ParentId, x.Name }).IsUnique();
        modelBuilder.Entity<ServiceCategory>()
            .HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ServiceUnit>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<ServicePrice>()
            .HasOne(x => x.Policy)
            .WithOne(x => x.ServicePrice)
            .HasForeignKey<ServicePricePolicy>(x => x.ServicePriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ServicePricePolicy>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
