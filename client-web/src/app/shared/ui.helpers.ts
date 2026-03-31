import { StatusValue } from '../core/app.models';

export const customerStatusOptions = [
  { value: 0, label: 'Lead' },
  { value: 1, label: 'Ativo' },
  { value: 2, label: 'Inativo' },
];

export const approvalModuleOptions = [
  { value: 0, label: 'Pedido de venda' },
  { value: 1, label: 'Pedido de compra' },
];

export const approvalStatusOptions = [
  { value: 0, label: 'Nao requerido' },
  { value: 1, label: 'Pendente' },
  { value: 2, label: 'Aprovado' },
  { value: 3, label: 'Rejeitado' },
];

export const approvalDecisionOptions = [
  { value: 0, label: 'Pendente' },
  { value: 1, label: 'Aprovado' },
  { value: 2, label: 'Rejeitado' },
];

export const salesOrderStatusLabels: Record<number, string> = {
  0: 'Draft',
  1: 'Approved',
  2: 'Dispatched',
  3: 'Cancelled',
};

export const purchaseOrderStatusLabels: Record<number, string> = {
  0: 'Draft',
  1: 'Approved',
  2: 'Partially Received',
  3: 'Received',
  4: 'Cancelled',
};

export const invoiceStatusLabels: Record<number, string> = {
  0: 'Issued',
  1: 'Paid',
  2: 'Cancelled',
};

export const financialStatusLabels: Record<number, string> = {
  0: 'Open',
  1: 'Partially Paid',
  2: 'Paid',
  3: 'Overdue',
  4: 'Cancelled',
};

export const stockMovementLabels: Record<number, string> = {
  0: 'Inbound',
  1: 'Outbound',
  2: 'Adjustment',
  3: 'Reservation',
  4: 'Release',
};

export const alertSeverityLabels: Record<number, string> = {
  0: 'Info',
  1: 'Warning',
  2: 'Critical',
};

export const alertStatusLabels: Record<number, string> = {
  0: 'Open',
  1: 'Resolved',
};

export const notificationSeverityLabels: Record<number, string> = {
  0: 'Info',
  1: 'Success',
  2: 'Warning',
  3: 'Critical',
};

export function formatCurrency(value?: number | null): string {
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    maximumFractionDigits: 2,
  }).format(value ?? 0);
}

export function formatDateTime(value?: string | Date | null): string {
  if (!value) {
    return '--';
  }

  const date = value instanceof Date ? value : new Date(value);
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date);
}

export function formatDateOnly(value?: string | Date | null): string {
  if (!value) {
    return '--';
  }

  const date = value instanceof Date ? value : new Date(value);
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'medium',
  }).format(date);
}

export function formatStatus(value: StatusValue | null | undefined, map?: Record<number, string>): string {
  if (value === null || value === undefined) {
    return '--';
  }

  if (typeof value === 'string' && Number.isNaN(Number(value))) {
    return value;
  }

  const numeric = Number(value);
  if (Number.isNaN(numeric)) {
    return String(value);
  }

  return map?.[numeric] ?? String(numeric);
}

export function toneForStatus(value: StatusValue | null | undefined, map?: Record<number, string>): string {
  const label = formatStatus(value, map).toLowerCase();

  if (label.includes('approved') || label.includes('received') || label.includes('paid') || label.includes('active') || label.includes('success') || label.includes('resolved')) {
    return 'tone-good';
  }

  if (label.includes('warning') || label.includes('partial') || label.includes('pending') || label.includes('overdue')) {
    return 'tone-warn';
  }

  if (label.includes('critical') || label.includes('cancelled') || label.includes('inactive') || label.includes('rejected')) {
    return 'tone-bad';
  }

  return 'tone-neutral';
}

export function branchScopedLabel(branchName?: string | null): string {
  return branchName?.trim() ? branchName : 'Global';
}
