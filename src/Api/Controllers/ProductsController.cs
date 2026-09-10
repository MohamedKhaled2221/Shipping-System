using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShippingSystem.Api.Contracts;
using ShippingSystem.Application.Common.Models;
using ShippingSystem.Application.Products.Commands.CreateProduct;
using ShippingSystem.Application.Products.Commands.RestockProduct;
using ShippingSystem.Application.Products.Commands.UpdateProduct;
using ShippingSystem.Application.Products.Queries.GetProductById;
using ShippingSystem.Application.Products.Queries.GetProducts;

namespace ShippingSystem.Api.Controllers;

/// <summary>
/// FR-3.1, FR-5.x. Reads are [AllowAnonymous] — a product catalog is public storefront
/// data a Customer must be able to browse before they've registered/logged in, same as any
/// e-commerce site. Every write (create/update/restock) is Admin/shipping-employee only.
/// </summary>
[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    public ProductsController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var products = await _sender.Send(new GetProductsQuery(pageNumber, pageSize), cancellationToken);
        return Ok(products);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new CreateProductCommand(request.Name, request.Price, request.InitialQuantity), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new UpdateProductCommand(id, request.Name, request.Price), cancellationToken);
        return Ok(product);
    }

    /// <summary>Stock top-ups only — see RestockProductCommand's doc comment for why this is separate from Update.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/restock")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductDto>> Restock(Guid id, [FromBody] RestockProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(new RestockProductCommand(id, request.Quantity), cancellationToken);
        return Ok(product);
    }
}
