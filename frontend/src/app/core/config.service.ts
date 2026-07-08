import { Injectable } from '@angular/core';

interface AppConfig {
  apiBaseUrl: string;
}

/** Loads runtime configuration (written at deploy time) before the app starts. */
@Injectable({ providedIn: 'root' })
export class ConfigService {
  private config: AppConfig = { apiBaseUrl: '' };

  get apiBaseUrl(): string {
    return this.config.apiBaseUrl;
  }

  async load(): Promise<void> {
    try {
      const response = await fetch('config.json');
      if (response.ok) {
        this.config = { ...this.config, ...(await response.json()) };
      }
    } catch {
      // keep defaults: same-origin (dev proxy) API
    }
  }
}
