using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JWTRefreshTokenInDotNet6.Helpers;
using JWTRefreshTokenInDotNet6.Hubs;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JWTRefreshTokenInDotNet6.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EncryptionHelper _encryptionHelper;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminController(
            IAdminService adminService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            EncryptionHelper encryptionHelper,
            IConfiguration config,
            IHubContext<NotificationHub> hubContext
        )
        {
            _adminService = adminService;
            _context = context;
            _userManager = userManager;
            _encryptionHelper = encryptionHelper;
            _encryptionHelper.Initialize(config);
        }

        [HttpGet("vendorinfo")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<UserDto>>> GetAllVendorsAsync()
        {
            var vendors = await _adminService.GetAllVendorsAsync();
            return Ok(vendors);
        }

        [HttpGet("customerinfo")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<UserDto>>> GetAllCustomersAsync()
        {
            var customers = await _adminService.GetAllCustomersAsync();
            return Ok(customers);
        }

        [HttpPost("set-autopublish/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetAutoPublishPermission(
            string userId,
            [FromBody] bool AutoPublish
        )
        {
            var result = await _adminService.SetVendorAutoPublishPermissionAsync(
                userId,
                AutoPublish
            );

            if (result.Succeeded)
            {
                return Ok(new { message = "AutoPublish permission updated successfully" });
            }

            return BadRequest(new { message = "Failed to update AutoPublish permission" });
        }

        [HttpPost("set-canedit/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetVendorCanEdit(string userId, [FromBody] bool CanEdit)
        {
            var result = await _adminService.SetVendorCanEditAsync(userId, CanEdit);

            if (result.Succeeded)
            {
                return Ok(new { message = "CanEdit permission updated successfully" });
            }

            return BadRequest(new { message = "Failed to update CanEdit permission" });
        }

        [HttpPut("ReviewProduct/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewProduct(int requestId, [FromQuery] bool approve)
        {
            try
            {
                var result = await _adminService.ReviewProductAsync(requestId, approve);

                if (result == "NotFound")
                    return NotFound("Product request not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("pending-product-requests")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingProductRequests()
        {
            var requests = await _context
                .AddProductRequests.Include(r => r.Product)
                .Where(r => r.status == Status.Pending)
                .Select(r => new
                {
                    RequestId = r.Id,
                    ProductTitle = r.Product.Title,
                    Description = r.Product.Description,
                    Price = r.Product.Price,
                    VendorId = r.VendorId,
                    ProductId = r.Product.Id,
                    VendorName = _context
                        .Users.Where(u => u.Id == r.VendorId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault(),
                    ImageUrl = r.Product.ImageUrl,
                    Category = r.Product.Category,
                    NumOfUnits = r.Product.NumOfUnits,
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPut("ReviewVendorRequest/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewVendorRequest(
            int requestId,
            [FromQuery] bool approve
        )
        {
            try
            {
                var result = await _adminService.ReviewVendorAsync(requestId, approve);

                if (result == "NotFound")
                    return NotFound("Vendor request or user not found.");

                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error.");
            }
        }

        // [HttpPut("ReviewVendorRequest/{requestId}")]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> ReviewVendorRequest(
        //     int requestId,
        //     [FromQuery] bool approve
        // )
        // {
        //     try
        //     {
        //         var request = await _context.VendorRegisterRequests.FindAsync(requestId);
        //         if (request == null)
        //             return NotFound("Vendor request not found.");

        //         var user = await _userManager.FindByIdAsync(request.VendorId);
        //         if (user == null)
        //             return NotFound("Vendor user not found.");

        //         if (approve)
        //         {
        //             request.status = VendorStatus.Approved;
        //             user.CanRegister = true;
        //             await _userManager.UpdateAsync(user);
        //         }
        //         else
        //         {
        //             request.status = VendorStatus.Rejected;
        //             // _context.VendorRegisterRequests.Remove(request);
        //             await _userManager.DeleteAsync(user);
        //         }

        //         await _context.SaveChangesAsync();
        //         var vendorId = request.VendorId;

        //         var notification = new
        //         {
        //             Type = "ReviewResult",
        //             VendorId = vendorId,
        //             Message = approve
        //                 ? $"The Vendor '{request.VendorUsername}' has been approved."
        //                 : $"The Vendor '{request.VendorUsername}' has been rejected.",
        //             Approved = approve,
        //         };

        //         await _hubContext
        //             .Clients.Group("User-" + vendorId)
        //             .SendAsync("ReceiveNotification", notification);

        //         return Ok(new { message = "Vendor request reviewed successfully." });
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine(ex.Message);
        //         return StatusCode(500, "Internal server error.");
        //     }
        // }

        [HttpGet("pending-vendor-requests")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingVendorRequests()
        {
            var requests = await _context
                .VendorRegisterRequests.Where(r => r.status == VendorStatus.Pending)
                .Select(r => new
                {
                    RequestId = r.Id,
                    VendorUsername = r.VendorUsername,
                    VendorEmail = r.VendorEmail,
                    VendorId = r.VendorId,
                })
                .ToListAsync();

            return Ok(requests);
        }

        //AdminController
        [HttpPut("set-can-delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetAllVendorsCanDelete([FromQuery] bool canDelete)
        {
            var result = await _adminService.SetAllVendorsCanDeleteAsync(canDelete);

            if (result)
            {
                return Ok(new { message = "CanDelete permission updated successfully" });
            }

            return BadRequest(new { message = "Failed to update CanDelete permission" });
        }
    }
}
