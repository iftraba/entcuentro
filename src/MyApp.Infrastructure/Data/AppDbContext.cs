using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyApp.Infrastructure.Identity;

namespace MyApp.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppIdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppIdentityUser>(entity =>
        {
            entity.Property(u => u.Sexo).HasConversion<string>();

            entity.HasIndex(u => u.Dni)
                  .IsUnique()
                  .HasFilter("[Dni] IS NOT NULL");
        });
    }
}
