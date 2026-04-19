import { useQuery } from "@tanstack/react-query";
import { getProducts, type GetProductsParams } from "../services/inventoryApi";

export function useInventoryProducts(params: GetProductsParams = {}) {
  const normalizedParams: GetProductsParams = {
    activeOnly: true,
    page: 1,
    pageSize: 50,
    ...params
  };

  return useQuery({
    queryKey: [
      "inventory",
      "products",
      normalizedParams.category ?? "",
      normalizedParams.lowStockOnly ?? false,
      normalizedParams.activeOnly ?? true,
      normalizedParams.searchTerm ?? "",
      normalizedParams.page ?? 1,
      normalizedParams.pageSize ?? 50
    ],
    queryFn: ({ signal }) => getProducts(normalizedParams, signal)
  });
}
