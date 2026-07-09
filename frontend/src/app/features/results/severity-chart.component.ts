import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ScanSummary, SEVERITY_ORDER, Severity } from '../../core/models';

const SEVERITY_LABELS: Record<Severity, string> = {
  critical: 'Critical',
  high: 'High',
  medium: 'Medium',
  low: 'Low',
  info: 'Info',
};

interface SeverityRow {
  severity: Severity;
  label: string;
  count: number;
  cells: number[];
}

@Component({
  selector: 'app-severity-chart',
  template: `
    <div class="rows" role="img" aria-label="Findings by severity">
      @for (row of rows(); track row.severity) {
        <div class="row" [style.--sev]="'var(--sev-' + row.severity + ')'">
          <span class="label">{{ row.label }}</span>
          <span class="track" aria-hidden="true">
            @for (cell of row.cells; track $index) {
              <span class="cell"></span>
            }
          </span>
          <span class="count">{{ row.count }}</span>
        </div>
      }
    </div>
  `,
  styles: `
    .rows {
      display: flex;
      flex-direction: column;
      gap: 0.6rem;
      font-family: var(--font-mono);
    }

    .row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .label {
      width: 4.5rem;
      flex-shrink: 0;
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--text-primary);
    }

    .track {
      display: flex;
      flex-wrap: wrap;
      gap: 3px;
      flex: 1;
    }

    .cell {
      width: 12px;
      height: 16px;
      border-radius: 2px;
      background: var(--sev);
    }

    .count {
      min-width: 2.5ch;
      flex-shrink: 0;
      text-align: right;
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--text-primary);
      font-variant-numeric: tabular-nums;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SeverityChartComponent {
  readonly summary = input.required<ScanSummary>();

  protected readonly rows = computed<SeverityRow[]>(() => {
    const summary = this.summary();
    return SEVERITY_ORDER.map((severity) => ({
      severity,
      label: SEVERITY_LABELS[severity],
      count: summary[severity],
      cells: Array.from({ length: summary[severity] }, (_, i) => i),
    }));
  });
}
