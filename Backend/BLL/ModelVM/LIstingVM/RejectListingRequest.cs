namespace BLL.ModelVM.ListingVM
{
    public class RejectListingRequest
    {
        [MaxLength(2000)]
        public string? Note { get; set; }
    }
}