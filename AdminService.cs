using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JWTRefreshTokenInDotNet6.Hubs;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JWTRefreshTokenInDotNet6.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        private readonly ApplicationDbContext _context;

        public AdminService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext
        )
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task<List<UserDto>> GetAllVendorsAsync()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("Vendor");

            return usersInRole
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    AutoPublish = u.AutoPublish,
                    CanEdit = u.CanEdit,
                    CanDelete = u.CanDelete,
                })
                .ToList();
        }

        public async Task<List<UserDto>> GetAllCustomersAsync()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("Customer");

            return usersInRole
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                })
                .ToList();
        }

        public async Task<IdentityResult> SetVendorAutoPublishPermissionAsync(
            string userId,
            bool canAutoPublish
        )
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            user.AutoPublish = canAutoPublish;

            var result = await _userManager.UpdateAsync(user);
            return result;
        }

        public async Task<IdentityResult> SetVendorCanEditAsync(string userId, bool canEdit)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }
            user.CanEdit = canEdit;

            var result = await _userManager.UpdateAsync(user);
            return result;
        }

        public async Task<string> ReviewProductAsync(int requestId, bool approve)
        {
            var request = await _context
                .AddProductRequests.Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                return "NotFound";

            request.status = approve ? Status.Approved : Status.Rejected;

            var product = request.Product;
            product.Isapproved = approve;

            await _context.SaveChangesAsync();

            var vendorId = request.VendorId;

            var notification = new
            {
                Type = "ReviewResult",
                VendorId = vendorId,
                Message = approve
                    ? $"The product '{product.Title}' has been approved."
                    : $"The product '{product.Title}' has been rejected.",
                ProductId = product.Id,
                Approved = approve,
            };

            await _hubContext
                .Clients.Group("User-" + vendorId)
                .SendAsync("ReceiveNotification", notification);

            return approve ? "Product Approved" : "Product Rejected";
        }

        public async Task<string> ReviewVendorAsync(int requestId, bool approve)
        {
            var request = await _context.VendorRegisterRequests.FirstOrDefaultAsync(r =>
                r.Id == requestId
            );

            if (request == null)
                return "NotFound";

            var user = await _userManager.FindByIdAsync(request.VendorId);
            if (user == null)
                return "Vendor user not found.";

            if (approve)
            {
                user.CanRegister = true;
                await _userManager.UpdateAsync(user);
            }
            else
            {
                await _userManager.DeleteAsync(user);
            }

            request.status = approve ? VendorStatus.Approved : VendorStatus.Rejected;

            await _context.SaveChangesAsync();

            var vendorId = request.VendorId;

            var notification = new
            {
                Type = "ReviewResult",
                VendorId = vendorId,
                Message = approve
                    ? $"The vendor '{request.VendorUsername}' has been approved."
                    : $"The vendor '{request.VendorUsername}' has been rejected.",
                Approved = approve,
            };

            await _hubContext
                .Clients.Group("User-" + vendorId)
                .SendAsync("ReceiveNotification", notification);

            return approve ? "Vendor Approved" : "Vendor Rejected";
        }

        //AdminService
        public async Task<bool> SetAllVendorsCanDeleteAsync(bool canDelete)
        {
            // Get the "Vendor" role ID
            var vendorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Vendor");

            if (vendorRole == null)
                return false;

            // Get user IDs that belong to the Vendor role
            var vendorUserIds = await _context
                .UserRoles.Where(ur => ur.RoleId == vendorRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            // Get the actual user entities
            var vendorUsers = await _context
                .Users.Where(u => vendorUserIds.Contains(u.Id))
                .ToListAsync();

            // Update their CanDelete flag
            foreach (var vendor in vendorUsers)
            {
                vendor.CanDelete = canDelete;
            }

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
