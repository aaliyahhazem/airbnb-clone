
namespace BLL.ModelVM.MapsVM
{
    public class PropertyMapDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal PricePerNight { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? MainImageUrl { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}
