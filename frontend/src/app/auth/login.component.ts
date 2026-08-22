import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute, convertToParamMap } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
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
