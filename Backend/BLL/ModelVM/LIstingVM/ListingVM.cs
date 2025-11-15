
namespace BLL.ModelVM.LIstingVM
{
    public class ListingVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal PricePerNight { get; set; }
        public string Location { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int MaxGuests { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPromoted { get; set; }
        public DateTime? PromotionEndDate { get; set; }
        public IReadOnlyList<ListingImageVM> Images { get; set; } = new List<ListingImageVM>();
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
        public string CreatedBy { get; set; } = null!;
        public bool IsApproved { get; set; }
        public bool IsReviewed { get; set; }
    }
}
