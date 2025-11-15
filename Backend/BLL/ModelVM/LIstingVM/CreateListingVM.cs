using System.ComponentModel.DataAnnotations;

namespace BLL.ModelVM.LIstingVM
{
    public class CreateListingVM
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = null!;

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal PricePerNight { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = null!;

        // by google maps
        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int MaxGuests { get; set; }

        public bool IsPromoted { get; set; } = false;

        public DateTime? PromotionEndDate { get; set; }

        public List<string>? Tags { get; set; } = new();

        // Require at least one image
        [MinLength(1, ErrorMessage = "Please upload at least one image.")]
        public List<IFormFile> Images { get; set; } = new();
    }
}
