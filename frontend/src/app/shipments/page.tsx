'use client';

import { useState } from 'react';
import Link from 'next/link';
import { DashboardLayout } from '@/components/layout';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Button,
  Input,
  Select,
  Badge,
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
  TableEmpty,
  Pagination,
  SectionLoader,
} from '@/components/ui';
import { formatCurrency, formatDate } from '@/lib/utils';
import { useShipments, useShipmentStats } from '@/hooks';
import type { ShipmentFilters, ShipmentStatus } from '@/types/api';
import { Search, Filter, Download, Eye, ExternalLink, MoreHorizontal, Truck, Package } from 'lucide-react';

const statusOptions = [
  { value: '', label: 'All Statuses' },
  { value: 'Created', label: 'Created' },
  { value: 'Manifested', label: 'Manifested' },
  { value: 'PickedUp', label: 'Picked Up' },
  { value: 'InTransit', label: 'In Transit' },
  { value: 'OutForDelivery', label: 'Out for Delivery' },
  { value: 'Delivered', label: 'Delivered' },
  { value: 'NDR', label: 'NDR' },
  { value: 'RTOInitiated', label: 'RTO Initiated' },
  { value: 'RTODelivered', label: 'RTO Delivered' },
];

const courierOptions = [
  { value: '', label: 'All Couriers' },
  { value: 'shiprocket', label: 'Shiprocket' },
  { value: 'delhivery', label: 'Delhivery' },
  { value: 'bluedart', label: 'BlueDart' },
  { value: 'xpressbees', label: 'XpressBees' },
];

