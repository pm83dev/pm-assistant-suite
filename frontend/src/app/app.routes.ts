import { Routes } from '@angular/router';
import { ClientiComponent } from './pages/clienti/clienti.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { NoteComponent } from './pages/note/note.component';
import { OreComponent } from './pages/ore/ore.component';
import { ProgettiComponent } from './pages/progetti/progetti.component';
import { LoginComponent } from './auth/login.component';
import { ChatComponent } from './components/chat/chat.component';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },
  {
    path: 'clienti',
    component: ClientiComponent,
    canActivate: [authGuard]
  },
  {
    path: 'progetti',
    component: ProgettiComponent,
    canActivate: [authGuard]
  },
  {
    path: 'ore',
    component: OreComponent,
    canActivate: [authGuard]
  },
  {
    path: 'note',
    component: NoteComponent,
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    component: ChatComponent,
    canActivate: [authGuard]
  },
];
