
namespace BLL.Services.Impelementation
{
    class ListingService /*: IListingService*/
    {
        private readonly IUnitOfWork unitOfWork;

        public ListingService(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        //public async Task<Response<int>> CreateListingAsync(CreateListingVM vm, Guid hostUserId, string folder = "ListingsImages")
        //{
        //    try
        //    {
        //        if (vm == null)
        //            return new Response<int>(0, "Request is null.", false);

        //        if (vm.Images == null || !vm.Images.Any())
        //            return new Response<int>(0, "At least one image is required.", false);

        //        // 1) Create domain listing
        //        var listing = Listing.Create(
        //            title: vm.Title,
        //            description: vm.Description,
        //            pricePerNight: vm.PricePerNight,
        //            location: vm.Location,
        //            latitude: vm.Latitude,
        //            longitude: vm.Longitude,
        //            maxGuests: vm.MaxGuests,
        //            tags: vm.Tags ?? new List<string>(),
        //            userId: hostUserId,
        //            createdBy: "placeholder",     // repository will replace with full host name
        //            isPromoted: vm.IsPromoted,
        //            promotionEndDate: vm.PromotionEndDate
        //        );

        //        // 2) Save listing in DB (repo returns new ID)
        //        var listingId = await unitOfWork.Listings.CreateListingAsync(listing, hostUserId);

        //        if (listingId == 0)
        //            return new Response<int>(0, "Failed to create listing.", false);

        //        // 3) Upload images immediately using your Upload helper
        //        List<string> uploadedImageNames = new();

        //        foreach (var img in vm.Images)
        //        {
        //            var fileName = Upload.UploadFile(folder, img);

        //            // In case upload failed, the helper returns an error message -> handle it
        //            if (fileName.Contains("Exception") || fileName.Contains("Error", StringComparison.OrdinalIgnoreCase))
        //            {
        //                return new Response<int>(0, "Image upload failed: " + fileName, false);
        //            }

        //            uploadedImageNames.Add(fileName);
        //        }

        //        // 4) Add DB records for each uploaded image
        //        var createdListing = await unitOfWork.Listings.GetByIdAsync(listingId, true);
        //        var createdBy = createdListing?.CreatedBy ?? "unknown";

        //        await unitOfWork.ListingImages.AddImagesAsync(listingId, uploadedImageNames, createdBy);

        //        return new Response<int>(listingId, "Listing created successfully.", true);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while creating listing.");
        //        return new Response<int>(0, ex.Message, false);
        //    }
        //}

    }
}
