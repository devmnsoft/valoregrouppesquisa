# MANUAL DO ADMINISTRADOR
## Valora Insight™ — Release 1.0 Candidate

---

## 1. Visão Geral da Administração

O Valora Insight™ disponibiliza dois níveis principais de administração:
1. **Superadministrador (Admin Valora):** Responsável pela gestão global de organizações (tenants), catálogo de planos comerciais, ativação de módulos e métricas agregadas da plataforma.
2. **Administrador da Organização (Empresa Admin):** Responsável pela gestão de sua empresa: estrutura organizacional (entidades jurídicas, unidades e departamentos), convite de usuários, atribuição de perfis, configuração de pesquisas e supervisão de planos de ação.

---

## 2. Operações de Gestão de Organizações

### 2.1 Criar ou Provisionar Nova Organização
- **Via Interface (`/AdminValora`):**
  - Acesse o painel **Admin Valora** > **Organizações** > **Nova Organização**.
  - Informe o Nome Corporativo, Slug único, CNPJ e Plano Comercial inicial (Gratuito, Start, Growth, Profissional, Corporativo ou Enterprise).
  - O sistema automaticamente provisionará o schema de isolamento, a assinatura comercial e criará o perfil do administrador inicial.
- **Via Script SQL Automatizado:**
  - Execute `database/postgresql/provision_new_tenant.sql` customizando os valores de nome, slug e e-mail.

### 2.2 Gerenciar Planos e Módulos
- No painel da organização, acesse **Assinatura & Módulos**.
- Você pode ativar/desativar módulos individualmente:
  - `pesquisas`: Criação de formulários, perguntas e envio de questionários.
  - `diagnosticos`: Aplicação de frameworks de maturidade diagnóstica.
  - `analytics`: Dashboards analíticos, correlações e heatmaps.
  - `planos_acao`: Criação, atribuição e rastreamento de ações de evolução.
  - `benchmarks`: Comparações contextuais com coortes anônimas.

---

## 3. Gestão de Usuários e Permissões

### 3.1 Convidar Novos Colaboradores
1. Acesse o menu **Configurações** > **Usuários & Equipe**.
2. Clique em **Novo Usuário**.
3. Preencha o e-mail corporativo, nome completo e selecione o perfil desejado (`empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_unidade`, `participante`).
4. Um e-mail com link assinado e token criptográfico temporário será disparado para ativação de credenciais.

### 3.2 Desativação Segura de Acesso
- O administrador pode desativar qualquer usuário a qualquer momento.
- Ao clicar em **Desativar**, o campo `status` é alterado para `inactive`, o carimbo `access_version` é incrementado (invalidando tokens JWT emitidos anteriormente) e todas as sessões ativas são imediatamente revogadas.

---

## 4. Monitoramento e Saúde Operacional

### 4.1 Endpoints de Telemetria e Monitoramento
- **`/health/ready`:** Retorna o status de prontidão da API, banco de dados, fila de background jobs e storage.
- **`/api/v1/system-health`:** Painel de checagens históricas com latência de resposta em milissegundos.
- **`/api/v1/system-health/jobs`:** Fila de processamento de inteligência (jobs em execução, pendentes e com falha).

### 4.2 Interpretação de Logs Estruturados (Serilog)
Cada requisição processada pelo Valora Insight™ inclui os seguintes campos canônicos nos logs JSON:
- `CorrelationId`: Identificador rastreável que conecta a requisição HTTP com os jobs de banco e workers.
- `OrganizationId`: UUID do tenant executor (essencial para auditorias LGPD e isolamento multi-tenant).
- `UserId`: UUID do usuário autenticado (ou anônimo em submissões públicas de diagnóstico).
- `LogLevel`:
  - `Information`: Ciclos normais de negócio (login, cálculo de maturidade concluído, exportação solicitada).
  - `Warning`: Tentativa de acesso a recurso sem permissão, falhas transitórias de conexão reprocessadas.
  - `Error`: Exceções de sistema, falhas irrecuperáveis de banco ou violações de regras críticas.
