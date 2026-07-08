import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ScanJob } from '../../core/models';

@Component({
  selector: 'app-scan-progress',
  templateUrl: './scan-progress.component.html',
  styleUrl: './scan-progress.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScanProgressComponent {
  readonly job = input.required<ScanJob | null>();

  protected readonly label = computed(() => {
    const job = this.job();
    switch (job?.status) {
      case 'enriching': {
        const { enriched, totalFindings } = job.progress;
        return `Explaining findings with AI (${enriched}/${totalFindings})`;
      }
      case 'scanning':
        return 'Scanning with Semgrep…';
      default:
        return 'Starting the scanner… (this can take up to 30 seconds after idle)';
    }
  });

  protected readonly percent = computed(() => {
    const job = this.job();
    if (job?.status !== 'enriching' || job.progress.totalFindings === 0) {
      return null;
    }
    return Math.round((job.progress.enriched / job.progress.totalFindings) * 100);
  });
}
