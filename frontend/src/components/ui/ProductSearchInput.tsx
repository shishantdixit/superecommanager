'use client';

import { useState, useRef, useEffect } from 'react';
import { Search, Package, Loader2, X } from 'lucide-react';
import { useProducts } from '@/hooks';
import type { ProductListItem } from '@/services/inventory.service';

export interface SelectedProduct {
  sku: string;
  name: string;
  variantName?: string;
  unitPrice: number;
  productId: string;
  availableStock?: number;
}

interface ProductSearchInputProps {
  onSelect: (product: SelectedProduct) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  /** Filter products by channel. If undefined, shows all products. */
  channelId?: string;
}

export function ProductSearchInput({
  onSelect,
  placeholder = 'Search products by name or SKU...',
  disabled = false,
  className = '',
  channelId,
}: ProductSearchInputProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchTerm);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Fetch products based on search and channel filter
  const { data: productsData, isLoading } = useProducts({
    searchTerm: debouncedSearch,
    pageSize: 8,
    page: 1,
    isActive: true,
    channelId,
  });

  const products = productsData?.items || [];

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSelect = (product: ProductListItem) => {
    onSelect({
      sku: product.sku,
      name: product.name,
      unitPrice: product.sellingPrice,
      productId: product.id,
      availableStock: product.totalStock,
    });
    setSearchTerm('');
    setDebouncedSearch('');
    setIsOpen(false);
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(e.target.value);
    if (!isOpen) {
      setIsOpen(true);
    }
  };

  const handleInputFocus = () => {
    if (searchTerm.length > 0) {
      setIsOpen(true);
    }
  };

  const handleClear = () => {
    setSearchTerm('');
    setDebouncedSearch('');
    inputRef.current?.focus();
  };

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
        <input
          ref={inputRef}
          type="text"
          value={searchTerm}
          onChange={handleInputChange}
          onFocus={handleInputFocus}
          placeholder={placeholder}
          disabled={disabled}
          className="w-full rounded-md border border-input bg-background py-2 pl-10 pr-10 text-sm placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary disabled:cursor-not-allowed disabled:opacity-50"
        />
        {searchTerm && (
          <button
            type="button"
            onClick={handleClear}
            aria-label="Clear search"
            title="Clear search"
            className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
          >
            <X className="h-4 w-4" />
          </button>
        )}
      </div>

      {/* Dropdown Results */}
      {isOpen && (searchTerm.length > 0 || isLoading) && (
        <div
          className="absolute left-0 right-0 top-full z-[100] mt-1 max-h-72 overflow-auto rounded-md border border-border bg-background shadow-lg"
        >
          {isLoading ? (
            <div className="flex items-center justify-center gap-2 p-4 text-sm text-muted-foreground">
              <Loader2 className="h-4 w-4 animate-spin" />
              Searching...
            </div>
          ) : products.length === 0 ? (
            <div className="p-4 text-center text-sm text-muted-foreground">
              No products found for &quot;{debouncedSearch}&quot;
            </div>
          ) : (
            <div className="py-1">
              {products.map((product) => (
                <button
                  key={product.id}
                  type="button"
                  onMouseDown={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    handleSelect(product);
                  }}
                  className="flex w-full items-start gap-3 px-3 py-2.5 text-left hover:bg-accent transition-colors border-b border-border last:border-b-0 cursor-pointer"
                >
                  {product.imageUrl ? (
                    <img
                      src={product.imageUrl}
                      alt={product.name}
                      className="h-12 w-12 flex-shrink-0 rounded object-cover border border-border"
                    />
                  ) : (
                    <div className="flex h-12 w-12 flex-shrink-0 items-center justify-center rounded bg-muted border border-border">
                      <Package className="h-6 w-6 text-muted-foreground" />
                    </div>
                  )}
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-foreground line-clamp-2 leading-tight">
                      {product.name}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground truncate">
                      SKU: {product.sku}
                    </p>
                    <div className="mt-1 flex items-center gap-3 text-xs">
                      <span className="font-medium text-foreground">
                        ₹{product.sellingPrice.toFixed(2)}
                      </span>
                      <span
                        className={`font-medium ${
                          product.totalStock > 0 ? 'text-green-600' : 'text-red-500'
                        }`}
                      >
                        {product.totalStock > 0
                          ? `${product.totalStock} in stock`
                          : 'Out of stock'}
                      </span>
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
