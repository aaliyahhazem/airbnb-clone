namespace DAL.Repo.Abstraction
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);                          // Get user by email
        Task<User?> GetByIdAsync(Guid id);                                  // Get user by GUID id
        Task<IEnumerable<User>> GetActiveUsersAsync();                      // Get all active users
        Task<IEnumerable<User>> GetByRoleAsync(UserRole role);              // Get users by role
        Task DeactivateUserAsync(Guid userId);                              // Deactivate user

    }
}
