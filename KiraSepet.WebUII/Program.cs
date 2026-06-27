using KiraSepet.DataAccessLayer;
using KiraSepet.WebUI.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<Context>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/Index";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection("MailSettings"));

builder.Services.AddSession();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Context>();
    dbContext.Database.Migrate();
    dbContext.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Products', 'RentalStockCount') IS NULL
BEGIN
    ALTER TABLE Products
    ADD RentalStockCount int NOT NULL
        CONSTRAINT DF_Products_RentalStockCount DEFAULT(0);

    EXEC('UPDATE Products
          SET RentalStockCount = StockCount
          WHERE IsRentable = 1 AND RentalStockCount = 0');
END");

    dbContext.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('LegalTexts', 'U') IS NULL
BEGIN
    CREATE TABLE LegalTexts
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LegalTexts PRIMARY KEY,
        [Key] nvarchar(80) NOT NULL,
        Title nvarchar(160) NOT NULL,
        Content nvarchar(max) NOT NULL,
        UpdatedAt datetime2 NOT NULL
    );

    CREATE UNIQUE INDEX IX_LegalTexts_Key ON LegalTexts([Key]);
END

IF NOT EXISTS (SELECT 1 FROM LegalTexts WHERE [Key] = N'PaymentTerms')
BEGIN
    INSERT INTO LegalTexts ([Key], Title, Content, UpdatedAt)
    VALUES
    (
        N'PaymentTerms',
        N'Kiralama ve satın alma bilgilendirme metni',
        N'KiraSat üzerinden yapılan satın alma ve kiralama işlemlerinde kullanıcı, seçtiği ürünü açıklamada belirtilen niteliklere göre incelediğini ve işlem bedelini onayladığını kabul eder.

Kiralama işlemlerinde kullanıcı, ürünü teslim aldığı haliyle korumakla, kullanım süresi boyunca ürüne makul özeni göstermekle ve ürünü kararlaştırılan tarihte eksiksiz şekilde iade etmekle sorumludur.

Ürünün teslim alınması sırasında kullanıcı, üründe görünür bir hasar veya eksiklik olup olmadığını kontrol etmelidir. Teslim sonrası ortaya çıkan hasar, kayıp, eksik parça veya geç iade durumlarında satıcı ile kullanıcı arasında sorumluluk değerlendirmesi yapılabilir.

Satıcı, ilana eklediği ürün bilgilerinin doğru olduğunu, ürünün kullanıma uygun durumda bulunduğunu ve teslim sürecinde kullanıcıyı yanıltıcı bilgi vermeyeceğini kabul eder.

Platform, kullanıcı ile satıcı arasındaki alışveriş ve kiralama sürecini kolaylaştıran aracı bir hizmet sunar. Taraflar, işlemle ilgili mesajlaşma ve bildirimleri takip etmekle sorumludur.',
        SYSUTCDATETIME()
    );
END");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
