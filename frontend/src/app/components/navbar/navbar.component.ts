import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  isMenuCollapsed = true;
  private authService = inject(AuthService);
  private router = inject(Router);

  toggleMenu(): void {
    this.isMenuCollapsed = !this.isMenuCollapsed;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return this.authService.isAuthenticated();
  }
}
