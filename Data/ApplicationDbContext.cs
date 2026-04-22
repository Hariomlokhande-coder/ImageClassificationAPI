using ImageClassificationAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ImageClassificationAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<User> Users { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is User && (
                        e.State == EntityState.Added
                        || e.State == EntityState.Modified));

            var currentUsername = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";

            foreach (var entityEntry in entries)
            {
                var user = (User)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    user.CreatedDate = DateTime.UtcNow;
                    user.CreatedBy = currentUsername;
                    user.IsDeleted = false;
                }
                else
                {
                    user.UpdatedDate = DateTime.UtcNow;
                    user.UpdatedBy = currentUsername;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Global filter for IsDeleted
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        }
    }
}
