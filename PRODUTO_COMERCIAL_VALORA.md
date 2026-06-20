# Produto comercial Valora Pulse

## 1. O que é
Valora Pulse é uma plataforma SaaS multiempresa para criar questionários, enviar pesquisas, receber respostas, calcular indicadores de maturidade e publicar dashboards/relatórios para clientes da Valora Group.

## 2. Públicos
- **Valora**: administração comercial, consultoria, suporte e governança da plataforma.
- **Empresa cliente**: administradores, gestores de pesquisa, analistas e gestores de área.
- **Respondentes**: funcionários/participantes e convidados externos por link seguro.

## 3. Entidades
A nomenclatura técnica principal é **`organizations`** no Firebase. A interface mantém “Clientes/Empresas”. O modo local preserva `state.companies` como camada de compatibilidade. Entidades mínimas: `organizations`, `users`, `participants/employees` (também em `users` com `role` e `companyId`), `plans`, `modules`, `forms`, `surveys`, `responses`, `invitations`, `invoices`, `settings` e `logs`.

## 4. Perfis
A matriz oficial fica em `role-definitions.js`: `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`.

## 5. Planos
Planos possuem preço, status, destaque, padrão, limites (`maxActiveSurveys`, `maxResponsesMonth`, `maxManagers`, `maxEmployees`, `maxEmailsMonth` quando configurado), recursos comerciais e `enabledModules`. Apenas `admin_valora` deve criar/editar planos. Plano inativo não deve ser padrão nem destacado e não deve aparecer para novo cliente.

## 6. Módulos
A matriz oficial fica em `module-definitions.js` com clientes, usuários, funcionários, formulários, pesquisas, convites por e-mail, respostas, relatórios, certificados, financeiro, planos, módulos, ValoraBot, LGPD, exportações, benchmark, white label, backup e logs.

## 7. Cadastro de empresa
Uma empresa (`organizations`) nasce com dados comerciais, plano padrão ativo, status comercial (`trial`, `active`, `suspended`, `overdue`, `cancelled`, `inactive`) e pelo menos um `empresa_admin`. O formulário deve capturar administrador principal (`adminName`, `adminEmail`, `adminPhone`, `adminPosition`, `sendAccessInvite` e `initialPassword` apenas local/demo). Empresa sem responsável deve gerar alerta forte. Toda criação/alteração sensível deve gravar auditoria.

## 8. Funcionários/respondentes
Funcionários são perfis de empresa em `users`: nome, e-mail, telefone, cargo, departamento, role, status, preferências de e-mail, acesso ao portal, `isExternal`, `companyId`, criação e atualização. Empresa só cadastra `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`; nunca perfis globais Valora.

## 9. Questionários
Questionários (`forms`) pertencem a uma empresa ou são globais. Têm dimensões, perguntas, tipos, pesos, pontuação máxima e faixas de resultado. Gestores criam/alteram questionários da própria empresa conforme plano e módulo.

## 10. Pesquisas
Pesquisas (`surveys`) combinam empresa, formulário, prazo, token/link seguro, identificação, LGPD, status e regras de repetição. Empresa suspensa, inadimplente ou sem módulo/plano compatível não cria nova pesquisa.

## 11. Respostas e cálculo
Respostas concluídas alimentam dashboard, relatório e certificado pela mesma fonte. Rascunhos não entram no cálculo. Taxa de resposta = convites respondidos / convites enviados. Média geral usa respostas concluídas; dimensão usa perguntas daquela dimensão. Questão textual sem pontuação não derruba nota e múltipla escolha deve respeitar pontuação máxima.

## 12. Relatórios e dashboards
Dashboards são agregações por empresa, período, pesquisa, dimensão e área. `gestor_area` só vê a própria área; `analista_resultados` vê resultados, mas não cria pesquisa. Certificados usam o mesmo resultado persistido da resposta.

## 13. Limites do plano
Planos controlam módulos e limites de uso: pesquisas ativas, respostas/mês, gestores, funcionários/respondentes e e-mails/mês. Exceder limite deve bloquear nova ação comercial sem apagar dados existentes.

## 14. Permissões
Permissões são centralizadas em `role-definitions.js`, aplicadas no menu/botões e reforçadas nas Firestore Rules. O frontend nunca é fonte de verdade de segurança. Respostas públicas e logs devem passar por Cloud Functions/Admin SDK, não por escrita direta do navegador.
