using CurrencyMini.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyMini.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ConversionHistory> Conversions => Set<ConversionHistory>();
    public DbSet<FavoriteCurrencyPair> Favorites => Set<FavoriteCurrencyPair>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One account per email address.
        modelBuilder.Entity<AppUser>(b => b.HasIndex(u => u.Email).IsUnique());

        // A currency pair can only be favorited once *per user* (checked in the controller too; this backs it at the DB level).
        modelBuilder.Entity<FavoriteCurrencyPair>(b =>
            b.HasIndex(f => new { f.UserId, f.FromCurrency, f.ToCurrency }).IsUnique());

        modelBuilder.Entity<ConversionHistory>(b => b.HasIndex(c => c.UserId));
    }
}
