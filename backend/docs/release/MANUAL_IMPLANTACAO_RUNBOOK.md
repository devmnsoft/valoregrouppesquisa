# MANUAL DE IMPLANTAÇÃO E RUNBOOK DE PRODUÇÃO
## Valora Insight™ — Release 1.0 Candidate

---

## 1. Requisitos de Infraestrutura

### 1.1 Servidor de Banco de Dados
- **Motor:** PostgreSQL 16.x (com suporte nativo a `gen_random_uuid()` e extensão `pgcrypto`).
- **Recursos Mínimos (Piloto/Produção Pequena):** 2 vCPUs, 4 GB RAM, SSD 50 GB com IOPS provisionados.
- **Recursos Recomendados (Produção Corporativa):** 4 vCPUs, 16 GB RAM, SSD 150 GB NVMe.
- **Configuração de Conexão:** Pool de conexões (PgBouncer ou pool integrado Npgsql com `Maximum Pool Size=100`).

### 1.2 Servidor de Aplicação (.NET 10)
- **Runtime:** .NET 10.0 ASP.NET Core Runtime (ou SDK em ambiente de build/CI).
- **Recursos Mínimos:** 2 vCPUs, 2 GB RAM por instância (stateless, balanceado por Nginx/Traefik/Cloudflare).
- **Armazenamento de Arquivos:** Volume local montado ou Object Storage (S3 / Azure Blob Storage).

---

## 2. Variáveis de Ambiente Essenciais

| Variável | Descrição | Exemplo |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | `Production` |
| `ConnectionStrings__DefaultConnection` | String de conexão com o PostgreSQL | `Host=db.valora.internal;Port=5432;Database=valora_production;Username=valora_app;Password=SEGREDO_FORTE;Pooling=true;Maximum Pool Size=100;` |
| `Jwt__SigningKey` | Chave simétrica HMAC-SHA256 (mínimo 32 caracteres) | `ValoraInsightProductionSuperSecretKey2026!#` |
| `Jwt__Issuer` | Emissor do token JWT | `https://api.valora.com.br` |
| `Jwt__Audience` | Audiência do token JWT | `https://valora.com.br` |
| `App__MaintenanceModeEnabled` | Ativação do modo manutenção | `false` |
| `Valora__Processing__Enabled` | Habilitação do worker em segundo plano | `true` |
| `Reports__PdfEnabled` | Habilitação de exportação em PDF nativa | `true` |

---

## 3. Passo a Passo de Implantação

### Passo 1: Provisionamento do Banco de Dados
1. Crie o banco de dados limpo no PostgreSQL:
   ```sql
   CREATE DATABASE valora_production ENCODING 'UTF8';
   ```
2. Aplique o script canônico consolidado:
   ```bash
   psql -U postgres -d valora_production -f database/postgresql/script_completo.sql
   ```
3. Aplique as regras de menor privilégio de segurança:
   ```bash
   psql -U postgres -d valora_production -f database/postgresql/security_roles_least_privilege.sql
   ```

### Passo 2: Verificação de Integridade (Sanity Check)
Execute o script de sanity check pós-implantação:
```bash
psql -U postgres -d valora_production -f database/postgresql/sanity_check.sql
```
*Critério de Sucesso:* Mensagem `SANITY CHECK CONCLUÍDO COM SUCESSO: AMBIENTE 100% OPERACIONAL`.

### Passo 3: Provisionamento do Primeiro Tenant Piloto
Execute o script oficial de onboarding de novo cliente:
```bash
psql -U postgres -d valora_production -f database/postgresql/provision_new_tenant.sql
```

### Passo 4: Inicialização dos Serviços
Inicie o Valora.Api e Valora.Web:
```bash
dotnet Valora.Api.dll
dotnet Valora.Web.dll
```

### Passo 5: Verificação dos Health Checks
Acesse os endpoints de monitoramento:
- `GET /health`: Deve retornar `200 OK`
- `GET /health/database`: Deve retornar `200 OK` (`database: ok`)
- `GET /health/ready`: Deve retornar `200 OK` (`workers: enabled, database: ok, outbox: ok`)
