import { buildApiPath, fetchJson, type QueryParams } from "./apiClient";

export type ProductDto = {
  id: string;
  sku: string;
  name: string;
  description: string;
  category: string;
  unitPrice: number;
  currency: string;
  quantityOnHand: number;
  reorderLevel: number;
  maximumLevel: number | null;
  isLowStock: boolean;
  isActive: boolean;
};

export type GetProductsParams = {
  category?: string;
  lowStockOnly?: boolean;
  activeOnly?: boolean;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
};

export type CreateProductPayload = {
  name: string;
  sku: string;
  category: string;
  unitPrice: number;
  reorderLevel: number;
  initialQuantity: number;
  createdBy: string;
  description?: string;
  supplier?: string;
  warehouseLocation?: string;
  barcode?: string;
  currency?: string;
  maximumLevel?: number;
};

export type AdjustStockPayload = {
  delta: number;
  reason: string;
  adjustedBy: string;
};

function toProductQuery(params: GetProductsParams): QueryParams {
  return {
    category: params.category,
    lowStockOnly: params.lowStockOnly,
    activeOnly: params.activeOnly,
    searchTerm: params.searchTerm?.trim(),
    page: params.page,
    pageSize: params.pageSize
  };
}

export function getProducts(
  params: GetProductsParams = {},
  signal?: AbortSignal
): Promise<ProductDto[]> {
  const normalizedParams: GetProductsParams = {
    activeOnly: true,
    page: 1,
    pageSize: 50,
    ...params
  };

  return fetchJson<ProductDto[]>(buildApiPath("/products", toProductQuery(normalizedParams)), {
    signal
  });
}

export function getProductById(productId: string, signal?: AbortSignal): Promise<ProductDto> {
  return fetchJson<ProductDto>(buildApiPath(`/products/${productId}`), { signal });
}

export function createProduct(
  payload: CreateProductPayload,
  signal?: AbortSignal
): Promise<ProductDto> {
  return fetchJson<ProductDto>(buildApiPath("/products"), {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(payload),
    signal
  });
}

export function adjustProductStock(
  productId: string,
  payload: AdjustStockPayload,
  signal?: AbortSignal
): Promise<ProductDto> {
  return fetchJson<ProductDto>(buildApiPath(`/products/${productId}/stock-adjustments`), {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(payload),
    signal
  });
}
