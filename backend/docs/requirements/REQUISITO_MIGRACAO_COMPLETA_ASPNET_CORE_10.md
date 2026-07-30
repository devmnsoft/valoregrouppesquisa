# Requisito e Prompt de Implementação — Migração Completa do Valora Insight™ para ASP.NET Core 10

## 1. Objetivo deste documento

Este documento consolida:

1. o contexto funcional e comercial do Valora Insight™;
2. o diagnóstico técnico do repositório atual;
3. a arquitetura obrigatória da nova versão;
4. a migração integral do legado Firebase/JavaScript para ASP.NET Core 10, Dapper e PostgreSQL;
5. a criação do banco canônico em `backend/database/postgresql/script_completo.sql`;
6. o requisito de planos Gratuito, Profissional, Corporativo e Enterprise;
7. os critérios de segurança, qualidade, testes, migração, homologação e cutover;
8. um prompt completo para execução pelo Codex.

A implementação deve reaproveitar e corrigir a solução existente em `backend/Valora.sln`. Não criar `backend-v3`, outra solution, uma SPA paralela ou um segundo produto.

---

# 2. Diagnóstico técnico consolidado do repositório

## 2.1 Estruturas existentes que devem ser reaproveitadas

O projeto já possui a solução oficial:

- `backend/Valora.Api`;
- `backend/Valora.Application`;
- `backend/Valora.Domain`;
- `backend/Valora.Infrastructure`;
- `backend/Valora.Tests`;
- `backend/Valora.Web`;
- `backend/database/postgresql`.

Também já existem partes úteis que não devem ser descartadas sem análise:

- autenticação JWT e hashing de senha;
- Dapper e Npgsql;
- Serilog;
- middleware global de erro e correlação;
- repositórios de organizações, usuários, planos, formulários, pesquisas, respostas, certificados, relatórios, LGPD, e-mail, auditoria e migração;
- serviços de pesquisa pública, cálculo, resultado público e token de resultado;
- páginas Razor e JavaScript modular;
- validadores Node existentes;
- testes de contratos do legado;
- scripts e documentos de homologação, backup, restore, cutover e rollback.

## 2.2 Problemas estruturais encontrados

A implementação não pode considerar o backend atual concluído. Foram identificados os seguintes problemas concretos:

1. Todos os projetos oficiais ainda usam `net8.0`, e não `net10.0`.
2. Os `.csproj` estão compactados e inconsistentes. O projeto Web possui apenas Serilog e não possui configuração consolidada de cliente HTTP tipado, autenticação, antiforgery, resiliência e integração oficial com a API.
3. Existem arquivos com várias classes no mesmo arquivo, contrariando Clean Code e a separação solicitada. Exemplos:
   - `backend/Valora.Application/Services/OperationalServices.cs`;
   - `backend/Valora.Infrastructure/Repositories/OperationalRepositories.cs`;
   - `backend/Valora.Application/Contracts/Services/IOperationalServices.cs`;
   - arquivos agregadores de DTOs, contratos e migração.
4. Há código severamente comprimido em linhas únicas, dificultando revisão, depuração e manutenção.
5. Entidades importantes como `Organization`, `Unit` e `Communication` estão praticamente vazias no domínio oficial.
6. A camada de migração ainda não realiza migração real dos dados de negócio. O `MigrationApplyService` atual cria identificadores, mapeamentos e itens de rollback, mas não insere os documentos transformados nas tabelas finais.
7. O arquivo `migration/transform-to-postgres.js` é apenas um TODO.
8. Existem operações JavaScript oficiais que retornam sucesso simulado sem persistência real, por exemplo ações de salvar e alterar status de pesquisa.
9. Há serviços provisórios com comportamento inadequado para produção, como processamento de e-mail com destinatário fixo de desenvolvimento.
10. Existem contratos e catálogos de planos divergentes entre README, JavaScript, JSON compartilhado, Functions, documentação e PostgreSQL.
11. Existem dois caminhos de banco e múltiplos scripts que podem divergir. Não há hoje uma única fonte canônica chamada `script_completo.sql`.
12. Algumas tabelas existentes possuem poucas FKs, constraints e índices; outras guardam estruturas críticas apenas em `jsonb` genérico.
13. Há funcionalidades descritas como migradas, mas ainda dependentes de estados vazios, HTTP 501, runtime não homologado ou código de demonstração.
14. O legado contém regras de acesso, módulos, perfis e coleções que ainda não estão integralmente representados no domínio PostgreSQL.
15. A matriz oficial de paridade ainda marca pesquisa pública, resultado por token, registro de empresa, planos, certificados, e-mail, billing e suporte como parciais ou incompletos.

## 2.3 Decisão obrigatória

A nova versão produtiva deverá ter:

- ASP.NET Core 10 como runtime único;
- PostgreSQL como banco único;
- Dapper como acesso único a dados;
- `backend/Valora.sln` como solution única;
- `backend/Valora.Web` como frontend MVC/Razor único;
- `backend/Valora.Api` como API única;
- nenhum acesso do navegador ao Firebase;
- nenhuma Cloud Function necessária para a operação final;
- nenhum Firestore usado em runtime;
- nenhum `localStorage` como banco de negócio;
- nenhum dado fake ou sucesso simulado;
- legado preservado somente como referência e contingência durante a migração;
- desligamento do Firebase somente após homologação e cutover formal.

---

# 3. Arquitetura obrigatória

## 3.1 Target framework

