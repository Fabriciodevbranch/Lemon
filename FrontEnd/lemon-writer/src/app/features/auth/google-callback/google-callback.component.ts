import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-google-callback',
  template: '<main class="callback"><div class="pulse">L</div><h1>Preparing your writing desk…</h1><p>Securely completing Google sign-in.</p></main>',
  styles: [`.callback{min-height:100dvh;display:grid;place-content:center;text-align:center;background:#f7f2e9;color:#332a24}.pulse{display:grid;place-items:center;width:64px;height:64px;margin:0 auto 20px;border-radius:50%;background:#e5b84b;color:#26382f;font:700 2rem var(--font-heading);animation:pulse 1.3s infinite}.callback h1{margin:0;font-size:2.2rem}.callback p{color:#756960}@keyframes pulse{50%{transform:scale(1.08);box-shadow:0 0 0 14px rgba(229,184,75,.16)}}`],
  changeDetection: ChangeDetectionStrategy.Eager
})
export class GoogleCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auth = inject(AuthService);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const token = params.get('token');
    const id = params.get('id');
    const email = params.get('email');
    const displayName = params.get('displayName');
    if (!token || !id || !email || !displayName) {
      this.router.navigate(['/auth/login'], { queryParams: { error: 'google' } });
      return;
    }

    this.auth.handleGoogleCallback(token, {
      id, email, displayName, createdAt: new Date().toISOString()
    });
    this.router.navigate(['/library']);
  }
}
