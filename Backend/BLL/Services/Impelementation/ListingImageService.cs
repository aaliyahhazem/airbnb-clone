
using BLL.Helper;

public class ListingImageService : IListingImageService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper mapper;

    public ListingImageService(IUnitOfWork uow, IMapper mapper)
    {
        unitOfWork = uow;
        this.mapper = mapper;
    }

    public async Task<Response<List<ListingImageVM>>> GetImagesByListingAsync(int listingId, CancellationToken ct = default)
    {
        try
        {
            var imgs = await unitOfWork.ListingImages.GetActiveImagesByListingIdAsync(listingId, ct);
            var vms = mapper.Map<List<ListingImageVM>>(imgs);
            return new Response<List<ListingImageVM>>(vms, null, false);
        }
        catch (Exception ex)
        {
            return new Response<List<ListingImageVM>>(new List<ListingImageVM>(), ex.Message, true);
        }
    }

    public async Task<Response<int>> AddImagesAsync(int listingId, List<IFormFile> files, Guid hostId, CancellationToken ct = default)
    {
        if (files == null || !files.Any()) return new Response<int>(0, "No files provided", true);

        try
        {
            var isOwner = await unitOfWork.Listings.IsOwnerAsync(listingId, hostId, ct);
            if (!isOwner) return new Response<int>(0, "Not owner", true);

            var uploaded = new List<string>();
            foreach (var f in files)
            {
                var fname = await Upload.UploadFile("listings", f);

                // Check if upload failed
                if (Upload.IsError(fname))
                    return new Response<int>(0, fname, true);

                uploaded.Add(fname);
            }

            var creator = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var addedCount = await unitOfWork.ListingImages.AddImagesAsync(listingId, uploaded, creator, ct);
            return new Response<int>(addedCount, null, false);
        }
        catch (Exception ex)
        {
            return new Response<int>(0, ex.Message, true);
        }
    }

    public async Task<Response<bool>> UpdateImageAsync(int imageId, IFormFile file, Guid hostId, CancellationToken ct = default)
    {
        if (file == null) return new Response<bool>(false, "No file provided", true);

        try
        {
            var image = await unitOfWork.ListingImages.GetImageByIdAsync(imageId, ct);
            if (image == null) return new Response<bool>(false, "Image not found", true);

            if (image.Listing.UserId != hostId) return new Response<bool>(false, "Not owner", true);

            var newFileName = await Upload.UploadFile("listings", file);

            // Check if upload failed
            if (Upload.IsError(newFileName))
                return new Response<bool>(false, newFileName, true);

            var oldFileName = image.ImageUrl;
            await Upload.RemoveFile("listings", oldFileName);

            var hostName = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var changed = image.UpdateImage(newFileName, hostName);
            if (!changed) return new Response<bool>(true, null, false);

            await unitOfWork.SaveChangesAsync();
            return new Response<bool>(true, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    public async Task<Response<bool>> DeleteImageByIdAsync(int imageId, Guid hostId, CancellationToken ct = default)
    {
        try
        {
            var performer = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var imageHost = await unitOfWork.ListingImages.IsImageOwnerAsync(imageId, hostId, ct);
            if (!imageHost)
                return new Response<bool>(false, "Not owner", true);

            var image = await unitOfWork.ListingImages.GetImageByIdAsync(imageId, ct);
            if (image == null)
                return new Response<bool>(false, "Image not found", true);

            var removeResult = await Upload.RemoveFile("listings", image.ImageUrl);
            // Continue even if file doesn't exist

            var ok = await unitOfWork.ListingImages.HardDeleteImageById(imageId, performer, ct);
            if (!ok)
                return new Response<bool>(false, "Database deletion failed", true);

            return new Response<bool>(true, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

   
    public async Task<Response<bool>> SoftDeleteImagesAsync(List<int> imageIds, Guid hostId, CancellationToken ct = default)
    {
        if (imageIds == null || !imageIds.Any()) return new Response<bool>(false, "No image ids", true);

        try
        {
            var performer = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var ok = await unitOfWork.ListingImages.SoftDeleteImagesAsync(imageIds, performer, ct);
            if (!ok) return new Response<bool>(false, "No images deleted", true);

            return new Response<bool>(true, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    public async Task<Response<bool>> SoftDeleteByAdminAsync(int imageId, Guid performedByUserId, CancellationToken ct = default)
    {
        try
        {
            var performer = (await unitOfWork.Users.GetByIdAsyncForlisting(performedByUserId))?.FullName
                            ?? performedByUserId.ToString();

            var ok = await unitOfWork.ListingImages.SoftDeleteImagesAsync(new List<int> { imageId }, performer, ct);
            if (!ok) return new Response<bool>(false, "No image deleted", true);

            return new Response<bool>(true, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    public async Task<Response<bool>> SetMainImageAsync(int listingId, int imageId, Guid hostId, CancellationToken ct = default)
    {
        try
        {
            var imageOwner = await unitOfWork.ListingImages.IsImageOwnerAsync(imageId, hostId, ct);
            if (!imageOwner) return new Response<bool>(false, "Not owner", true);

            var performer = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var ok = await unitOfWork.ListingImages.SetMainImageAsync(listingId, imageId, performer, ct);
            if (!ok) return new Response<bool>(false, "Set main image failed", true);

            return new Response<bool>(true, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    
    
}
