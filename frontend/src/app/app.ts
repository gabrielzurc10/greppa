import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { ScanApiService } from './core/scan-api.service';
import { ScanPollingService } from './core/scan-polling.service';
import { ThemeService } from './core/theme.service';
import { ScanJob } from './core/models';
import { selectScannableFiles } from './core/upload-filter';
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
  protected readonly theme = inject(ThemeService);
  private pollSub: Subscription | null = null;

  protected readonly job = signal<ScanJob | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly phase = signal<Phase>('idle');
  protected readonly result = computed(() => this.job()?.result ?? null);

  constructor() {
    // The dot-grid background lives on <body>; let it "brew" while scanning.
    effect(() => {
      document.body.classList.toggle('scanning', this.phase() === 'running');
    });
  }

  protected onFilesSelected(files: File[]): void {
    this.error.set(null);
    this.job.set(null);

    const selection = selectScannableFiles(files);
    if (selection.error) {
      this.fail(selection.error);
      return;
    }

    this.phase.set('running');

    this.api.startScan(selection.files).subscribe({
      next: ({ jobId }) => this.pollJob(jobId),
      error: (err) => this.fail(this.uploadErrorMessage(err)),
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

  private uploadErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 413) {
      return 'That upload is too large. The limit is 50 MB in total and 1 MB per file — try leaving out build output and dependency folders like node_modules.';
    }
    if (err.status === 0) {
      return 'Could not reach the scanner. Please check your connection and try again.';
    }
    return err.error?.error ?? 'Upload failed. Please try again.';
  }

  private fail(message: string): void {
    this.error.set(message);
    this.phase.set('error');
  }
}
