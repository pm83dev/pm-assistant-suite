import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute, convertToParamMap } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="login-container">
      <h2>Login</h2>
      <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
        <div>
          <label for="username">Username</label>
          <input id="username" formControlName="username" type="text" required />
        </div>
        <div>
          <label for="password">Password</label>
          <input id="password" formControlName="password" type="password" required />
        </div>
        <button type="submit" [disabled]="loginForm.invalid">Login</button>
      </form>
      <p *ngIf="error" class="error">{{ error }}</p>
      <p>Don't have an account? <a routerLink="/register">Register</a></p>
    </div>
  `,
  styles: [
    `
      .login-container {
        max-width: 400px;
        margin: 50px auto;
        padding: 20px;
        border: 1px solid #ccc;
        border-radius: 8px;
      }
      .error {
        color: red;
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

  onSubmit() {
    if (this.loginForm.valid) {
      const { username, password } = this.loginForm.value;
      if (username && password) {
        this.authService.login(username, password).subscribe({
          next: () => {
            const redirectUrl = this.route.snapshot.queryParams['redirect'] || '/dashboard';
            this.router.navigate([redirectUrl]);
          },
          error: (err) => {
            console.error('Login failed', err);
            this.error = 'Credenziali non valide';
          },
        });
      }
    }
  }
}
