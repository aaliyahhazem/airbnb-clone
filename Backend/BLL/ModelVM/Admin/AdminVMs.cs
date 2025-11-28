namespace BLL.ModelVM.Admin
{
    public class UserAdminVM
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public int TotalBookings { get; set; }
        public int TotalListings { get; set; }
    }

    public class ListingAdminVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Location { get; set; } = null!;
        public decimal PricePerNight { get; set; }
        public bool IsApproved { get; set; }
        public bool IsReviewed { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotionEndDate { get; set; }
        public Guid HostId { get; set; }
        public string HostName { get; set; } = null!;
        public int ReviewsCount { get; set; }
    }

    public class BookingAdminVM
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = null!;
        public Guid GuestId { get; set; }
        public string GuestName { get; set; } = null!;
        public Guid HostId { get; set; }
        public string HostName { get; set; } = null!;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string BookingStatus { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public int? PaymentId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class RevenuePointVM
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Total { get; set; }
    }

    public class PromotionSummaryVM
    {
        public int ListingId { get; set; }
        public string Title { get; set; } = null!;
        public Guid HostId { get; set; }
        public string HostName { get; set; } = null!;
        public DateTime? PromotionEndDate { get; set; }
        public bool IsActive { get; set; }
    }
}