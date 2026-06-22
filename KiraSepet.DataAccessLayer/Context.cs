using KiraSepet.EntityLayer;
using Microsoft.EntityFrameworkCore;


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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Business>()
                .Property(x => x.CommissionRate)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Business>()
    .Property(x => x.Status)
    .HasConversion<string>()
    .HasMaxLength(20)
    .HasDefaultValue(BusinessStatus.Pending);

            modelBuilder.Entity<AppNotification>()
    .Property(x => x.Title)
    .HasMaxLength(120);

            modelBuilder.Entity<AppNotification>()
                .Property(x => x.Message)
                .HasMaxLength(500);

            modelBuilder.Entity<AppNotification>()
                .HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });

            modelBuilder.Entity<Product>()
    .HasOne(x => x.Business)
    .WithMany()
    .HasForeignKey(x => x.BusinessId)
    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AppUser>()
                .Property(x => x.IsEmailVerified)
                .HasDefaultValue(true);
        }





        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<RentalOrder> RentalOrders { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Business> Businesses { get; set; }
        public DbSet<AppNotification> AppNotifications { get; set; }
        public DbSet<AboutPageContent> AboutPageContents { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<ContactInfo> ContactInfos { get; set; }

    }
}




//Mantığı; Entity Framework artık AppUser class’ını da veritabanı tablosu olarak görecek. 

//bir diğer adım ise MAGRATİON oluşturma. Entity Framework’e diyoruz ki:
//Yeni tablo geldi Git bunun için veritabanı planı hazırla ve bana bir migration dosyası oluştur.

//Ne yaptım?

//AppUser tablosunu migration ile veritabanına ekledim.

//Mantığı:

// Entity Framework class’ı okuyup SQL tablosuna çevirdi.

