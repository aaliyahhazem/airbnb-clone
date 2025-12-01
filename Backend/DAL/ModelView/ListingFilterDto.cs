public class ListingFilterDto
{
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Rooms { get; set; }

    // single keyword or a comma-separated string from query param
    public string? Amenity { get; set; }
    //filter with availability

    public string? Type { get; set; }
    public string? Destination { get; set; }

    public string? TitleContains { get; set; }

    // Dynamic Priority & Engagement System Filters
    public int? MinPriority { get; set; }
    public int? MinBookingCount { get; set; }
    public int? MinFavoriteCount { get; set; }
    public decimal? MinRating { get; set; }
}
