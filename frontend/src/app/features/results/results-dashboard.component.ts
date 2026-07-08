import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ScanResult } from '../../core/models';
import { SeverityChartComponent } from './severity-chart.component';
import { FindingsTableComponent } from './findings-table.component';

@Component({
  selector: 'app-results-dashboard',
  imports: [SeverityChartComponent, FindingsTableComponent],
  templateUrl: './results-dashboard.component.html',
  styleUrl: './results-dashboard.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultsDashboardComponent {
  readonly result = input.required<ScanResult>();
  readonly newScan = output<void>();
}
