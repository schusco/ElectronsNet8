using Microsoft.EntityFrameworkCore;

namespace Electrons.Net8.Api
{
    public class ElectronsDbContext(DbContextOptions<ElectronsDbContext> options) : DbContext(options)
    {
        public DbSet<Models.Player> Player { get; set; } = default!;
        public DbSet<Models.History> History { get; set; } = default!;
        public DbSet<Models.Event> Events { get; set; } = default!;
        public DbSet<Models.Award> Awards { get; set; } = default!;
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>().HaveMaxLength(255);
        }
    }
}
