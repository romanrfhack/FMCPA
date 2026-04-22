export interface PlatformHealthResponse {
  status: string;
  timestampUtc: string;
  totalDuration: string;
  checks: PlatformHealthCheck[];
}

export interface PlatformHealthCheck {
  name: string;
  status: string;
  description?: string | null;
  duration: string;
}
