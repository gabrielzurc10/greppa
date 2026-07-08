import { Injectable, computed, signal } from '@angular/core';

export type ThemeMode = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'greppa-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly media = matchMedia('(prefers-color-scheme: dark)');
  private readonly systemDark = signal(this.media.matches);

  readonly mode = signal<ThemeMode>(this.readStored());

  /** The theme actually in effect, after resolving 'system'. */
  readonly resolved = computed<'light' | 'dark'>(() => {
    const mode = this.mode();
    return mode === 'system' ? (this.systemDark() ? 'dark' : 'light') : mode;
  });

  constructor() {
    this.apply(this.mode());
    this.media.addEventListener('change', (e) => this.systemDark.set(e.matches));
  }

  set(mode: ThemeMode): void {
    // Stamp the DOM before signals fire so consumers reading CSS vars see the new theme.
    this.apply(mode);
    localStorage.setItem(STORAGE_KEY, mode);
    this.mode.set(mode);
  }

  private apply(mode: ThemeMode): void {
    if (mode === 'system') {
      delete document.documentElement.dataset['theme'];
    } else {
      document.documentElement.dataset['theme'] = mode;
    }
  }

  private readStored(): ThemeMode {
    const stored = localStorage.getItem(STORAGE_KEY);
    return stored === 'light' || stored === 'dark' ? stored : 'system';
  }
}
