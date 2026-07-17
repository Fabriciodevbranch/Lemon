import { Component, inject, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { SnapshotCardComponent } from './snapshot-card/snapshot-card.component';
import { CompareViewComponent } from './compare-view/compare-view.component';
import { SnapshotsService } from '../../core/services/snapshots.service';
import { Snapshot } from '../../core/models/snapshot.model';

@Component({
  selector: 'app-timeline',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, NavbarComponent, SnapshotCardComponent, CompareViewComponent],
  templateUrl: './timeline.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './timeline.component.scss'
})
export class TimelineComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private snapshotsService = inject(SnapshotsService);
  private snackBar = inject(MatSnackBar);

  bookId = this.route.snapshot.paramMap.get('bookId')!;
  chapterId = this.route.snapshot.paramMap.get('chapterId')!;

  snapshots = signal<Snapshot[]>([]);
  loading = signal(true);
  selectedForCompare = signal<Snapshot[]>([]);
  comparingSnapshot = signal<Snapshot | null>(null);
  compareMode = signal(false);

  ngOnInit(): void {
    this.snapshotsService.getSnapshots(this.bookId, this.chapterId).subscribe({
      next: (snaps) => { this.snapshots.set(snaps); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleCompareMode(): void {
    this.compareMode.update(v => !v);
    this.selectedForCompare.set([]);
  }

  onSelectForCompare(snapshot: Snapshot): void {
    const current = this.selectedForCompare();
    if (current.find(s => s.id === snapshot.id)) {
      this.selectedForCompare.update(list => list.filter(s => s.id !== snapshot.id));
    } else if (current.length < 2) {
      this.selectedForCompare.update(list => [...list, snapshot]);
    }
  }

  isSelectedForCompare(snapshot: Snapshot): boolean {
    return !!this.selectedForCompare().find(s => s.id === snapshot.id);
  }

  canCompare(): boolean {
    return this.selectedForCompare().length === 2;
  }

  restoreVersion(snapshot: Snapshot): void {
    if (!confirm(`Restore to version: "${snapshot.message}"? This will overwrite the current chapter content.`)) return;
    this.snapshotsService.restoreToVersion(this.bookId, this.chapterId, snapshot.id).subscribe({
      next: () => this.snackBar.open('Restored to this version!', 'Dismiss', { duration: 3000 }),
      error: () => this.snackBar.open('Could not restore version.', 'Dismiss', { duration: 3000 })
    });
  }

  grabContent(snapshot: Snapshot): void {
    this.snapshotsService.grabContentFromVersion(this.bookId, this.chapterId, snapshot.id).subscribe({
      next: () => this.snackBar.open('Content grabbed and applied to editor!', 'Dismiss', { duration: 3000 }),
      error: () => this.snackBar.open('Could not grab content.', 'Dismiss', { duration: 3000 })
    });
  }
}
