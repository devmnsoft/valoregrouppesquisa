# Release Candidate — Valora Pulse

## Versão da release

**Valora Group™ 8.6.4 RC1**

## Data

**2026-06-21**

## Objetivo da release

Formalizar a etapa de homologação final e release candidate do Valora Pulse, consolidando o escopo que deve ser validado antes de demonstração comercial, homologação com cliente, deploy controlado e eventual publicação em produção.

Esta etapa não introduz novas funcionalidades. O foco é validar estabilidade, segurança, experiência, Firebase, operação, relatórios, integrações, cobrança, logs, Telegram e fluxos ponta a ponta, registrando bugs e pendências sem mascarar falhas.

## Escopo validado

A validação desta release candidate cobre, em roteiro documental e checagens técnicas estáticas:

- inicialização local/demo;
- modo Firebase/produção preparado;
- autenticação e perfis;
- isolamento por empresa;
- criação e administração comercial de empresas;
- planos, limites e bloqueios comerciais;
- funcionários e perfis;
- questionários, perguntas, pesos, dimensões e faixas;
- pesquisas, links, tokens, validade e convites;
- participação pública com LGPD;
- respostas, cálculo, dashboards e relatórios;
- exportações PDF, Word, Excel, CSV e JSON;
- plano de ação;
- notificações;
- logs, Telegram e mascaramento;
- integrações, API keys e webhooks;
- financeiro, faturas e suspensão;
- responsividade mobile em 360px;
- regressão JavaScript com `node --check`.

## Funcionalidades incluídas

- Portal Admin Valora.
- Portal Empresa.
- Perfis operacionais: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`.
- Cadastro e gestão de empresas, planos, módulos e usuários.
- Questionários dinâmicos.
- Pesquisas com links públicos e tokens.
- Aceite LGPD.
- Dashboards por perfil.
- Relatórios e exportações.
- Plano de ação.
- Central de notificações.
- Logs de auditoria.
- Telegram operacional.
- E-mail em modo local e Functions em modo Firebase.
- Financeiro básico e cobrança.
- Firestore Rules e Cloud Functions como base de produção.

## Funcionalidades fora do escopo

- Desenvolvimento de novas funcionalidades de produto.
- Redesenho visual amplo.
- Troca de arquitetura.
- Migração automática de dados reais de clientes.
- Publicação efetiva em produção sem homologação manual real.
- Garantia de entrega SMTP real sem credenciais e ambiente configurado.
- Validação Firebase real sem projeto Firebase, Auth, claims, dados seed e emuladores/projeto publicados.
- Teste de carga, pentest formal e auditoria jurídica LGPD completa.

## Riscos conhecidos

- A persistência local por `localStorage` é adequada para demonstração, mas não substitui ambiente multiusuário de produção.
- A homologação Firebase real depende de ambiente externo com credenciais, regras publicadas, Functions publicadas, usuários, claims e dados seed.
- A entrega SMTP, Telegram, webhooks e integrações dependem de chaves e serviços externos configurados.
- A CSP ainda mantém exceção temporária para estilos inline, conforme documentação do README.
- Testes automatizados atuais cobrem principalmente sintaxe e partes específicas; a homologação funcional completa ainda precisa ser executada manualmente.
- Não houve evidência nesta etapa de teste real em múltiplos navegadores/dispositivos físicos.

## Bugs bloqueantes

Nenhum bug bloqueante foi confirmado por execução automatizada nesta etapa documental.

Se qualquer item abaixo falhar na homologação real, deve ser registrado como bloqueante em `BUGS_HOMOLOGACAO.md` e corrigido antes da aprovação final:

- login indisponível;
- cadastro de empresa ou usuário indisponível;
- resposta de pesquisa indisponível;
- cálculo incorreto;
- vazamento entre empresas;
- erro crítico em tela principal;
- falha grave de segurança;
- token ou segredo exposto no frontend.

## Bugs não bloqueantes

Nenhum bug não bloqueante foi confirmado por execução automatizada nesta etapa documental.

A matriz `BUGS_HOMOLOGACAO.md` deve ser preenchida durante a execução real da homologação.

## Pendências

- Executar checklist de homologação final item a item.
- Executar roteiro ponta a ponta em modo local/demo.
- Executar roteiro ponta a ponta em modo Firebase real ou emuladores.
- Registrar evidências de console, prints, exportações e logs.
- Validar mobile em 360px.
- Validar Firestore Rules com usuários e claims reais.
- Validar Functions com payloads válidos e inválidos.
- Validar SMTP, Telegram e integrações com credenciais reais.
- Classificar e corrigir bugs bloqueantes encontrados.

## Decisão final

**Aprovado com ressalvas.**

Justificativa: a release candidate está documentada, a regressão técnica de sintaxe JavaScript executada não encontrou falhas e não foram confirmados bugs bloqueantes nesta etapa. Porém, a aprovação para produção depende da execução real da homologação funcional, Firebase, integrações, mobile, segurança e evidências operacionais.
