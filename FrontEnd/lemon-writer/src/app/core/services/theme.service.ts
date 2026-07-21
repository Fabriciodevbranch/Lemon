import { HttpClient } from '@angular/common/http';
import { Injectable, effect, inject, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { environment } from '../../../environments/environment';

export type ThemeName = 'cozy-amber' | 'cozy-forest' | 'cozy-night' | 'custom';
interface AppearancePreference { theme?: ThemeName; customVariables: Record<string, string>; }

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly THEME_KEY = 'lemon_writer_theme';
  private readonly CUSTOM_VARS_KEY = 'lemon_writer_custom_vars';
  private readonly themes: ThemeName[] = ['cozy-amber', 'cozy-forest', 'cozy-night', 'custom'];
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private loadedUserId?: string;

  readonly currentTheme = signal<ThemeName>(this.loadTheme());

  constructor() {
    effect(() => {
      const user = this.auth.currentUser$();
      if (!user || !this.auth.hasValidToken()) { this.loadedUserId = undefined; return; }
      if (this.loadedUserId === user.id) return;
      this.loadedUserId = user.id;
      this.loadAccountPreference();
    });
  }

  initializeLocal(): void {
    this.applyTheme(this.currentTheme(), false);
    this.applyCustomVars(this.loadCustomVars());
  }

  applyTheme(theme: ThemeName, persist = true): void {
    if (!this.themes.includes(theme)) theme = 'cozy-amber';
    document.body.classList.remove('theme-cozy-amber', 'theme-cozy-forest', 'theme-cozy-night', 'theme-custom');
    document.body.classList.add(`theme-${theme}`);
    localStorage.setItem(this.THEME_KEY, theme);
    this.currentTheme.set(theme);
    if (persist) this.persistAccountPreference();
  }

  setCustomVar(varName: string, value: string): void {
    document.documentElement.style.setProperty(varName, value);
    const vars = this.loadCustomVars();
    vars[varName] = value;
    localStorage.setItem(this.CUSTOM_VARS_KEY, JSON.stringify(vars));
    this.persistAccountPreference();
  }

  loadSavedCustomVars(): void { this.applyCustomVars(this.loadCustomVars()); }

  private loadAccountPreference(): void {
    this.http.get<AppearancePreference>(`${environment.apiUrl}/appearance`).subscribe({
      next: preference => {
        if (!preference.theme) { this.persistAccountPreference(); return; }
        localStorage.setItem(this.CUSTOM_VARS_KEY, JSON.stringify(preference.customVariables ?? {}));
        this.applyCustomVars(preference.customVariables ?? {});
        this.applyTheme(preference.theme, false);
      },
      error: () => { /* Keep the local preference if account synchronization is unavailable. */ }
    });
  }

  private persistAccountPreference(): void {
    if (!this.auth.isLoggedIn) return;
    const preference: AppearancePreference = { theme: this.currentTheme(), customVariables: this.loadCustomVars() };
    this.http.put<void>(`${environment.apiUrl}/appearance`, preference).subscribe({
      error: () => { /* Local storage remains the fallback when persistence is temporarily unavailable. */ }
    });
  }

  private applyCustomVars(vars: Record<string, string>): void {
    for (const [key, value] of Object.entries(vars)) document.documentElement.style.setProperty(key, value);
  }

  private loadTheme(): ThemeName {
    const theme = localStorage.getItem(this.THEME_KEY) as ThemeName | null;
    return theme && this.themes.includes(theme) ? theme : 'cozy-amber';
  }

  private loadCustomVars(): Record<string, string> {
    const raw = localStorage.getItem(this.CUSTOM_VARS_KEY);
    if (!raw) return {};
    try { return JSON.parse(raw) as Record<string, string>; } catch { return {}; }
  }
}
