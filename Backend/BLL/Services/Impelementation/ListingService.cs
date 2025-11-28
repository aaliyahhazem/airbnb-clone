using BLL.ModelVM.LIstingVM;

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

    // Creates a new property listing with images and amenities.
    // First uploaded image automatically becomes the main image.
    //Response containing the newly created listing ID
    // The listing is created in "Pending Review" status and requires admin approval.
    public async Task<Response<int>> CreateAsync(ListingCreateVM vm, Guid hostId, CancellationToken ct = default)
    {
        if (vm == null) return new Response<int>(0, "Input is null", true);

        try
        {
            // 1) upload images
            var uploaded = new List<string>();
            if (vm.Images != null)
            {
                foreach (var f in vm.Images)
                {
                    var filename = await Upload.UploadFile("listings", f);
                    // Check if upload failed
                    if (Upload.IsError(filename))
                        return new Response<int>(0, filename, true);

                    uploaded.Add(filename);
                }
            }

            // 2) amenities
            var parsedAmenities = vm.Amenities ?? new List<string>();

            // 3) host name
            var hostFullName = await ResolveFullNameAsync(hostId, ct);

            // 4) create aggregate
            var temp = Listing.Create(
                vm.Title,
                vm.Description,
                vm.PricePerNight,
                vm.Location,
                vm.Latitude,
                vm.Longitude,
                vm.MaxGuests,
                hostId,
                hostFullName,
                uploaded.FirstOrDefault() ?? string.Empty, // main image
                vm.Destination,
                vm.Type,
                vm.NumberOfRooms,
                vm.NumberOfBathrooms,
                parsedAmenities
            );

            // extra images
            var additional = uploaded.Skip(1).ToList();

            // 5) persist
            var id = await unitOfWork.Listings.CreateAsync(
                temp,
                uploaded.FirstOrDefault() ?? string.Empty,// main image 
                additional, // extra images
                parsedAmenities, // amenities
                hostId,
                ct);

            return new Response<int>(id, null, false);
        }
        catch (Exception ex)
        {
            return new Response<int>(0, ex.Message, true);
        }
    }

    // Retrieves detailed information about a specific listing.
    // Includes all images, amenities, location data, and pricing.
    // Only returns non-deleted listings.
    public async Task<Response<ListingDetailVM?>> GetByIdWithImagesAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var listing = await unitOfWork.Listings.GetListingByIdAsync(id, ct);
            if (listing == null) return new Response<ListingDetailVM?>(null, "Not found", true);

            var vm = mapper.Map<ListingDetailVM>(listing);
            return new Response<ListingDetailVM?>(vm, null, false);
        }
        catch (Exception ex)
        {
            return new Response<ListingDetailVM?>(null, ex.Message, true);
        }
    }

    // Retrieves paginated list of approved listings for public view.
    // Supports filtering by location, price range, amenities, and guest capacity.
    // Only shows approved, non-deleted listings.
    public async Task<Response<List<ListingOverviewVM>>> GetPagedOverviewAsync(int page, int pageSize, ListingFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var (listings, total) = await unitOfWork.Listings.GetUserViewAsync(filter, page, pageSize, ct);
            var vms = mapper.Map<List<ListingOverviewVM>>(listings);
            return new Response<List<ListingOverviewVM>>(vms, null, false);
        }
        catch (Exception ex)
        {
            return new Response<List<ListingOverviewVM>>(new List<ListingOverviewVM>(), ex.Message, true);
        }
    }

    // Host listings
    public async Task<Response<List<ListingOverviewVM>>> GetByUserAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        try
        {
            var (listings, total) = await unitOfWork.Listings.GetHostViewAsync(userId, null, page, pageSize, ct);
            var vms = mapper.Map<List<ListingOverviewVM>>(listings);
            return new Response<List<ListingOverviewVM>>(vms, null, false);
        }
        catch (Exception ex)
        {
            return new Response<List<ListingOverviewVM>>(new List<ListingOverviewVM>(), ex.Message, true);
        }
    }

    public async Task<Response<List<ListingOverviewVM>>> GetAllForAdminAsync(
    int page,
    int pageSize,
    CancellationToken ct = default)
    {
        try
        {
            // No filter (get all listings), include deleted for admin
            var (listings, total) = await unitOfWork.Listings.GetAdminViewAsync(
                filter: null,
                page: page,
                pageSize: pageSize,
                includeDeleted: true,
                ct: ct
            );

            var vms = mapper.Map<List<ListingOverviewVM>>(listings);

            return new Response<List<ListingOverviewVM>>(vms, null, false);
        }
        catch (Exception ex)
        {
            return new Response<List<ListingOverviewVM>>(new List<ListingOverviewVM>(), ex.Message, true);
        }
    }


    // Updates an existing listing with new data, images, and amenities.
    // Can add new images and remove existing ones.
    // If listing was previously approved, it's marked for re-review.
    // Only the listing owner can update their listing.
    public async Task<Response<ListingUpdateVM>> UpdateAsync(
    int listingId,
    Guid hostId,
    ListingUpdateVM vm,
    CancellationToken ct = default)
    {
        if (vm == null)
            return new Response<ListingUpdateVM>(null, "Input is null", true);

        // 1. Owner check
        var isOwner = await unitOfWork.Listings.IsOwnerAsync(listingId, hostId, ct);
        if (!isOwner)
            return new Response<ListingUpdateVM>(null, "Not owner", true);

        try
        {
            var hostFullName = await ResolveFullNameAsync(hostId, ct);

            // Fetch existing listing to preserve Destination and Type
            var existingListing = await unitOfWork.Listings.GetByIdAsync(listingId, ct);
            if (existingListing == null)
                return new Response<ListingUpdateVM>(null, "Listing not found", true);

            // Validate provided values (only if they are provided)
            if (vm.PricePerNight.HasValue && (vm.PricePerNight < 1 || vm.PricePerNight > 100000))
                return new Response<ListingUpdateVM>(null, "PricePerNight must be between 1 and 100000", true);

            if (vm.MaxGuests.HasValue && (vm.MaxGuests < 1 || vm.MaxGuests > 50))
                return new Response<ListingUpdateVM>(null, "MaxGuests must be between 1 and 50", true);

            if (vm.Latitude.HasValue && (vm.Latitude < -90 || vm.Latitude > 90))
                return new Response<ListingUpdateVM>(null, "Latitude must be between -90 and 90", true);

            if (vm.Longitude.HasValue && (vm.Longitude < -180 || vm.Longitude > 180))
                return new Response<ListingUpdateVM>(null, "Longitude must be between -180 and 180", true);

            // 2. Handle image removal
            if (vm.RemoveImageIds != null && vm.RemoveImageIds.Any())
            {
                foreach (var imageId in vm.RemoveImageIds)
                {
                    var image = await unitOfWork.ListingImages.GetImageByIdAsync(imageId, ct);
                    if (image == null) continue;

                    // Validation checks
                    if (image.ListingId != listingId)
                        return new Response<ListingUpdateVM>(null, "Image does not belong to this listing", true);

                    if (image.Listing.UserId != hostId)
                        continue;

                    // Delete file from disk
                    var deleteResult = await Upload.RemoveFile("listings", image.ImageUrl);
                    // Continue even if file delete fails

                    // Hard delete from DB
                    await unitOfWork.ListingImages.HardDeleteImageById(imageId, hostFullName, ct);
                }
            }

            // 3. Upload new images
            var newImageUrls = new List<string>();
            if (vm.NewImages != null)
            {
                foreach (var file in vm.NewImages)
                {
                    var fileName = await Upload.UploadFile("listings", file);

                    // Check if upload failed
                    if (Upload.IsError(fileName))
                        return new Response<ListingUpdateVM>(null, fileName, true);

                    newImageUrls.Add(fileName);
                }
            }

            // 4. Create a minimal aggregate with updated scalar fields
            // Use provided values or fall back to existing values
            var updatedListing = Listing.Create(
                    vm.Title ?? existingListing.Title,
                    vm.Description ?? existingListing.Description,
                    vm.PricePerNight ?? existingListing.PricePerNight,
                    vm.Location ?? existingListing.Location,
                    vm.Latitude ?? existingListing.Latitude,    
                    vm.Longitude ?? existingListing.Longitude,
                    vm.MaxGuests ?? existingListing.MaxGuests,
                    hostId,
                    hostFullName,
                    string.Empty,
                    vm.Destination ?? existingListing.Destination,
                    vm.Type ?? existingListing.Type,
                    vm.NumberOfRooms ?? existingListing.NumberOfRooms,
                    vm.NumberOfBathrooms ?? existingListing.NumberOfBathrooms
                );

            // 5. Save updated fields + new images
            var ok = await unitOfWork.Listings.UpdateAsync(
                listingId,
                hostId,
                updatedListing,
                newMainImageUrl: null,
                newAdditionalImages: newImageUrls,
                imagesToRemove: vm.RemoveImageIds , // Already handled above
                keywordNames: vm.Amenities,
                ct: ct
            );

            if (!ok)
                return new Response<ListingUpdateVM>(null, "Update failed", true);

            // 6. Return updated data
            var finalListing = await unitOfWork.Listings.GetListingByIdAsync(listingId, ct);
            var vmOut = mapper.Map<ListingUpdateVM>(finalListing);

            return new Response<ListingUpdateVM>(vmOut, null, false);
        }
        catch (Exception ex)
        {
            return new Response<ListingUpdateVM>(null, ex.Message, true);
        }
    }

    // Soft deletes a listing (marks as deleted but keeps in database).
    // Only the listing owner can delete their listing.
    // Deleted listings are hidden from public view but preserved for records.
    public async Task<Response<bool>> SoftDeleteByOwnerAsync(int listingId, Guid hostId, CancellationToken ct = default)
    {
        var isOwner = await unitOfWork.Listings.IsOwnerAsync(listingId, hostId, ct);
        if (!isOwner) return new Response<bool>(false, "Not owner", true);

        try
        {
            var ok = await unitOfWork.Listings.DeleteAsync(listingId, hostId, ct);
            return new Response<bool>(ok, ok ? null : "Delete failed", !ok);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    // Approves a listing for public visibility (Admin only).
    // Sets IsApproved=true and IsReviewed=true.
    // Approved listings appear in public search results.
    // Records who approved it and when for audit trail.
    public async Task<Response<bool>> ApproveAsync(int id, Guid approverUserId, CancellationToken ct = default)
    {
        var approver = await userManager.FindByIdAsync(approverUserId.ToString());
        if (approver == null) return new Response<bool>(false, "Not found", true);

        var isAdmin = await userManager.IsInRoleAsync(approver, "Admin");
        if (!isAdmin) return new Response<bool>(false, "Not admin", true);

        try
        {
            var ok = await unitOfWork.Listings.ApproveAsync(id, approverUserId, ct);
            return new Response<bool>(ok, ok ? null : "Approve failed", !ok);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    // Rejects a listing with optional feedback note (Admin only).
    // Sets IsApproved=false and IsReviewed=true.
    // Host can see rejection note and resubmit after making changes.
    // Records who rejected it, when, and why for audit trail.
    public async Task<Response<bool>> RejectAsync(int id, Guid approverUserId, string? note, CancellationToken ct = default)
    {
        var approver = await userManager.FindByIdAsync(approverUserId.ToString());
        if (approver == null) return new Response<bool>(false, "Not found", true);

        var isAdmin = await userManager.IsInRoleAsync(approver, "Admin");
        if (!isAdmin) return new Response<bool>(false, "Not admin", true);

        try
        {
            var ok = await unitOfWork.Listings.RejectAsync(id, approverUserId, note, ct);
            return new Response<bool>(ok, ok ? null : "Reject failed", !ok);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    
    // Set main image
    public async Task<Response<bool>> SetMainImageAsync(int listingId, int imageId, Guid hostId, CancellationToken ct = default)
    {
        var isOwner = await unitOfWork.Listings.IsOwnerAsync(listingId, hostId, ct);
        if (!isOwner) return new Response<bool>(false, "Not owner", true);

        try
        {
            var performer = await ResolveFullNameAsync(hostId, ct);
            var ok = await unitOfWork.ListingImages.SetMainImageAsync(listingId, imageId, performer, ct);
            return new Response<bool>(ok, ok ? null : "Set main image failed", !ok);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }


    // Promotes a listing to appear at top of search results until end date.
    // Can be performed by Admin .
    // Validates that:
    // - User is Admin or listing owner
    // - Listing is not already promoted
    // - Promotion end date is in the future
    // - Listing is approved (only approved listings can be promoted)
    public async Task<Response<bool>> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default)
    {
        try
        {
            //  Get and validate user exists
            var performer = await userManager.FindByIdAsync(performedByUserId.ToString());
            if (performer == null)
                return new Response<bool>(false, "User not found", true);

            // Check if user is Admin
            var isAdmin = await userManager.IsInRoleAsync(performer, "Admin");

            if (!isAdmin)
            {
              return new Response<bool>(false, "Only Admin  can promote listings", true);
            }

            // Validate promotion end date before calling repository
            if (promotionEndDate <= DateTime.UtcNow)
                return new Response<bool>(false, "Promotion end date must be in the future", true);

            // Call repository to promote
            var ok = await unitOfWork.Listings.PromoteAsync(id, promotionEndDate, performedByUserId, ct);
            return new Response<bool>(ok, null, false);
        }
       
        catch (Exception ex)
        {
            // Unexpected errors
            return new Response<bool>(false, $"Failed to promote listing: {ex.Message}", true);
        }
    }

    // Unpromotes a listing (cancels active promotion).
    // Can be performed by Admin.
    // Used to manually cancel a promotion before it expires.
    public async Task<Response<bool>> UnpromoteAsync(int id, Guid performedByUserId, CancellationToken ct = default)
    {
        try
        {
            var performer = await userManager.FindByIdAsync(performedByUserId.ToString());
            if (performer == null)
                return new Response<bool>(false, "User not found", true);

            var isAdmin = await userManager.IsInRoleAsync(performer, "Admin");
            if (!isAdmin)
            {
                return new Response<bool>(false, "Only Admin can unpromote listings", true);
            }

            var ok = await unitOfWork.Listings.UnpromoteAsync(id, performedByUserId, ct);
            return new Response<bool>(ok, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }
    // Extends an existing promotion with a new end date.
    // New end date must be after current promotion end date.
    // Only Admin can extend promotions.
    public async Task<Response<bool>> ExtendPromotionAsync(int id, DateTime newPromotionEndDate, Guid performedByUserId, CancellationToken ct = default)
    {
        try
        {
            var performer = await userManager.FindByIdAsync(performedByUserId.ToString());
            if (performer == null)
                return new Response<bool>(false, "User not found", true);

            var isAdmin = await userManager.IsInRoleAsync(performer, "Admin");
            if (!isAdmin)
            {
                return new Response<bool>(false, "Only Admincan extend promotions", true);
            }

            var ok = await unitOfWork.Listings.ExtendPromotionAsync(id, newPromotionEndDate, performedByUserId, ct);
            return new Response<bool>(ok, null, false);
        }
        catch (Exception ex)
        {
            return new Response<bool>(false, ex.Message, true);
        }
    }

    // Get home view listings.


    public async Task<Response<List<HomeVM>>> GetHomeViewAsync(int page, int pageSize, ListingFilterDto? filter = null, CancellationToken ct = default)
    {
        try
        {
            var (listings, total) = await unitOfWork.Listings.GetUserViewAsync(filter, page, pageSize, ct);
            var vms = mapper.Map<List<HomeVM>>(listings);
            return new Response<List<HomeVM>>(vms, null, false);
        }
        catch (Exception ex)
        {
            return new Response<List<HomeVM>>(new List<HomeVM>(), ex.Message, true);
        }
    }
}

