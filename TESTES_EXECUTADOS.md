# Testes executados — Valora Group™ 8.6.0

## Validação técnica

- sintaxe JavaScript de `app.js` e `pdf.js`;
- compilação Python de `server.py`;
- servidor vinculado somente a `127.0.0.1`;
- seleção automática de porta;
- endpoint de saúde;
- ausência da senha SMTP no pacote.

## Jornada pública

- home carrega sem tela branca;
- logo, WhatsApp e ValoraBot aparecem;
- ValoraBot abre e fecha;
- logo do cabeçalho e rodapé retorna à home;
- bloco de 5 minutos contém explicação;
- pesquisa escolhida pelo administrador aparece em destaque;
- layout validado em desktop, tablet e celular sem overflow do documento.

## Administrador geral

- login;
- dashboards e menus;
- criação, edição e exclusão de usuário com confirmação;
- editor de formulário com os seis tipos de pergunta;
- criação, edição e revisão de formulário;
- criação instantânea de pesquisa;
- link com `survey`, `token` e validade;
- compartilhamento por WhatsApp e e-mail;
- escolha da pesquisa da home;
- módulos com confirmação;
- logs e backup somente no perfil global;
- relatório PDF baixado e validado.

## Empresa

- login e dashboard;
- isolamento do menu;
- ausência de backup e logs;
- acesso a formulários, pesquisas, respostas, relatórios e plano da própria empresa.

## Participante

- abertura de link seguro;
- identificação e aceite LGPD;
- resposta ao questionário;
- cálculo e tela de resultado;
- navegação para área pessoal mesmo após URL com query string;
- histórico e certificados.

## Documentos

Foram gerados e inspecionados:

- relatório global PDF;
- relatório individual PDF;
- certificado PDF.

Os arquivos foram reconhecidos como PDF 1.4, com página A4 ou paisagem, e renderizados para inspeção visual sem corte crítico ou sobreposição.

## E-mail

- configuração em modo caixa de saída;
- geração de convite `.eml`;
- geração de resultado `.eml`;
- identidade visual e remetente configurável;
- senha ausente do frontend e do pacote.

## Matriz manual complementar — Firebase e frontend

> Status inicial: pendente de execução em ambiente com Firebase Auth, Firestore Rules e Cloud Functions apontando para o projeto correto. Registrar evidências, navegador, usuário, empresa e data em cada item.

### Autenticação Firebase

- [ ] Login Firebase com usuário ativo: autentica, carrega perfil no Firestore e exibe menus compatíveis com o papel.
- [ ] Logout: encerra sessão Firebase, limpa estado visual da aplicação e impede retorno por histórico do navegador a áreas autenticadas.
- [ ] Usuário inativo: autenticação pode ocorrer, mas a aplicação bloqueia uso após leitura do perfil inativo e mostra mensagem adequada.
- [ ] Usuário sem perfil: autenticação sem documento em `users/{uid}` não libera menus nem dados internos.

### Perfis autenticados

- [ ] Admin Valora: acessa empresas, usuários, planos, logs, backups, pesquisas, respostas e relatórios globais.
- [ ] Empresa Admin: acessa somente dados da própria empresa, cria usuários permitidos e não visualiza logs globais, backup global ou empresas de terceiros.
- [ ] Participante: acessa próprio perfil, histórico e certificados; não visualiza dados administrativos ou respostas de terceiros.

### Jornada pública

- [ ] Link público válido: abre pesquisa via Cloud Function, sem leitura direta de `surveys` no client.
- [ ] Link público inválido/expirado/encerrado: exibe erro amigável e não mostra perguntas.
- [ ] Envio de resposta válido: registra resposta, mostra confirmação e gera token/resultado quando aplicável.
- [ ] Resultado: resultado público abre somente com token válido e não expõe dados de outras respostas.

### Responsividade e navegadores

- [ ] Mobile 360px: home, login, pesquisa pública, formulário e resultado não apresentam overflow horizontal nem botões inacessíveis.
- [ ] Desktop: dashboards, tabelas, modais, editor de formulário e relatórios renderizam sem quebras visuais críticas.

### Exportações

- [ ] Exportação global disponível apenas para Admin Valora.
- [ ] Exportação de empresa contém somente dados da empresa autenticada.
- [ ] PDF/CSV exportado não inclui tokens, hashes, dados de empresas terceiras ou campos sensíveis desnecessários.
