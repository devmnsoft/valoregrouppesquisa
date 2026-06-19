# Documentação do Produto — Valora Group™ 8.6.0

## Proposta

O Valora Group transforma formulários de cultura, governança, controller e advisory em jornadas mensuráveis: configuração, convite seguro, resposta com consentimento, resultado, certificado e acompanhamento executivo.

A página inicial apresenta a pesquisa definida pelo Administrador geral como destaque. O bloco “5 minutos” comunica o tempo médio da experiência essencial e explica que o prazo pode variar conforme o formulário.

## Perfis e isolamento

### Administrador geral Valora

Responsável por toda a operação da plataforma. Pode criar e excluir clientes, usuários, planos, módulos, formulários, pesquisas, faturas e configurações. Visualiza indicadores globais, logs e backup/migração.

### Consultor Valora

Visualiza clientes, pesquisas, formulários, respostas e relatórios, sem acesso ao financeiro, módulos, configurações sensíveis, logs ou backup.

### Administrador da empresa

Administra somente sua organização: usuários internos permitidos, formulários, pesquisas, respostas, relatórios, plano e cadastro empresarial.

### Gestor de pesquisa

Opera formulários e campanhas da própria empresa, sem acesso global.

### Participante

Visualiza somente suas pesquisas, respostas, resultados, certificados e solicitações LGPD.

## Fluxo de formulário

1. Definir nome, categoria, descrição e tempo estimado.
2. Criar dimensões.
3. Adicionar perguntas com um dos seis componentes disponíveis.
4. Definir peso e pontuação.
5. Informar alternativas e pontos, quando aplicável.
6. Escolher método de cálculo: média normalizada ou soma percentual.
7. Definir faixas de resultado, descrições e recomendações.
8. Executar a revisão do formulário.
9. Salvar, editar, clonar, excluir ou criar pesquisa instantânea.

### Tipos de pergunta

| Tipo | Controle | Pontuação |
|---|---|---|
| Escala | 1 a 5 | valor convertido para o máximo e multiplicado pelo peso |
| Escolha única | radio button | pontos da alternativa marcada |
| Múltipla escolha | checkbox | soma dos pontos das alternativas marcadas, limitada ao máximo |
| Resposta curta | text box | nota opcional quando preenchida |
| Resposta longa | text area | nota opcional quando preenchida |
| Uma resposta correta | radio button de prova | máximo quando correta, zero quando incorreta |

## Fluxo de pesquisa

1. Selecionar empresa e formulário.
2. Definir título, descrição, status, início e expiração.
3. Configurar identificação, anonimato, consentimento, repetição e exibição do resultado.
4. Gerar token seguro.
5. Compartilhar por cópia, WhatsApp ou e-mail.
6. Acompanhar respostas e consumo do plano.
7. Renovar token, encerrar ou excluir a campanha.

## Jornada do participante

1. Validação do token, status e validade.
2. Explicação de finalidade, confidencialidade e tempo estimado.
3. Identificação como pessoa física ou jurídica.
4. Cadastro de nome, e-mail, telefone, WhatsApp e documento quando solicitado.
5. Leitura e aceite do termo LGPD.
6. Resposta ao formulário.
7. Cálculo conforme configuração do formulário.
8. Resultado por dimensão, nível e recomendação.
9. Envio de resultado por e-mail quando autorizado.
10. Histórico e certificado na área pessoal.

## Dashboards

### Global

Clientes ativos, MRR contratado, faturas, pesquisas, respostas, índice médio, distribuição por plano, maturidade por dimensão, alertas, atividade e uso dos módulos.

### Empresa

Plano, consumo mensal, pesquisas ativas, respostas, taxa de participação, índice médio, dimensões, campanhas recentes e próximos passos.

### Participante

Pesquisas disponíveis, respostas concluídas, média individual, certificados e histórico.

## Financeiro

O MRR é calculado a partir dos planos efetivamente vinculados aos clientes ativos. A área mostra clientes por plano, valores contratados, faturas pagas, em aberto e vencidas. Os dados da demonstração são locais; integrações com cobrança devem ser feitas no backend de produção.

## E-mail

O sistema oferece:

- template com identidade Valora Group;
- convite de pesquisa;
- resultado individual;
- mensagem de teste;
- variáveis editáveis;
- modo caixa de saída `.eml`;
- modo SMTP no servidor local.

A credencial SMTP nunca deve ser inserida em `app.js` ou no Firebase Hosting.

## Auditoria

Ações relevantes geram log: login, criação, edição, exclusão, compartilhamento, exportação, configuração, envio, backup e erros. O menu de logs e o backup são exclusivos do Administrador geral Valora.

## Acessibilidade

- contraste baseado nas cores da logo;
- foco visível por teclado;
- link de salto para conteúdo;
- labels em campos;
- textos responsivos e sem sobreposição;
- modais com fechamento por botão e tecla Escape;
- suporte a redução de movimento;
- tabelas com rolagem interna em telas menores;
- mensagens de erro e confirmação em linguagem direta.
