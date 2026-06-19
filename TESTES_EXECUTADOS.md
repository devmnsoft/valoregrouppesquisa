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
