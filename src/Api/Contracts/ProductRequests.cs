namespace ShippingSystem.Api.Contracts;

public sealed record CreateProductRequest(string Name, decimal Price, int InitialQuantity);

public sealed record UpdateProductRequest(string Name, decimal Price);

public sealed record RestockProductRequest(int Quantity);
