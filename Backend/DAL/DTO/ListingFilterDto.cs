public class ListingFilterDto
{
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Rooms { get; set; }

    // single keyword or a comma-separated string from query param
    public string? Amenity { get; set; }
    //filter with availability

    //for search in title
    public string? TitleContains { get; set; }
}
