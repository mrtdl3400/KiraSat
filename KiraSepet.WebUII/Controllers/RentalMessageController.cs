using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
namespace KiraSepet.WebUII.Controllers;

public class RentalMessageController : Controller
{
    private readonly Context _context;

    public RentalMessageController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Start(int productId)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return RedirectToAction("Index", "Login");
        }

        var tenant = _context.AppUsers.FirstOrDefault(x => x.Email == userEmail);
        var product = _context.Products.FirstOrDefault(x => x.Id == productId && !x.IsDeleted);

        if (tenant == null || product == null || product.BusinessId == null)
        {
            return NotFound();
        }

        var business = _context.Businesses.FirstOrDefault(x => x.Id == product.BusinessId);

        if (business == null)
        {
            return NotFound();
        }

        if (business.OwnerUserId == tenant.Id)
        {
            TempData["RentalMessageError"] = "Kendi ilanınıza mesaj gönderemezsiniz.";
            return RedirectToAction("ProductDetails", "Product", new { id = productId });
        }

        var conversation = _context.RentalConversations.FirstOrDefault(x =>
            x.ProductId == productId &&
            x.TenantUserId == tenant.Id &&
            x.OwnerUserId == business.OwnerUserId);

        if (conversation == null)
        {
            conversation = new RentalConversation
            {
                ProductId = productId,
                TenantUserId = tenant.Id,
                OwnerUserId = business.OwnerUserId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.RentalConversations.Add(conversation);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
    }

    

    [HttpGet]
    public IActionResult StartForRentalOrder(int rentalOrderId)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return RedirectToAction("Index", "Login");
        }

        var owner = _context.AppUsers.FirstOrDefault(x => x.Email == userEmail);
        var rentalOrder = _context.RentalOrders.FirstOrDefault(x => x.Id == rentalOrderId);

        if (owner == null || rentalOrder == null)
        {
            return NotFound();
        }

        var product = _context.Products.FirstOrDefault(x => x.Id == rentalOrder.ProductId && !x.IsDeleted);

        if (product == null || product.BusinessId == null)
        {
            return NotFound();
        }

        var business = _context.Businesses.FirstOrDefault(x =>
            x.Id == product.BusinessId &&
            x.OwnerUserId == owner.Id);

        var tenant = _context.AppUsers.FirstOrDefault(x => x.Email == rentalOrder.UserEmail);

        if (business == null || tenant == null)
        {
            return NotFound();
        }

        var conversation = _context.RentalConversations.FirstOrDefault(x =>
            x.ProductId == product.Id &&
            x.TenantUserId == tenant.Id &&
            x.OwnerUserId == owner.Id);

        if (conversation == null)
        {
            conversation = new RentalConversation
            {
                ProductId = product.Id,
                TenantUserId = tenant.Id,
                OwnerUserId = owner.Id,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.RentalConversations.Add(conversation);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Conversation), new { id = conversation.Id });
    }
    public IActionResult Conversation(int id)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return RedirectToAction("Index", "Login");
        }

        var currentUser = _context.AppUsers.FirstOrDefault(x => x.Email == userEmail);

        if (currentUser == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var conversation = _context.RentalConversations.FirstOrDefault(x => x.Id == id);

        if (conversation == null)
        {
            return NotFound();
        }

        if (conversation.TenantUserId != currentUser.Id &&
            conversation.OwnerUserId != currentUser.Id)
        {
            return Forbid();
        }

        var messages = _context.RentalMessages
            .Where(x => x.RentalConversationId == id)
            .OrderBy(x => x.SentAt)
            .ToList();

        var alternativeProductIds = messages
    .Select(x => Regex.Match(x.Text, @"ProductDetails\?id=(\d+)"))
    .Where(x => x.Success)
    .Select(x => int.Parse(x.Groups[1].Value))
    .Distinct()
    .ToList();

        ViewBag.MessageProductCards = _context.Products
            .Where(x => alternativeProductIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionary(x => x.Id);

        var unreadMessages = messages
        .Where(x => x.SenderUserId != currentUser.Id && !x.IsRead)
        .ToList();

        if (unreadMessages.Any())
        {
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            _context.SaveChanges();
        }

        var product = _context.Products.Find(conversation.ProductId);

        var business = product?.BusinessId is int businessId
            ? _context.Businesses.Find(businessId)
            : null;

        var counterpartId = conversation.TenantUserId == currentUser.Id
            ? conversation.OwnerUserId
            : conversation.TenantUserId;

        var counterpart = _context.AppUsers.Find(counterpartId);

        ViewBag.CurrentUserId = currentUser.Id;
        ViewBag.Conversation = conversation;
        ViewBag.Product = product;
        ViewBag.IsOwner = conversation.OwnerUserId == currentUser.Id;
        ViewBag.BusinessName = business?.CompanyName ?? "İşletme bilgisi bulunamadı";
        ViewBag.CounterpartName = counterpart?.NameSurname ?? "Bilinmiyor";
        ViewBag.CounterpartEmail = counterpart?.Email ?? "E-posta bulunamadı";

        ViewBag.AlternativeProducts = conversation.OwnerUserId == currentUser.Id && product?.BusinessId != null
    ? _context.Products
        .Where(x => x.BusinessId == product.BusinessId &&
                    x.Id != product.Id &&
                    !x.IsDeleted &&
                    x.IsRentable &&
                    x.RentalStockCount > 0)
        .OrderBy(x => x.ProductName)
        .ToList()
    : new List<Product>();


        return View(messages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Send(int conversationId, string text)
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return RedirectToAction("Index", "Login");
        }

        var currentUser = _context.AppUsers.FirstOrDefault(x => x.Email == userEmail);
        var conversation = _context.RentalConversations.FirstOrDefault(x => x.Id == conversationId);

        if (currentUser == null || conversation == null)
        {
            return NotFound();
        }

        if (conversation.TenantUserId != currentUser.Id &&
            conversation.OwnerUserId != currentUser.Id)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return RedirectToAction(nameof(Conversation), new { id = conversationId });
        }

        var message = new RentalMessage
        {
            RentalConversationId = conversationId,
            SenderUserId = currentUser.Id,
            Text = text.Trim(),
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.RentalMessages.Add(message);

        conversation.LastMessageAt = DateTime.UtcNow;

        _context.SaveChanges();

        return RedirectToAction(nameof(Conversation), new { id = conversationId });
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userEmail = HttpContext.Session.GetString("UserEmail");

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return RedirectToAction("Index", "Login");
        }

        var currentUser = _context.AppUsers.FirstOrDefault(x => x.Email == userEmail);

        if (currentUser == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var conversations = _context.RentalConversations
            .Where(x => x.TenantUserId == currentUser.Id ||
                        x.OwnerUserId == currentUser.Id)
            .OrderByDescending(x => x.LastMessageAt)
            .ToList();



        var productIds = conversations.Select(x => x.ProductId).ToList();
        var conversationIds = conversations.Select(x => x.Id).ToList();

        ViewBag.Products = _context.Products.Where(x => productIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.ProductName);

        ViewBag.LatestMessages = _context.RentalMessages
            .Where(x => conversationIds.Contains(x.RentalConversationId))
            .ToList()
            .GroupBy(x => x.RentalConversationId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(message => message.SentAt).First().Text);

        ViewBag.UnreadConversationIds = _context.RentalMessages
            .Where(x => conversationIds.Contains(x.RentalConversationId) &&
                        x.SenderUserId != currentUser.Id &&
                        !x.IsRead)
            .Select(x => x.RentalConversationId)
            .Distinct()
            .ToHashSet();

        var counterpartIds = conversations
    .Select(x => x.TenantUserId == currentUser.Id
        ? x.OwnerUserId
        : x.TenantUserId)
    .Distinct()
    .ToList();

        ViewBag.CounterpartNames = _context.AppUsers
            .Where(x => counterpartIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.NameSurname);

        ViewBag.CounterpartEmails = _context.AppUsers
            .Where(x => counterpartIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Email);

        var businessIds = _context.Products
            .Where(x => productIds.Contains(x.Id) && x.BusinessId != null)
            .Select(x => x.BusinessId!.Value)
            .Distinct()
            .ToList();

        ViewBag.BusinessNames = _context.Businesses
            .Where(x => businessIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.CompanyName);

        ViewBag.ProductBusinessIds = _context.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.BusinessId);

        ViewBag.CurrentUserId = currentUser.Id;

        return View(conversations);
    }
}
    


