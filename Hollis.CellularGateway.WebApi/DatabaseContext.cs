using Hollis.CellularGateway.WebApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hollis.CellularGateway.WebApi;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<SimCard> SimCards { get; set; }

    public DbSet<ShortMessage> ShortMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortMessage>(entity =>
        {
            entity.HasOne(e => e.SimCard)
                .WithMany()
                .HasForeignKey(e => e.SimCardId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.TargetPhoneNumber)
                .HasMaxLength(20);
        });
    }
}
