namespace DAL.Repo.Abstraction
{
    public interface IListingImageRepository : IGenericRepository<ListingImage>
    {
        //AddImagesToListAsync
        Task<List<ListingImage>> AddImagesToListAsync(int listingId, string imgPath ,string CreatedBy);
        //DeleteImagesFromListAsync
        Task<bool> DeleteImagesFromListAsync(int listingId,int listingImageId, string DeletedBy);

        //get All Images By ListingId for view details
        Task<List<ListingImage>> GetAllImagesByListingIdAsync(int listingId);

        //update Image in listing
        Task<ListingImage> UpdateImageInListingAsync(int listingId, int listingImageId, string newImageUrl, string UpdatedBy);
        





    }
}