Atualizar todos os projetos oficiais para:

```xml
<TargetFramework>net10.0</TargetFramework>
```

Criar ou atualizar:

- `global.json` para o SDK .NET 10;
- `Directory.Build.props`;
- `Directory.Packages.props`, preferencialmente com Central Package Management;
- `.editorconfig`;
- nullable habilitado;
- implicit usings habilitado;
- analyzers habilitados;
- warnings como erros em CI;
- versões estáveis e compatíveis de Dapper, Npgsql, Serilog, autenticação, OpenAPI e testes.

Não usar pacotes prerelease sem justificativa registrada.

## 3.2 Dependências entre camadas

### `Valora.Domain`

Deve conter apenas:

- agregados;
- entidades;
- value objects;
- enums;
- regras de domínio;
- eventos de domínio;
- exceções de domínio;
- especificações puras quando necessárias.

Não pode referenciar Dapper, Npgsql, ASP.NET, Serilog, Firebase, JSON de infraestrutura ou banco.

### `Valora.Application`

Deve conter:

- casos de uso;
- comandos e queries;
- DTOs de entrada e saída;
- validators;
- interfaces de repositories e gateways;
- interfaces de serviços externos;
- orquestração transacional;
- regras de autorização e entitlement;
- mapeadores;
- contratos de paginação;
- contratos de resultado.

Pode referenciar apenas `Valora.Domain` e abstrações do .NET.

### `Valora.Infrastructure`

Deve conter:

- implementação Dapper dos repositories;
- conexão e transação Npgsql;
- SQL organizado por agregado;
- e-mail SMTP;
- integração de CNPJ/CEP;
- hashing e JWT;
- storage;
- exportações;
- serviços de data/hora e identificadores;
- implementação de auditoria;
- fila/outbox;
- implementação do importador Firebase.

Pode referenciar Application e Domain.

### `Valora.Api`

Deve conter:

- controllers finos;
- autenticação e autorização;
- filtros;
- middleware;
- health checks;
- OpenAPI;
- rate limiting;
- versionamento de API;
- validação de entrada;
- composição de dependências.

Não pode conter SQL ou regra de negócio.

### `Valora.Web`

Deve permanecer em ASP.NET Core MVC/Razor com Bootstrap 5, JavaScript puro e jQuery/AJAX quando necessário.

Deve:

- consumir exclusivamente a API oficial;
- usar clientes HTTP tipados;
- ter tratamento central de erro;
- ter autenticação segura por cookie HttpOnly ou estratégia formal integrada à API;
- usar antiforgery nas operações mutáveis;
- ser responsivo e mobile-first;
- manter layouts público e administrativo separados;
- não acessar banco diretamente;
- não referenciar Infrastructure;
- não usar Firebase SDK;
- não retornar sucesso sem resposta real da API.

### `Valora.Worker`

Adicionar ao `backend/Valora.sln` quando necessário para:

- fila de e-mails;
- fila de WhatsApp;
- notificações;
- expiração de convites;
- lembretes;
- limpeza de tokens;
- processamento de relatórios;
- tarefas agendadas;
- outbox e retries.

O Worker deve usar os mesmos contratos de Application e implementações de Infrastructure, com idempotência, lease, retry e dead-letter.

### `Valora.Migration`

Adicionar um console app .NET 10 à solution para executar a migração controlada do Firebase/legado para PostgreSQL.

Ele não poderá ser dependência de runtime da API/Web.

## 3.3 Regras de organização de código

1. Uma única classe, interface, enum, record ou struct top-level por arquivo.
2. Nome do arquivo igual ao tipo principal.
3. Não manter arquivos agregadores como `OperationalServices.cs` ou `OperationalRepositories.cs`.
4. Não colocar várias interfaces no mesmo arquivo.
5. Não colocar DTOs de domínios diferentes no mesmo arquivo.
6. Não criar classes genéricas do tipo `Helper`, `Utils`, `Manager` ou `Service` sem responsabilidade clara.
7. Métodos assíncronos devem terminar com `Async`.
8. Métodos de I/O devem receber `CancellationToken`.
9. Nenhum método deve ter responsabilidade excessiva.
10. Nenhum controller deve conter regra de negócio.
11. Nenhum repository deve montar regra comercial.
12. Nenhuma entidade deve ser um record vazio sem comportamento ou atributos reais.
13. Código identado, formatado e validado por `dotnet format`.
14. Remover duplicidade de `using`, classes, rotas, contratos, serviços e registrations.
15. Proibir código minificado ou comprimido em arquivos C# e JavaScript mantidos.

## 3.4 Tratamento de exceções e logs

Toda fronteira crítica deve possuir tratamento de erro e log estruturado:

- caso de uso;
- repository;
- integração externa;
- processamento de fila;
- migração;
- geração de arquivo;
- envio de e-mail;
- autenticação;
- operações administrativas.

Usar `ILogger<T>` e Serilog com:

- `CorrelationId`;
- `TraceId`;
- usuário quando permitido;
- organização;
- operação;
- entidade;
- identificador seguro;
- duração;
- resultado;
- código de erro.

Não criar `catch` vazio. Não esconder exceção. Não registrar senha, token bruto, hash, connection string, payload sensível ou documento completo.

O middleware global deve traduzir exceções em respostas seguras. Try/catch local deve ser usado para adicionar contexto, compensar transações, realizar retry ou traduzir exceções. Evitar `catch` mecânico que apenas repete o mesmo log e relança sem contexto.

