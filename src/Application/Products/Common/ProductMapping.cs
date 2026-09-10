using ShippingSystem.Application.Common.Models;
using ShippingSystem.Domain.Entities;

namespace ShippingSystem.Application.Products.Common;

internal static class ProductMapping
{
    public static ProductDto ToDto(Product product) =>
        new(product.Id, product.Name, product.Price, product.QuantityAvailable);
}
