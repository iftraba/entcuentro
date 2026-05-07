using PlantillaDotNet.Shared.DTOs;

namespace PlantillaDotNet.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<UserDto> UpdateAsync(string id, UpdateUserRequest request);
    Task DeleteAsync(string id);
    Task AssignRolAsync(string userId, string rol);
    Task RemoveRolAsync(string userId, string rol);
}
