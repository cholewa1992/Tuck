using System.Collections.Generic;
using System.Configuration;
using Tuck.Model;
using Microsoft.EntityFrameworkCore;

namespace Tuck
{
    public class TuckContext : DbContext
    {
        public DbSet<BuffInstance> Buffs { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<WarEffortContribution> Contributions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) 
            => options.UseSqlite("Data Source=tuck.db");
    
    }
}