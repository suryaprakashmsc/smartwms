import { useMemo } from "react";
import { useInventoryProducts } from "./useInventoryProducts";

export type InventoryDashboardMetrics = {
  activeSkus: number;
  totalUnits: number;
  lowStockCount: number;
  inventoryValue: number;
};

export function useInventoryDashboard() {
  const productsQuery = useInventoryProducts({
    activeOnly: true,
    page: 1,
    pageSize: 200
  });

  const metrics = useMemo<InventoryDashboardMetrics>(() => {
    const products = productsQuery.data ?? [];

    return {
      activeSkus: products.length,
      totalUnits: products.reduce((sum, item) => sum + item.quantityOnHand, 0),
      lowStockCount: products.filter((item) => item.isLowStock).length,
      inventoryValue: products.reduce((sum, item) => sum + item.quantityOnHand * item.unitPrice, 0)
    };
  }, [productsQuery.data]);

  const lowStockProducts = useMemo(
    () => (productsQuery.data ?? []).filter((item) => item.isLowStock),
    [productsQuery.data]
  );

  return {
    ...productsQuery,
    metrics,
    lowStockProducts
  };
}
