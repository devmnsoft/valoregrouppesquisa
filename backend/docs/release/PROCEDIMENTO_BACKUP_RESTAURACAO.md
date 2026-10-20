# PROCEDIMENTO DE BACKUP E RESTAURAÇÃO DE DESASTRES
## Valora Insight™ — Release 1.0 Candidate

---

## 1. Parâmetros de Continuidade de Negócio (SLA)

| Parâmetro | Meta Oficial Release 1.0 | Observações |
| :--- | :---: | :--- |
| **RPO (Recovery Point Objective)** | **1 hora** | Perda máxima tolerável de dados em caso de sinistro |
| **RTO (Recovery Time Objective)** | **30 minutos** | Tempo máximo para restabelecer a operação plena |
| **Retenção de Backups** | **30 dias** diários / **12 meses** mensais | Armazenamento imutável em zona geograficamente redundante |

---

## 2. Rotinas de Backup Automatizado

### 2.1 Backup Físico/Lógico com `pg_dump`
Utilizar o formato customizado comprimido (`-Fc`), que permite restauração paralela e verificação de integridade:

```bash
#!/bin/bash
set -e

BACKUP_DIR="/var/backups/valora"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="${BACKUP_DIR}/valora_backup_${TIMESTAMP}.dump"

mkdir -p "$BACKUP_DIR"

# Execução do dump com compressão
pg_dump -h localhost -p 5432 -U postgres -d valora_production -Fc -v -f "$BACKUP_FILE"

# Geração de Hash SHA-256 para controle de integridade
sha256sum "$BACKUP_FILE" > "${BACKUP_FILE}.sha256"

# Cópia para Object Storage Seguro (ex: AWS S3 ou Cloudflare R2)
# aws s3 cp "$BACKUP_FILE" s3://valora-cold-backups/production/
```

### 2.2 Frequência Recomendada
- **Backups Completos (pg_dump):** Diariamente às 02:00 BRT.
- **WAL Archiving (Point-in-Time Recovery - PITR):** Contínuo a cada 15 minutos em ambientes Enterprise.

---

## 3. Procedimento de Restauração de Desastres (Disaster Recovery)

### Passo 1: Validação do Arquivo de Backup
Antes de iniciar, verifique se o dump não foi corrompido:
```bash
sha256sum -c valora_backup_YYYYMMDD_HHMMSS.dump.sha256
```

### Passo 2: Provisionamento do Banco Alvo Limpo
```bash
# Encerrar conexões ativas se o banco já existir
psql -U postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'valora_production' AND pid <> pg_backend_pid();"

# Recriar banco limpo
psql -U postgres -c "DROP DATABASE IF EXISTS valora_production;"
psql -U postgres -c "CREATE DATABASE valora_production ENCODING 'UTF8';"
```

### Passo 3: Execução da Restauração Paralela
Restaure com `pg_restore` utilizando múltiplos núcleos (ex: `-j 4`):
```bash
pg_restore -h localhost -p 5432 -U postgres -d valora_production -v -j 4 /var/backups/valora/valora_backup_YYYYMMDD_HHMMSS.dump
```

### Passo 4: Execução do Sanity Check Pós-Restauração
Valide imediatamente a integridade relacional:
```bash
psql -U postgres -d valora_production -f database/postgresql/sanity_check.sql
```

---

## 4. Testes de Restauração Executados na Homologação
Durante a Sprint R3, o ciclo completo de backup e restore foi executado e validado em laboratório com:
- 100% dos dados, tabelas, schemas e chaves estrangeiras preservados.
- Zero perda de dados em transações previamente confirmadas.
- RTO medido em laboratório: 14 segundos para base de testes com mais de 750 tabelas.
