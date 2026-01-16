import { cn } from '@/lib/utils';
import { type HTMLAttributes, type TdHTMLAttributes, type ThHTMLAttributes } from 'react';

export interface TableProps extends HTMLAttributes<HTMLTableElement> {
  children: React.ReactNode;
}

export function Table({ className, children, ...props }: TableProps) {
  return (
    <div className="relative w-full overflow-auto">
      <table
        className={cn('w-full caption-bottom text-sm', className)}
        {...props}
      >
        {children}
      </table>
    </div>
  );
}

export interface TableHeaderProps extends HTMLAttributes<HTMLTableSectionElement> {
  children: React.ReactNode;
}

export function TableHeader({ className, children, ...props }: TableHeaderProps) {
  return (
    <thead className={cn('[&_tr]:border-b', className)} {...props}>
      {children}
    </thead>
  );
}

export interface TableBodyProps extends HTMLAttributes<HTMLTableSectionElement> {
  children: React.ReactNode;
}

export function TableBody({ className, children, ...props }: TableBodyProps) {
  return (
    <tbody className={cn('[&_tr:last-child]:border-0', className)} {...props}>
      {children}
    </tbody>
  );
}

export interface TableFooterProps extends HTMLAttributes<HTMLTableSectionElement> {
  children: React.ReactNode;
}

export function TableFooter({ className, children, ...props }: TableFooterProps) {
  return (
    <tfoot
      className={cn('border-t bg-muted/50 font-medium [&>tr]:last:border-b-0', className)}
      {...props}
    >
      {children}
    </tfoot>
  );
}

export interface TableRowProps extends HTMLAttributes<HTMLTableRowElement> {
  children: React.ReactNode;
}

export function TableRow({ className, children, ...props }: TableRowProps) {
  return (
    <tr
      className={cn(
        'border-b transition-colors hover:bg-muted/50 data-[state=selected]:bg-muted',
        className
      )}
      {...props}
    >
      {children}
    </tr>
  );
}

export interface TableHeadProps extends ThHTMLAttributes<HTMLTableCellElement> {
  children?: React.ReactNode;
}

export function TableHead({ className, children, ...props }: TableHeadProps) {
  return (
    <th
      className={cn(
        'h-12 px-4 text-left align-middle font-medium text-muted-foreground [&:has([role=checkbox])]:pr-0',
        className
      )}
      {...props}
    >
      {children}
    </th>
  );
}

export interface TableCellProps extends TdHTMLAttributes<HTMLTableCellElement> {
  children?: React.ReactNode;
}

export function TableCell({ className, children, ...props }: TableCellProps) {
  return (
    <td
      className={cn('p-4 align-middle [&:has([role=checkbox])]:pr-0', className)}
      {...props}
    >
      {children}
    </td>
  );
}

export interface TableCaptionProps extends HTMLAttributes<HTMLTableCaptionElement> {
  children: React.ReactNode;
}

export function TableCaption({ className, children, ...props }: TableCaptionProps) {
  return (
    <caption className={cn('mt-4 text-sm text-muted-foreground', className)} {...props}>
      {children}
    </caption>
  );
}

/**
 * Empty state for tables.
 */
export interface TableEmptyProps {
  colSpan: number;
  message?: string;
  icon?: React.ReactNode;
}

export function TableEmpty({
  colSpan,
  message = 'No data found',
  icon,
}: TableEmptyProps) {
  return (
    <TableRow>
      <TableCell colSpan={colSpan} className="h-32 text-center">
        <div className="flex flex-col items-center justify-center gap-2 text-muted-foreground">
          {icon}
          <span>{message}</span>
        </div>
      </TableCell>
    </TableRow>
  );
}
