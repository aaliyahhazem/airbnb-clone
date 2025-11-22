namespace BLL.ModelVM.ListingVM
{
    public class ListingOverviewVM
    {
        public string Title { get; set; } = null!;
        public string Description { get; set; }
        public decimal PricePerNight { get; set; }
        public string Location { get; set; } = null!;
        public string? MainImageUrl { get; set; }

        //public bool IsPromoted { get; set; }
        //public bool IsApproved { get; set; }

        public List<string> Amenities { get; set; } = new List<string>();
    }
}
