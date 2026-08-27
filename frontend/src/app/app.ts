import { Component } from '@angular/core';
import { RouterOutlet, RouterLinkActive } from '@angular/router';
import { NavbarComponent } from './components/navbar/navbar.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NavbarComponent, RouterOutlet],
  template: `
    <app-navbar></app-navbar>
    <main class="container py-4">
      <router-outlet></router-outlet>
    </main>
  `,
})
export class AppComponent { }
