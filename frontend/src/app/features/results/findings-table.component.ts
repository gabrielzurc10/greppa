import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Finding } from '../../core/models';

@Component({
  selector: 'app-findings-table',
  templateUrl: './findings-table.component.html',
  styleUrl: './findings-table.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FindingsTableComponent {
  readonly findings = input.required<Finding[]>();
}
