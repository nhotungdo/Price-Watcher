using System;

namespace PriceWatcher.Dtos
{
    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int Stars { get; set; }
        public string? Content { get; set; }
    }

    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserAvatarUrl { get; set; }
        public int Stars { get; set; }
        public string? Content { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
