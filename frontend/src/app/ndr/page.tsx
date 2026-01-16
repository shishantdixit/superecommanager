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
  Avatar,
  SectionLoader,
} from '@/components/ui';
import { formatDateTime } from '@/lib/utils';
import { useNdrCases, useNdrStats } from '@/hooks';
import type { NdrFilters, NdrStatus, NdrPriority } from '@/types/api';
import {
  Search,
  Filter,
  Download,
  Eye,
  Phone,
  MessageSquare,
  MoreHorizontal,
  AlertTriangle,
  Clock,
  UserPlus,
} from 'lucide-react';

const statusOptions = [
  { value: '', label: 'All Statuses' },
  { value: 'Open', label: 'Open' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'ReattemptScheduled', label: 'Reattempt Scheduled' },
  { value: 'Resolved', label: 'Resolved' },
  { value: 'RTOInitiated', label: 'RTO Initiated' },
  { value: 'Closed', label: 'Closed' },
];

const priorityOptions = [
  { value: '', label: 'All Priorities' },
  { value: 'Critical', label: 'Critical' },
  { value: 'High', label: 'High' },
  { value: 'Medium', label: 'Medium' },
  { value: 'Low', label: 'Low' },
];

const assigneeOptions = [
  { value: '', label: 'All Assignees' },
  { value: 'unassigned', label: 'Unassigned' },
];

export default function NdrPage() {
  const [filters, setFilters] = useState<NdrFilters>({
    pageNumber: 1,
    pageSize: 10,
  });
  const [searchQuery, setSearchQuery] = useState('');

  const apiFilters: NdrFilters = {
    ...filters,
    search: searchQuery || undefined,
  };

  const { data, isLoading, error } = useNdrCases(apiFilters);
  const { data: stats } = useNdrStats();

  const ndrCases = data?.items || [];
  const totalItems = data?.totalCount || 0;
  const totalPages = data?.totalPages || 1;

  const handleFilterChange = (key: keyof NdrFilters, value: string) => {
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
    <DashboardLayout title="NDR Management">
      {/* Stats Cards */}
      <div className="mb-6 grid gap-4 md:grid-cols-4">
        <StatCard
          label="Open Cases"
          value={stats?.openCases || 0}
          icon={<AlertTriangle className="h-5 w-5" />}
          color="error"
        />
        <StatCard
          label="In Progress"
          value={stats?.inProgressCases || 0}
          icon={<Clock className="h-5 w-5" />}
          color="warning"
        />
        <StatCard
          label="Unassigned"
          value={0}
          icon={<UserPlus className="h-5 w-5" />}
          color="info"
        />
        <StatCard
          label="Resolved Today"
          value={stats?.resolvedCases || 0}
          icon={<AlertTriangle className="h-5 w-5" />}
          color="success"
        />
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>NDR Cases</CardTitle>
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
                placeholder="Search by AWB, order, or customer..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                leftIcon={<Search className="h-4 w-4" />}
              />
            </div>
            <Select
              options={statusOptions}
              value={filters.status || ''}
              onChange={(e) => handleFilterChange('status', e.target.value as NdrStatus)}
              className="w-44"
            />
            <Select
              options={priorityOptions}
              value={filters.priority || ''}
              onChange={(e) => handleFilterChange('priority', e.target.value as NdrPriority)}
              className="w-36"
            />
            <Select
              options={assigneeOptions}
              value={filters.assignedToId || ''}
              onChange={(e) => handleFilterChange('assignedToId', e.target.value)}
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
              Failed to load NDR cases. Please try again.
            </div>
          ) : (
            <>
              {/* NDR Table */}
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>AWB / Order</TableHead>
                    <TableHead>Customer</TableHead>
                    <TableHead>Reason</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Priority</TableHead>
                    <TableHead>Assigned To</TableHead>
                    <TableHead>Attempts</TableHead>
                    <TableHead className="w-16">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {ndrCases.length === 0 ? (
                    <TableEmpty
                      colSpan={8}
                      message="No NDR cases found"
                      icon={<AlertTriangle className="h-8 w-8" />}
                    />
                  ) : (
                    ndrCases.map((ndr) => (
                      <TableRow key={ndr.id}>
                        <TableCell>
                          <div>
                            <Link
                              href={`/ndr/${ndr.id}`}
                              className="font-medium text-primary hover:underline"
                            >
                              {ndr.awbNumber}
                            </Link>
                            <p className="text-xs text-muted-foreground">
                              <Link href={`/orders/${ndr.orderId}`} className="hover:underline">
                                {ndr.orderNumber}
                              </Link>
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div>
                            <p className="font-medium">{ndr.customerName}</p>
                            <p className="text-xs text-muted-foreground">{ndr.customerPhone}</p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <div>
                            <p className="text-sm">{ndr.reason}</p>
                            <p className="text-xs text-muted-foreground">
                              {ndr.address?.city}, {ndr.address?.state}
                            </p>
                          </div>
                        </TableCell>
                        <TableCell>
                          <NdrStatusBadge status={ndr.status} />
                        </TableCell>
                        <TableCell>
                          <PriorityBadge priority={ndr.priority} />
                        </TableCell>
                        <TableCell>
                          {ndr.assignedToName ? (
                            <div className="flex items-center gap-2">
                              <Avatar name={ndr.assignedToName} size="sm" />
                              <span className="text-sm">{ndr.assignedToName}</span>
                            </div>
                          ) : (
                            <Button variant="outline" size="sm" className="h-7 text-xs">
                              Assign
                            </Button>
                          )}
                        </TableCell>
                        <TableCell>
                          <span className="text-sm font-medium">{ndr.attemptCount}</span>
                        </TableCell>
                        <TableCell>
                          <div className="flex items-center gap-1">
                            <Link
                              href={`/ndr/${ndr.id}`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="View Details"
                            >
                              <Eye className="h-4 w-4 text-muted-foreground" />
                            </Link>
                            <a
                              href={`tel:${ndr.customerPhone}`}
                              className="rounded p-1.5 hover:bg-muted"
                              title="Call Customer"
                            >
                              <Phone className="h-4 w-4 text-muted-foreground" />
                            </a>
                            <button
                              className="rounded p-1.5 hover:bg-muted"
                              title="Send Message"
                            >
                              <MessageSquare className="h-4 w-4 text-muted-foreground" />
                            </button>
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
              {ndrCases.length > 0 && (
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

function NdrStatusBadge({ status }: { status: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
    Open: 'error',
    InProgress: 'warning',
    ReattemptScheduled: 'info',
    Resolved: 'success',
    RTOInitiated: 'error',
    Closed: 'default',
  };

  const labels: Record<string, string> = {
    InProgress: 'In Progress',
    ReattemptScheduled: 'Reattempt Scheduled',
    RTOInitiated: 'RTO Initiated',
  };

  return (
    <Badge variant={variants[status] || 'default'} size="sm">
      {labels[status] || status}
    </Badge>
  );
}

function PriorityBadge({ priority }: { priority: string }) {
  const variants: Record<string, 'success' | 'warning' | 'error' | 'info' | 'default'> = {
    Critical: 'error',
    High: 'warning',
    Medium: 'info',
    Low: 'default',
  };

  return (
    <Badge variant={variants[priority] || 'default'} size="sm">
      {priority}
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
  color: 'error' | 'warning' | 'info' | 'success';
}) {
  const colors = {
    error: 'bg-error/10 text-error',
    warning: 'bg-warning/10 text-warning',
    info: 'bg-info/10 text-info',
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
