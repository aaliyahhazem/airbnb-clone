using AutoMapper;
using BLL.ModelVM.ListingVM;
using BLL.Services.Abstractions;
using DAL.Entities;
using DAL.Repo.Abstraction;
using Microsoft.AspNetCore.Identity;

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

    // Create a new listing
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
                    var filename = Upload.UploadFile("listings", f);
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
                uploaded.FirstOrDefault() ?? string.Empty,
                parsedAmenities
            );

            // extra images
            var additional = uploaded.Skip(1).ToList();

            // 5) persist
            var id = await unitOfWork.Listings.CreateAsync(
                temp,
                uploaded.FirstOrDefault() ?? string.Empty,
                additional,
                parsedAmenities,
                hostId,
                ct);

            return new Response<int>(id, null, false);
        }
        catch (Exception ex)
        {
            return new Response<int>(0, ex.Message, true);
        }
    }

    // Retrieve listing details
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

    // Public overview
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

    // Update listing
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

            // 2. HARD DELETE images selected for removal
            if (vm.RemoveImageIds != null && vm.RemoveImageIds.Any())
            {
                foreach (var imageId in vm.RemoveImageIds)
                {
                    var image = await unitOfWork.ListingImages.GetImageByIdAsync(imageId, ct);
                    if (image == null)
                        continue; // image not found, ignore

                    // 👇 IMPORTANT CHECKS
                    // 1) must belong to this listing
                    if (image.ListingId != listingId)
                        throw new Exception("This image not found in this listing");

                    // 2) listing must belong to this host
                    if (image.Listing.UserId != hostId)
                        continue;

                    // delete file from disk
                    Upload.RemoveFile("listings", image.ImageUrl);

                    // hard delete from DB
                    await unitOfWork.ListingImages.HardDeleteImageById(imageId, hostFullName, ct);
                }
            }
            // 3. Upload new images
            var newImageUrls = new List<string>();
            if (vm.NewImages != null)
            {
                foreach (var file in vm.NewImages)
                {
                    var fileName = Upload.UploadFile("listings", file);
                    newImageUrls.Add(fileName);
                }
            }

            // 4. Create a minimal aggregate with updated scalar fields
            var updatedListing = Listing.Create(
                vm.Title,
                vm.Description,
                vm.PricePerNight,
                vm.Location,
                vm.Latitude,
                vm.Longitude,
                vm.MaxGuests,
                hostId,
                hostFullName,
                string.Empty
            );

            // 5. Save updated fields + new images (no soft-delete needed)
            var ok = await unitOfWork.Listings.UpdateAsync(
                listingId,
                hostId,
                updatedListing,
                newMainImageUrl: null,
                newAdditionalImages: newImageUrls,
                imagesToRemove: null,         // already hard deleted
                keywordNames: vm.Amenities,
                ct: ct
            );

            if (!ok)
                return new Response<ListingUpdateVM>(null, "Update failed", true);

            // 6. Return updated data to frontend
            var finalListing = await unitOfWork.Listings.GetListingByIdAsync(listingId, ct);
            var vmOut = mapper.Map<ListingUpdateVM>(finalListing);

            return new Response<ListingUpdateVM>(vmOut, null, false);
        }
        catch (Exception ex)
        {
            return new Response<ListingUpdateVM>(null, ex.Message, true);
        }
    }

    // Soft delete listing
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

    // Approve listing
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

    // Reject listing
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

    // Promote listing
    public async Task<Response<bool>> PromoteAsync(int id, DateTime promotionEndDate, Guid performedByUserId, CancellationToken ct = default)
    {
        var performer = await userManager.FindByIdAsync(performedByUserId.ToString());
        if (performer == null) return new Response<bool>(false, "Performer not found", true);

        var isAdmin = await userManager.IsInRoleAsync(performer, "Admin");
        if (!isAdmin)
        {
            var isOwner = await unitOfWork.Listings.IsOwnerAsync(id, performedByUserId, ct);
            if (!isOwner) return new Response<bool>(false, "Not admin or owner", true);
        }

        try
        {
            var ok = await unitOfWork.Listings.PromoteAsync(id, promotionEndDate, performedByUserId, ct);
            return new Response<bool>(ok, ok ? null : "Promote failed", !ok);
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
}
