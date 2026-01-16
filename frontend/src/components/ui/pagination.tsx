'use client';

import { cn } from '@/lib/utils';
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-react';

export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalItems: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  pageSizeOptions?: number[];
  showPageSize?: boolean;
  className?: string;
}

export function Pagination({
  currentPage,
  totalPages,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
  showPageSize = true,
  className,
}: PaginationProps) {
  const startItem = (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, totalItems);

  const canGoPrevious = currentPage > 1;
  const canGoNext = currentPage < totalPages;

  // Calculate visible page numbers
  const getPageNumbers = () => {
    const pages: (number | string)[] = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible) {
      return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    // Always show first page
    pages.push(1);

    // Calculate range around current page
    let start = Math.max(2, currentPage - 1);
    let end = Math.min(totalPages - 1, currentPage + 1);

    // Adjust range if at edges
    if (currentPage <= 3) {
      end = Math.min(4, totalPages - 1);
    } else if (currentPage >= totalPages - 2) {
      start = Math.max(2, totalPages - 3);
    }

    // Add ellipsis if needed
    if (start > 2) {
      pages.push('...');
    }

    // Add middle pages
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    // Add ellipsis if needed
    if (end < totalPages - 1) {
      pages.push('...');
    }

    // Always show last page
    if (totalPages > 1) {
      pages.push(totalPages);
    }

    return pages;
  };

  return (
    <div className={cn('flex items-center justify-between gap-4', className)}>
      {/* Page size selector */}
      {showPageSize && onPageSizeChange && (
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">Show</span>
          <select
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className="h-8 rounded border border-input bg-background px-2 text-sm focus:border-primary focus:outline-none"
          >
            {pageSizeOptions.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
          <span className="text-sm text-muted-foreground">per page</span>
        </div>
      )}

      {/* Item count */}
      <div className="text-sm text-muted-foreground">
        Showing {startItem} to {endItem} of {totalItems} results
      </div>

      {/* Page navigation */}
      <div className="flex items-center gap-1">
        {/* First page */}
        <button
          onClick={() => onPageChange(1)}
          disabled={!canGoPrevious}
          className="flex h-8 w-8 items-center justify-center rounded border border-input bg-background hover:bg-muted disabled:opacity-50 disabled:pointer-events-none"
          title="First page"
        >
          <ChevronsLeft className="h-4 w-4" />
        </button>

        {/* Previous page */}
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={!canGoPrevious}
          className="flex h-8 w-8 items-center justify-center rounded border border-input bg-background hover:bg-muted disabled:opacity-50 disabled:pointer-events-none"
          title="Previous page"
        >
          <ChevronLeft className="h-4 w-4" />
        </button>

        {/* Page numbers */}
        {getPageNumbers().map((page, index) => (
          <button
            key={index}
            onClick={() => typeof page === 'number' && onPageChange(page)}
            disabled={page === '...'}
            className={cn(
              'flex h-8 min-w-8 items-center justify-center rounded px-2 text-sm',
              page === currentPage
                ? 'bg-primary text-primary-foreground'
                : page === '...'
                  ? 'cursor-default'
                  : 'border border-input bg-background hover:bg-muted'
            )}
          >
            {page}
          </button>
        ))}

        {/* Next page */}
        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={!canGoNext}
          className="flex h-8 w-8 items-center justify-center rounded border border-input bg-background hover:bg-muted disabled:opacity-50 disabled:pointer-events-none"
          title="Next page"
        >
          <ChevronRight className="h-4 w-4" />
        </button>

        {/* Last page */}
        <button
          onClick={() => onPageChange(totalPages)}
          disabled={!canGoNext}
          className="flex h-8 w-8 items-center justify-center rounded border border-input bg-background hover:bg-muted disabled:opacity-50 disabled:pointer-events-none"
          title="Last page"
        >
          <ChevronsRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}

/**
 * Simple pagination with just prev/next buttons.
 */
export interface SimplePaginationProps {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  className?: string;
}

export function SimplePagination({
  currentPage,
  totalPages,
  onPageChange,
  className,
}: SimplePaginationProps) {
  return (
    <div className={cn('flex items-center justify-center gap-2', className)}>
      <button
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage === 1}
        className="flex items-center gap-1 rounded border border-input bg-background px-3 py-1.5 text-sm hover:bg-muted disabled:opacity-50 disabled:pointer-events-none"
      >
        <ChevronLeft className="h-4 w-4" />
        Previous
      </button>
      <span className="text-sm text-muted-foreground">
        Page {currentPage} of {totalPages}
      </span>
      <button
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage === totalPages}
        className="flex items-center gap-1 rounded border border-input bg-background px-3 py-1.5 text-sm hover:bg-muted disabled:opacity-50 disabled:pointer-events-none"
      >
        Next
        <ChevronRight className="h-4 w-4" />
      </button>
    </div>
  );
}