---

# 4. Escopo funcional integral a migrar

Migrar e homologar todas as jornadas do legado, incluindo:

1. Home pública comercial.
2. Diagnóstico gratuito em destaque.
3. Registro de empresa.
4. Login, logout, sessão e recuperação de senha.
5. Perfis e permissões.
6. Empresas, grupos econômicos, pessoas jurídicas, unidades e setores.
7. Usuários, funcionários, participantes e convidados.
8. Planos, assinaturas, limites, capacidades, consumo e billing manual.
9. Módulos e menus por permissão/plano.
10. Formulários, versões, dimensões, perguntas e opções.
11. Programa oficial Valora Insight™.
12. Pesquisas/campanhas.
13. Links públicos e tokens seguros.
14. Convites por e-mail e WhatsApp.
15. Resposta pública.
16. Cálculo determinístico.
17. Resultados individuais e consolidados.
18. Links de resultado e recuperação.
19. Certificados e validação pública.
20. Relatórios e exportações.
21. Dashboards e indicadores.
22. Benchmark interno.
23. Comparação entre pesquisas, setores, unidades e empresas.
24. Histórico de evolução.
25. Saúde organizacional.
26. Plano de ação 30/60/90.
27. Notificações e alertas.
28. LGPD, consentimentos e solicitações do titular.
29. Auditoria e logs operacionais.
30. E-mail transacional e fila.
31. Visualização e fila de WhatsApp.
32. Central de atendimento, tickets, mensagens, categorias, SLA e base de conhecimento.
33. Integrações, API keys, webhooks e logs de integração.
34. White label.
35. Configurações públicas e privadas.
36. Internacionalização.
37. CNPJ e enriquecimento econômico.
38. Backup, restore, migração, reconciliação e rollback.

Não considerar uma funcionalidade migrada apenas porque existe uma View, um endpoint vazio, um DTO ou um estado sem dados. Ela deve possuir persistência real, regra de negócio, autorização, testes e jornada funcional.

---

# 5. Requisito oficial dos planos

A fonte canônica deverá possuir somente os planos públicos:

- `free` — Gratuito;
- `professional` — Profissional;
- `corporate` — Corporativo;
- `enterprise` — Enterprise.

`essential` e `growth` deverão permanecer apenas como planos legados internos para migração, sem aparecer na venda pública.

## 5.1 Gratuito

- 1 conta;
- 1 CNPJ;
- 1 unidade;
- 1 pesquisa oficial Valora;
- 1 resposta válida vitalícia por CNPJ;
- 1 resultado;
- resultado básico;
- envio do resultado por e-mail;
- prévia de WhatsApp;
- certificado simples quando habilitado;
- sem comparação entre setores, ciclos, unidades ou empresas.

A segunda resposta deve ser bloqueada no backend, mesmo após exclusão lógica ou troca de navegador.

## 5.2 Profissional

- 1 CNPJ;
- 1 unidade;
- vários setores;
- vários ciclos de diagnóstico;
- pesquisas oficiais Valora;
- análise integrada entre pesquisas;
- comparação entre setores;
- histórico de evolução;
- relatório executivo da empresa;
- plano de ação 30/60/90;
- indicado para empresa única ou franquia individual.

O cliente não pode alterar perguntas, pesos, fórmulas, opções ou faixas da pesquisa oficial.

## 5.3 Corporativo

- 1 pessoa jurídica contratante;
- várias unidades;
- vários setores por unidade;
- comparação entre unidades;
- ranking interno privado;
- consolidado da empresa;
- comparação de setores equivalentes;
- evolução por unidade;
- modo de rede/franquia dentro da mesma estrutura empresarial autorizada.

Não permitir segunda pessoa jurídica independente.

## 5.4 Enterprise

- grupos econômicos e holdings;
- vários CNPJs;
- várias marcas;
- várias unidades;
- vários setores;
- comparação entre empresas, unidades, marcas e regiões;
- dashboard do grupo;
- benchmark interno;
- governança de acesso por grupo, empresa, unidade e área;
- white label;
- integrações;
- acompanhamento executivo;
- limites conforme contrato.

## 5.5 Entitlements

Implementar limites canônicos:

- `legalEntities`;
- `units`;
- `departments`;
- `activeSurveys`;
- `lifetimeResponses`;
- `monthlyResponses`;
- `managers`;
- `employees`;
- `emailInvites`;
- `whatsappMessages`;
- `storageMb`;
- `languages`;
- `diagnosticCycles`;
- `publicLinks`;
- `forms`.

Implementar capacidades:

- `officialValoraProgram`;
- `crossSurveyAnalysis`;
- `crossDepartmentAnalysis`;
- `multipleUnits`;
- `unitComparison`;
- `multipleLegalEntities`;
- `intercompanyComparison`;
- `groupDashboard`;
- `franchiseMode`;
- `multilingual`;
- `economicEnrichment`;
- `whatsappPreview`;
- `consolidatedReports`;
- `actionPlan`;
- `whiteLabel`;
- `executiveFollowUp`;
- `integrations`;
- `exports`.

Todo limite deve ser aplicado em transação no servidor. Ocultar botão não é enforcement.

---

# 6. Programa oficial Valora Insight™

Preservar o núcleo oficial:

1. Cultura e Propósito;
2. Gestão e Governança;
3. Liderança;
4. Pessoas e Talentos;
5. Resultados e Crescimento;
6. 25 perguntas;
7. escala de 1 a 5;
8. máximo de 125 pontos;
9. cálculo determinístico no backend.

