namespace BLL.ModelVM.ListingVM
{
    public class ListingCreateVM
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = null!;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = null!;

        [Required, Range(1, 100000)]
        public decimal PricePerNight { get; set; }

        [Required, MaxLength(255)]
        public string Location { get; set; } = null!;

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Required, Range(1, 50)]
        public int MaxGuests { get; set; }
        
        [Required]
        public string Destination { get; set; }
        [Required]
        public string Type { get; set; }
        [Required]
        public int NumberOfRooms { get; set; }
        [Required]
        public int NumberOfBathrooms { get; set; }


        public List<string>? Amenities { get; set; }   // e.g. ["beach","family"]

        // images to upload on create (first image -> main image)
        public List<IFormFile>? Images { get; set; }
        //public bool IsPromoted { get; set; }
        //public DateTime? PromotionEndDate { get; set; }
       
    }
}
