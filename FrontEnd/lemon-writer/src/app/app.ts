import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.scss'
})
export class App implements OnInit {
  private themeService = inject(ThemeService);

  ngOnInit(): void {
    const theme = this.themeService.currentTheme();
    this.themeService.applyTheme(theme);
    this.themeService.loadSavedCustomVars();
  }
}
