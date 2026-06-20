# Produto comercial Valora Pulse

Esta evolução consolida a base SaaS multiempresa do Valora Pulse: perfis, módulos, planos, limites, cadastro de empresas, funcionários/respondentes e controles de acesso.

## Fluxo comercial recomendado

1. `admin_valora` cadastra plano e módulos liberados.
2. `admin_valora` cadastra a empresa com dados fiscais/contato/endereço.
3. `admin_valora` cria o administrador principal da empresa (`empresa_admin`).
4. `empresa_admin` cadastra funcionários com perfil e departamento quando aplicável.
5. `gestor_pesquisa` cria formulários/pesquisas e envia convites se o plano permitir.
6. `analista_resultados` acompanha respostas e relatórios.
7. `gestor_area` consulta apenas dados da própria área.
8. `participante` e `convidado_externo` respondem pesquisas sem acesso administrativo.

## Segurança multiempresa

- `companyId` delimita escopo operacional.
- Perfis globais não podem ser criados por empresas.
- Firestore Rules reconhecem `analista_resultados`, `gestor_area` e `convidado_externo`.
- Respostas públicas continuam bloqueadas no Firestore direto e passam por Cloud Function com token.
- Logs continuam sem escrita direta pelo frontend.

## Próximo passo sugerido

Finalizar Firebase Repository com dados reais do Firestore.

## Fluxo comercial operacional Firebase

A jornada SaaS validada para homologação é: seed mínimo, primeiro `admin_valora`, criação de organização, criação do admin da empresa, cadastro de funcionários, formulário, pesquisa, convite por e-mail via Cloud Function, resposta por link seguro, cálculo backend, dashboards, respostas, relatórios/certificados e auditoria. O modo local/demo continua disponível para demonstração com outbox local.

## Evolução comercial — onboarding guiado

A experiência comercial passa a ter uma jornada prescritiva no dashboard da empresa. O componente **Primeiros passos da implantação** mostra percentual concluído, etapa atual, próximo passo, alertas de plano/perfil e CTAs contextuais para dados da empresa, plano, administrador, funcionários, questionários, pesquisas, convites, respostas e relatórios.

O botão **Configurar minha primeira pesquisa** abre um wizard de seis etapas: revisão cadastral, cadastro rápido de funcionários, escolha/criação do questionário, configuração da pesquisa com LGPD, preparação do envio e conclusão. Quando o cliente não possui questionário, o fluxo cria o **Diagnóstico Essencial de Maturidade** com dimensões de Cultura, Processos e Liderança.

O Admin Valora passa a enxergar o **Status de implantação do cliente** em visão global, incluindo empresas novas, em configuração, com funcionários, com pesquisa criada, com convites, com respostas, implantadas e travadas. Isso melhora demonstrações, CS e priorização comercial sem remover funcionalidades existentes.

Estados vazios inteligentes foram adicionados para funcionários, formulários, pesquisas, respostas e relatórios, sempre com orientação e CTA para a próxima tela. O ValoraBot também responde perguntas de onboarding como “Como começo?”, “Qual próximo passo?” e “Por que meu dashboard está vazio?” considerando o estágio real da empresa.
## Evolução — dashboards executivos e indicadores centralizados

### Camada central de indicadores

A evolução adiciona `analytics-service.js` como fonte única para dashboards, respostas, relatórios e exportações. As principais funções são:

- `getGlobalDashboardMetrics(state)`: clientes, MRR, planos, convites, respostas, implantação e alertas globais.
- `getCompanyDashboardMetrics(state, companyId)`: uso do plano, implantação, convites, respostas, dimensões e recomendações da empresa.
- `getSurveyMetrics(state, surveyId)`: taxa de resposta e resultado de uma pesquisa.
- `getResponseMetrics(state, filters)`: respostas concluídas, média `normalized5`, percentual, dimensões, distribuição e extremos.
- `getPlanUsageMetrics(state, companyId)`: uso versus limites de plano.
- `getOnboardingMetrics(state, companyId)`: status de implantação.
- `getDimensionMetrics(responses)`: maturidade por dimensão.
- `getInvitationMetrics(state, companyId)`: convites enviados e taxa de resposta.
- `getTimeSeriesMetrics(state, filters)`: evolução mensal.

### Regras comerciais dos indicadores

- Resposta só entra nos indicadores se estiver concluída (`completed`, `finished`, `answered`, `done` ou equivalentes normalizados).
- Convite enviado só conta com status `sent`, `opened`, `answered` ou `resent`.
- Taxa de resposta = respostas concluídas únicas / convites enviados únicos.
- Duplicidades usam `id` da resposta/convite ou a combinação pesquisa + e-mail.
- Média geral usa `normalized5`.
- Percentual = `normalized5 / 5 * 100`.
- Dimensão consolida apenas os resultados dimensionais gravados em `byDimension`.
- Estados vazios mostram orientação de próxima ação.
- Alertas automáticos cobrem baixa taxa de resposta, dimensão crítica e alto uso do plano.

### Dashboards

#### Admin Valora

Exibe visão comercial com clientes totais, ativos, implantação, travados, MRR, plano mais utilizado, respostas no mês, taxa de resposta, clientes por plano, implantação, MRR por plano, evolução e atenção comercial.

#### Empresa

Exibe funcionários, pesquisas ativas, convites enviados, respostas concluídas, taxa, média geral, dimensão forte/crítica, uso do plano, evolução, distribuição e recomendações.

#### Perfis

- `analista_resultados` recebe visão analítica sem ações de criação/envio.
- Gestores e administradores continuam com ações rápidas quando suas permissões permitem.
- Escopo de empresa continua respeitando `companyId` e funções existentes de permissão.

## Evolução comercial — relatórios executivos premium

A Central de relatórios passa a operar com uma camada central (`report-service.js`) que consome os indicadores consolidados do `analytics-service.js`. Isso reduz risco de números divergentes entre dashboards, respostas e arquivos exportados.

A entrega comercial agora contempla relatórios para Admin Valora, empresas clientes, pesquisas, dimensões, implantação, uso de plano e participantes individuais. Os documentos trazem resumo executivo automático, recomendações consultivas, plano de ação sugerido, métricas principais e tabelas compatíveis com PDF, Word, Excel, CSV e JSON.

O visual mantém a identidade Valora Group, com linguagem executiva e estrutura adequada para diretoria, RH, governança, controladoria e consultores. Estados sem dados são tratados com mensagens orientativas, e cada exportação gera auditoria.

## Evolução comercial: Plano de ação

O módulo **Plano de ação** transforma diagnósticos em execução acompanhável. Ele permite demonstrar valor consultivo contínuo, pois conecta dimensões críticas, baixa adesão e recomendações executivas a responsáveis, prazos, evidências e evolução por cliente.