Criar versionamento imutável de formulário. Campanhas antigas devem continuar ligadas à versão usada.

Adicionar módulos oficiais futuros, sem permitir edição pelo cliente:

- Cultura Organizacional;
- Liderança;
- Governança;
- Controladoria e Desempenho;
- Jurídico, Integridade e Compliance;
- Pessoas e Talentos;
- Processos e Eficiência;
- Resultados e Crescimento;
- Transformação Organizacional;
- Integração de Unidades;
- Maturidade de Franquias.

Adicionar ao final das pesquisas oficiais o campo qualitativo:

> Em suas palavras, como você se sente trabalhando nesta empresa hoje?

Regras:

- texto longo;
- até 2.000 caracteres;
- opcional por padrão;
- sem pontuação;
- não altera índice;
- aviso para não informar dados pessoais desnecessários;
- disponível em todos os idiomas;
- relatórios devem respeitar anonimização mínima.

---

# 7. Estrutura empresarial e análise integrada

Implementar conceitos distintos:

- organização contratante;
- grupo econômico;
- pessoa jurídica;
- CNPJ raiz;
- matriz/filial;
- marca;
- unidade;
- setor/departamento;
- ciclo de diagnóstico;
- escopo da campanha.

Cada resposta deve salvar snapshot imutável de:

- organização;
- grupo;
- pessoa jurídica;
- unidade;
- setor;
- ciclo;
- versão do formulário;
- idioma;
- perfil do participante;
- anonimato;
- data de conclusão.

O motor de análise integrada deve calcular:

- resultado geral;
- resultado por dimensão;
- resultado por setor;
- resultado por unidade;
- resultado por empresa;
- resultado por grupo;
- evolução histórica;
- dispersão;
- alinhamento entre liderança e colaboradores;
- recorrência de fragilidades;
- correlação entre pesquisas;
- impacto de planos de ação;
- mapa de tensões;
- insights transversais.

Correlação não pode ser apresentada como causalidade.

---

# 8. CNPJ obrigatório e internacionalização

## 8.1 CNPJ

O CNPJ deve ser obrigatório para:

- criação da conta;
- diagnóstico gratuito;
- cadastro de pessoa jurídica;
- Enterprise multiempresa;
- configuração de franquia;
- relatório empresarial.

Validar dígitos no backend e consultar serviço externo por cliente HTTP tipado, timeout, retry controlado e circuit breaker.

Guardar:

- razão social;
- nome fantasia;
- situação cadastral;
- abertura;
- matriz/filial;
- natureza jurídica;
- porte;
- capital social;
- CNAE principal e secundários;
- endereço;
- município e UF;
- origem e data da consulta.

Se a API externa falhar, salvar como pendente sem perder dados e permitir revisão de Admin Valora.

## 8.2 Idiomas

Implementar:

- `pt-BR`;
- `en-US`;
- `es-ES`;
- `zh-CN`.

Traduzir e versionar:

- interface;
- formulários;
- perguntas;
- opções;
- termos LGPD;
- resultados;
- recomendações;
- certificados;
- relatórios;
- e-mails;
- WhatsApp;
- erros e validações.

Não traduzir automaticamente em runtime as pesquisas oficiais. Cada tradução deve manter o mesmo ID lógico, peso, pontuação e versão.

---

# 9. Banco PostgreSQL canônico

## 9.1 Arquivo obrigatório

Criar:

```text
backend/database/postgresql/script_completo.sql
```

Este será o único bootstrap canônico do banco.

Atualizar todos os scripts, documentação, Docker, CI, MigrationRunner e ferramentas Windows/Linux para utilizar `script_completo.sql`.

Arquivar ou remover da execução automática os scripts completos duplicados, evitando duas fontes concorrentes. Não apagar migrations históricas necessárias sem preservar rastreabilidade.

## 9.2 Regras de idempotência

O script deve executar sem erro:

1. em banco vazio;
2. novamente no mesmo banco;
3. em banco parcialmente criado;
4. em banco da versão anterior;
5. após falha intermediária corrigida.

Usar:

- `CREATE SCHEMA IF NOT EXISTS`;
- `CREATE EXTENSION IF NOT EXISTS`;
- `CREATE TABLE IF NOT EXISTS`;
- `ALTER TABLE ... ADD COLUMN IF NOT EXISTS`;
- `CREATE INDEX IF NOT EXISTS`;
- `CREATE OR REPLACE FUNCTION`;
- `DROP TRIGGER IF EXISTS` seguido de `CREATE TRIGGER`;
- blocos `DO $$ ... $$` consultando `pg_constraint`, `pg_class`, `pg_trigger` e `information_schema` antes de criar constraints que não suportem `IF NOT EXISTS`;
- `INSERT ... ON CONFLICT ... DO UPDATE/NOTHING` para seeds;
- nomes explícitos para PK, FK, UK, CK, índices e triggers.

Não usar `DROP TABLE`, `TRUNCATE`, exclusão destrutiva ou recriação que apague dados.

## 9.3 Estruturas mínimas obrigatórias

O script deve consolidar, com FKs e índices:

