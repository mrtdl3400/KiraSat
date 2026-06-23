using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;

namespace KiraSepet.WebUII.Controllers;

public class SupportController : Controller
{
    private readonly Context _context;

    public SupportController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult New()
    {
        if (HttpContext.Session.GetString("UserEmail") == null)
        {
            return RedirectToAction("Index", "Login");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(string subject, string text)
    {
        var email = HttpContext.Session.GetString("UserEmail");

        if (email == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(text))
        {
            TempData["SupportError"] = "Konu ve mesaj boş bırakılamaz.";
            return RedirectToAction(nameof(New));
        }

        var user = _context.AppUsers.FirstOrDefault(x => x.Email == email);

        if (user == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var conversation = new SupportConversation
        {
            UserId = user.Id,
            Subject = subject.Trim(),
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };

        _context.SupportConversations.Add(conversation);
        _context.SaveChanges();

        var message = new SupportMessage
        {
            SupportConversationId = conversation.Id,
            SenderUserId = user.Id,
            Text = text.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.SupportMessages.Add(message);
        _context.SaveChanges();

        TempData["SupportSuccess"] = "Destek talebiniz gönderildi.";
        return RedirectToAction(nameof(New));
    }
}