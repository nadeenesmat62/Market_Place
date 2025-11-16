using JWTRefreshTokenInDotNet6.Models;

namespace JWTRefreshTokenInDotNet6.Services
{
    public interface IVendorService
    {
        Task<List<Product>> GetAllProductsAsync(string vendorid);

        // Task <List<string>> GetProductPurchasedAsync(string vendorid, int productid);
        Task DeleteProductAsync(int id, string vendorId);
        Task<Product> UpdateProductAsync(int id, ProductDto dto, string vendorId);
        Task<List<ProductWithPurchasesDto>> GetProductPurchasedAsync(string vendorId);
    }
}
