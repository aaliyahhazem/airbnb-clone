namespace BLL.ModelVM.ListingVM
{
    public class ListingUpdateVM
    {
        [ MaxLength(150)]
        public string? Title { get; set; } = null!;

        [MaxLength(2000)]
        public string Description { get; set; } = null!;

        [ Range(1, 100000)]
        public decimal PricePerNight { get; set; }

        [MaxLength(255)]
        public string Location { get; set; } = null!;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 50)]
        public int MaxGuests { get; set; }

        public List<string>? Amenities { get; set; }

        // Images to add on update (we will upload these)
        public List<IFormFile>? NewImages { get; set; }

        // Existing image ids to soft-delete
        public List<int>? RemoveImageIds { get; set; }


        // Output properties (for response)
        public List<ListingImageVM>? Images { get; set; }  // Saved images
        public ListingImageVM? MainImage { get; set; }


        //public bool IsPromoted { get; set; }
        //public DateTime? PromotionEndDate { get; set; }
    }
}
