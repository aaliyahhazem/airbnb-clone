public class ListingService : IListingService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper mapper;
    private readonly UserManager<User> userManager;

    public ListingService(IUnitOfWork uow, IMapper mapper, UserManager<User> userManager)
    {
        unitOfWork = uow;
        this.mapper = mapper;
        this.userManager = userManager;
    }

    private async Task<string> ResolveFullNameAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.FullName ?? userId.ToString();
    }

    public async Task<Response<int>> CreateAsync(ListingCreateVM vm, Guid hostId, CancellationToken ct = default)
    {
        if (vm == null) return new Response<int>(0, "Input is null", true);
        try
        {
            // upload images
            var uploaded = new List<string>();
            if (vm.Images != null && vm.Images.Any())
            {
                foreach (var f in vm.Images)
                {
                    var filename = Upload.UploadFile("listings", f);
                    uploaded.Add(filename);
                }
            }

            // build a temporary Listing entity via domain Create (createdBy will be hostFullName)
            var hostFullName = await ResolveFullNameAsync(hostId, ct);
            var temp = Listing.Create(
                vm.Title, vm.Description, vm.PricePerNight, vm.Location,
                vm.Latitude, vm.Longitude, vm.MaxGuests,
                vm.Tags ?? new List<string>(), hostId, hostFullName,
                uploaded.FirstOrDefault() ?? string.Empty,
                vm.IsPromoted, vm.PromotionEndDate
            );

            var additional = uploaded.Skip(1).ToList();
            var id = await unitOfWork.Listings.CreateAsync(temp, uploaded.FirstOrDefault() ?? string.Empty, additional, hostId, ct);

            return new Response<int>(id, null, false);
        }
       
        catch (Exception ex)
        {
            return new Response<int>(0, ex.Message, true);
        }
    }

    public async Task<Response<ListingDetailVM?>> GetByIdWithImagesAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var listing = await unitOfWork.Listings.GetListingByIdAsync(id, ct);
            if (listing == null) return new Response<ListingDetailVM?>(null, "Not found", true);

            var vm = mapper.Map<ListingDetailVM>(listing);
            return new Response<ListingDetailVM?>(vm, null, false);
        }
        catch (Exception ex) { return new Response<ListingDetailVM?>(null, ex.Message, true); }
    }

    public async Task<Response<List<ListingOverviewVM>>> GetPagedOverviewAsync(int page, int pageSize, Expression<Func<Listing, bool>>? filter = null, CancellationToken ct = default)
    {
        try
        {
            var (listings, total) = await unitOfWork.Listings.GetUserViewAsync(filter, page, pageSize, ct);
            var vms = mapper.Map<List<ListingOverviewVM>>(listings);
            return new Response<List<ListingOverviewVM>>(vms, null, false);
        }
        catch (Exception ex) { return new Response<List<ListingOverviewVM>>(new List<ListingOverviewVM>(), ex.Message, true); }
    }

    public async Task<Response<List<ListingOverviewVM>>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var (listings, total) = await unitOfWork.Listings.GetHostViewAsync(userId, null, page, pageSize, ct);
            var vms = mapper.Map<List<ListingOverviewVM>>(listings);
            return new Response<List<ListingOverviewVM>>(vms, null, false);
        }
        catch (Exception ex) { return new Response<List<ListingOverviewVM>>(new List<ListingOverviewVM>(), ex.Message, true); }
    }

    public async Task<Response<bool>> UpdateAsync(int listingId, Guid hostId, ListingUpdateVM vm, CancellationToken ct = default)
    {
        if (vm == null) return new Response<bool>(false, "Input is null", true);

        try
        {
            // Build a Listing DTO object for repo update usage (domain update expects Listing class)
            var hostFullName = await ResolveFullNameAsync(hostId, ct);
            // Create a small Listing instance for new values (not persisted) — used only to pass values to repository Update.
            var dummyListing = Listing.Create(
                vm.Title, vm.Description, vm.PricePerNight, vm.Location,
                vm.Latitude, vm.Longitude, vm.MaxGuests, vm.Tags ?? new List<string>(),
                hostId, hostFullName, string.Empty, vm.IsPromoted, vm.PromotionEndDate
            );

            // Upload new images if any, gather file names
            var newImageUrls = new List<string>();
            if (vm.NewImages != null && vm.NewImages.Any())
            {
                foreach (var f in vm.NewImages)
                {
                    var fileName = Upload.UploadFile("listings", f);
                    newImageUrls.Add(fileName);
                }
            }

            // call repository update: imagesToRemove = vm.RemoveImageIds
            var ok = await unitOfWork.Listings.UpdateAsync(listingId, hostId, dummyListing,
                newMainImageUrl: null, // not passing main change here
                newAdditionalImages: newImageUrls,
                imagesToRemove: vm.RemoveImageIds,
                ct: ct);

            return new Response<bool>(ok, ok ? null : "Update failed", !ok);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

    public async Task<Response<bool>> SoftDeleteByOwnerAsync(int listingId, Guid hostId, CancellationToken ct = default)
    {
        try
        {
            var ok = await unitOfWork.Listings.DeleteAsync(listingId, hostId, ct);
            return new Response<bool>(ok, ok ? null : "Delete failed", !ok);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

    public async Task<Response<bool>> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default)
    {
        try
        {
            var ok = await unitOfWork.Listings.ApproveAsync(id, approverUserId, ct);
            return new Response<bool>(ok, ok ? null : "Approve failed", !ok);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

    public async Task<Response<bool>> RejectAsync(int id, Guid approverUserId, string? note, CancellationToken ct = default)
    {
        try
        {
            var ok = await unitOfWork.Listings.RejectAsync(id, approverUserId, note, ct);
            return new Response<bool>(ok, ok ? null : "Reject failed", !ok);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

    public async Task<Response<bool>> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default)
    {
        try
        {
            var ok = await unitOfWork.Listings.PromoteAsync(id, promotionEndDate, performedByUserId, ct);
            return new Response<bool>(ok, ok ? null : "Promote failed", !ok);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

    public async Task<Response<bool>> SetMainImageAsync(int listingId, int imageId, Guid hostId, CancellationToken ct = default)
    {
        try
        {
            // validate owner via repo helper
            var isOwner = await unitOfWork.Listings.IsOwnerAsync(listingId, hostId, ct);
            if (!isOwner) return new Response<bool>(false, "Not owner", true);

            var performer = await ResolveFullNameAsync(hostId, ct);
            var ok = await unitOfWork.ListingImages.SetMainImageAsync(listingId, imageId, performer, ct);

            return new Response<bool>(ok, ok ? null : "Set main image failed", !ok);
        }
        catch (Exception ex) { return new Response<bool>(false, ex.Message, true); }
    }

   
}
