using API_Gestor_Minimal.Model;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace API_Gestor_Minimal.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) 
        {

        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();

    }
}
