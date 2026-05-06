export interface SecurityActivitySummary {
  generatedAtUtc: string;
  sinceUtc: string;
  recentSecurityEventCount: number;
  loginSucceededCount: number;
  loginFailedCount: number;
  lockoutDeniedCount: number;
  temporaryLockoutCount: number;
  lockoutResetCount: number;
  passwordResetCount: number;
  roleChangedCount: number;
  userDeactivatedCount: number;
  userReactivatedCount: number;
  activeLockedUserCount: number;
  usersWithFailedAttemptsCount: number;
}

export interface SecurityAuditEvent {
  id: string;
  occurredUtc: string;
  actionType: string;
  title: string;
  detail: string;
  entityType: string;
  entityId: string;
  reference: string | null;
  navigationPath: string;
  metadataJson: string | null;
}

export interface LockedApplicationUser {
  id: string;
  userName: string;
  displayName: string;
  roleCode: string;
  isActive: boolean;
  accessFailedCount: number;
  lockoutEndUtc: string | null;
  lastLoginUtc: string | null;
}

export interface SecurityEventsFilter {
  userName?: string;
  eventType?: string;
  fromUtc?: string;
  toUtc?: string;
  take?: number;
}
