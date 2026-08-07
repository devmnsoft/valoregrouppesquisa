# Valora Insight™ — Enterprise V6

## Auditoria inicial

O novo sistema já possuía arquitetura em camadas, autenticação JWT/BFF, autorização por papéis e permissões, escopo por `organization_id`, auditoria, planos e limites, assinatura básica, dashboards, pesquisas, respostas, resultados, comparativos, recomendações, plano de ação, relatórios, certificados, membros, estrutura organizacional, comunicação por e-mail, notificações, LGPD, exportação e migração. O menu já era agrupado por jornada e os módulos operacionais existentes foram preservados.

As lacunas encontradas eram a ausência de uma console exclusiva e consolidada do administrador geral, CRM persistente, filtros de carteira, cadastro uniforme de integrações/automações/templates/alertas, emissão segura de API keys e pré-validação CSV simples. Planos e assinaturas existiam, mas não continham todos os dados comerciais de cobrança. Nenhuma Cloud Function ou fluxo legado foi removido.

## Evolução implementada

- Console responsiva **Admin Valora**, exclusiva do papel `admin_valora`, com visão geral, inteligência da carteira, clientes, CRM, planos, assinaturas, alertas, templates, integrações, automações, white label, importações e API.
- Carteira paginada e filtrada no banco por nome, CNPJ, e-mail, plano, status, saúde e período; ações de bloqueio/reativação são persistidas e auditadas.
- CRM persistente com funil, lead, empresa, contato, plano pretendido, responsável, próxima ação e observações.
- Registro genérico persistente e extensível para planos comerciais, assinaturas operacionais, integrações, templates, automações, alertas e branding. Configurações aceitam JSON; segredos continuam fora do banco e devem vir do ambiente.
- Assinaturas receberam ciclo, valor contratado, renovação, forma informada, contato financeiro e observações.
- API keys com escopos permitidos, segredo aleatório exibido somente na emissão, persistência exclusiva do SHA-256/prefixo e auditoria. O middleware de consumo dos endpoints externos fica para a etapa de publicação da API, evitando expor uma API incompleta.
- Importação CSV para unidades, setores, membros, respondentes e estrutura com prévia, limite de 500 linhas por lote, erros por linha e token de confirmação determinístico. Esta fase não confirma linhas inválidas automaticamente.
- Layout enterprise com hero, navegação horizontal, métricas, cards, tabela responsiva, badges, loading, erro amigável, vazio, formulários e adaptação mobile.
- Índices de carteira, CRM, itens enterprise e chaves reduzem leituras desnecessárias. Listagens têm paginação server-side limitada a 100 itens.

## Segurança e regras

- A carteira global e o CRM exigem `admin_valora`; demais registros usam o `organization_id` do token. Escritas sensíveis geram `audit_logs`.
- A chave em texto claro nunca é salva; SMTP, webhooks e demais segredos não devem ser enviados em `configuration`.
- O BFF mantém o bearer no servidor, encaminha antiforgery/correlation id e a tela não usa `innerHTML` com conteúdo externo sem escape.
- Status de empresa é restrito a `active`, `onboarding`, `at_risk`, `blocked`, `cancelled`, `trial` e `delinquent`.
- A camada de aplicação rejeita tipos de módulos, escopos e CSV inválidos com mensagens de negócio.

## Arquivos principais

- `Valora.Application/Enterprise/*`: contratos e regras.
- `Valora.Infrastructure/Repositories/EnterpriseRepository.cs`: consultas paginadas e persistência Dapper.
- `Valora.Api/Controllers/EnterpriseController.cs`: endpoints autenticados.
- `Valora.Web/Controllers/EnterpriseController.cs`, `Views/Enterprise/Index.cshtml` e `wwwroot/js/pages/enterprise-page.js`: console Razor funcional.
- `Valora.Web/wwwroot/css/valora-admin.css`: experiência premium e mobile.
- `database/postgresql/script_completo.sql`: schema e índices idempotentes.
- `Valora.Tests/EnterpriseV6Tests.cs`: validação real do CSV.

## Como testar

1. Configure `ConnectionStrings__DefaultConnection`, aplique `database/postgresql/script_completo.sql` e inicie `Valora.Api` e `Valora.Web`.
2. Entre com usuário de papel `admin_valora` e abra `/AdminValora`.
3. Confira métricas e filtros, crie um lead, altere um cliente, crie registros nos módulos e confira `audit_logs`.
4. Em Importação, envie CSV UTF-8 separado por `;`, com cabeçalho `nome;email`; valide os apontamentos.
5. Em API, selecione escopos e copie a chave no momento da criação; confirme no banco que somente `key_prefix` e `key_hash` existem.
6. Execute `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln` e `dotnet test backend/Valora.sln` em uma máquina com .NET SDK 10.

## Limitações e próximos passos

O pagamento real, disparo automático de WhatsApp, cofre externo de segredos, confirmação transacional da importação e autenticação da futura API pública não foram ativados sem infraestrutura/credenciais. A estrutura operacional está persistida, mas esses adaptadores devem ser publicados em fases próprias. Próximos passos: adicionar worker de automações, integração com secret manager, rotação/revogação de chaves, rate limiting e endpoints públicos somente leitura; implementar confirmação de importação em transação e testes PostgreSQL em CI.
