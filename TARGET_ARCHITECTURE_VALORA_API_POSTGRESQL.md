# Arquitetura Alvo Valora Pulse™ — API ASP.NET Core + PostgreSQL

## Objetivo

Consolidar uma arquitetura híbrida e evolutiva, sem Big Bang, onde o Firebase permanece operacional enquanto a API ASP.NET Core assume gradualmente autenticação, planos, pesquisas públicas, respostas, resultados, certificados, comunicações, auditoria e relatórios.

## Frontend

- HTML/CSS/JavaScript atual, publicado em IIS.
- Consome a API exclusivamente por `api-client.js` e `api-repository.js` nos módulos preparados.
- Não guarda segredos, tokens SMTP, senhas de banco ou chaves administrativas.
- Não envia SMTP diretamente.
- Não calcula o resultado como fonte final em produção; cálculo local permanece apenas como fallback/compatibilidade durante a transição.

## Backend

- ASP.NET Core API.
- Dapper para persistência explícita e previsível.
- PostgreSQL via Npgsql.
- JWT Bearer para autenticação da API.
- BCrypt.Net-Next para hash de senha.
- Swagger para documentação técnica em desenvolvimento.
- Logging estruturado.
- Serviços de domínio para regras de plano, cálculo Valora Insight™, certificados, e-mail, auditoria e relatórios.

## PostgreSQL

- Dados relacionais com integridade referencial.
- Transações para registro de respostas, resultado, certificado, comunicação e auditoria.
- Histórico de comunicações.
- Auditoria em `audit.audit_logs`.
- Controle de uso mensal em `billing.usage_monthly`.

## Gateway

O gateway de comunicação pode continuar separado no curto prazo para reduzir risco operacional. Após estabilização da API, os recursos do gateway podem ser absorvidos pelo backend principal.

## Modos de dados

```js
DATA_PROVIDER: 'firebase' | 'api' | 'hybrid'
```

- `firebase`: sistema atual continua funcionando e permanece como padrão seguro.
- `api`: frontend usa ASP.NET Core/PostgreSQL para os fluxos já migrados.
- `hybrid`: compara Firebase x API em modo controlado, registrando divergências sem bloquear produção.

## Ordem segura

1. Manter Firebase como fonte operacional.
2. Subir PostgreSQL local e aplicar migrations.
3. Validar `/health`, `/health/database`, `/plans/public`, auth e jornada pública em ambiente controlado.
4. Rodar export/transform/import Firestore → PostgreSQL.
5. Usar `hybrid` para comparação.
6. Ativar `api` por módulo, começando por planos e jornada pública.