export default function ShipmentsPage() {
  const [filters, setFilters] = useState<ShipmentFilters>({
    pageNumber: 1,
    pageSize: 10,
  });
  const [searchQuery, setSearchQuery] = useState('');

  const apiFilters: ShipmentFilters = {
    ...filters,
    search: searchQuery || undefined,
  };

  const { data, isLoading, error } = useShipments(apiFilters);
  const { data: stats } = useShipmentStats();

  const shipments = data?.items || [];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const handleFilterChange = (key: keyof ShipmentFilters, value: string) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value || undefined,
      pageNumber: 1,
    }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, pageNumber: page }));
  };

  const handlePageSizeChange = (size: number) => {
    setFilters((prev) => ({ ...prev, pageSize: size, pageNumber: 1 }));
  };

  return (
    <DashboardLayout title="Shipments">
      {/* Stats Cards */}
      <div className="mb-6 grid gap-4 md:grid-cols-4">
        <StatCard
          label="In Transit"
          value={stats?.inTransit || 0}
          icon={<Truck className="h-5 w-5" />}
          color="info"
        />
        <StatCard
          label="Out for Delivery"
          value={stats?.outForDelivery || 0}
          icon={<Package className="h-5 w-5" />}
          color="primary"
        />
        <StatCard
          label="NDR Cases"
          value={stats?.ndrCases || 0}
          icon={<Package className="h-5 w-5" />}
          color="warning"
        />
        <StatCard
          label="Delivered Today"
          value={stats?.delivered || 0}
          icon={<Package className="h-5 w-5" />}
          color="success"
        />
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>All Shipments</CardTitle>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" leftIcon={<Download className="h-4 w-4" />}>
              Export
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {/* Filters */}
          <div className="mb-6 flex flex-wrap items-center gap-4">
            <div className="flex-1 min-w-[200px]">
              <Input
                placeholder="Search by AWB, order number, or customer..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                leftIcon={<Search className="h-4 w-4" />}
              />
            </div>
            <Select
              options={statusOptions}
              value={filters.status || ''}
              onChange={(e) => handleFilterChange('status', e.target.value as ShipmentStatus)}
              className="w-44"
            />
            <Select
              options={courierOptions}
              value={filters.courierCode || ''}
              onChange={(e) => handleFilterChange('courierCode', e.target.value)}
              className="w-40"
            />
            <Button variant="outline" size="sm" leftIcon={<Filter className="h-4 w-4" />}>
              More Filters
            </Button>
          </div>

          {/* Loading State */}
          {isLoading ? (
            <SectionLoader />
          ) : error ? (
            <div className="py-12 text-center text-error">
              Failed to load shipments. Please try again.
            </div>
          ) : (
            <>
              {/* Shipments Table */}
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>AWB / Order</TableHead>
                    <TableHead>Customer</TableHead>
                    <TableHead>Courier</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Est. Delivery</TableHead>
                    <TableHead className="text-right">Cost</TableHead>
                    <TableHead className="w-16">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {shipments.length === 0 ? (
                    <TableEmpty
                      colSpan={7}
                      message="No shipments found"
                      icon={<Truck className="h-8 w-8" />}
                    />
                  ) : (
                    shipments.map((shipment) => (
                      <TableRow key={shipment.id}>
                        <TableCell>
                          <div>
                            <Link
                              href={`/shipments/${shipment.id}`}
                              className="font-medium text-primary hover:underline"
                            >
                              {shipment.awbNumber}
                            </Link>
                            <p className="text-xs text-muted-foreground">
                              <Link href={`/orders/${shipment.orderId}`} className="hover:underline">
                                {shipment.orderNumber}
                              </Link>
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div>
                            <p className="font-medium">{shipment.orderNumber}</p>
                            <p className="text-xs text-muted-foreground">
                              {(shipment.shippingCost ?? 0) > 0 ? `Weight: ${shipment.weight}kg` : ''}
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <span className="text-sm">{shipment.courierName}</span>
                        </TableCell>
                        <TableCell>
                          <ShipmentStatusBadge status={shipment.status} />
                        </TableCell>
                        <TableCell>
                          <div className="text-sm">
                            {shipment.estimatedDeliveryDate && (
                              <p>{formatDate(shipment.estimatedDeliveryDate)}</p>
                            )}
                            {shipment.actualDeliveryDate && (
                              <p className="text-xs text-success">
                                Delivered: {formatDate(shipment.actualDeliveryDate)}
                              </p>
                            )}
                          </div>
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {formatCurrency(shipment.shippingCost ?? 0)}
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Link
                              href={`/shipments/${shipment.id}`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="View Shipment"
                            >
                              <Eye className="h-4 w-4 text-muted-foreground" />
                            </Link>
                            {shipment.trackingUrl && (
                              <a
                                href={shipment.trackingUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="rounded p-1.5 hover:bg-muted"
                                title="Track on Courier Site"
                              >
                                <ExternalLink className="h-4 w-4 text-muted-foreground" />
                              </a>
                            )}
                            <button className="rounded p-1.5 hover:bg-muted" title="More Actions">
                              <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
                            </button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>

              {/* Pagination */}
              {shipments.length > 0 && (
                <div className="mt-4">
                  <Pagination
                    currentPage={filters.pageNumber || 1}
                    totalPages={totalPages}
                    totalItems={totalItems}
                    pageSize={filters.pageSize || 10}
                    onPageChange={handlePageChange}
                    onPageSizeChange={handlePageSizeChange}
                  />
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </DashboardLayout>
  );
}

function ShipmentStatusBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default' | 'primary'> = {
    Created: 'default',
    Manifested: 'info',
    PickedUp: 'info',
    InTransit: 'primary',
    OutForDelivery: 'primary',
    Delivered: 'success',
    NDR: 'warning',
    RTOInitiated: 'error',
    RTODelivered: 'error',
    Cancelled: 'default',
  };

  const labels: Record<string, string> = {
    InTransit: 'In Transit',
    OutForDelivery: 'Out for Delivery',
    PickedUp: 'Picked Up',
    RTOInitiated: 'RTO Initiated',
    RTODelivered: 'RTO Delivered',
  };

  return (
    <Badge variant={variants[status] || 'default'} size="sm">
      {labels[status] || status}
    </Badge>
  );
}

function StatCard({
  label,
  value,
  icon,
  color,
}: {
  label: string;
  value: number;
  icon: React.ReactNode;
  color: 'info' | 'primary' | 'warning' | 'success';
}) {
  const colors = {
    info: 'bg-info/10 text-info',
    primary: 'bg-primary/10 text-primary',
    warning: 'bg-warning/10 text-warning',
    success: 'bg-success/10 text-success',
  };

  return (
    <Card>
      <CardContent className="flex items-center gap-4 p-4">
        <div className={`flex h-10 w-10 items-center justify-center rounded-full ${colors[color]}`}>
          {icon}
        </div>
        <div>
          <p className="text-sm text-muted-foreground">{label}</p>
          <p className="text-xl font-semibold">{value}</p>
        </div>
      </CardContent>
    </Card>
  );
}
