namespace BLL.ModelVM.Review
{
    public class CreateReviewVM
    {
        public int BookingId { get; set; }
        
        public int Rating { get; set; }
        public string Comment { get; set; } = null!;
        
    }
}
