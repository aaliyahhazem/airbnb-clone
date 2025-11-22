namespace DAL.Repo.Implementation
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email!.ToLower() == email.ToLower());
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserName!.ToLower() == userName.ToLower());
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.DateCreated)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role)
        {
            return await _context.Users
                .Where(u => u.Role == role)
                .ToListAsync();
        }

        public async Task DeactivateUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.SetActive(false);
                await _context.SaveChangesAsync();
            }
        }
    }
}