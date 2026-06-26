# PostgreSQL local
Subir banco:
```powershell
docker compose -f docker-compose.postgres.yml up -d
docker ps
```
Rodar migrations por Node:
```powershell
node scripts/run-postgres-migrations.js
```
Ou pela API em Development: `POST http://localhost:5080/admin/database/migrate`.
