import type { UserRole } from '../../core/api/api.types';

export interface NavItem {
  labelKey: string;
  route: string;
  icon: string;
}

export interface NavSection {
  titleKey: string;
  items: NavItem[];
  /** Restricts the whole section to one role; omitted means visible to both. */
  onlyRole?: UserRole;
}

export const NAV_SECTIONS: NavSection[] = [
  {
    titleKey: 'Overview',
    items: [{ labelKey: 'Dashboard', route: '/dashboard', icon: 'dashboard' }],
  },
  {
    titleKey: 'Messaging',
    items: [
      { labelKey: 'Quick Send', route: '/quick-send', icon: 'quickSend' },
      { labelKey: 'Bulk Send', route: '/bulk-send', icon: 'bulkSend' },
      { labelKey: 'Campaigns', route: '/campaigns', icon: 'campaigns' },
      { labelKey: 'Scheduled Sends', route: '/scheduled-sends', icon: 'clock' },
      { labelKey: 'History', route: '/history', icon: 'history' },
      { labelKey: 'Reports', route: '/reports', icon: 'reports' },
    ],
  },
  {
    titleKey: 'CRM',
    onlyRole: 'Account',
    items: [{ labelKey: 'Customers', route: '/customers', icon: 'customers' }],
  },
  {
    titleKey: 'Billing',
    onlyRole: 'Account',
    items: [{ labelKey: 'Request Credits', route: '/payments', icon: 'credits' }],
  },
  {
    titleKey: 'Administration',
    onlyRole: 'Superadmin',
    items: [
      { labelKey: 'Accounts', route: '/accounts', icon: 'accounts' },
      { labelKey: 'Credit Requests', route: '/payments/review', icon: 'inbox' },
      { labelKey: 'Content Filter', route: '/spam-keywords', icon: 'shield' },
      { labelKey: 'Admin Budget', route: '/admin-budget', icon: 'wallet' },
      { labelKey: 'Sending Window', route: '/sending-window', icon: 'clock' },
      { labelKey: 'Accounts Logs', route: '/accounts-logs', icon: 'logs' },
    ],
  },
];
