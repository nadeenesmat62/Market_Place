using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JWTRefreshTokenInDotNet6.Helpers;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JWTRefreshTokenInDotNet6.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly EncryptionHelper _encryptionHelper;

        // private readonly ICustomerService _customerService;

        public CustomerService(
            ApplicationDbContext context,
            EncryptionHelper encryptionHelper,
            UserManager<ApplicationUser> userManager
        )
        {
            _context = context;
            _encryptionHelper = encryptionHelper;
            _userManager = userManager;
        }

        public async Task<UserDto?> GetCustomerByIdAsync(string customerId)
        {
            var user = await _userManager.FindByIdAsync(customerId);

            if (user == null)
                return null;

            var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");

            if (!isCustomer)
                return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
            };
        }

        public async Task<List<Product>> ApprovedProducts()
        {
            var approvedProducts = await _context.Products.Where(p => p.Isapproved).ToListAsync();

            return approvedProducts;
        }

        public async Task<List<OrderedProductDto>> GetPurchasedProductsByCustomerIdAsync(
            string customerId
        )
        {
            var OrderedProducts = await _context
                .OrderItems.Where(oi => oi.Order.CustomerId == customerId)
                .Select(oi => new OrderedProductDto
                {
                    ProductTitle = oi.Product.Title,
                    Price = oi.Price,
                    Quantity = oi.Quantity,
                })
                .ToListAsync();

            return OrderedProducts;
        }

        public async Task<Product> GetProductById(int productId, string userId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p =>
                p.Id == productId && p.Isapproved
            );

            if (product == null)
                return null;

            var alreadyViewed = await _context.ProductViews.AnyAsync(v =>
                v.ProductId == productId && v.CustomerId == userId
            );

            if (!alreadyViewed)
            {
                product.NumOfViews += 1;

                _context.ProductViews.Add(
                    new ProductViewNum { ProductId = productId, CustomerId = userId }
                );

                await _context.SaveChangesAsync();
            }

            return product;
        }

        //     public async Task<PaymentDto> GetCreditCardAsync(string customerId)
        // {
        //     var card = await _context.CreditCards
        //         .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        //     if (card == null) return null;

        //     // فك تشفير رقم البطاقة
        //     var decryptedCardNumber = _encryptionHelper.Decrypt(card.EncryptedCardNumber);

        //     // إنشاء PaymentDto لتخزين البيانات التي سيتم إرجاعها
        //     PaymentDto paymentDto = new PaymentDto
        //     {
        //         NumOfCard = decryptedCardNumber,
        //         CardType = card.CardType,
        //         ExpirDate = card.ExpiryDate
        //     };

        //     // تخزين البيانات الجديدة في جدول creditCard
        //     var newCreditCard = new CreditCard
        //     {
        //         CustomerId = customerId,
        //         EncryptedCardNumber = _encryptionHelper.Encrypt(paymentDto.NumOfCard),  // تشفير رقم البطاقة قبل التخزين
        //         CardType = paymentDto.CardType,
        //         ExpiryDate = paymentDto.ExpirDate
        //     };

        //     // إضافة البيانات إلى جدول CreditCards
        //     _context.CreditCards.Add(newCreditCard);
        //     await _context.SaveChangesAsync();  // حفظ التغييرات في قاعدة البيانات

        //     return paymentDto;
        // }

        public async Task<string> AddToFavorite(string customerId, int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p =>
                p.Id == productId && p.Isapproved
            );
            if (product == null)
                throw new Exception("Product not found or not approved");

            var customer = await _userManager.FindByIdAsync(customerId);
            if (customer == null)
                throw new Exception("Customer not found");

            var favorite = await _context
                .Favorites.Include(f => f.Products)
                .FirstOrDefaultAsync(fav => fav.CustomerId == customerId);

            if (favorite == null)
            {
                favorite = new Favorite
                {
                    CustomerId = customerId,
                    Products = new List<Product> { product },
                };
                _context.Favorites.Add(favorite);
            }
            else
            {
                bool alreadyFavorited = favorite.Products.Any(p => p.Id == productId);
                if (alreadyFavorited)
                    return "Product is already in favorites";

                favorite.Products.Add(product);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message;
                throw new Exception($"Save failed: {innerMessage}", dbEx);
            }

            return "Added to favorite";
        }

        public async Task<string> RemoveFromFavorite(string customerId, int productId)
        {
            var favorite = await _context
                .Favorites.Include(f => f.Products)
                .FirstOrDefaultAsync(f => f.CustomerId == customerId);

            if (favorite == null)
                throw new Exception("No favorites found");

            var product = favorite.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
                throw new Exception("Product not found in favorite list");

            // Remove product from the favorite
            favorite.Products.Remove(product);

            await _context.SaveChangesAsync(); // Update FavoriteProduct table

            // Explicitly re-load the Products collection from the DB to get the latest state
            await _context.Entry(favorite).Collection(f => f.Products).LoadAsync();

            // Now, if it's empty, remove the Favorite itself
            if (!favorite.Products.Any())
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync(); // Delete from Favorites table
            }

            return "Removed from favorite";
        }

        public async Task<OrderDto> PurchaseProducts(string customerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cartItems = await _context
                    .CartItems.Include(ci => ci.Product)
                    .Include(ci => ci.Cart)
                    .Where(ci => ci.Cart.CustomerId == customerId)
                    .ToListAsync();

                if (cartItems == null || !cartItems.Any())
                    throw new Exception("Cart is empty or does not exist.");

                var cart = cartItems.First().Cart;

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderItems = new List<OrderItem>(),
                };

                decimal totalPrice = 0;

                foreach (var cartItem in cartItems)
                {
                    if (cartItem.Product.NumOfUnits < cartItem.Quantity)
                        throw new Exception(
                            $"Product {cartItem.Product.Title} does not have enough stock."
                        );

                    cartItem.Product.NumOfUnits -= cartItem.Quantity;

                    decimal itemPrice = (decimal)cartItem.Product.Price * cartItem.Quantity;

                    var orderItem = new OrderItem
                    {
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity,
                        Price = itemPrice,
                        VendorId = cartItem.Product.VendorId,
                    };

                    totalPrice += itemPrice;
                    order.OrderItems.Add(orderItem);
                }

                order.TotalPrice = totalPrice;

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cartItems);
                _context.Carts.Remove(cart);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Map to DTO
                var orderDto = new OrderDto
                {
                    Id = order.Id,
                    CustomerId = order.CustomerId,
                    TotalPrice = order.TotalPrice,
                    OrderItems = order
                        .OrderItems.Select(oi => new OrderItemDto
                        {
                            ProductId = oi.ProductId,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            VendorId = oi.VendorId,
                            ProductTitle = cartItems
                                .First(ci => ci.ProductId == oi.ProductId)
                                .Product.Title,
                        })
                        .ToList(),
                };

                return orderDto;
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception("Save failed: " + innerMessage, ex);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Cart> GetOrCreateCartAsync(string customerId)
        {
            var cart = await _context
                .Carts.Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart { CustomerId = customerId, CartItems = new List<CartItem>() };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            return cart;
        }

        public async Task<bool> AddProductToCartAsync(
            string customerId,
            int productId,
            int quantity = 1
        )
        {
            var product = await _context
                .Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId && p.Isapproved);

            if (product == null)
                return false;
            var cart = await GetOrCreateCartAsync(customerId);
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.CartItems.Add(new CartItem { ProductId = productId, Quantity = quantity });
            }
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveProductFromCartAsync(string customerId, int productId)
        {
            var cart = await GetOrCreateCartAsync(customerId);
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem == null)
                return false;

            cart.CartItems.Remove(existingItem);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCartItemQuantityAsync(
            string customerId,
            int productId,
            int quantity
        )
        {
            var cart = await GetOrCreateCartAsync(customerId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            var product = await _context.Products.FirstOrDefaultAsync(ci => ci.Id == productId);

            if (cartItem == null || product == null)
                return false;

            if (product.NumOfUnits >= quantity)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity; // تحديث الكمية
                }

                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                return false; // إذا كانت الكمية المطلوبة أكثر من المتاحة في المخزون
            }
        }

        public async Task<Cart> GetCartDetailsAsync(string customerId)
        {
            return await _context
                .Carts.Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<bool> RemoveFromCartAsync(string customerId, int productId)
        {
            var cart = await _context
                .Carts.Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
                return false;

            var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (item == null)
                return false;

            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                product.NumOfUnits += item.Quantity;
                _context.Products.Update(product);
            }

            _context.CartItems.Remove(item);

            await _context.SaveChangesAsync();
            return true;
        }

        // public async Task<bool> AddToCart(string customerId, int productId, int quantity)
        // {
        //     var product = await _context.Products.FindAsync(productId);
        //     if (product == null || !product.Isapproved)
        //         return false;

        //     var cart = await _context.Cart
        //         .Include(c => c.CartItems)
        //         .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        //     if (cart == null)
        //     {
        //         var customer = await _userManager.FindByIdAsync(customerId);
        //         if (customer == null)
        //             return false;

        //         cart = new Cart
        //         {
        //             CustomerId = customerId,
        //             CartItems = new List<CartItem>()
        //         };

        //         _context.Cart.Add(cart);
        //     }

        //     var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        //     int currentCustomerQty = existingItem?.Quantity ?? 0;

        //     int totalQuantityInCarts = await _context.CartItems
        //         .Where(ci => ci.ProductId == productId)
        //         .SumAsync(ci => (int?)ci.Quantity) ?? 0;

        //     int totalQuantityExcludingThisCustomer = totalQuantityInCarts - currentCustomerQty;
        //     if (totalQuantityExcludingThisCustomer + quantity + currentCustomerQty > product.NumOfUnits)
        //         return false;

        //     if (existingItem != null)
        //     {
        //         existingItem.Quantity += quantity;
        //     }
        //     else
        //     {
        //         var newItem = new CartItem
        //         {
        //             Cart = cart,
        //             CartId = cart.Id,
        //             Product = product,
        //             ProductId = productId,
        //             CustomerId = customerId,
        //             Quantity = quantity
        //         };
        //         cart.CartItems.Add(newItem);
        //     }

        //     cart.Quantity = cart.CartItems.Sum(ci => ci.Quantity);

        //     product.NumOfUnits -= quantity;

        //     await _context.SaveChangesAsync();
        //     return true;
        // }

        // public async Task<bool> RemoveFromCart(string customerId, int productId)
        // {
        //     var cart = await _context.Cart.Include(c => c.CartItems)
        //                      .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        //     if (cart == null)
        //         return false;

        //     var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
        //     if (item == null)
        //         return false;

        //     var product = await _context.Products.FindAsync(productId);
        //     if (product != null)
        //     {
        //         product.NumOfUnits += item.Quantity;
        //     }

        //     _context.CartItems.Remove(item);

        //     cart.Quantity = cart.CartItems
        //         .Where(ci => ci.ProductId != productId)
        //         .Sum(ci => ci.Quantity);

        //     await _context.SaveChangesAsync();
        //     return true;
        // }

        private bool CheckCreditCard(PaymentDto pay)
        {
            if (pay.NumOfCard.Length != 16 || !pay.NumOfCard.All(char.IsDigit))
                return false;

            var supportedCardTypes = new List<string> { "Visa" };
            if (!supportedCardTypes.Contains(pay.CardType))
                return false;

            if (pay.ExpirDate < DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task<List<Product>> GetFavorites(string customerId)
        {
            var favorite = await _context
                .Favorites.Where(f => f.CustomerId == customerId)
                .SelectMany(f => f.Products)
                .ToListAsync();

            return favorite;
        }

        public async Task<List<CartItem>> GetCart(string customerId)
        {
            var cart = await _context
                .Carts.Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
                return new List<CartItem>();

            return cart.CartItems.ToList();
        }

        public async Task<bool> SaveCreditCard(PaymentDto pay, string customerId)
        {
            if (pay == null)
                throw new ArgumentNullException(nameof(pay));

            if (string.IsNullOrEmpty(customerId))
                throw new ArgumentException("CustomerId is required");

            var customer = await _userManager.FindByIdAsync(customerId);
            if (customer == null)
                throw new Exception("Customer not found.");

            if (!CheckCreditCard(pay))
                throw new Exception("Invalid credit card details.");

            var EncryCard = _encryptionHelper.Encrypt(pay.NumOfCard);

            var creditCard = new CreditCard
            {
                CardName = pay.CardName,
                CustomerId = customerId,
                EncryptedCardNumber = EncryCard,
                CardType = pay.CardType,
                ExpirDate = pay.ExpirDate,
            };

            _context.CreditCards.Add(creditCard);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<CreditCard?> GetCreditCardInfo(string customerId)
        {
            var customer = await _userManager.FindByIdAsync(customerId);
            if (customer == null)
                return null; // لو العميل مش موجود

            var creditCard = await _context.CreditCards.FirstOrDefaultAsync(c =>
                c.CustomerId == customerId
            );

            return creditCard; // ممكن ترجع null لو مفيش كريدت كارد
        }
    }
}
