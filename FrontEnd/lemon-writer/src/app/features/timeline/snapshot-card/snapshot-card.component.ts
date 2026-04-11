import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TimeAgoPipe } from '../../../shared/pipes/time-ago.pipe';
import { Snapshot } from '../../../core/models/snapshot.model';

@Component({
  selector: 'app-snapshot-card',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatTooltipModule, TimeAgoPipe],
  templateUrl: './snapshot-card.component.html',
  styleUrl: './snapshot-card.component.scss'
})
export class SnapshotCardComponent {
  @Input({ required: true }) snapshot!: Snapshot;
  @Input() isSelected = false;
  @Input() compareMode = false;
  @Output() viewVersion = new EventEmitter<Snapshot>();
  @Output() restoreVersion = new EventEmitter<Snapshot>();
  @Output() grabContent = new EventEmitter<Snapshot>();
  @Output() selectForCompare = new EventEmitter<Snapshot>();
}
