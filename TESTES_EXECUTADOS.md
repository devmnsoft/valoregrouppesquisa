# Testes executados — Valora Pulse

Data: 2026-06-19

## Escopo testado nesta entrega

Foram executadas verificações estáticas e uma revisão manual guiada das jornadas solicitadas. Como este ambiente não possui navegador interativo acoplado ao servidor local durante a execução, os cenários de clique foram documentados como roteiro obrigatório de QA manual para homologação.

## Checks automáticos executados

- `node --check app.js` — validação de sintaxe JavaScript concluída sem erros.

## Cenário 1 — Admin Valora

Roteiro manual:

1. Acessar `#login`.
2. Entrar com `admin@valoragroup.com` / `Valora@2026`.
3. Abrir dashboard global.
4. Cadastrar empresa em **Clientes**.
5. Cadastrar usuário empresa admin em **Usuários**.
6. Criar formulário global em **Formulários e provas**.
7. Criar pesquisa em **Pesquisas e links**.
8. Abrir **Respostas** e **Relatórios**.
9. Gerar relatório PDF.

Resultado esperado: admin vê menus globais, consegue criar empresas, formulários, pesquisas, usuários, respostas, relatórios, módulos, configurações, backup e logs.

## Cenário 2 — Empresa Admin

Roteiro manual:

1. Entrar com `gestor@empresa.com` / `Empresa@2026`.
2. Abrir **Empresa › Usuários**.
3. Cadastrar funcionário com nome, e-mail, telefone, cargo/função, área/departamento e status ativo.
4. Pesquisar por nome/e-mail e filtrar por status.
5. Selecionar funcionário, selecionar todos e limpar seleção.
6. Abrir **Formulários e provas**, criar formulário próprio.
7. Abrir **Pesquisas e links**, criar pesquisa ativa.
8. Compartilhar pesquisa e usar **Enviar para funcionários**.
9. Confirmar geração de convite e, no modo local, criação de `.eml` em `data/outbox`.
10. Acompanhar respostas, dashboards e relatórios da própria empresa.

Resultado esperado: empresa não escolhe outra empresa, não vê dados de terceiros e o convite fica registrado em `state.invitations`.

## Cenário 3 — Gestor de Pesquisa

Roteiro manual:

1. Entrar com `rh@empresa.com` / `Empresa@2026`.
2. Criar/clonar questionário da própria empresa.
3. Configurar perguntas com tipos escala, escolha única, múltipla, texto curto/longo e resposta correta.
4. Criar pesquisa e enviar convites.
5. Acompanhar respostas, gráficos e relatórios da empresa.

Resultado esperado: gestor não acessa financeiro global, empresas de terceiros, módulos críticos ou backup.

## Cenário 4 — Participante

Roteiro manual:

1. Abrir link seguro recebido por e-mail/outbox.
2. Preencher identificação.
3. Aceitar LGPD quando exigido.
4. Responder perguntas obrigatórias.
5. Concluir.
6. Ver resultado se configurado.
7. Entrar como participante e consultar histórico/certificado.

Resultado esperado: resposta concluída alimenta área de respostas, dashboard, relatório e certificado com os mesmos números.

## Cenário 5 — Cálculo

Roteiro manual:

1. Criar formulário com cinco perguntas: escala, escolha única, múltipla escolha, texto com `scoreWhenFilled` e resposta correta.
2. Configurar pesos, máximos e dimensões.
3. Responder com valores conhecidos.
4. Conferir manualmente:
   - escala = nota/5 × máximo × peso;
   - escolha única = score da alternativa × peso;
   - múltipla = soma das alternativas limitada ao máximo × peso;
   - texto sem score não prejudica nota;
   - resposta correta = máximo quando correta;
   - percentual = nota normalizada/5 × 100;
   - dimensão só consolida perguntas vinculadas.

Resultado esperado: `calculateSurveyResult(form, answers)` gera pontuação bruta, máxima, percentual, nota 0–5, dimensões e faixa final coerentes.

## Cenário 6 — Mobile 360px

Roteiro manual:

1. Ajustar viewport para 360px.
2. Testar login, menu e navegação.
3. Cadastrar funcionário.
4. Criar pergunta no editor.
5. Responder questionário.
6. Abrir dashboard e área de respostas.

Resultado esperado: tabelas permanecem legíveis com `data-label`, botões continuam acessíveis e não há quebra crítica de layout.

## Observações e riscos restantes

- Teste visual em navegador real ainda é necessário para validar responsividade e fluxo de modal em todos os dispositivos.
- Status `opened` e `expired` dependem de backend/Cloud Functions para rastreamento automático em produção.
- Envio SMTP real deve ser configurado apenas no servidor local ou em secrets do Firebase Functions.
- LGPD precisa de validação jurídica antes de produção.

## Gestão de perfis e permissões — 2026-06-20

### Teste 1 — Admin Valora
- Cadastrar empresa com plano/status obrigatórios.
- Criar administrador principal da empresa no cadastro do cliente.
- Criar consultor Valora via tela global de usuários.
- Verificar menu completo do `admin_valora` e menu reduzido do `consultor_valora`.
- Confirmar que ações críticas continuam auditadas.

### Teste 2 — Empresa Admin
- Cadastrar `participante`, `gestor_pesquisa`, `analista_resultados` e `gestor_area`.
- Validar que `gestor_area` exige departamento/área.
- Tentar criar `admin_valora` e `consultor_valora`; a matriz e validação bloqueiam.

### Teste 3 — Gestor Pesquisa
- Acessar menus de visão geral, pesquisas, formulários, funcionários/envio, respostas e relatórios.
- Criar questionário e enviar pesquisa.
- Verificar ausência de plano/financeiro e dados críticos da empresa.

### Teste 4 — Analista Resultados
- Acessar visão geral, respostas e relatórios.
- Verificar ausência de criação de questionários, envio e cadastro de funcionários.

### Teste 5 — Participante
- Acessar área pessoal, pesquisas disponíveis, histórico, certificados e LGPD.
- Tentar acessar admin/empresa; deve exibir acesso não permitido.

### Teste 6 — Convidado externo
- Abrir link público de pesquisa e responder.
- Confirmar ausência de menu administrativo/portal completo.

### Teste 7 — Segurança
- Tentar manipular `role`/`companyId` no payload local; `saveUser` bloqueia perfis globais por empresa e alteração do próprio perfil.
- Revisar `firestore.rules` para bloquear perfis globais por empresa, alteração comum de campos sensíveis e edição fora da própria empresa.
