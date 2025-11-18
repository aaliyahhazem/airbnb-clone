public class ListingImageCreateVM
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    public int ListingId { get; set; }
}
