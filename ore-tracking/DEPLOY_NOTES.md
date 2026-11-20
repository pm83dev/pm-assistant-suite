# Deploy Ore-Tracking su IIS

## File da copiare sul server
Copiare **tutta** la cartella `publish/` in `C:\inetpub\ore-tracking-backend\`:

- OreTracking.exe
- OreTracking.dll
- appsettings.json
- web.config
- runtimes/
- logs/
- wwwroot/

## Database
**IMPORTANTE**: Copiare anche `app.db` dalla cartella `ore-tracking/` del progetto.
Il database contiene i dati esistenti e **non deve essere sovrascritto**.

Se non hai un database locale, puoi generare una struttura vuota:
```powershell
cd C:\DevAgentPM\pm-assistant-suite\ore-tracking
dotnet ef database drop --force
dotnet ef database create
```
Poi copia il `app.db` generato sul server.

## Permessi IIS
Dare permessi di scrittura alla cartella per l'account dell'Application Pool:
```powershell
# Esempio per Application Pool "OreTrackingPool"
$acl = Get-Acl "C:\inetpub\ore-tracking-backend"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.AddAccessRule($rule)
Set-Acl "C:\inetpub\ore-tracking-backend" $acl
```

## Configurazione
Verificare `appsettings.json`:
- `AllowedOrigins` → inserire il dominio production
- `Jwt:Key` → stessa chiave usata dal frontend
- `AdminUser:Password` → cambiare se necessario

## Avvio
In IIS Manager:
1. Creare nuova Application Pool (.NET CLR Version: No Managed Code)
2. Creare Application con path fisico pointing a `C:\inetpub\ore-tracking-backend`
3. L'app sarà disponibile su `http://localhost` o dominio configurato

## URL API
- Swagger: `http://localhost/swagger`
- API: `http://localhost/api/...`
- Frontend: `http://localhost/`
