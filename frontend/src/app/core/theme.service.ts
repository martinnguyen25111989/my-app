import { Injectable, effect, signal } from '@angular/core';

const THEME_KEY = 'em_dark_mode';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly isDark = signal<boolean>(this.loadPreference());

  constructor() {
    effect(() => {
      document.body.classList.toggle('dark-theme', this.isDark());
      localStorage.setItem(THEME_KEY, String(this.isDark()));
    });
  }

  toggle(): void {
    this.isDark.update(v => !v);
  }

  private loadPreference(): boolean {
    const saved = localStorage.getItem(THEME_KEY);
    if (saved !== null) return saved === 'true';
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }
}
