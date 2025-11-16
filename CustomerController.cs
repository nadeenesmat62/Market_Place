using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
// using JWTRefreshTokenInDotNet6.Hubs;
using JWTRefreshTokenInDotNet6.Helpers;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Services;
using Microsoft.AspNetCore.Authorization;
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
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EncryptionHelper _encryptionHelper;

        public CustomerController(
            ICustomerService customerService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            EncryptionHelper encryptionHelper,
            IConfiguration config
        )
        {
            _customerService = customerService;
            _context = context;
            _userManager = userManager;
            _encryptionHelper = encryptionHelper;
            _encryptionHelper.Initialize(config);
        }

        [HttpGet("customerInfo/{customerId}")]
        public async Task<IActionResult> GetCustomerById(string customerId)
        {
            var customer = await _customerService.GetCustomerByIdAsync(customerId);

            if (customer == null)
                return NotFound("Customer not found or not in Customer role.");

            return Ok(customer);
        }

        [HttpGet("Products")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetApprovedProducts()
        {
            try
            {
                var products = await _customerService.ApprovedProducts();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("AddToFavorite/{productId}")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToFavorite(int productId)
        {
            var customerId = await GetCustomerIdAsync();
            if (customerId is null)
            {
                return Unauthorized("Invalid token or uid not found.");
            }

            try
            {
                var AddFav = await _customerService.AddToFavorite(customerId, productId);
                return Ok(AddFav);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("DeleteFromFavorite/{productId}")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RemoveFromFavorite(int productId)
        {
            var customerId = await GetCustomerIdAsync();
            if (customerId is null)
            {
                return Unauthorized("Invalid token or uid not found.");
            }

            try
            {
                var RemoveFav = await _customerService.RemoveFromFavorite(customerId, productId);
                return Ok(RemoveFav);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddToCart/{productId}")]
        public async Task<IActionResult> AddProductToCart(int productId)
        {
            var customerId = await GetCustomerIdAsync();

            if (customerId is null)
            {
                return Unauthorized("Invalid token or uid not found.");
            }

            var result = await _customerService.AddProductToCartAsync(customerId, productId, 1);

            if (result)
            {
                return Ok(new { message = "Product added to cart successfully." });
            }
            else
            {
                return BadRequest("Product could not be added to the cart.");
            }
        }

        [HttpPut("updateQuantity/{productId}/{quantity}")]
        public async Task<IActionResult> UpdateCartItemQuantity(int productId, int quantity)
        {
            var customerId = await GetCustomerIdAsync();

            var result = await _customerService.UpdateCartItemQuantityAsync(
                customerId,
                productId,
                quantity
            );

            if (result)
            {
                return Ok(new { message = "Cart item updated successfully." });
            }
            else
            {
                return BadRequest("Failed to update cart item.");
            }
        }

        [HttpGet("allCartProduct")]
        public async Task<IActionResult> GetCartDetails()
        {
            var customerId = await GetCustomerIdAsync();
            var cart = await _customerService.GetCartDetailsAsync(customerId);

            if (cart == null)
            {
                return NotFound();
            }

            var result = new
            {
                cart.Id,
                Items = cart
                    .CartItems.Select(item => new
                    {
                        item.ProductId,
                        item.Product.Title,
                        item.Product.Price,
                        item.Quantity,
                    })
                    .ToList(),
            };

            return Ok(result);
        }

        [HttpDelete("RemoveProductFromCart/{productId}")]
        public async Task<IActionResult> RemoveProductFromCart(int productId)
        {
            var customerId = await GetCustomerIdAsync();

            if (customerId is null)
            {
                return Unauthorized("Invalid token or uid not found.");
            }

            var result = await _customerService.RemoveProductFromCartAsync(customerId, productId);

            if (result)
            {
                return Ok(new { message = "Product deleted to cart successfully." });
            }
            else
            {
                return BadRequest("Product could not be deleted to the cart.");
            }
        }

        // [HttpPost("AddToCart/{productId}/{quantity}")]
        // // [Authorize(Roles = "Customer")]

        // public async Task<IActionResult> AddToCart(int productId, int quantity)
        // {
        //     var customerId = await GetCustomerIdAsync();
        //     var AddCart = await _customerService.AddToCart(customerId, productId, quantity);
        //     if (!AddCart)
        //         return BadRequest("Failed to add product to cart");
        //     return Ok("Product added to cart");
        // }

        // [HttpDelete("RemoveFromCart/{productId}")]
        // // [Authorize(Roles = "Customer")]
        // public async Task<IActionResult> RemoveFromCart(int productId)
        // {
        //     var customerId = await GetCustomerIdAsync();
        //     var RemoveCart = await _customerService.RemoveFromCart(customerId, productId);
        //     if (!RemoveCart)
        //         return NotFound("Product not found in cart");
        //     return Ok("Product removed from cart");
        // }

        [HttpPost("payment")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ProcessPayment()
        {
            var customerId = await GetCustomerIdAsync();

            try
            {
                var order = await _customerService.PurchaseProducts(customerId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Favorites")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetFavorites()
        {
            var customerId = await GetCustomerIdAsync();

            var FavP = await _customerService.GetFavorites(customerId);
            return Ok(FavP);
        }

        // [HttpGet("Cart")]
        // // [Authorize(Roles = "Customer")]
        // public async Task<IActionResult> GetCart()
        // {
        //     var customerId = await GetCustomerIdAsync();
        //     var cartItems = await _customerService.GetCart(customerId);
        //     return Ok(cartItems);
        // }
        [HttpGet("ordered-products")]
        public async Task<IActionResult> GetPurchasedProducts()
        {
            var customerId = await GetCustomerIdAsync();
            var products = await _customerService.GetPurchasedProductsByCustomerIdAsync(customerId);
            return Ok(products);
        }

        [HttpPost("Savecreditcard")]
        // [Authorize(Roles = "Customer")]
        public async Task<IActionResult> SaveCreditCard([FromBody] PaymentDto paymentDto)
        {
            var customerId = await GetCustomerIdAsync();
            if (string.IsNullOrEmpty(customerId))
                return Unauthorized("Invalid or missing token.");

            if (paymentDto == null)
                return BadRequest("Payment details are required.");

            try
            {
                var result = await _customerService.SaveCreditCard(paymentDto, customerId);

                if (result)
                    return Ok(new { message = "Credit card saved successfully." });

                return BadRequest("Failed to save the credit card.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, ex.Message); // هات رسالة الخطأ الحقيقة بدل كلام عام
            }
        }

        [HttpGet("creditcardinfo")]
        public async Task<IActionResult> GetCreditCardInfo()
        {
            var customerId = await GetCustomerIdAsync();
            if (string.IsNullOrEmpty(customerId))
                return Unauthorized("Invalid token or UID not found.");

            var creditCard = await _customerService.GetCreditCardInfo(customerId);

            if (creditCard == null)
            {
                // ✅ مفيش كريدت كارد، رجع Response فاضية عادي
                return Ok(
                    new
                    {
                        CardName = "",
                        CardType = "",
                        EncryptedCardNumber = "",
                        ExpirDate = (DateTime?)null,
                    }
                );
            }

            var decryptedCard = _encryptionHelper.Decrypt(creditCard.EncryptedCardNumber);

            return Ok(
                new
                {
                    CardName = creditCard.CardName,
                    CardType = creditCard.CardType,
                    EncryptedCardNumber = decryptedCard,
                    ExpirDate = creditCard.ExpirDate,
                }
            );
        }

        [HttpGet("getProductById/{productId}")]
        public async Task<IActionResult> GetProductById(int productId)
        {
            var customerId = await GetCustomerIdAsync();
            try
            {
                var product = await _customerService.GetProductById(productId, customerId);
                if (product == null)
                {
                    return NotFound(new { error = "Product not found" });
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private async Task<string> GetCustomerIdAsync()
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
