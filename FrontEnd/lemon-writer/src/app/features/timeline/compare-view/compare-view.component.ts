import { Component, Input, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SnapshotsService } from '../../../core/services/snapshots.service';
import { Snapshot, SnapshotDiff } from '../../../core/models/snapshot.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-compare-view',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './compare-view.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './compare-view.component.scss'
})
export class CompareViewComponent implements OnInit {
  @Input({ required: true }) bookId!: string;
  @Input({ required: true }) chapterId!: string;
  @Input({ required: true }) snapshotA!: Snapshot;
  @Input({ required: true }) snapshotB!: Snapshot;

  private snapshotsService = inject(SnapshotsService);

  diff = signal<SnapshotDiff | null>(null);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.snapshotsService.compareVersions(
      this.bookId, this.chapterId, this.snapshotA.id, this.snapshotB.id
    ).subscribe({
      next: (diff) => { this.diff.set(diff); this.loading.set(false); },
      error: () => { this.error.set('Could not compare versions.'); this.loading.set(false); }
    });
  }
}
