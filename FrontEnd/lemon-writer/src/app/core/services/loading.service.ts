import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  readonly visible = signal(false);
  private pending = 0;
  private delay?: ReturnType<typeof setTimeout>;

  begin(): void {
    this.pending++;
    if (this.pending === 1) this.delay = setTimeout(() => this.visible.set(true), 140);
  }

  end(): void {
    this.pending = Math.max(0, this.pending - 1);
    if (this.pending) return;
    if (this.delay) clearTimeout(this.delay);
    this.delay = undefined;
    this.visible.set(false);
  }
}
