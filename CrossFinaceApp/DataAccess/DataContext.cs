using CrossFinaceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossFinaceApp.DataAccess
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions options) : base (options)
        {
            Database.Migrate();
        }

        public DbSet<Person> People { get; set; }
        public DbSet<Agreement> Agreements { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<FinancialState> FinancialStates { get; set; }
    }
}
