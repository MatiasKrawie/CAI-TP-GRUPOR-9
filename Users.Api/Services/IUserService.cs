using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Users.Api.DTOs;

namespace Users.Api.Services
{
    public interface IUserService
    {
        Task<UserResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<BlockUserResponse> UpdateAsync(int id, BlockUserRequest request);

        Task<UserResponse> GetByIdAsync(int id);
    }
}