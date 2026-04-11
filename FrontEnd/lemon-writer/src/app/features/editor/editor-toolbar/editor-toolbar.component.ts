import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-editor-toolbar',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, MatTooltipModule, MatDividerModule],
  templateUrl: './editor-toolbar.component.html',
  styleUrl: './editor-toolbar.component.scss'
})
export class EditorToolbarComponent {
  @Input() wordCount = 0;
  @Input() autoSaveStatus: 'saved' | 'saving' | 'unsaved' = 'saved';
  @Output() takeSnapshot = new EventEmitter<void>();
  @Output() seeTimeline = new EventEmitter<void>();
  @Output() seeDrafts = new EventEmitter<void>();
}
