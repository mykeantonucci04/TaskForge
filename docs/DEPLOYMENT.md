# TaskForge Deployment & Configuration Guide

Questa guida descrive la configurazione e le variabili d'ambiente necessarie per effettuare il deploy di **TaskForge** su **Render** con database PostgreSQL su **Neon**.

---

## Checklist Variabili d'Ambiente (Render Environment Variables)

Configura le seguenti variabili nel dashboard di Render (**Environment** tab del Web Service):

| Nome Variabile | Obbligatoria (Prod) | Descrizione | Origine / Come ottenerla | Esempio / Default |
|---|---|---|---|---|
| `DATABASE_URL` | **SÌ** (o `ConnectionStrings__DefaultConnection`) | Connection string PostgreSQL in formato URI fornita da Neon. Viene convertita automaticamente in formato Npgsql con SSL abilitato. | Dashboard Neon → **Connection Details** → stringa formato `postgresql://...` | `postgresql://neondb_owner:npg_xxx@ep-xxx.eu-central-1.aws.neon.tech/taskforge_db?sslmode=require` |
| `ConnectionStrings__DefaultConnection` | No (se `DATABASE_URL` è impostata) | Connection string in formato standard Npgsql. | Alternativa a `DATABASE_URL` | `Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true` |
| `ASPNETCORE_ENVIRONMENT` | No (Consigliata) | Ambiente di runtime ASP.NET Core (`Production` o `Development`). | Impostato su Render | `Production` |
| `PORT` | Gestita da Render | Porta HTTP su cui il server si mette in ascolto. L'applicazione ascolta automaticamente su `http://0.0.0.0:$PORT`. | Assegnata dinamicamente da Render | `8080` (o porta assegnata) |
| `Jwt__Secret` | **SÌ** (in Prod) | Chiave segreta simmetrica per la firma e verifica dei token JWT (minimo 32 caratteri). | Generata localmente con terminale: `openssl rand -base64 32` | `K8s7d+F3...` *(non usare valori di default in produzione)* |
| `Jwt__Issuer` | No | Issuer (emittente) dichiarato nei token JWT. | Arbitrario | `TaskForgeApi` |
| `Jwt__Audience` | No | Audience (destinatario) dei token JWT. | Arbitrario | `TaskForgeClient` |
| `Jwt__ExpiryMinutes` | No | Durata di validità dell'Access Token in minuti. | Configurazione | `120` |

---

## Secrets GitHub Actions (per CI/CD automatico)

Se utilizzi il workflow di deploy [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml):

| Nome Secret | Descrizione | Origine |
|---|---|---|
| `RENDER_DEPLOY_HOOK_URL` | URL del Deploy Hook generato da Render per innescare la ricompilazione/ridistribuzione al push su `main`. | Render Web Service → **Settings** → **Deploy Hook** |

---

## Configurazione del Servizio su Render

1. **Tipo Servizio**: Web Service
2. **Environment**: Docker (oppure Native .NET se supportato)
   - Dockerfile Path: `./docker/Dockerfile` (o `./Dockerfile`)
   - Docker Build Context: `.`
3. **Health Check Path**: `/health` (restituisce `200 OK` quando il database e l'API sono sani)
4. **Auto-Deploy**: Abilitato tramite branch `main` oppure tramite Deploy Hook.

---

## Validazione all'Avvio & Log di Debug

In ambiente `Production`:
- Se `DATABASE_URL` o `ConnectionStrings__DefaultConnection` non è configurata, l'avvio fallisce immediatamente con un errore chiaro nei log.
- Se `Jwt__Secret` manca o contiene la chiave di default di sviluppo, l'avvio fallisce immediatamente.
- Nei log di Render viene stampata la fonte di configurazione (es. `Database connection resolved from: DATABASE_URL (Environment Variable)`), senza mai mostrare password o token sensibili.
