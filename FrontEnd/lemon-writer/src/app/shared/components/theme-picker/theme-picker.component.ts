import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { ThemeService, ThemeName } from '../../../core/services/theme.service';
import { FormsModule } from '@angular/forms';

interface CustomColor {
  varName: string;
  label: string;
  default: string;
}

@Component({
  selector: 'app-theme-picker',
  standalone: true,
  imports: [MatButtonToggleModule, MatIconModule, FormsModule],
  templateUrl: './theme-picker.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './theme-picker.component.scss'
})
export class ThemePickerComponent {
  private themeService = inject(ThemeService);
  currentTheme = this.themeService.currentTheme;

  themes: { value: ThemeName; label: string; icon: string }[] = [
    { value: 'cozy-amber', label: 'Cozy Amber', icon: '🕯️' },
    { value: 'cozy-forest', label: 'Cozy Forest', icon: '🌿' },
    { value: 'cozy-night', label: 'Cozy Night', icon: '🌙' },
    { value: 'custom', label: 'Custom', icon: '🎨' }
  ];

  customColors: CustomColor[] = [
    { varName: '--custom-bg-color', label: 'Background', default: '#f5f5f5' },
    { varName: '--custom-surface-color', label: 'Surface', default: '#ffffff' },
    { varName: '--custom-primary-color', label: 'Primary', default: '#6200ea' },
    { varName: '--custom-primary-light', label: 'Primary Light', default: '#ede7f6' },
    { varName: '--custom-text-primary', label: 'Text Primary', default: '#212121' },
    { varName: '--custom-text-secondary', label: 'Text Secondary', default: '#757575' },
    { varName: '--custom-border-color', label: 'Border', default: '#e0e0e0' },
  ];

  colorValues: Record<string, string> = this.initColorValues();

  selectTheme(theme: ThemeName): void {
    this.themeService.applyTheme(theme);
  }

  onColorChange(varName: string, value: string): void {
    this.colorValues[varName] = value;
    this.themeService.setCustomVar(varName, value);
    if (this.currentTheme() !== 'custom') {
      this.themeService.applyTheme('custom');
    }
  }

  private initColorValues(): Record<string, string> {
    const values: Record<string, string> = {};
    for (const color of this.customColors) {
      const saved = getComputedStyle(document.documentElement).getPropertyValue(color.varName).trim();
      values[color.varName] = saved || color.default;
    }
    return values;
  }
}
