import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { ThemePickerComponent } from '../../shared/components/theme-picker/theme-picker.component';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, NavbarComponent, ThemePickerComponent, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatDividerModule, MatSnackBarModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss'
})
export class SettingsComponent {
  private fb = inject(FormBuilder);
  private snackBar = inject(MatSnackBar);

  profileForm: FormGroup = this.fb.group({
    displayName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]]
  });

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.snackBar.open('Profile updated!', 'Dismiss', { duration: 3000 });
  }
}
