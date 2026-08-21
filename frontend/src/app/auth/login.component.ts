import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="login-page d-flex align-items-center justify-content-center">
      <div class="card login-card shadow-lg">
        <div class="card-body p-4 p-md-5">
          <div class="text-center mb-4">
            <div class="login-icon mx-auto mb-3">
              <svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <circle cx="12" cy="12" r="9"></circle>
                <polyline points="12 7 12 12 15.5 14"></polyline>
              </svg>
            </div>
            <h1 class="h4 mb-1">Ore Tracking</h1>
            <p class="text-muted small mb-0">Accedi per gestire le tue attività</p>
          </div>

          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>
            <div class="form-floating mb-3">
              <input
                id="username"
                formControlName="username"
                type="text"
                class="form-control"
                placeholder="Username"
                autocomplete="username"
              />
              <label for="username">Username</label>
            </div>

            <div class="form-floating mb-3">
              <input
                id="password"
                formControlName="password"
                type="password"
                class="form-control"
                placeholder="Password"
                autocomplete="current-password"
              />
              <label for="password">Password</label>
            </div>

            <div *ngIf="error" class="alert alert-danger py-2 small mb-3">
              {{ error }}
            </div>

            <button
              type="submit"
              class="btn btn-primary w-100 py-2"
              [disabled]="loginForm.invalid || loading"
            >
              <span *ngIf="loading" class="spinner-border spinner-border-sm me-2"></span>
              {{ loading ? 'Accesso in corso...' : 'Accedi' }}
            </button>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .login-page {
        min-height: 100vh;
        padding: 1.5rem;
      }

      .login-card {
        width: 100%;
        max-width: 400px;
        border: 1px solid #333;
        border-radius: 16px;
      }

      .login-icon {
        width: 56px;
        height: 56px;
        display: flex;
        align-items: center;
        justify-content: center;
        border-radius: 50%;
        background: rgba(13, 110, 253, 0.15);
        color: #6ea8fe;
        font-size: 1.5rem;
      }
    `,
  ],
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);

  loginForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  error = '';
  loading = false;

  onSubmit() {
    if (this.loginForm.valid) {
      const { username, password } = this.loginForm.value;
      if (username && password) {
        this.error = '';
        this.loading = true;
        this.authService.login(username, password).subscribe({
          next: () => {
            const redirectUrl = this.route.snapshot.queryParams['redirect'] || '/dashboard';
            this.router.navigate([redirectUrl]);
          },
          error: (err) => {
            console.error('Login failed', err);
            this.error = 'Credenziali non valide';
            this.loading = false;
          },
        });
      }
    }
  }
}
