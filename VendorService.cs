using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JWTRefreshTokenInDotNet6.Models;
using JWTRefreshTokenInDotNet6.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JWTRefreshTokenInDotNet6.Services
{
    public class VendorService : IVendorService
    {
        private readonly ApplicationDbContext _context;

        public VendorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsAsync(string vendorid)
        {
            var products = await _context.Products.Where(p => p.VendorId == vendorid).ToListAsync();

            if (products == null || products.Count == 0)
            {
                return null;
            }

            return products;
        }

        public async Task<List<ProductWithPurchasesDto>> GetProductPurchasedAsync(string vendorId)
        {
            var data = await (
                from oi in _context.OrderItems
                join o in _context.Orders on oi.OrderId equals o.Id
                join c in _context.Users on o.CustomerId equals c.Id
                join p in _context.Products on oi.ProductId equals p.Id
                where oi.VendorId == vendorId
                select new
                {
                    p.Id,
                    ProductName = p.Title,
                    CustomerId = c.Id,
                    c.UserName,
                    c.FirstName,
                    c.LastName,
                    c.Email,
                }
            ).ToListAsync();

            var result = data.GroupBy(x => new { x.Id, x.ProductName })
                .Select(g => new ProductWithPurchasesDto
                {
                    ProductId = g.Key.Id,
                    ProductName = g.Key.ProductName,
                    Purchases = g.GroupBy(c => c.CustomerId)
                        .Select(cg => cg.First()) // تجنب التكرار
                        .Select(c => new CustomerPurchaseInfoDto
                        {
                            Id = c.CustomerId,
                            UserName = c.UserName,
                            FirstName = c.FirstName,
                            LastName = c.LastName,
                            Email = c.Email,
                        })
                        .ToList(),
                })
                .ToList();

            return result;
        }

        //VendorService
        public async Task DeleteProductAsync(int id, string vendorId)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new Exception("Product not found");
            var vendor = await _context.Users.FindAsync(vendorId);
            if (vendor == null)
                throw new Exception("Vendor not found");

            if (vendor.CanDelete == true)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM FavoriteProduct WHERE ProductId = {0}",
                    id
                );

                var cartItems = await _context
                    .CartItems.Where(ci => ci.ProductId == id)
                    .ToListAsync();
                _context.CartItems.RemoveRange(cartItems);

                var relatedViews = await _context
                    .ProductViews.Where(v => v.ProductId == id)
                    .ToListAsync();
                _context.ProductViews.RemoveRange(relatedViews);

                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "You do not have permission to delete this product."
                );
            }
        }

        public async Task<Product> UpdateProductAsync(int id, ProductDto dto, string vendorId)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return null;

            var vendor = await _context.Users.FindAsync(vendorId);
            if (vendor == null)
                return null;
            if (vendor.CanEdit && product.VendorId == vendorId)
            {
                product.Title = dto.Title;
                product.Description = dto.Description;
                product.ImageUrl = dto.ImageUrl;
                product.Category = dto.Category;
                product.NumOfUnits = dto.NumOfUnits;
                product.Price = dto.Price;
                await _context.SaveChangesAsync();
                return product;
            }
            else
            {
                throw new UnauthorizedAccessException(
                    "You do not have permission to edit this product."
                );
            }
        }
    }
}
