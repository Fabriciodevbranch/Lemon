import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/library', pathMatch: 'full' },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      }
    ]
  },
  {
    path: 'library',
    canActivate: [authGuard],
    loadComponent: () => import('./features/library/library.component').then(m => m.LibraryComponent)
  },
  {
    path: 'books/:bookId/settings',
    canActivate: [authGuard],
    loadComponent: () => import('./features/book-settings/book-settings.component').then(m => m.BookSettingsComponent)
  },
  {
    path: 'books/:bookId/chapters/:chapterId/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/editor/editor.component').then(m => m.EditorComponent)
  },
  {
    path: 'books/:bookId/chapters/:chapterId/timeline',
    canActivate: [authGuard],
    loadComponent: () => import('./features/timeline/timeline.component').then(m => m.TimelineComponent)
  },
  {
    path: 'books/:bookId/chapters/:chapterId/drafts',
    canActivate: [authGuard],
    loadComponent: () => import('./features/drafts/drafts.component').then(m => m.DraftsComponent)
  },
  {
    path: 'books/:bookId/chapters/:chapterId/drafts/:draftId/edit',
    canActivate: [authGuard],
    loadComponent: () => import('./features/drafts/draft-editor/draft-editor.component').then(m => m.DraftEditorComponent)
  },
  {
    path: 'settings',
    canActivate: [authGuard],
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent)
  },
  { path: '**', redirectTo: '/library' }
];
