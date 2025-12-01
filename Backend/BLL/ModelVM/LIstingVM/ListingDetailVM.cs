public class ListingDetailVM
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal PricePerNight { get; set; }
    public string Location { get; set; } = default!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int MaxGuests { get; set; }

    public bool IsApproved { get; set; }

    public string? MainImageUrl { get; set; }        

    public List<string> Amenities { get; set; } = new();
    public List<ListingImageVM> Images { get; set; } = new();

    public string Destination { get; set; }
    public string Type { get; set; }

    public int Bedrooms { get; set; }        
    public int Bathrooms { get; set; }

    // Dynamic Priority & Engagement System
    public int Priority { get; set; }
    public int ViewCount { get; set; }
    public int FavoriteCount { get; set; }
    public int BookingCount { get; set; }
}
