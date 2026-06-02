using KiraSepet.EntityLayer;
using Microsoft.EntityFrameworkCore;
using KiraSepet.EntityLayer;

namespace KiraSepet.DataAccessLayer
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=KiraSepetDb;Trusted_Connection=True;TrustServerCertificate=True");
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<RentalOrder> RentalOrders { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

    }
}




//Mantığı; Entity Framework artık AppUser class’ını da veritabanı tablosu olarak görecek. 

//bir diğer adım ise MAGRATİON oluşturma. Entity Framework’e diyoruz ki:
//Yeni tablo geldi Git bunun için veritabanı planı hazırla ve bana bir migration dosyası oluştur.

//Ne yaptım?

//AppUser tablosunu migration ile veritabanına ekledim.

//Mantığı:

// Entity Framework class’ı okuyup SQL tablosuna çevirdi.

