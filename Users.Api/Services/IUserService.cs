using System;
using System.Threading.Tasks;
using Users.Api.DTOs;

namespace Users.Api.Services
{
    public interface IUserService
    {
        Task<UserResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<BlockUserResponse> UpdateAsync(Guid id, BlockUserRequest request); 
        Task<UserResponse> GetByIdAsync(Guid id); 
    }
}