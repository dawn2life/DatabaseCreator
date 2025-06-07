using Microsoft.EntityFrameworkCore;

namespace DatabaseCreator.Data.EfCore
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Future DbSets can be added here.
        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     base.OnModelCreating(modelBuilder);
        // }
    }
}
