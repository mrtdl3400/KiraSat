using KiraSepet.DataAccessLayer;
using KiraSepet.WebUI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using System.Text.Json;

namespace KiraSepet.WebUI.Controllers
{
    public class RegisterController : Controller
    {
        private readonly Context _context;
        private readonly MailSettings _mailSettings;
        private readonly IDataProtector _protector;

        public RegisterController(Context context, IOptions<MailSettings> mailSettings, IDataProtectionProvider dataProtectionProvider)
        {
            _context = context;
            _mailSettings = mailSettings.Value;
            _protector = dataProtectionProvider.CreateProtector("KiraSepet.EmailVerification");
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(AppUser appUser, bool personalDataConsent, bool termsConsent)
        {
            appUser.Email = appUser.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(appUser.NameSurname) || string.IsNullOrWhiteSpace(appUser.Email) || string.IsNullOrWhiteSpace(appUser.Password))
                return Error("Lütfen tüm alanları doldurun.", appUser);
            try { _ = new MailAddress(appUser.Email); } catch { return Error("Geçerli bir e-posta adresi girin.", appUser); }
            if (appUser.Password.Length < 8) return Error("Şifre en az 8 karakter olmalı.", appUser);
            if (!personalDataConsent || !termsConsent)
                return Error("Kayıt olmak için zorunlu sözleşme onaylarını kabul etmelisiniz.", appUser);
            var existingUser = _context.AppUsers.FirstOrDefault(x => x.Email == appUser.Email);
            if (existingUser != null)
            {
                if (existingUser.IsEmailVerified)
                    return Error("Bu e-posta adresi zaten kayıtlı. Giriş yapmayı deneyin.", appUser);

                try
                {
                    var existingToken = _protector.Protect(JsonSerializer.Serialize(new VerificationToken { Email = existingUser.Email, ExpiresAt = DateTime.UtcNow.AddHours(24) }));
                    var existingLink = Url.Action(nameof(VerifyEmail), "Register", new { token = existingToken }, Request.Scheme)!;
                    SendEmail(existingUser, existingLink);
                }
                catch
                {
                    return Error("Doğrulama e-postası gönderilemedi. Lütfen tekrar deneyin.", appUser);
                }

                TempData["RegisterMessage"] = "Bu e-posta için doğrulama bağlantısı yeniden gönderildi.";
                return RedirectToAction("Index", "Login");
            }

            appUser.Role = "User";
            appUser.IsEmailVerified = false;
            appUser.Password = new PasswordHasher<AppUser>().HashPassword(appUser, appUser.Password);
            _context.AppUsers.Add(appUser);
            _context.SaveChanges();

            var token = _protector.Protect(JsonSerializer.Serialize(new VerificationToken { Email = appUser.Email, ExpiresAt = DateTime.UtcNow.AddHours(24) }));
            var link = Url.Action(nameof(VerifyEmail), "Register", new { token }, Request.Scheme)!;
            try { SendEmail(appUser, link); }
            catch
            {
                _context.AppUsers.Remove(appUser);
                _context.SaveChanges();
                return Error("Doğrulama e-postası gönderilemedi. Lütfen tekrar deneyin.", appUser);
            }

            TempData["RegisterMessage"] = "Doğrulama bağlantısı e-posta adresinize gönderildi.";
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public IActionResult VerifyEmail(string token)
        {
            try
            {
                var data = JsonSerializer.Deserialize<VerificationToken>(_protector.Unprotect(token));
                if (data == null || data.ExpiresAt < DateTime.UtcNow) return View("VerificationResult", false);
                var user = _context.AppUsers.FirstOrDefault(x => x.Email == data.Email);
                if (user == null) return View("VerificationResult", false);
                user.IsEmailVerified = true;
                _context.SaveChanges();
                return View("VerificationResult", true);
            }
            catch { return View("VerificationResult", false); }
        }

        private IActionResult Error(string message, AppUser user) { ViewBag.Error = message; return View(user); }
        private void SendEmail(AppUser user, string link)
        {
            if (string.IsNullOrWhiteSpace(_mailSettings.Password)) throw new InvalidOperationException();
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
            message.To.Add(MailboxAddress.Parse(user.Email));
            message.Subject = "KiraSat e-posta doğrulama";
            message.Body = new TextPart("html") { Text = $"<p>Merhaba {user.NameSurname},</p><p>Hesabınızı etkinleştirmek için <a href=\"{link}\">e-posta adresinizi doğrulayın</a>.</p><p>Bağlantı 24 saat geçerlidir.</p>" };
            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            client.Authenticate(_mailSettings.Mail.Trim(), _mailSettings.Password.Trim());
            client.Send(message);
            client.Disconnect(true);
        }
        private class VerificationToken { public string Email { get; set; } = string.Empty; public DateTime ExpiresAt { get; set; } }
    }
}
