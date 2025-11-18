namespace BLL.ModelVM.ListingVM
{
    public class ListingDetailVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal PricePerNight { get; set; }
        public string Location { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int MaxGuests { get; set; }
        public List<string> Tags { get; set; } = new();
        public string? MainImageUrl { get; set; }
        public List<ListingImageVM> Images { get; set; } = new();
        public bool IsPromoted { get; set; }
        public bool IsApproved { get; set; }
    }
}
