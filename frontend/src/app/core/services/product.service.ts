import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateProductRequest,
  PagedResult,
  ProductDto,
  RestockProductRequest,
  UpdateProductRequest,
} from '../models/product.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly baseUrl = `${environment.apiBaseUrl}/products`;

  constructor(private readonly http: HttpClient) {}

  /** FR-3.1 — public catalog, no auth required. */
  getProducts(pageNumber = 1, pageSize = 20): Observable<PagedResult<ProductDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber).set('pageSize', pageSize);
    return this.http.get<PagedResult<ProductDto>>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${this.baseUrl}/${id}`);
  }

  /** Admin only. */
  create(request: CreateProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>(this.baseUrl, request);
  }

  /** Admin only. */
  update(id: string, request: UpdateProductRequest): Observable<ProductDto> {
    return this.http.put<ProductDto>(`${this.baseUrl}/${id}`, request);
  }

  /** Admin only — stock top-ups (FR-5.x), kept separate from name/price edits. */
  restock(id: string, request: RestockProductRequest): Observable<ProductDto> {
    return this.http.post<ProductDto>(`${this.baseUrl}/${id}/restock`, request);
  }
}
