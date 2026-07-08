import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ConfigService } from './config.service';
import { ScanJob } from './models';

@Injectable({ providedIn: 'root' })
export class ScanApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(ConfigService);

  startScan(files: File[]): Observable<{ jobId: string }> {
    const formData = new FormData();
    for (const file of files) {
      formData.append('files', file, (file as any).webkitRelativePath || file.name);
    }
    return this.http.post<{ jobId: string }>(`${this.config.apiBaseUrl}/api/scans`, formData);
  }

  getJob(jobId: string): Observable<ScanJob> {
    return this.http.get<ScanJob>(`${this.config.apiBaseUrl}/api/scans/${jobId}`);
  }
}
