using JWTRefreshTokenInDotNet6.Controllers;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Services;

public interface ICustomerService
{
    // Task<PaymentDto> GetCreditCardAsync(string customerId);
    Task<List<Product>> ApprovedProducts();
    Task<string> AddToFavorite(string customerId, int productId);
    Task<String> RemoveFromFavorite(string customerId, int productId);
    Task<OrderDto> PurchaseProducts(string customerId);
    Task<bool> RemoveFromCartAsync(string customerId, int productId);

    // bool CheckCreditCard(PaymentDto pay);
    Task<List<Product>> GetFavorites(string customerId);
    Task<List<CartItem>> GetCart(string customerId);
    Task<bool> SaveCreditCard(PaymentDto pay, string customerId);
    Task<CreditCard?> GetCreditCardInfo(string customerId);
    Task<Product> GetProductById(int productId, string userId);
    Task<Cart> GetOrCreateCartAsync(string customerId);
    Task<bool> AddProductToCartAsync(string customerId, int productId, int quantity);
    Task<bool> UpdateCartItemQuantityAsync(string customerId, int productId, int quantity);
    Task<Cart> GetCartDetailsAsync(string customerId);
    Task<bool> RemoveProductFromCartAsync(string customerId, int productId);
    Task<UserDto> GetCustomerByIdAsync(string customerId);
    Task<List<OrderedProductDto>> GetPurchasedProductsByCustomerIdAsync(string customerId);
}
