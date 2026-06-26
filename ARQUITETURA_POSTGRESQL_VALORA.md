# Arquitetura PostgreSQL Valora Pulse™

## Decisão arquitetural

Recomenda-se uma migração gradual, mantendo o Firebase/Firestore ativo enquanto módulos são reimplementados e validados em uma API backend com PostgreSQL. O objetivo é retirar regras críticas do frontend, melhorar integridade relacional, auditoria, relatórios e controle real de planos.

## Arquitetura final proposta

```text
Frontend Valora Pulse™
  -> ASP.NET Core API
      -> PostgreSQL
      -> SMTP/Communication Service
      -> Certificate Service
      -> Audit Service
```

### Frontend
- Telas e UX.
- Consumo da API via `DATA_PROVIDER=api` ou `hybrid`.
- Renderização de dashboards, relatórios e downloads.
- Sem senha SMTP, segredo JWT, chave privada Firebase Admin ou regras comerciais finais.

### Backend
- Autenticação e emissão de JWT.
- Autorização por tenant, papel, plano e capability.
- Regras de plano, limites e uso mensal.
- Persistência transacional de pesquisas, respostas e resultados.
- Cálculo oficial do Valora Insight™.
- Geração de devolutiva, certificado PDF/PNG e arquivos.
- Fila de e-mails transacionais.
- Auditoria estruturada e trilha LGPD.

### PostgreSQL
- Persistência relacional com integridade referencial.
- Histórico e auditoria.
- Relatórios consolidados.
- Modelo multiempresa por `organization_id`.
- Soft delete por `is_deleted`.

## Stack backend
- ASP.NET Core 8 ou superior.
- C# 10+.
- Dapper e Npgsql.
- PostgreSQL.
- JWT Bearer ou ASP.NET Identity com hashes de senha.
- Swagger/OpenAPI.
- Serilog ou logger estruturado.

## Autenticação

Opção recomendada para a transição:
1. Manter Firebase Auth no modo híbrido para usuários existentes.
2. Criar tabela `valora.users` com `firebase_uid` opcional.
3. Validar ID Token Firebase no backend enquanto o usuário ainda não migrou.
4. Para novos tenants, usar JWT/ASP.NET Identity com senha sempre hasheada.
5. Nunca importar senhas do Firebase nem senhas em texto puro.

## Fronteiras de responsabilidade

| Responsabilidade | Frontend | Backend | PostgreSQL |
|---|---:|---:|---:|
| Renderizar telas | Sim | Não | Não |
| Autenticar credenciais | Não | Sim | Hash/estado |
| Validar permissões | Sinalização UX | Sim | Apoio por dados |
| Aplicar limites de plano | Não | Sim | Sim |
| Salvar respostas | Chama API | Sim | Sim |
| Calcular resultados | Prévia apenas | Sim | Persistência |
| Emitir certificado | Download | Sim | Metadados |
| Enviar e-mail | Não | Sim | Fila/log |
| Auditoria | Evento cliente opcional | Sim | Sim |

## Módulos backend planejados

- `Auth`: login, registro de empresa, recuperação de senha, `/me`.
- `Organizations`: tenants, unidades e configurações.
- `Billing`: planos, limites, capabilities, assinaturas e uso.
- `Surveys`: formulários, perguntas, pesquisas, convites e links públicos.
- `Responses`: respostas, pontuações e devolutivas.
- `Certificates`: PDF/PNG e código de validação.
- `Communication`: e-mail, WhatsApp futuro, templates, tentativas e retry.
- `Audit`: logs imutáveis por usuário, IP, recurso e ação.

## Estratégia de migração gradual

### Fase 1 — API base
Criar solution ASP.NET Core, conexão PostgreSQL, health check, JWT/Auth, multiempresa e auditoria.

### Fase 2 — Cadastros centrais
Migrar `organizations`, `users`, `roles`, `plans`, `subscriptions`, `units`, `settings`.

### Fase 3 — Pesquisas
Migrar `forms`, `questions`, `surveys`, `survey_links`, `invitations`.

### Fase 4 — Respostas e resultados
Migrar `responses`, `answers`, `result_scores`, `dimension_scores`, `valora_insight_devolutivas`.

### Fase 5 — Certificados e relatórios
Migrar `certificates`, `reports`, `report_files`, `certificate_files`.

### Fase 6 — Comunicação
Migrar `communications`, `email_jobs`, `whatsapp_jobs`, `email_templates`.

### Fase 7 — Admin e planos
Ativar `plan_limits`, `plan_capabilities`, `usage_monthly`, `service_deliverables`.

### Fase 8 — Desativação gradual do Firebase
Frontend passa a ler API; Firestore fica backup temporário; validar dados; congelar Firestore; remover dependências Firebase após aceite.

## Decisões pendentes

- Provedor principal de autenticação no estado final: ASP.NET Identity puro ou federação com Firebase/Google.
- Estratégia de armazenamento de arquivos: disco, S3-compatible, Azure Blob ou Firebase Storage temporário.
- Provedor transacional de e-mail: SMTP simples, Brevo, SES, SendGrid ou gateway atual.
- Política de retenção LGPD e anonimização por pesquisa.
