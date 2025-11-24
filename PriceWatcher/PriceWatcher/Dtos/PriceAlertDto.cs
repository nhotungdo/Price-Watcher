namespace PriceWatcher.Dtos
{
    public class CreatePriceAlertDto
    {
        public int ProductId { get; set; }
        public decimal TargetPrice { get; set; }
        public string? Channel { get; set; } // "email", "push", "sms"
    }

    public class PriceAlertDto
    {
        public int AlertId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal? CurrentPrice { get; set; }
        public bool IsActive { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime? LastNotifiedAt { get; set; }
    }
}
