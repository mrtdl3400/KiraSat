using Microsoft.AspNetCore.Identity;
using KiraSepet.DataAccessLayer;
using KiraSepet.WebUI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.Json;



namespace KiraSepet.WebUII.Controllers
{
    public class ForgotPasswordController : Controller
    {
        private readonly Context _context;
        private readonly MailSettings _mailSettings;
        private readonly IDataProtector _protector;

        public ForgotPasswordController(
            Context context,
            IOptions<MailSettings> mailSettings,
            IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _mailSettings = mailSettings.Value;
            _protector = dataProtectionProvider.CreateProtector("KiraSepet.PasswordReset");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string email)
        {
            var user = _context.AppUsers
            .FirstOrDefault(x => x.Email.Trim().ToLower() == email.Trim().ToLower());

            if (user != null)
            {


                var tokenPayload = new PasswordResetToken
                {
                    Email = user.Email,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                };

                var token = _protector.Protect(JsonSerializer.Serialize(tokenPayload));

                var resetLink = Url.Action(
                    "ResetPassword",
                    "ForgotPassword",
                    new { token },


                    Request.Scheme);
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(
                    _mailSettings.DisplayName,
                    _mailSettings.Mail));

                message.To.Add(MailboxAddress.Parse(email));

                message.Subject = "KiraSepet Şifre Sıfırlama";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                     <p>Merhaba {user.NameSurname},</p>
                     <p>Şifrenizi yenilemek için aşağıdaki linke tıklayın:</p>
                     <p><a href=""{resetLink}"">Şifremi Yenile</a></p>
                     <p>Bu link 30 dakika geçerlidir.</p>"
                };

                try
                {
                    if (string.IsNullOrWhiteSpace(_mailSettings.Password))
                    {
                        ViewBag.Error = "Mail şifresi yapılandırılmamış. Lütfen User Secrets içine MailSettings:Password ekleyin.";
                        return View();
                    }

                    using (var client = new SmtpClient())
                    {
                        client.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);

                        client.Authenticate(_mailSettings.Mail.Trim(), _mailSettings.Password.Trim());

                        client.Send(message);

                        client.Disconnect(true);
                    }

                    ViewBag.Message = "Şifre yenileme linki mail adresinize gönderildi.";
                    return View();
                }

                catch (Exception ex)
                {
                    ViewBag.Error = "Mail gönderilemedi: " + ex.Message;
                    return View();
                }
            }
            ViewBag.Error = "Bu email adresi sistemde bulunamadı.";
            return View();

                   


        }


        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var resetToken = ReadToken(token);

            if (resetToken == null)
            {
                ViewBag.Error = "Şifre yenileme linki geçersiz veya süresi dolmuş.";
                return View();
            }

            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string token, string password, string confirmPassword)
        {
            var resetToken = ReadToken(token);

            if (resetToken == null)
            {
                ViewBag.Error = "Şifre yenileme linki geçersiz veya süresi dolmuş.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Token = token;
                ViewBag.Error = "Yeni şifre boş olamaz.";
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Token = token;
                ViewBag.Error = "Şifreler eşleşmiyor.";
                return View();
            }

            var user = _context.AppUsers
                .FirstOrDefault(x => x.Email.Trim().ToLower() == resetToken.Email.Trim().ToLower());

            if (user == null)
            {
                ViewBag.Error = "Kullanıcı bulunamadı.";
                return View();
            }

            var passwordHasher = new PasswordHasher<AppUser>();
            user.Password = passwordHasher.HashPassword(user, password);
            _context.SaveChanges();

            ViewBag.Message = "Şifreniz başarıyla yenilendi. Yeni şifrenizle giriş yapabilirsiniz.";
            return View();
        }

        private PasswordResetToken? ReadToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                var json = _protector.Unprotect(token);
                var resetToken = JsonSerializer.Deserialize<PasswordResetToken>(json);

                if (resetToken == null || resetToken.ExpiresAt < DateTime.UtcNow)
                {
                    return null;
                }

                return resetToken;
            }
            catch
            {
                return null;
            }
        }
        private class PasswordResetToken
        {
            public string Email { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }
    }
}
