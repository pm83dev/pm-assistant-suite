import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { ClientiComponent } from './pages/clienti/clienti.component';
import { ProgettiComponent } from './pages/progetti/progetti.component';
import { OreComponent } from './pages/ore/ore.component';
import { NoteComponent } from './pages/note/note.component';
import { LoginComponent } from './auth/login.component';

const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'clienti', component: ClientiComponent },
  { path: 'progetti', component: ProgettiComponent },
  { path: 'ore', component: OreComponent },
  { path: 'note', component: NoteComponent },
  { path: 'login', component: LoginComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }