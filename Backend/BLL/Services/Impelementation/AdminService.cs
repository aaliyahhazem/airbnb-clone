using BLL.ModelVM.Admin;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Impelementation
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepo;
        private readonly IUnitOfWork _uow;

        public AdminService(IAdminRepository adminRepo, IUnitOfWork uow)
        {
            _adminRepo = adminRepo;
            _uow = uow;
        }

        public async Task<Response<List<UserSummaryVM>>> GetAllUsersAsync()
        {
            try
            {
                var users = await _adminRepo.GetAllUsersAsync();
                var mapped = users.Select(u => new UserSummaryVM { Id = u.Id, Email = u.Email, FullName = u.FullName, Role = u.Role.ToString(), IsActive = u.IsActive }).ToList();
                return Response<List<UserSummaryVM>>.SuccessResponse(mapped);
            }
            catch (Exception ex)
            {
                return Response<List<UserSummaryVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<UserSummaryVM>> GetUserByIdAsync(Guid id)
        {
            try
            {
                var u = await _adminRepo.GetUserByIdAsync(id);
                if (u == null) return Response<UserSummaryVM>.FailResponse("User not found");
                var vm = new UserSummaryVM { Id = u.Id, Email = u.Email, FullName = u.FullName, Role = u.Role.ToString(), IsActive = u.IsActive };
                return Response<UserSummaryVM>.SuccessResponse(vm);
            }
            catch (Exception ex)
            {
                return Response<UserSummaryVM>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<bool>> DeactivateUserAsync(Guid id)
        {
            try
            {
                await _adminRepo.GetUserByIdAsync(id);
                await _uow.SaveChangesAsync();
                return Response<bool>.SuccessResponse(true);
            }
            catch (Exception ex)
            {
                return Response<bool>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<SystemStatsVM>> GetSystemStatsAsync()
        {
            try
            {
                var stats = new SystemStatsVM
                {
                    TotalUsers = await _adminRepo.CountUsersAsync(),
                    TotalListings = await _adminRepo.CountListingsAsync(),
                    TotalBookings = await _adminRepo.CountBookingsAsync()
                };
                return Response<SystemStatsVM>.SuccessResponse(stats);
            }
            catch (Exception ex)
            {
                return Response<SystemStatsVM>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<Listing>>> GetAllListingsAsync()
        {
            try
            {
                var listings = await _adminRepo.GetAllListingsAsync();
                return Response<List<Listing>>.SuccessResponse(listings.ToList());
            }
            catch (Exception ex)
            {
                return Response<List<Listing>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<Booking>>> GetAllBookingsAsync()
        {
            try
            {
                var bookings = await _adminRepo.GetAllBookingsAsync();
                return Response<List<Booking>>.SuccessResponse(bookings.ToList());
            }
            catch (Exception ex)
            {
                return Response<List<Booking>>.FailResponse(ex.Message);
            }
        }

        // ------------------ New implementations --------------------

        public async Task<Response<List<UserAdminVM>>> GetUsersFilteredAsync(string? search = null, string? role = null, bool? isActive = null, int page = 1, int pageSize = 50)
        {
            try
            {
                var users = (await _uow.Users.GetAllAsync()).AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    users = users.Where(u => u.FullName.ToLower().Contains(s) || u.Email.ToLower().Contains(s) || u.UserName.ToLower().Contains(s));
                }

                if (!string.IsNullOrWhiteSpace(role))
                {
                    users = users.Where(u => u.Role.ToString().ToLower() == role.Trim().ToLower());
                }

                if (isActive.HasValue)
                {
                    users = users.Where(u => u.IsActive == isActive.Value);
                }

                var allUsers = users.ToList();
                // fetch bookings and listings to compute counts
                var bookings = await _uow.Bookings.GetAllAsync();
                var listings = await _uow.Listings.GetAllAsync();

                var result = allUsers
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserAdminVM
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FullName = u.FullName,
                        Role = u.Role.ToString(),
                        IsActive = u.IsActive,
                        TotalBookings = bookings.Count(b => b.GuestId == u.Id),
                        TotalListings = listings.Count(l => l.UserId == u.Id)
                    }).ToList();

                return Response<List<UserAdminVM>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<List<UserAdminVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<ListingAdminVM>>> GetListingsFilteredAsync(string? status = null, int page = 1, int pageSize = 50)
        {
            try
            {
                var listings = (await _uow.Listings.GetAllAsync()).AsQueryable();

                if (!string.IsNullOrWhiteSpace(status))
                {
                    switch (status.Trim().ToLower())
                    {
                        case "approved":
                            listings = listings.Where(l => l.IsApproved);
                            break;
                        case "pending":
                            listings = listings.Where(l => !l.IsReviewed);
                            break;
                        case "rejected":
                            listings = listings.Where(l => l.IsReviewed && !l.IsApproved);
                            break;
                        case "promoted":
                            listings = listings.Where(l => l.IsPromoted);
                            break;
                    }
                }

                var allListings = listings.ToList();
                var users = await _uow.Users.GetAllAsync();
                var reviews = await _uow.Reviews.GetAllAsync();

                var result = allListings
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new ListingAdminVM
                    {
                        Id = l.Id,
                        Title = l.Title,
                        Location = l.Location,
                        PricePerNight = l.PricePerNight,
                        IsApproved = l.IsApproved,
                        IsReviewed = l.IsReviewed,
                        IsPromoted = l.IsPromoted,
                        PromotionEndDate = l.PromotionEndDate,
                        HostId = l.UserId,
                        HostName = users.FirstOrDefault(u => u.Id == l.UserId)?.FullName ?? l.UserId.ToString(),
                        ReviewsCount = reviews.Count(r => r.Booking.ListingId == l.Id)
                    }).ToList();

                return Response<List<ListingAdminVM>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<List<ListingAdminVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<ListingAdminVM>>> GetListingsPendingApprovalAsync()
        {
            try
            {
                var listings = (await _uow.Listings.GetAllAsync()).Where(l => !l.IsReviewed).ToList();
                var users = await _uow.Users.GetAllAsync();

                var result = listings.Select(l => new ListingAdminVM
                {
                    Id = l.Id,
                    Title = l.Title,
                    Location = l.Location,
                    PricePerNight = l.PricePerNight,
                    IsApproved = l.IsApproved,
                    IsReviewed = l.IsReviewed,
                    IsPromoted = l.IsPromoted,
                    PromotionEndDate = l.PromotionEndDate,
                    HostId = l.UserId,
                    HostName = users.FirstOrDefault(u => u.Id == l.UserId)?.FullName ?? l.UserId.ToString(),
                    ReviewsCount = 0
                }).ToList();

                return Response<List<ListingAdminVM>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<List<ListingAdminVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<BookingAdminVM>>> GetBookingsDetailedAsync(int page = 1, int pageSize = 100)
        {
            try
            {
                var bookings = (await _uow.Bookings.GetAllAsync()).OrderByDescending(b => b.CreatedAt).ToList();
                var listings = await _uow.Listings.GetAllAsync();
                var users = await _uow.Users.GetAllAsync();
                var payments = await _uow.Payments.GetAllAsync();

                var items = bookings
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b =>
                    {
                        var listing = listings.FirstOrDefault(l => l.Id == b.ListingId);
                        var guest = users.FirstOrDefault(u => u.Id == b.GuestId);
                        var host = listing != null ? users.FirstOrDefault(u => u.Id == listing.UserId) : null;
                        var payment = payments.FirstOrDefault(p => p.BookingId == b.Id);

                        return new BookingAdminVM
                        {
                            Id = b.Id,
                            ListingId = b.ListingId,
                            ListingTitle = listing?.Title ?? b.ListingId.ToString(),
                            GuestId = b.GuestId,
                            GuestName = guest?.FullName ?? b.GuestId.ToString(),
                            HostId = listing?.UserId ?? Guid.Empty,
                            HostName = host?.FullName ?? listing?.UserId.ToString() ?? string.Empty,
                            CheckInDate = b.CheckInDate,
                            CheckOutDate = b.CheckOutDate,
                            TotalPrice = b.TotalPrice,
                            BookingStatus = b.BookingStatus.ToString(),
                            PaymentStatus = b.PaymentStatus.ToString(),
                            PaymentId = payment?.Id,
                            PaymentAmount = payment?.Amount,
                            PaidAt = payment?.PaidAt
                        };
                    }).ToList();

                return Response<List<BookingAdminVM>>.SuccessResponse(items);
            }
            catch (Exception ex)
            {
                return Response<List<BookingAdminVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<RevenuePointVM>>> GetRevenueTrendAsync(int months = 12)
        {
            try
            {
                months = Math.Max(1, months);
                var payments = (await _uow.Payments.GetAllAsync()).Where(p => p.PaidAt != default).ToList();

                var from = DateTime.UtcNow.AddMonths(-months + 1);
                var filtered = payments.Where(p => p.PaidAt >= from);

                var grouped = filtered
                    .GroupBy(p => new { p.PaidAt.Year, p.PaidAt.Month })
                    .Select(g => new RevenuePointVM
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Total = g.Sum(x => x.Amount)
                    }).ToList();

                // Ensure all months appear (fill zeros)
                var list = new List<RevenuePointVM>();
                for (var i = months - 1; i >= 0; i--)
                {
                    var dt = DateTime.UtcNow.AddMonths(-i);
                    var gp = grouped.FirstOrDefault(g => g.Year == dt.Year && g.Month == dt.Month);
                    list.Add(new RevenuePointVM { Year = dt.Year, Month = dt.Month, Total = gp?.Total ?? 0m });
                }

                return Response<List<RevenuePointVM>>.SuccessResponse(list);
            }
            catch (Exception ex)
            {
                return Response<List<RevenuePointVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<PromotionSummaryVM>>> GetActivePromotionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var listings = (await _uow.Listings.GetAllAsync()).Where(l => l.IsPromoted && (!l.PromotionEndDate.HasValue || l.PromotionEndDate > now)).ToList();
                var users = await _uow.Users.GetAllAsync();

                var result = listings.Select(l => new PromotionSummaryVM
                {
                    ListingId = l.Id,
                    Title = l.Title,
                    HostId = l.UserId,
                    HostName = users.FirstOrDefault(u => u.Id == l.UserId)?.FullName ?? l.UserId.ToString(),
                    PromotionEndDate = l.PromotionEndDate,
                    IsActive = true
                }).ToList();

                return Response<List<PromotionSummaryVM>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<List<PromotionSummaryVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<PromotionSummaryVM>>> GetPromotionsHistoryAsync(DateTime? from = null, DateTime? to = null)
        {
            try
            {
                var listings = (await _uow.Listings.GetAllAsync()).Where(l => l.PromotionEndDate.HasValue).ToList();
                var users = await _uow.Users.GetAllAsync();

                if (from.HasValue) listings = listings.Where(l => l.PromotionEndDate >= from.Value).ToList();
                if (to.HasValue) listings = listings.Where(l => l.PromotionEndDate <= to.Value).ToList();

                var result = listings.Select(l => new PromotionSummaryVM
                {
                    ListingId = l.Id,
                    Title = l.Title,
                    HostId = l.UserId,
                    HostName = users.FirstOrDefault(u => u.Id == l.UserId)?.FullName ?? l.UserId.ToString(),
                    PromotionEndDate = l.PromotionEndDate,
                    IsActive = l.IsPromoted && l.PromotionEndDate > DateTime.UtcNow
                }).ToList();

                return Response<List<PromotionSummaryVM>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<List<PromotionSummaryVM>>.FailResponse(ex.Message);
            }
        }

        public async Task<Response<List<PromotionSummaryVM>>> GetExpiringPromotionsAsync(int days = 7)
        {
            try
            {
                var now = DateTime.UtcNow;
                var until = now.AddDays(days);
                var listings = (await _uow.Listings.GetAllAsync()).Where(l => l.IsPromoted && l.PromotionEndDate.HasValue && l.PromotionEndDate >= now && l.PromotionEndDate <= until).ToList();
                var users = await _uow.Users.GetAllAsync();

                var result = listings.Select(l => new PromotionSummaryVM
                {
                    ListingId = l.Id,
                    Title = l.Title,
                    HostId = l.UserId,
                    HostName = users.FirstOrDefault(u => u.Id == l.UserId)?.FullName ?? l.UserId.ToString(),
                    PromotionEndDate = l.PromotionEndDate,
                    IsActive = true
                }).ToList();

                return Response<List<PromotionSummaryVM>>.SuccessResponse(result);
            }
            catch (Exception ex)
            {
                return Response<List<PromotionSummaryVM>>.FailResponse(ex.Message);
            }
        }
    }
}
