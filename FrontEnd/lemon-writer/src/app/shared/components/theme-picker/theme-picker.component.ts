import { Component, inject } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { ThemeService, ThemeName } from '../../../core/services/theme.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-theme-picker',
  standalone: true,
  imports: [MatButtonToggleModule, MatIconModule, FormsModule],
  templateUrl: './theme-picker.component.html',
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

  selectTheme(theme: ThemeName): void {
    this.themeService.applyTheme(theme);
  }
}