- schema e migrations;
- organizações;
- grupos econômicos;
- pessoas jurídicas;
- unidades;
- departamentos;
- marcas;
- endereços;
- usuários;
- perfis;
- roles;
- permissions;
- role_permissions;
- sessions;
- refresh tokens;
- password reset;
- MFA quando implementado;
- planos;
- limites;
- capacidades;
- assinaturas;
- faturas;
- consumo mensal e vitalício;
- módulos;
- módulos por organização;
- configurações;
- branding;
- formulários;
- versões;
- traduções;
- dimensões;
- perguntas;
- opções;
- pesquisas;
- ciclos;
- escopos;
- links;
- convites;
- participantes;
- respostas;
- respostas por pergunta;
- scores;
- scores por dimensão;
- recomendações;
- insights cruzados;
- snapshots analíticos;
- comparativos;
- certificados;
- validações de certificado;
- relatórios;
- exportações;
- templates de e-mail;
- jobs de e-mail;
- jobs de WhatsApp;
- comunicações;
- notificações;
- planos de ação;
- comentários e evidências;
- consentimentos LGPD;
- solicitações de privacidade;
- suporte, tickets, mensagens, categorias, SLA e base de conhecimento;
- integrações;
- API keys;
- webhooks;
- eventos de integração;
- outbox;
- idempotency keys;
- auditoria;
- logs operacionais;
- eventos de sistema;
- batches de migração;
- fontes;
- registros;
- mapeamentos;
- conflitos;
- checkpoints;
- itens de rollback;
- backup/restore metadata quando necessário.

## 9.4 Qualidade do schema

- UUID com `gen_random_uuid()`;
- `timestamptz` em UTC;
- `citext` para e-mails e slugs quando adequado;
- soft delete padronizado;
- `created_at`, `updated_at`, `created_by`, `updated_by`;
- constraints de status e valores;
- índices em todas as FKs e filtros frequentes;
- unique constraints compostas por tenant;
- isolamento por `organization_id`;
- CNPJ normalizado e único no escopo correto;
- token apenas em hash;
- payload sensível nunca em texto aberto;
- limites ilimitados representados de forma explícita e não por números mágicos como `2147483647`;
- evitar `jsonb` como substituto de modelagem relacional para dados essenciais;
- `jsonb` permitido para snapshots, metadata e payloads versionados.

## 9.5 Teste obrigatório do banco

Criar teste automatizado que:

1. sobe PostgreSQL descartável;
2. executa `script_completo.sql`;
3. executa o mesmo arquivo novamente;
4. valida tabelas, colunas, constraints, triggers, índices e seeds;
5. executa operações mínimas de insert/update/delete lógico;
6. valida `updated_at`;
7. valida FKs e unicidade;
8. valida plano e pesquisa oficial;
9. falha o CI se qualquer comando gerar erro.

---

# 10. Migração real Firebase → PostgreSQL

## 10.1 Princípio

A migração atual deve ser corrigida. Não basta gravar `migration_mappings`. O apply deve inserir ou atualizar os dados reais nas tabelas finais.

## 10.2 Fontes a inventariar

Mapear todas as coleções e subcoleções do legado, incluindo, conforme existência real:

- `settings`;
- `plans`;
- `modules`;
- `companies`;
- `organizations`;
- `organizationSlugs`;
- `users`;
- `companyUsers`;
- `participants`;
- `employees`;
- `forms`;
- dimensões, perguntas e opções embutidas ou separadas;
- `surveys`;
- `surveyLinks`;
- `publicLinks`;
- `invitations`;
- `responses`;
- answers e scores;
- tokens de resultado;
- `certificates`;
- `communications`;
- `emailJobs`;
- `notifications`;
- `actionPlans`;
- `invoices`;
- `supportConversations`;
- `supportMessages`;
- `supportTickets`;
- `supportCategories`;
- `supportSlaPolicies`;
- `knowledgeBase`;
- `integrations`;
- `apiKeys`;
- `webhooks`;
- `integrationLogs`;
- `consents`;
- `privacyRequests`;
- `logs`;
- `systemLogs`;
- outras coleções encontradas em `app.js`, repositories, Functions, rules, seeds e backups.

## 10.3 Ferramenta de migração

O projeto `Valora.Migration` deve possuir comandos:

- `inventory`;
- `export`;
- `validate`;
- `dry-run`;
- `apply`;
- `reconcile`;
- `resume`;
- `rollback`;
- `readiness`;
- `report`.

Aceitar export oficial do Firestore, JSON normalizado e, quando configurado de forma segura, leitura administrativa do Firebase apenas no ambiente de migração.

Nenhuma service account deve ser versionada.

## 10.4 Ordem de importação

Respeitar dependências:

1. settings, planos, capacidades e módulos;
2. organizações e grupos;
3. pessoas jurídicas, unidades e departamentos;
4. usuários e permissões;
5. formulários, versões, dimensões, perguntas e opções;
6. pesquisas, ciclos, links e convites;
7. participantes, respostas, answers e scores;
8. certificados, relatórios e exportações;
9. comunicações, e-mails e notificações;
10. planos de ação;
11. LGPD;
12. suporte;
13. integrações;
14. billing;
15. auditoria e histórico permitido.

## 10.5 Identificadores e idempotência

- manter tabela de mapeamento legado → UUID;
- armazenar coleção, ID legado, tipo de destino e UUID;
- usar chave única por fonte/coleção/ID;
- permitir retomar após falha;
- não duplicar dados ao executar novamente;
- usar transações por lote;
- checkpoints;
- checksum da fonte;
- contagem por entidade;
- relatório de divergências;
- amostragem de conteúdo;
- reconciliação financeira e de respostas.

## 10.6 Firebase Authentication

