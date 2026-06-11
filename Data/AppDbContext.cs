using AvecADeskApi.DTOs.Auth;
using AvecADeskApi.Model;

using Microsoft.EntityFrameworkCore;

namespace AvecADeskApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<UserLoginDTO> UserLoginDTOs { get; set; }
    public DbSet<User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Isse EF Core samjh jayega ki ye table nahi, result set hai
        modelBuilder.Entity<UserLoginDTO>().HasNoKey();
    }
}