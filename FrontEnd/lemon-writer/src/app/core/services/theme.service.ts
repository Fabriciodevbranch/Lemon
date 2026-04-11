import { Injectable, signal } from '@angular/core';

export type ThemeName = 'cozy-amber' | 'cozy-forest' | 'cozy-night' | 'custom';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly THEME_KEY = 'lemon_writer_theme';
  private readonly CUSTOM_VARS_KEY = 'lemon_writer_custom_vars';

  currentTheme = signal<ThemeName>(this.loadTheme());

  applyTheme(theme: ThemeName): void {
    const body = document.body;
    body.classList.remove('theme-cozy-amber', 'theme-cozy-forest', 'theme-cozy-night', 'theme-custom');
    body.classList.add(`theme-${theme}`);
    localStorage.setItem(this.THEME_KEY, theme);
    this.currentTheme.set(theme);
  }

  setCustomVar(varName: string, value: string): void {
    document.documentElement.style.setProperty(varName, value);
    const vars = this.loadCustomVars();
    vars[varName] = value;
    localStorage.setItem(this.CUSTOM_VARS_KEY, JSON.stringify(vars));
  }

  loadSavedCustomVars(): void {
    const vars = this.loadCustomVars();
    for (const [key, value] of Object.entries(vars)) {
      document.documentElement.style.setProperty(key, value as string);
    }
  }

  private loadTheme(): ThemeName {
    return (localStorage.getItem(this.THEME_KEY) as ThemeName) || 'cozy-amber';
  }

  private loadCustomVars(): Record<string, string> {
    const raw = localStorage.getItem(this.CUSTOM_VARS_KEY);
    if (!raw) return {};
    try {
      return JSON.parse(raw);
    } catch {
      return {};
    }
  }
}