Inventariar usuários do Firebase Auth.

Como senhas não devem ser copiadas de forma insegura:

- importar identidade, e-mail, nome, status, claims e vínculos;
- não registrar hashes Firebase em logs ou UI;
- marcar conta como `password_reset_required` quando não houver migração criptográfica homologada;
- emitir fluxo seguro de redefinição;
- preservar bloqueios e status;
- mapear claims para roles/permissions oficiais.

## 10.7 Tokens

- nunca importar token bruto para log ou relatório;
- migrar somente hash quando compatível e seguro;
- preferir regenerar links de pesquisa, resultado e certificados;
- manter janela de compatibilidade controlada somente durante cutover;
- expirar tokens legados após validação.

## 10.8 Rollback

Rollback deve ser real:

- registrar before-image quando houver update;
- registrar IDs inseridos;
- excluir logicamente ou restaurar valores somente do batch;
- nunca apagar dados anteriores ao batch;
- emitir relatório;
- exigir confirmação explícita e perfil autorizado.

---

# 11. Segurança obrigatória

1. Multi-tenant por `organization_id` em todas as queries.
2. Usuário de empresa nunca acessa outra empresa.
3. Gestor de área limitado ao setor autorizado.
4. Admin de grupo vê somente empresas autorizadas.
5. Queries sempre parametrizadas.
6. Nenhum SQL concatenado com entrada do usuário.
7. Token armazenado apenas como hash SHA-256 ou algoritmo adequado.
8. Senha com hasher forte e salt.
9. Refresh token rotativo e revogável.
10. Cookie Secure, HttpOnly e SameSite adequado.
11. Antiforgery na Web.
12. CORS restritivo.
13. CSP, HSTS e headers de segurança.
14. Rate limiting em login, cadastro, pesquisa pública, resultado e recuperação.
15. Validação e sanitização de upload.
16. Proteção SSRF em webhooks e integrações.
17. HMAC em webhooks.
18. API keys exibidas uma única vez e armazenadas em hash.
19. Auditoria append-only.
20. Logs sanitizados.
21. Nenhum segredo em Git, JavaScript, JSON público ou View.
22. LGPD com minimização, retenção, anonimização e trilha de consentimento.
23. Comentários qualitativos protegidos por amostra mínima.
24. Nenhum stack trace em produção.

---

# 12. Testes e gates obrigatórios

## 12.1 Testes

Criar e corrigir:

- testes unitários de domínio;
- testes de Application;
- testes de repositories Dapper;
- testes de integração PostgreSQL;
- testes de autenticação e autorização;
- testes de isolamento multiempresa;
- testes de planos e limites;
- testes da pesquisa oficial e cálculo 125 pontos;
- testes de pesquisa pública;
- testes de resultado por token;
- testes de certificado;
- testes de e-mail/outbox;
- testes de LGPD;
- testes de CNPJ;
- testes de tradução;
- testes de migração com fixtures reais anonimizadas;
- testes de execução dupla do SQL;
- testes E2E desktop e mobile;
- testes de regressão dos bugs de login móvel, CORS, link de resultado e certificado.

## 12.2 Architecture tests

Falhar quando houver:

- Domain referenciando Infrastructure/API/Web;
- Application referenciando Infrastructure;
- Web acessando banco;
- Web usando Firebase;
- mais de um tipo top-level por arquivo;
- controller contendo Dapper/SQL;
- repository sem interface;
- classe duplicada;
- dependência circular;
- `NotImplementedException`;
- TODO de produção;
- dados fake;
- método de sucesso simulado;
- segredo ou connection string versionado.

## 12.3 Comandos mínimos

A entrega deve passar, em ambiente com SDK .NET 10 e PostgreSQL/Docker:

```bash
dotnet --info
dotnet restore backend/Valora.sln
dotnet build backend/Valora.sln -c Release --no-restore
dotnet test backend/Valora.sln -c Release --no-build
dotnet format backend/Valora.sln --verify-no-changes
dotnet list backend/Valora.sln package --vulnerable --include-transitive
npm ci
npm run check
npm run security:check
npm run backend:official-validate
npm run backend:sql-schema-validate
npm run db:scriptbd-validate
```

Atualizar os scripts npm para o novo nome `script_completo.sql` e criar um comando específico, por exemplo:

```bash
npm run db:banco-completo-validate
```

Nenhum teste pode ser marcado como sucesso se não foi executado. Registrar impedimentos reais.

---

# 13. Migração visual e experiência

A Web oficial deve reproduzir e melhorar as jornadas do legado:

- Home comercial;
- diagnóstico gratuito;
- pesquisa pública;
- resultado;
- certificado;
- login e cadastro;
- dashboards;
- planos;
- empresas;
- usuários;
- formulários;
- pesquisas;
- respostas;
- relatórios;
- LGPD;
- e-mail;
- WhatsApp;
- suporte;
- configurações.

Requisitos:

- Bootstrap 5;
- responsivo;
- mobile-first;
- acessibilidade;
- mensagens em linguagem clara;
- modais, toasts e loading reais;
- sem scroll horizontal indevido;
- botões com estado de processamento;
- prevenção de envio duplicado;
- estados vazios honestos;
- nenhum texto técnico para usuário final;
- identidade Valora Group;
- compatibilidade desktop e celular.

---

# 14. Entregáveis obrigatórios

