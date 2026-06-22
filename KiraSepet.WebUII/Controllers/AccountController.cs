using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using KiraSepet.WebUI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;

namespace KiraSepet.WebUI.Controllers;

public class AccountController(Context context, IOptions<MailSettings> mailSettings) : Controller
{
    private readonly Context _context = context;
    private readonly MailSettings _mailSettings = mailSettings.Value;

    public IActionResult Profile() => View();
    public IActionResult Settings() => View();

    public IActionResult About() => View(_context.AboutPageContents.FirstOrDefault() ?? new AboutPageContent());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult About(AboutPageContent model)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var about = _context.AboutPageContents.FirstOrDefault();
        if (about == null) { about = new AboutPageContent(); _context.AboutPageContents.Add(about); }
        about.Title = model.Title?.Trim() ?? "KiraSat";
        about.Content = model.Content?.Trim() ?? string.Empty;
        about.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();
        TempData["AboutSuccess"] = "Hakkımızda içeriği güncellendi.";
        return RedirectToAction(nameof(About));
    }

    [HttpGet]
    public IActionResult Contact()
    {
        ViewBag.ContactInfo = _context.ContactInfos.FirstOrDefault() ?? new ContactInfo();
        return View(new ContactMessage());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(ContactMessage model)
    {
        if (HttpContext.Session.GetString("UserRole") == "Admin") return Forbid();
        if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Subject) || string.IsNullOrWhiteSpace(model.Message))
        {
            ViewBag.Error = "Lütfen tüm alanları doldurun.";
            ViewBag.ContactInfo = _context.ContactInfos.FirstOrDefault() ?? new ContactInfo();
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        _context.ContactMessages.Add(model);

        var admins = _context.AppUsers.Where(x => x.Role == "Admin").ToList();
        foreach (var admin in admins)
        {
            _context.AppNotifications.Add(new AppNotification
            {
                UserId = admin.Id,
                Title = "Yeni iletişim mesajı",
                Message = $"{model.Name} adlı kullanıcıdan yeni bir mesaj geldi: {model.Subject}"
            });
        }

        _context.SaveChanges();
        TempData["ContactSuccess"] = "Mesajınız alındı. En kısa sürede size dönüş yapacağız.";
        return RedirectToAction(nameof(Contact));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateContactInfo(ContactInfo model)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var info = _context.ContactInfos.FirstOrDefault();
        if (info == null) { info = new ContactInfo(); _context.ContactInfos.Add(info); }
        info.Email = model.Email?.Trim() ?? string.Empty;
        info.Phone = model.Phone?.Trim() ?? string.Empty;
        info.WorkingHours = model.WorkingHours?.Trim() ?? string.Empty;
        _context.SaveChanges();
        TempData["ContactInfoSuccess"] = "Destek bilgileri güncellendi.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult ContactMessages()
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        return View(_context.ContactMessages.OrderByDescending(x => x.CreatedAt).ToList());
    }

    public IActionResult MyMessages()
    {
        var email = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrWhiteSpace(email)) return RedirectToAction("Index", "Login");
        return View(_context.ContactMessages.Where(x => x.Email == email).OrderByDescending(x => x.CreatedAt).ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReplyToAdmin(int id, string reply)
    {
        var email = HttpContext.Session.GetString("UserEmail");
        if (string.IsNullOrWhiteSpace(email) || HttpContext.Session.GetString("UserRole") == "Admin") return Forbid();
        var message = _context.ContactMessages.FirstOrDefault(x => x.Id == id && x.Email == email);
        if (message == null) return NotFound();
        if (string.IsNullOrWhiteSpace(reply)) return RedirectToAction(nameof(MyMessages));
        message.UserReply = reply.Trim();
        message.UserRepliedAt = DateTime.UtcNow;
        foreach (var admin in _context.AppUsers.Where(x => x.Role == "Admin"))
            _context.AppNotifications.Add(new AppNotification { UserId = admin.Id, Title = "Kullanıcı yanıtı", Message = $"{message.Name} destek mesajına yanıt verdi: {message.Subject}" });
        _context.SaveChanges();
        TempData["UserReplySuccess"] = "Yanıtınız admine gönderildi.";
        return RedirectToAction(nameof(MyMessages));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ReplyToContactMessage(int id, string reply)
    {
        if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
        var message = _context.ContactMessages.Find(id);
        if (message == null) return NotFound();
        if (string.IsNullOrWhiteSpace(reply)) { TempData["ReplyError"] = "Yanıt boş olamaz."; return RedirectToAction(nameof(ContactMessages)); }

        message.AdminReply = reply.Trim();
        message.RepliedAt = DateTime.UtcNow;
        var messageOwner = _context.AppUsers.FirstOrDefault(x => x.Email == message.Email);
        if (messageOwner != null)
        {
            _context.AppNotifications.Add(new AppNotification
            {
                UserId = messageOwner.Id,
                Title = "Destek yanıtı",
                Message = $"'{message.Subject}' konulu mesajınıza destek ekibi yanıt verdi."
            });
        }
        _context.SaveChanges();

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
            email.To.Add(MailboxAddress.Parse(message.Email));
            email.Subject = $"KiraSat destek yanıtı: {message.Subject}";
            email.Body = new TextPart("html") { Text = $"<p>Merhaba {message.Name},</p><p>Mesajınıza yanıtımız:</p><p>{message.AdminReply}</p>" };
            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
            client.Authenticate(_mailSettings.Mail.Trim(), _mailSettings.Password.Trim());
            client.Send(email);
            client.Disconnect(true);
            TempData["ReplySuccess"] = "Yanıt kaydedildi ve e-posta olarak gönderildi.";
        }
        catch { TempData["ReplySuccess"] = "Yanıt kaydedildi; ancak e-posta gönderilemedi."; }
        return RedirectToAction(nameof(ContactMessages));
    }
}
