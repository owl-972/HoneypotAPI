using Microsoft.EntityFrameworkCore;
using HoneypotAPI.Models;

namespace HoneypotAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Request> Requests { get; set; }
        public DbSet<Response> Responses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes
            modelBuilder.Entity<Request>().HasIndex(r => r.Timestamp);
            modelBuilder.Entity<Request>().HasIndex(r => r.IpAddress);
            modelBuilder.Entity<Request>().HasIndex(r => r.Endpoint);
            modelBuilder.Entity<Response>().HasIndex(r => r.RequestId);
            modelBuilder.Entity<Response>().HasIndex(r => r.ResponseStatus);

            // Configure relationship
            modelBuilder.Entity<Response>()
                .HasOne(r => r.Request)
                .WithOne(req => req.Response)
                .HasForeignKey<Response>(r => r.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}