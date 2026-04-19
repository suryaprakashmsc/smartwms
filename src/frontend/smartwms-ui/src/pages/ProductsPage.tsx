import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState, type ChangeEvent, type FormEvent } from "react";
import { useInventoryProducts } from "../hooks/useInventoryProducts";
import { createProduct, type CreateProductPayload, type GetProductsParams, type ProductDto } from "../services/inventoryApi";

type ProductFormState = {
  name: string;
  sku: string;
  category: string;
  unitPrice: string;
  reorderLevel: string;
  initialQuantity: string;
  maximumLevel: string;
  description: string;
  supplier: string;
  warehouseLocation: string;
  barcode: string;
  currency: string;
  createdBy: string;
};

const initialProductFormState: ProductFormState = {
  name: "",
  sku: "",
  category: "General",
  unitPrice: "0",
  reorderLevel: "5",
  initialQuantity: "0",
  maximumLevel: "",
  description: "",
  supplier: "",
  warehouseLocation: "",
  barcode: "",
  currency: "USD",
  createdBy: "demo.user"
};

function formatCurrency(amount: number, currencyCode: string): string {
  try {
    return new Intl.NumberFormat("en-US", {
      style: "currency",
      currency: currencyCode,
      maximumFractionDigits: 2
    }).format(amount);
  } catch {
    return `${currencyCode} ${amount.toFixed(2)}`;
  }
}

function getRowTone(product: ProductDto): string {
  if (!product.isActive) {
    return "Inactive";
  }

  return product.isLowStock ? "Low stock" : "Healthy";
}

export default function ProductsPage() {
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState("");
  const [lowStockOnly, setLowStockOnly] = useState(false);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [createErrorMessage, setCreateErrorMessage] = useState<string | null>(null);
  const [productForm, setProductForm] = useState<ProductFormState>(initialProductFormState);

  const params = useMemo<GetProductsParams>(
    () => ({
      searchTerm,
      lowStockOnly,
      activeOnly: true,
      page: 1,
      pageSize: 100
    }),
    [searchTerm, lowStockOnly]
  );

  const productsQuery = useInventoryProducts(params);

  const createProductMutation = useMutation({
    mutationFn: (payload: CreateProductPayload) => createProduct(payload),
    onSuccess: async () => {
      setCreateErrorMessage(null);
      setProductForm(initialProductFormState);
      await queryClient.invalidateQueries({ queryKey: ["inventory", "products"] });
    },
    onError: (error: Error) => {
      setCreateErrorMessage(error.message);
    }
  });

  const handleProductFormChange = (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = event.target;
    setProductForm((current) => ({
      ...current,
      [name]: value
    }));
  };

  const handleCreateProductSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const unitPrice = Number(productForm.unitPrice);
    const reorderLevel = Number(productForm.reorderLevel);
    const initialQuantity = Number(productForm.initialQuantity);

    if (!Number.isFinite(unitPrice) || unitPrice < 0) {
      setCreateErrorMessage("Unit price must be a positive number.");
      return;
    }

    if (!Number.isFinite(reorderLevel) || reorderLevel < 0) {
      setCreateErrorMessage("Reorder level must be zero or greater.");
      return;
    }

    if (!Number.isFinite(initialQuantity) || initialQuantity < 0) {
      setCreateErrorMessage("Initial quantity must be zero or greater.");
      return;
    }

    const maximumLevel = productForm.maximumLevel.trim()
      ? Number(productForm.maximumLevel)
      : undefined;

    if (maximumLevel !== undefined && (!Number.isFinite(maximumLevel) || maximumLevel < 0)) {
      setCreateErrorMessage("Maximum level must be blank or a positive number.");
      return;
    }

    setCreateErrorMessage(null);

    createProductMutation.mutate({
      name: productForm.name.trim(),
      sku: productForm.sku.trim(),
      category: productForm.category.trim(),
      unitPrice,
      reorderLevel,
      initialQuantity,
      description: productForm.description.trim() || undefined,
      supplier: productForm.supplier.trim() || undefined,
      warehouseLocation: productForm.warehouseLocation.trim() || undefined,
      barcode: productForm.barcode.trim() || undefined,
      currency: productForm.currency.trim() || undefined,
      maximumLevel,
      createdBy: productForm.createdBy.trim() || "demo.user"
    });
  };

  return (
    <section>
      <p className="eyebrow">Inventory API</p>
      <h2>Products</h2>

      <div className="panel toolbar-panel">
        <label className="field">
          <span>Search</span>
          <input
            type="search"
            className="search-input"
            placeholder="Search by SKU, name, or category"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
          />
        </label>

        <label className="checkbox-row">
          <input
            type="checkbox"
            checked={lowStockOnly}
            onChange={(event) => setLowStockOnly(event.target.checked)}
          />
          <span>Low stock only</span>
        </label>

        <button
          type="button"
          className="primary-button inline"
          onClick={() => void productsQuery.refetch()}
          disabled={productsQuery.isFetching}
        >
          {productsQuery.isFetching ? "Refreshing..." : "Refresh"}
        </button>

        <button
          type="button"
          className="secondary-button"
          onClick={() => setShowCreateForm((current) => !current)}
          disabled={createProductMutation.isPending}
        >
          {showCreateForm ? "Hide add form" : "Add product"}
        </button>
      </div>

      {showCreateForm ? (
        <div className="panel create-product-panel">
          <div className="status-line">
            <h3 className="panel-title">Add New Product</h3>
          </div>

          <form className="product-create-form" onSubmit={handleCreateProductSubmit}>
            <div className="product-form-grid">
              <label className="field">
                <span>Name</span>
                <input
                  type="text"
                  name="name"
                  className="search-input"
                  value={productForm.name}
                  onChange={handleProductFormChange}
                  required
                />
              </label>

              <label className="field">
                <span>SKU</span>
                <input
                  type="text"
                  name="sku"
                  className="search-input"
                  value={productForm.sku}
                  onChange={handleProductFormChange}
                  required
                />
              </label>

              <label className="field">
                <span>Category</span>
                <input
                  type="text"
                  name="category"
                  className="search-input"
                  value={productForm.category}
                  onChange={handleProductFormChange}
                  required
                />
              </label>

              <label className="field">
                <span>Currency</span>
                <input
                  type="text"
                  name="currency"
                  className="search-input"
                  value={productForm.currency}
                  onChange={handleProductFormChange}
                  maxLength={3}
                />
              </label>

              <label className="field">
                <span>Unit Price</span>
                <input
                  type="number"
                  name="unitPrice"
                  className="search-input"
                  value={productForm.unitPrice}
                  onChange={handleProductFormChange}
                  min={0}
                  step="0.01"
                  required
                />
              </label>

              <label className="field">
                <span>Initial Quantity</span>
                <input
                  type="number"
                  name="initialQuantity"
                  className="search-input"
                  value={productForm.initialQuantity}
                  onChange={handleProductFormChange}
                  min={0}
                  required
                />
              </label>

              <label className="field">
                <span>Reorder Level</span>
                <input
                  type="number"
                  name="reorderLevel"
                  className="search-input"
                  value={productForm.reorderLevel}
                  onChange={handleProductFormChange}
                  min={0}
                  required
                />
              </label>

              <label className="field">
                <span>Maximum Level (optional)</span>
                <input
                  type="number"
                  name="maximumLevel"
                  className="search-input"
                  value={productForm.maximumLevel}
                  onChange={handleProductFormChange}
                  min={0}
                />
              </label>

              <label className="field">
                <span>Supplier (optional)</span>
                <input
                  type="text"
                  name="supplier"
                  className="search-input"
                  value={productForm.supplier}
                  onChange={handleProductFormChange}
                />
              </label>

              <label className="field">
                <span>Warehouse Location (optional)</span>
                <input
                  type="text"
                  name="warehouseLocation"
                  className="search-input"
                  value={productForm.warehouseLocation}
                  onChange={handleProductFormChange}
                />
              </label>

              <label className="field">
                <span>Barcode (optional)</span>
                <input
                  type="text"
                  name="barcode"
                  className="search-input"
                  value={productForm.barcode}
                  onChange={handleProductFormChange}
                />
              </label>

              <label className="field">
                <span>Created By</span>
                <input
                  type="text"
                  name="createdBy"
                  className="search-input"
                  value={productForm.createdBy}
                  onChange={handleProductFormChange}
                  required
                />
              </label>
            </div>

            <label className="field">
              <span>Description (optional)</span>
              <textarea
                name="description"
                className="form-textarea"
                value={productForm.description}
                onChange={handleProductFormChange}
                rows={3}
              />
            </label>

            <div className="form-actions">
              <button type="submit" className="primary-button inline" disabled={createProductMutation.isPending}>
                {createProductMutation.isPending ? "Saving..." : "Save product"}
              </button>
              <button
                type="button"
                className="secondary-button"
                onClick={() => {
                  setCreateErrorMessage(null);
                  setProductForm(initialProductFormState);
                }}
                disabled={createProductMutation.isPending}
              >
                Reset form
              </button>
            </div>

            {createProductMutation.isSuccess ? (
              <p className="subtle-text success-text">Product created successfully.</p>
            ) : null}
            {createErrorMessage ? <p className="subtle-text error-text">{createErrorMessage}</p> : null}
          </form>
        </div>
      ) : null}

      <div className="panel">
        <div className="table-row table-head products-row">
          <span>SKU</span>
          <span>Product</span>
          <span>On-hand</span>
          <span>Unit price</span>
          <span>Status</span>
        </div>

        {productsQuery.isLoading ? (
          <p className="subtle-text">Loading products...</p>
        ) : productsQuery.isError ? (
          <p className="subtle-text">{productsQuery.error.message}</p>
        ) : productsQuery.data?.length ? (
          productsQuery.data.map((product) => (
            <div key={product.id} className="table-row products-row">
              <span>{product.sku}</span>
              <span>{product.name}</span>
              <span>{product.quantityOnHand}</span>
              <span>{formatCurrency(product.unitPrice, product.currency)}</span>
              <strong>{getRowTone(product)}</strong>
            </div>
          ))
        ) : (
          <p className="subtle-text">No products found for this filter.</p>
        )}
      </div>
    </section>
  );
}
