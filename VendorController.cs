using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    public class VendorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVendorService _vendorService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public VendorController(
            IVendorService vendorService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext
        )
        {
            _vendorService = vendorService;
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpGet("getallproduct")]
        public async Task<IActionResult> GetAllProducts()
        {
            var vendorid = await GetVendorIdAsync();
            if (vendorid is null)
            {
                return Unauthorized("Vendor is not found");
            }
            List<Product> allproducts = await _vendorService.GetAllProductsAsync(vendorid);
            return Ok(allproducts);
        }

        [HttpGet("products-with-purchases")]
        public async Task<IActionResult> GetVendorProductsWithPurchases()
        {
            var vendorId = await GetVendorIdAsync();
            if (vendorId is null)
                return Unauthorized("Invalid token or uid not found.");

            var result = await _vendorService.GetProductPurchasedAsync(vendorId);
            return Ok(result);
        }

        // [HttpGet("getproductpurchased/{productid}")]
        // public async Task<IActionResult> GetProductPurchased(int productid)
        // {
        //     var vendorid = await GetVendorIdAsync();
        //     if (vendorid is null)
        //         return Unauthorized("Invalid token or uid not found.");

        //     var purchased = await _vendorService.GetProductPurchasedAsync(vendorid, productid);
        //     return Ok(purchased);
        // }

        [HttpDelete("deleteproduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var Vendorid = await GetVendorIdAsync();
            if (Vendorid is null)
            {
                return Unauthorized("Invalid token or uid not found.");
            }
            await _vendorService.DeleteProductAsync(id, Vendorid);
            return NoContent();
        }

        [HttpPut("updateproduct/{id}")]
        public async Task<IActionResult> DynamicUpdate(int id, [FromBody] ProductDto updates)
        {
            var vendorId = await GetVendorIdAsync();
            if (vendorId is null)
                return Unauthorized("Invalid or missing token.");
            var product = await _vendorService.UpdateProductAsync(id, updates, vendorId);
            if (product is null)
            {
                return Ok("not found");
            }
            return Ok(product);
        }

        // [HttpPost("addproduct")]
        // public async Task<IActionResult> AddNewProduct([FromBody] ProductDto dto)
        // {
        //     var Vendorid = await GetVendorIdAsync();
        //     if (Vendorid is null)
        //     {
        //         return Unauthorized("Invalid token or uid not found.");
        //     }

        //     var user = await _userManager.FindByIdAsync(Vendorid);

        //     var product = new Product
        //     {
        //         Title = dto.Title,
        //         Description = dto.Description,
        //         Price = dto.Price,
        //         NumOfUnits = dto.NumOfUnits,
        //         ImageUrl = dto.ImageUrl,
        //         Category = dto.Category,
        //         VendorId = Vendorid,
        //         VendorName = user.FirstName + " " + user.LastName,
        //         Isapproved = false,
        //         NumOfViews = 0,
        //     };

        //     await _context.Products.AddAsync(product);
        //     await _context.SaveChangesAsync();

        //     var addProductRequest = new AddProductRequest
        //     {
        //         VendorId = Vendorid,
        //         ProductId = product.Id,
        //         status = Status.Pending,
        //     };

        //     await _context.AddProductRequests.AddAsync(addProductRequest);
        //     await _context.SaveChangesAsync();
        //     await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
        //     {
        //         Type = "NewProductRequest",
        //         Message = $"Vendor {user.UserName} requested to add a new product: {product.Title}",
        //         ProductId = product.Id,
        //         VendorId = Vendorid
        //     });

        //     return Ok(new { message = "Product added successfully.", productId = product.Id });
        // }

        [HttpPost("addproduct")]
        public async Task<IActionResult> AddNewProduct([FromBody] ProductDto dto)
        {
            var Vendorid = await GetVendorIdAsync();
            if (Vendorid is null)
            {
                return Unauthorized("Invalid token or uid not found.");
            }

            var user = await _userManager.FindByIdAsync(Vendorid);

            var isAutoApproved = user.AutoPublish;

            var product = new Product
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                NumOfUnits = dto.NumOfUnits,
                ImageUrl = dto.ImageUrl,
                Category = dto.Category,
                VendorId = Vendorid,
                VendorName = user.FirstName + " " + user.LastName,
                Isapproved = isAutoApproved, // <-- لو AutoPublish = true
                NumOfViews = 0,
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            if (!isAutoApproved)
            {
                var addProductRequest = new AddProductRequest
                {
                    VendorId = Vendorid,
                    ProductId = product.Id,
                    status = Status.Pending,
                };

                await _context.AddProductRequests.AddAsync(addProductRequest);
                await _context.SaveChangesAsync();

                await _hubContext
                    .Clients.Group("Admins")
                    .SendAsync(
                        "ReceiveNotification",
                        new
                        {
                            Type = "NewProductRequest",
                            Message = $"Vendor {user.UserName} requested to add a new product: {product.Title}",
                            ProductId = product.Id,
                            VendorId = Vendorid,
                        }
                    );
            }

            return Ok(new { message = "Product added successfully.", productId = product.Id });
        }

        [HttpGet("vendor-status/{vendorId}")]
        public async Task<IActionResult> GetVendorStatus(string vendorId)
        {
            var vendorRequest = await _context.VendorRegisterRequests.FirstOrDefaultAsync(v =>
                v.VendorId == vendorId
            );

            if (vendorRequest == null)
            {
                return NotFound("Vendor not found");
            }

            return Ok(new { status = vendorRequest.status.ToString() });
        }

        private async Task<string> GetVendorIdAsync()
        {
            var headerValue = Request.Headers["Authorization"].ToString();

            if (
                string.IsNullOrEmpty(headerValue)
                || !headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            )
                return null;

            var token = headerValue.Substring("Bearer ".Length).Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                    return null;

                var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

                var uid = jwtToken?.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;

                if (!string.IsNullOrEmpty(uid))
                {
                    var userExists = await _context.Users.AnyAsync(u => u.Id == uid);
                    return userExists ? uid : null;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token parsing failed: {ex.Message}");
                return null;
            }
        }
    }
}

// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using JWTRefreshTokenInDotNet6.Models;
// using JWTRefreshTokenInDotNet6.Services;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.AspNetCore.SignalR;
// using Microsoft.AspNetCore.Authorization;
// using JWTRefreshTokenInDotNet6.Hubs;

// namespace JWTRefreshTokenInDotNet6.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class VendorController : ControllerBase
//     {

//         private readonly ApplicationDbContext _context;
//         private readonly IVendorService _vendorService;
//         private readonly UserManager<ApplicationUser> _userManager;
//         private readonly IHubContext<NotificationHub> _hubContext;

//         public VendorController(IVendorService vendorService, ApplicationDbContext context, UserManager<ApplicationUser> userManager,IHubContext<NotificationHub> hubContext)
//         {

//             _vendorService = vendorService;
//             _context = context;
//             _userManager = userManager;
//             _hubContext = hubContext;
//         }

//         [Authorize(Roles = "Vendor")]
//         [HttpGet("getallproduct")]
//         public async Task<IActionResult> GetAllProducts()
//         {
//             var vendorid = await GetVendorIdAsync();
//             if (vendorid is null)
//             {
//                 return Unauthorized("Vendor is not found");
//             }
//             List<Product> allproducts = await _vendorService.GetAllProductsAsync(vendorid);
//             return Ok(allproducts);
//         }

//         [HttpGet("getproductpurchased/{productid}")]
//         public async Task<IActionResult> GetProductPurchased(int productid)
//         {
//             var vendorid = await GetVendorIdAsync();
//             if (vendorid is null)
//                 return Unauthorized("Invalid token or uid not found.");

//             var purchased = await _vendorService.GetProductPurchasedAsync(vendorid, productid);
//             return Ok(purchased);
//         }
//         [Authorize(Roles = "Vendor")]
//         [HttpDelete("deleteproduct/{id}")]
//         public async Task<IActionResult> DeleteProduct(int id)
//         {
//             var Vendorid = await GetVendorIdAsync();
//             if (Vendorid is null)
//             {
//                 return Unauthorized("Invalid token or uid not found.");
//             }
//             await _vendorService.DeleteProductAsync(id);
//             return NoContent();
//         }
//         [Authorize(Roles = "Vendor")]

//         [HttpPut("updateproduct/{id}")]
//         public async Task<IActionResult> DynamicUpdate(int id, [FromBody] Dictionary<string, string> updates)
//         {
//             var vendorId = await GetVendorIdAsync();
//             if (vendorId is null)
//                 return Unauthorized("Invalid or missing token.");
//             var product = await _vendorService.UpdateProductAsync(id, updates);
//             if (product is null) { return Ok("not found"); }
//             return Ok(product);
//         }

//         [HttpPost("addproduct")]
//         public async Task<IActionResult> AddNewProduct([FromBody] ProductDto dto)
//         {
//             var Vendorid = await GetVendorIdAsync();
//             if (Vendorid is null)
//             {
//                 return Unauthorized("Invalid token or uid not found.");
//             }
//             var user = await _userManager.FindByIdAsync(Vendorid);

//             var product = new Product
//             {
//                 Title = dto.Title,
//                 Description = dto.Description,
//                 Price = dto.Price,
//                 NumOfUnits = dto.NumOfUnits,
//                 ImageUrl = dto.ImageUrl,
//                 Category = dto.Category,
//                 VendorId = Vendorid,
//                 VendorName = user.UserName,
//                 Isapproved = false,
//                 NumOfViews = 0,
//                 CustomersWhoPurchase = new List<string>()
//             };
//             await _context.Products.AddAsync(product);
//             await _context.SaveChangesAsync();
//             var addProductRequest = new AddProductRequest
//             {
//                 VendorId = Vendorid,
//                 ProductId = product.Id,
//                 status = Status.Pending,

//             };
//             await _context.AddProductRequests.AddAsync(addProductRequest);
//             await _context.SaveChangesAsync();
//             _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", Vendorid, $"New product added: {product.Title}");
//             return Ok(new { requestId = addProductRequest.Id });

//         }

//         private async Task<string> GetVendorIdAsync()
//         {
//             var headerValue = Request.Headers["Authorization"].ToString();

//             if (string.IsNullOrEmpty(headerValue) || !headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
//                 return null;

//             var token = headerValue.Substring("Bearer ".Length).Trim();

//             try
//             {
//                 var handler = new JwtSecurityTokenHandler();

//                 if (!handler.CanReadToken(token))
//                     return null;

//                 var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

//                 var uid = jwtToken?.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;

//                 if (!string.IsNullOrEmpty(uid))
//                 {
//                     var userExists = await _context.Users.AnyAsync(u => u.Id == uid);
//                     return userExists ? uid : null;
//                 }

//                 return null;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Token parsing failed: {ex.Message}");
//                 return null;
//             }
//         }

//     }

// }
