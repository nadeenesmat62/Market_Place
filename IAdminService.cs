using JWTRefreshTokenInDotNet6.Controllers;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Services;
using Microsoft.AspNetCore.Identity;

namespace JWTRefreshTokenInDotNet6.Services
{
    public interface IAdminService
    {
        Task<List<UserDto>> GetAllVendorsAsync();
        Task<List<UserDto>> GetAllCustomersAsync();
        Task<IdentityResult> SetVendorAutoPublishPermissionAsync(
            string userId,
            bool canAutoPublish
        );
        Task<IdentityResult> SetVendorCanEditAsync(string userId, bool canEdit);
        Task<string> ReviewProductAsync(int requestId, bool approve);
        Task<string> ReviewVendorAsync(int requestId, bool approve);
        Task<bool> SetAllVendorsCanDeleteAsync(bool canDelete);
    }
}
