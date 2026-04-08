using CleantopiaCRM.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<GhnProvince> GhnProvinces => Set<GhnProvince>();
    public DbSet<GhnWard> GhnWards => Set<GhnWard>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
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
    }
}
