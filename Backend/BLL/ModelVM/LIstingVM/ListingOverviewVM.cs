namespace BLL.ModelVM.ListingVM
{
    public class ListingOverviewVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public decimal PricePerNight { get; set; }
        public string Location { get; set; } = null!;
        public string? MainImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsPromoted { get; set; }
        public bool IsApproved { get; set; }
    }
}
