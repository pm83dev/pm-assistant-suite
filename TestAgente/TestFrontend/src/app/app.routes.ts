import { Routes } from '@angular/router';
import { ClientiComponent } from './pages/clienti/clienti.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { NoteComponent } from './pages/note/note.component';
import { OreComponent } from './pages/ore/ore.component';
import { ProgettiComponent } from './pages/progetti/progetti.component';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'clienti', component: ClientiComponent },
  { path: 'progetti', component: ProgettiComponent },
  { path: 'ore', component: OreComponent },
  { path: 'note', component: NoteComponent },
];
