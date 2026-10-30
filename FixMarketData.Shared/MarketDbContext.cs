using Microsoft.EntityFrameworkCore;

namespace FixMarketData.Shared;

public sealed class MarketDbContext(DbContextOptions<MarketDbContext> options) : DbContext(options)
{
    public DbSet<MarketDataTick> MarketDataTicks => Set<MarketDataTick>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MarketDataTick>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Symbol).IsRequired().HasMaxLength(32);
            entity.Property(x => x.BidPrice).HasPrecision(18, 5);
            entity.Property(x => x.AskPrice).HasPrecision(18, 5);
            entity.Property(x => x.BidSize).HasPrecision(18, 5);
            entity.Property(x => x.AskSize).HasPrecision(18, 5);
            entity.Property(x => x.Timestamp).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
