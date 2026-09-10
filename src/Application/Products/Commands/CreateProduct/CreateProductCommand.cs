using MediatR;
using ShippingSystem.Application.Common.Models;

namespace ShippingSystem.Application.Products.Commands.CreateProduct;

/// <summary>FR-3.1 — Admin/shipping-employee adds a new product to the catalog with its starting stock.</summary>
public sealed record CreateProductCommand(string Name, decimal Price, int InitialQuantity) : IRequest<ProductDto>;
