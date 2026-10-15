import { EnvironmentInjector, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter, take } from 'rxjs';
import { ClienteManagementService } from './services/clienti/clienti-management.service';
import { ProjectManagementService } from './services/progetti/progetti-management.service';
import { OreManagementService } from './services/ore/ore-management.service';
import { NoteManagementService } from './services/note/note-management.service';
import { DashboardManagementService } from './services/dashboard/dashboard-management.service';

/**
 * APP_INITIALIZER che collega il cambio di pagina al reload dei dati.
 *
 * I servizi di gestione sono singleton root: caricano i dati una sola volta
 * all'avvio. Senza questo listener, al cambio di pagina (es. Dashboard ->
 * Progetti) la pagina mostrerebbe lo stato vuoto/vecchio in memoria.
 *
 * Al primo avvio l'inizializzatore non fa nulla (i servizi si caricano da soli
 * nel costruttore): attende il primo NavigationEnd, poi si aggancia e ad ogni
 * successivo NavigationEnd richiama loadData() di ogni servizio, così i dati
 * sono sempre freschi quando l'utente entra in una pagina.
 *
 * Il listener è unico e viene rimosso dalla funzione di cleanup restituita,
 * evitando leak in test/e2e.
 */
export function initPageReload(): () => void {
  const injector = inject(EnvironmentInjector);
  const router = injector.get(Router);
  const clientiManagement = injector.get(ClienteManagementService);
  const progettiManagement = injector.get(ProjectManagementService);
  const oreManagement = injector.get(OreManagementService);
  const noteManagement = injector.get(NoteManagementService);
  const dashboardManagement = injector.get(DashboardManagementService);

  const reloadAll = (): void => {
    clientiManagement.loadData();
    progettiManagement.loadData();
    oreManagement.loadData();
    noteManagement.loadData();
    dashboardManagement.loadData();
  };

  const events$ = router.events.pipe(
    filter((e): e is NavigationEnd => e instanceof NavigationEnd),
  );

  let reloadSub: { unsubscribe(): void } | null = null;

  // Aspetta il primo NavigationEnd (fine della navigazione iniziale) prima di
  // agganciare l'listener, così non duplica il caricamento fatto dai
  // costruttori dei servizi all'avvio.
  const initSub = events$.pipe(take(1)).subscribe(() => {
    reloadSub = events$.subscribe(reloadAll);
  });

  // L'inizializzatore completa subito (non blocca il boot dell'app): il
  // listener si attiva autonomamente al primo NavigationEnd. La cleanup
  // restituita disiscrive sia l'attesa iniziale sia (se già attivo) il
  // listener di reload, evitando leak in test/e2e.
  return () => {
    initSub.unsubscribe();
    reloadSub?.unsubscribe();
  };
}
