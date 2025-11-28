namespace BLL.ModelVM.ListingVM
{
    public class ListingUpdateVM
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? PricePerNight { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? MaxGuests { get; set; }
        public string? Destination { get; set; }
        public string? Type { get; set; }
        public int? NumberOfRooms { get; set; }
        public int? NumberOfBathrooms { get; set; }
        public List<string>? Amenities { get; set; }
        public List<IFormFile>? NewImages { get; set; }
        public List<int>? RemoveImageIds { get; set; }
        public List<ListingImageVM>? Images { get; set; }
        public ListingImageVM? MainImage { get; set; }
    }
}
