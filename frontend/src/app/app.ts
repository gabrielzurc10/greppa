import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { ScanApiService } from './core/scan-api.service';
import { ScanPollingService } from './core/scan-polling.service';
import { ScanJob } from './core/models';
import { UploadComponent } from './features/upload/upload.component';
import { ScanProgressComponent } from './features/progress/scan-progress.component';
import { ResultsDashboardComponent } from './features/results/results-dashboard.component';

type Phase = 'idle' | 'running' | 'done' | 'error';

@Component({
  selector: 'app-root',
  imports: [UploadComponent, ScanProgressComponent, ResultsDashboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly api = inject(ScanApiService);
  private readonly polling = inject(ScanPollingService);
  private pollSub: Subscription | null = null;

  protected readonly job = signal<ScanJob | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly phase = signal<Phase>('idle');
  protected readonly result = computed(() => this.job()?.result ?? null);

  protected onFilesSelected(files: File[]): void {
    this.error.set(null);
    this.job.set(null);
    this.phase.set('running');

    this.api.startScan(files).subscribe({
      next: ({ jobId }) => this.pollJob(jobId),
      error: (err) => this.fail(err?.error?.error ?? 'Upload failed. Please try again.'),
    });
  }

  protected reset(): void {
    this.pollSub?.unsubscribe();
    this.job.set(null);
    this.error.set(null);
    this.phase.set('idle');
  }

  private pollJob(jobId: string): void {
    this.pollSub = this.polling.poll(jobId).subscribe({
      next: (job) => {
        this.job.set(job);
        if (job.status === 'completed') {
          this.phase.set('done');
        } else if (job.status === 'failed') {
          this.fail(job.error ?? 'The scan failed. Please try again.');
        }
      },
      error: () => this.fail('Lost connection to the scanner. Please try again.'),
    });
  }

  private fail(message: string): void {
    this.error.set(message);
    this.phase.set('error');
  }
}