1. Solution completa em .NET 10.
2. Todos os projetos compilando.
3. Código separado e formatado.
4. Interfaces para repositories e serviços externos.
5. Domínio preenchido com entidades reais.
6. API funcional.
7. Web MVC funcional.
8. Worker funcional.
9. Ferramenta de migração funcional.
10. `backend/database/postgresql/script_completo.sql` idempotente.
11. Seeds oficiais dos quatro planos.
12. Pesquisa Valora versionada nos quatro idiomas.
13. Migração real de dados com dry-run/apply/reconcile/rollback.
14. Testes automatizados.
15. Docker Compose de homologação.
16. Configuração IIS/Linux sem segredo.
17. Documentação de instalação.
18. Mapa Firebase → PostgreSQL.
19. Runbook de migração.
20. Checklist de homologação.
21. Plano de cutover.
22. Plano de rollback.
23. Relatório de paridade funcional.
24. PR com commits organizados e descrição completa.

---

# 15. Critérios de aceite finais

A implementação só estará concluída quando:

- o build Release for zero erro;
- todos os testes executáveis passarem;
- `script_completo.sql` executar duas vezes sem erro;
- nenhuma tabela/constraint/trigger/index duplicado causar falha;
- nenhuma funcionalidade produtiva depender de Firebase;
- nenhuma página oficial usar dados fake;
- nenhum método retornar sucesso sem persistência real;
- o importador inserir dados reais nas tabelas finais;
- reconciliação confirmar contagens e relacionamentos;
- autenticação e recuperação funcionarem;
- pesquisa pública funcionar;
- resultado por link funcionar;
- certificado gerar, baixar e validar;
- e-mail entrar na fila e ser enviado/processado corretamente;
- planos bloquearem limites no backend;
- Gratuito aceitar apenas uma resposta vitalícia por CNPJ;
- Profissional aceitar um CNPJ e uma unidade;
- Corporativo aceitar uma pessoa jurídica e várias unidades;
- Enterprise aceitar vários CNPJs;
- análises entre pesquisas/setores/unidades/empresas respeitarem o plano;
- os quatro idiomas funcionarem;
- CNPJ obrigatório e consulta econômica funcionarem;
- isolamento multiempresa for comprovado;
- login móvel e layout responsivo forem homologados;
- backup e rollback forem testados;
- o legado puder ser desligado sem perda funcional ou de dados.

---

# 16. PROMPT COMPLETO PARA O CODEX

## Instrução

Analise profundamente o repositório `devmnsoft/valoregrouppesquisa`, tomando este arquivo como requisito obrigatório e fonte de escopo. Implemente a migração completa do sistema para ASP.NET Core 10, Clean Code, DDD, Dapper e PostgreSQL, reaproveitando e corrigindo exclusivamente a solution oficial `backend/Valora.sln`.

Não crie `backend-v3`, outra solution, React, Angular, Vue, Vite, SPA paralela ou novo produto. `projeto .NET predecessor removido` e o legado Firebase da raiz são somente fontes de comparação e não devem receber novas funcionalidades. A versão final de produção deve executar apenas `Valora.Web`, `Valora.Api`, `Valora.Worker` e PostgreSQL, sem Firebase no runtime.

### Antes de alterar

1. Leia este documento inteiro.
2. Faça inventário da raiz, `functions`, regras Firestore, repositories JavaScript, serviços JavaScript, `backend`, `projeto .NET predecessor removido`, banco, testes, scripts e documentação.
3. Liste todas as funcionalidades do legado e mapeie cada uma para API, Application, Domain, Infrastructure, Web, Worker e banco.
4. Identifique classes vazias, tipos duplicados, arquivos com vários tipos, métodos simulados, HTTP 501, TODOs, dados fake, SQL incompatível e dependências Firebase.
5. Registre um baseline de build/testes antes das alterações.
6. Não apague dados, arquivos de marca ou código legado antes de existir paridade comprovada.

### Implementação arquitetural

1. Atualize todos os projetos para `net10.0`.
2. Crie `global.json`, `Directory.Build.props`, `Directory.Packages.props` e `.editorconfig` coerentes.
3. Atualize pacotes para versões estáveis compatíveis com .NET 10.
4. Corrija todas as referências entre projetos e remova dependências circulares.
5. Mantenha Domain puro.
6. Mantenha Application dependente apenas de Domain.
7. Mantenha Infrastructure implementando contratos da Application.
8. Mantenha API e Web sem SQL.
9. Faça Web consumir somente API por clientes HTTP tipados.
10. Adicione `Valora.Worker` e `Valora.Migration` à solution quando necessários.
11. Separe todo tipo top-level em arquivo próprio.
12. Desmonte arquivos concentradores como `OperationalServices.cs`, `OperationalRepositories.cs` e `IOperationalServices.cs`.
13. Formate todo código; não deixe C# ou JavaScript comprimido em uma linha.
14. Injete `ILogger<T>` nas fronteiras críticas.
15. Use tratamento de exceção contextual, middleware global, correlação e sanitização.
16. Adicione `CancellationToken` em I/O.
17. Remova `NotImplementedException`, TODO produtivo, sucesso simulado, dados fake e destinatários fixos de desenvolvimento.

### Banco

