using KiraSepet.DataAccessLayer;
using KiraSepet.WebUI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;


namespace KiraSepet.WebUII.Controllers
{
    public class ForgotPasswordController : Controller
    {
        private readonly Context _context;
        private readonly MailSettings _mailSettings;

        public ForgotPasswordController(Context context, IOptions<MailSettings> mailSettings)
        {
            _context = context;
            _mailSettings = mailSettings.Value;
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
                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(
                    _mailSettings.DisplayName,
                    _mailSettings.Mail));

                message.To.Add(MailboxAddress.Parse(email));

                message.Subject = "KiraSepet Şifre Sıfırlama";

                message.Body = new TextPart("plain")
                {
                    Text = $"Merhaba {user.NameSurname}, şifre sıfırlama talebiniz alınmıştır."
                };

                using (var client = new SmtpClient())
                {
                    Console.WriteLine("OKUNAN MAIL: " + _mailSettings.Mail);
                    Console.WriteLine("OKUNAN PASSWORD UZUNLUK: " + _mailSettings.Password.Length);

                    Console.WriteLine("OKUNAN HOST: " + _mailSettings.Host);
                    Console.WriteLine("OKUNAN PORT: " + _mailSettings.Port);

                    //client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                    //client.AuthenticationMechanisms.Remove("XOAUTH2");



                    //client.Authenticate(_mailSettings.Mail.Trim(), _mailSettings.Password.Trim());

                    //client.Send(message);

                    //client.Disconnect(true);
                }

                ViewBag.Message = "Şifre sıfırlama maili gönderildi.";
                return View();
            }
            ViewBag.Error = "Bu email adresi sistemde bulunamadı.";
            return View();
        }
    }
}