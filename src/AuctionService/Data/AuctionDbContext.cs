using AuctionService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;
public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Auction> Auctions { get; set; }
    public DbSet<Item> Items { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

}