1. Crie `backend/database/postgresql/script_completo.sql` como fonte canônica.
2. Consolide todo o schema necessário do legado e do backend.
3. Atualize ferramentas, runners, Docker, CI e documentação para esse arquivo.
4. Faça o script idempotente para tabelas, colunas, PKs, FKs, UKs, CKs, índices, functions, triggers e seeds.
5. Para constraints sem `IF NOT EXISTS`, use blocos `DO $$` consultando catálogos PostgreSQL.
6. Não use `DROP TABLE`, `TRUNCATE` ou operação destrutiva.
7. Crie FKs e índices faltantes.
8. Modele grupos econômicos, pessoas jurídicas, unidades, departamentos, ciclos, versões, traduções, insights, outbox, idempotência e migração.
9. Use UUID, timestamptz, citext, soft delete, auditoria e isolamento por organização.
10. Não use número mágico para ilimitado.
11. Inclua seeds idempotentes dos planos `free`, `professional`, `corporate`, `enterprise`, roles, permissions, modules e pesquisa oficial.
12. Crie teste que execute o script duas vezes no mesmo PostgreSQL e valide o schema.

### Migração Firebase

1. Corrija a migração atual para inserir dados reais, não apenas mappings.
2. Implemente o console `Valora.Migration` com inventory, validate, dry-run, apply, reconcile, resume, rollback, readiness e report.
3. Mapeie todas as coleções e subcoleções reais encontradas.
4. Importe na ordem correta de dependências.
5. Use transações por lote, checkpoints, checksums e idempotência.
6. Preserve IDs legados em tabela de mapeamento.
7. Gere relatório de totais, conflitos, registros ignorados e divergências.
8. Importe usuários sem expor hashes; force redefinição de senha quando necessário.
9. Regenere tokens públicos de forma segura.
10. Implemente rollback real por batch.
11. Adicione fixtures anonimizadas e testes de migração.
12. Não acessar Firebase em runtime após o cutover.

### Funcionalidades

Implemente paridade real de todas as funções listadas neste documento, incluindo Home, cadastro, login, perfis, planos, empresas, usuários, formulários, pesquisas, links, respostas, resultados, certificados, relatórios, e-mail, WhatsApp, dashboards, comparativos, ações, notificações, LGPD, suporte, integrações, auditoria, configurações, white label e exportações.

Implemente integralmente as novas regras:

- quatro planos oficiais;
- uma resposta vitalícia por CNPJ no Gratuito;
- um CNPJ/uma unidade no Profissional;
- uma pessoa jurídica/várias unidades no Corporativo;
- vários CNPJs e grupo econômico no Enterprise;
- pesquisa oficial imutável;
- análises cruzadas;
- campo qualitativo;
- CNPJ obrigatório;
- português, inglês, espanhol e mandarim;
- prévia de WhatsApp;
- relatórios por escopo e plano;
- anonimização mínima.

### Segurança

1. Garanta tenant em todas as queries.
2. Parametrize todo SQL.
3. Proteja cookies, JWT, refresh tokens, antiforgery, CORS, CSP, HSTS e rate limits.
4. Armazene tokens/API keys apenas em hash.
5. Não exponha segredos, payloads ou stack traces.
6. Aplique autorização por role, permission, organização, empresa, unidade e setor.
7. Audite operações críticas.
8. Valide uploads, webhooks e integrações.
9. Mantenha LGPD e anonimização.

### Testes e validação

1. Crie testes unitários, integração, arquitetura e E2E.
2. Teste PostgreSQL real descartável.
3. Teste execução dupla de `script_completo.sql`.
4. Teste isolamento multiempresa.
5. Teste limites dos planos.
6. Teste pesquisa pública e cálculo.
7. Teste resultado, certificado, e-mail e recuperação.
8. Teste migração real e rollback.
9. Teste desktop e mobile.
10. Execute todos os comandos definidos neste documento.
11. Não declare teste como aprovado se não foi executado.

### Forma de trabalho

1. Trabalhe em branch própria baseada na `main` atual.
2. Faça commits pequenos e coerentes por camada/fase.
3. Não faça big-bang sem checkpoints.
4. Preserve compatibilidade enquanto a migração não estiver homologada.
5. Atualize a documentação conforme implementar.
6. Abra PR em modo draft durante a execução.
7. Na descrição da PR, informe:
   - baseline;
   - arquitetura final;
   - arquivos alterados;
   - migração realizada;
   - banco criado;
   - testes executados;
   - resultados;
   - riscos;
   - pendências reais;
   - passos de homologação;
   - rollback.
8. Não faça deploy de produção nem cutover automático.

### Proibições

- Não criar frontend paralelo.
- Não manter Firebase como fallback oculto na versão final.
- Não usar Entity Framework.
- Não acessar banco pela Web.
- Não usar repository sem interface.
- Não juntar várias classes no mesmo arquivo.
- Não criar catch vazio.
- Não retornar sucesso fake.
- Não deixar TODO de produção.
- Não ocultar falha com estado vazio enganoso.
- Não criar tabela sem PK e constraints adequadas.
- Não criar SQL destrutivo.
- Não versionar segredo.
- Não apagar o legado antes da confirmação de paridade e backup.
- Não afirmar conclusão parcial como migração completa.

### Saída final esperada

Entregue uma PR pronta para homologação contendo:

- solution .NET 10 compilável;
- código limpo e separado;
- DDD/Application/Infrastructure/API/Web/Worker/Migration coerentes;
- PostgreSQL completo em `script_completo.sql`;
- migração real Firebase → PostgreSQL;
- paridade funcional;
- novas regras dos planos;
- testes;
- runbooks;
- relatório de evidências;
- zero erro conhecido de build;
- nenhuma funcionalidade produtiva simulada;
- nenhuma dependência Firebase no runtime final.
