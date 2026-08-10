// Mirror of SMPP.Web.Services.ChartColors - keep the two in step so the Dashboard and History
// charts render the same status/source in the same color everywhere.
import type { Schemas } from '../../core/api/api.types';

type MessageStatus = Schemas['MessageStatus'];
type MessageSource = Schemas['MessageSource'];

const STATUS_COLORS: Record<MessageStatus, string> = {
  Delivered: '#0ca30c',
  Sent: '#2a78d6',
  Processing: '#fab219',
  Expired: '#4a3aa7',
  Undelivered: '#ec835a',
  Failed: '#d03b3b',
};

const SOURCE_COLORS: Record<MessageSource, string> = {
  QuickSend: '#2a78d6',
  BulkSend: '#eb6834',
  PublicApi: '#1baf7a',
};

const SOURCE_LABELS: Record<MessageSource, string> = {
  QuickSend: 'Quick Send',
  BulkSend: 'Bulk Send',
  PublicApi: 'Public API',
};

export function statusColor(status: MessageStatus): string {
  return STATUS_COLORS[status] ?? '#898781';
}

export function sourceColor(source: MessageSource): string {
  return SOURCE_COLORS[source] ?? '#898781';
}

export function sourceLabel(source: MessageSource): string {
  return SOURCE_LABELS[source] ?? source;
}
