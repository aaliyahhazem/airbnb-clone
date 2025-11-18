
public class ListingImageUpdateVM
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    public int ImageId { get; set; }
}
