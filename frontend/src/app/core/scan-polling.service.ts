import { Injectable, inject } from '@angular/core';
import { Observable, switchMap, takeWhile, timer } from 'rxjs';
import { ScanApiService } from './scan-api.service';
import { ScanJob } from './models';

const POLL_INTERVAL_MS = 2500;

@Injectable({ providedIn: 'root' })
export class ScanPollingService {
  private readonly api = inject(ScanApiService);

  /** Emits the job every poll until it reaches a terminal state (inclusive). */
  poll(jobId: string): Observable<ScanJob> {
    return timer(0, POLL_INTERVAL_MS).pipe(
      switchMap(() => this.api.getJob(jobId)),
      takeWhile((job) => job.status !== 'completed' && job.status !== 'failed', true),
    );
  }
}
