// Mirrors Api/Contracts/ProductRequests.cs and Application/Common/Models/SharedDtos.cs

export interface ProductDto {
  id: string;
  name: string;
  price: number;
  quantityAvailable: number;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateProductRequest {
  name: string;
  price: number;
  initialQuantity: number;
}

export interface UpdateProductRequest {
  name: string;
  price: number;
}

export interface RestockProductRequest {
  quantity: number;
}
