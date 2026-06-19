namespace KiraSepet.EntityLayer
{
    public class Business
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        // Vergi numarasındaki baştaki 0'lar kaybolmasın diye string.
        public string TaxNumber { get; set; } = string.Empty;

        // Bu işletmenin sahibi olan kullanıcının Id'si.
        public int OwnerUserId { get; set; }

        // Admin onaylamadan işletme satış/kiralama yapamasın.
        public bool IsApproved { get; set; } = false;

        public BusinessStatus Status { get; set; } = BusinessStatus.Pending;

        // Ornegin 10 = yüzde 10 platform komisyonu.
        public decimal CommissionRate { get; set; } = 10m;
    }
}
