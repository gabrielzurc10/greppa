export type Severity = 'critical' | 'high' | 'medium' | 'low' | 'info';

export type JobStatus = 'queued' | 'scanning' | 'enriching' | 'completed' | 'failed';

export interface ScanProgress {
  totalFindings: number;
  enriched: number;
}

export interface ScanSummary {
  critical: number;
  high: number;
  medium: number;
  low: number;
  info: number;
  total: number;
  filesScanned: number;
}

export interface Finding {
  id: string;
  severity: Severity;
  filePath: string;
  line: number;
  endLine: number;
  ruleId: string;
  message: string;
  reason: string;
  suggestedFix: string;
}

export interface ScanResult {
  summary: ScanSummary;
  findings: Finding[];
}

export interface ScanJob {
  jobId: string;
  status: JobStatus;
  progress: ScanProgress;
  error: string | null;
  result: ScanResult | null;
}

export const SEVERITY_ORDER: Severity[] = ['critical', 'high', 'medium', 'low', 'info'];
