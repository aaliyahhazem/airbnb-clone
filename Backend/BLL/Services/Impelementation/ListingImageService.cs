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
        catch (Exception ex) { return new Response<List<ListingImageVM>>(new List<ListingImageVM>(), ex.Message, true); }
    }

    public async Task<Response<int>> AddImagesAsync(int listingId, List<IFormFile> files, Guid hostId, CancellationToken ct = default)
    {
        if (files == null || !files.Any()) return new Response<int>(0, "No files provided", true);
        try
        {
            var uploaded = new List<string>();
            foreach (var f in files)
            {
                var fname = Upload.UploadFile("listings", f);
                uploaded.Add(fname);
            }

            // find creator name via unit of work (or have caller pass it)
            var creator = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();


            var addedCount = await unitOfWork.ListingImages.AddImagesAsync(listingId, uploaded, creator, ct);
            return new Response<int>(addedCount, null, false);
        }
        catch (Exception ex) { return new Response<int>(0, ex.Message, true); }
    }

    public async Task<Response<bool>> UpdateImageAsync(int imageId, IFormFile file, Guid hostId, CancellationToken ct = default)
    {
        if (file == null) return new Response<bool>(false, "No file provided", true);

        try
        {
            // get image + listing
            var image = await unitOfWork.ListingImages.GetImageByIdAsync(imageId, ct);
            if (image == null) return new Response<bool>(false, "Image not found", true);

            if (image.Listing.UserId != hostId) return new Response<bool>(false, "Not owner", true);

            // upload new file
            var newFileName = Upload.UploadFile("listings", file);

            // remove old file from disk (best-effort)
            var oldFileName = image.ImageUrl;
            var removeResult = Upload.RemoveFile("listings", oldFileName);

            // update image entity using domain method
            var hostName = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var changed = image.UpdateImage(newFileName, hostName);
            if (!changed) return new Response<bool>(true, null, false);

            await unitOfWork.SaveChangesAsync();
            return new Response<bool>(true, null, false);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

    public async Task<Response<bool>> SoftDeleteImagesAsync(List<int> imageIds, Guid hostId, CancellationToken ct = default)
    {
        if (imageIds == null || !imageIds.Any()) return new Response<bool>(false, "No image ids", true);
        try
        {
            // get owner's full name
            var performer = (await unitOfWork.Users.GetByIdAsyncForlisting(hostId))?.FullName ?? hostId.ToString();

            var ok = await unitOfWork.ListingImages.SoftDeleteImagesAsync(imageIds, performer, ct);
            if (!ok) return new Response<bool>(false, "No images deleted", true);

            // remove files from disk best-effort (could fetch image urls before deleting)
            // To keep it simple: don't do file deletion here unless you fetched filenames earlier.

            return new Response<bool>(true, null, false);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }
}
