namespace KiraSepet.EntityLayer;

public class AboutPageContent
{
    public int Id { get; set; }
    public string Title { get; set; } = "KiraSat";
    public string Content { get; set; } = "KiraSat, kullanıcıların ürünleri kolayca kiralayıp satın alabilmesi için geliştirilmiş bir platformdur.";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
