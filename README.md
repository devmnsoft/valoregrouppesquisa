# Valora Group™ 8.5 — versão funcional local

Plataforma web para criação, aplicação e análise de diagnósticos, pesquisas e provas de cultura, governança, liderança, pessoas, controladoria e advisory.

## Abrir no Windows

1. Extraia todo o ZIP para uma pasta nova.
2. Abra a pasta extraída.
3. Execute `INICIAR_SITE_WINDOWS.bat`.
4. Mantenha a janela do servidor aberta.
5. O navegador abrirá no endereço local escolhido automaticamente, como `http://127.0.0.1:8095`.

Não abra `index.html` diretamente. O servidor local é necessário para links, consulta de CEP/CNPJ, e-mail de teste e proteção contra bloqueios CORS.

No macOS ou Linux, execute:

```bash
./iniciar-site-mac-linux.sh
```

Requisito: Python 3.9 ou superior. O sistema usa apenas a biblioteca padrão do Python e não exige `npm`, instalação de pacotes ou internet para as funções principais.

## Credenciais locais de demonstração

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador geral Valora | `admin@valoragroup.com` | `Valora@2026` |
| Consultor Valora | `consultor@valoragroup.com` | `Valora@2026` |
| Administrador da empresa | `gestor@empresa.com` | `Empresa@2026` |
| Gestor de pesquisa | `rh@empresa.com` | `Empresa@2026` |
| Participante | `participante@empresa.com` | `123456` |

Estas credenciais existem somente para demonstração local. Em produção, substitua o armazenamento local por Firebase Authentication, redefina todos os acessos e aplique autenticação multifator aos administradores.

## Jornadas implementadas

### Administrador geral Valora

Acesso global a clientes, planos, financeiro, módulos, usuários, formulários, pesquisas, respostas, relatórios, configurações da home, e-mail, LGPD, logs e backup/migração. **Backup e logs são exclusivos deste perfil** e não aparecem nos portais da empresa ou do participante.

### Administrador da empresa e gestor de pesquisa

Acesso isolado ao ambiente da própria empresa, formulários, pesquisas, links seguros, usuários autorizados, respostas, indicadores, relatórios e consumo do plano. Nenhuma empresa visualiza dados de outra organização.

### Participante

Acesso às pesquisas disponíveis, histórico de respostas, resultados, relatórios individuais, certificados e solicitações de privacidade. Links públicos exigem identificação, consentimento LGPD e respeitam validade e status da pesquisa.

## Formulários e provas dinâmicas

Cada pergunta pode ser configurada individualmente como:

- escala de 1 a 5;
- escolha única por radio button;
- múltipla escolha por checkbox;
- resposta curta por text box;
- resposta longa por text area;
- questão de prova com uma única resposta correta.

O editor permite dimensões, ajuda ao respondente, obrigatoriedade, peso, pontuação máxima, nota por alternativa, resposta correta, nota de preenchimento, método de cálculo e faixas de resultado. Há revisão estrutural e campos com `spellcheck` do navegador.

Formulários podem ser criados, editados, clonados, excluídos e usados para criar uma pesquisa instantânea. A pesquisa gerada pode ser compartilhada imediatamente por link, WhatsApp ou e-mail.

## Links seguros e LGPD

Cada pesquisa possui token aleatório, status, data de início e expiração. A jornada do participante valida o token e a validade antes de exibir o questionário. O participante informa pessoa física ou jurídica, nome, e-mail, telefone, preferência de WhatsApp e, quando aplicável, CPF/CNPJ. O aceite LGPD é obrigatório quando configurado.

A versão local demonstra esta jornada. Em produção, use Cloud Functions para validar no servidor o hash do token, registrar tentativas, limitar taxa de acesso e impedir exposição direta dos documentos do Firestore.

## Relatórios e certificados

Exportações disponíveis, conforme perfil e plano:

- PDF nativo;
- Word compatível (`.doc`);
- Excel compatível (`.xls`);
- JSON;
- CSV;
- certificado PDF;
- certificado PNG.

Os PDFs são gerados localmente por `pdf.js`, sem CDN ou biblioteca externa.

## E-mail

O remetente padrão está preenchido como `valoragroup@mnsoft.com.br`.

O modo inicial é **Caixa de saída de teste**. Ao enviar, o servidor cria um arquivo `.eml` em `data/outbox`, permitindo validar assunto, conteúdo, logo, links e destinatário sem disparar mensagem real.

Para SMTP real, entre como Administrador geral e acesse **Home, e-mail e LGPD**. Informe servidor, porta, segurança, usuário e senha. A senha:

- não está no ZIP;
- não é gravada no `localStorage`;
- não entra em backup JSON;
- é salva apenas em `data/email_config.json`, no servidor local;
- também pode ser fornecida pela variável de ambiente `VALORA_SMTP_PASSWORD`.

Nunca publique esse arquivo nem inclua senhas no JavaScript, Firebase Hosting ou repositório.

## Dados e integrações

- CEP: ViaCEP com fallback BrasilAPI, por meio do servidor local.
- CNPJ: BrasilAPI, por meio do servidor local.
- Persistência de demonstração: `localStorage` do navegador.
- Estrutura de produção: Firebase Authentication, Firestore, Functions e Hosting, conforme `FIREBASE_SETUP.md`.

As consultas externas dependem de conexão com a internet e disponibilidade dos provedores.

## Planos iniciais parametrizáveis

| Plano | Valor inicial | Pesquisas ativas | Respostas/mês | Gestores |
|---|---:|---:|---:|---:|
| Gratuito | Grátis | 1 | 30 | 1 |
| Essencial | R$ 590/mês | 3 | 150 | 2 |
| Growth | R$ 1.490/mês | 12 | 1.000 | 8 |
| Enterprise | R$ 3.900/mês | ilimitadas | 10.000 | 50 |

O administrador geral pode editar valores, limites, destaques e funcionalidades.

## Arquivos principais

- `index.html`: estrutura da aplicação.
- `style.css`: identidade visual e responsividade.
- `app.js`: jornadas, permissões, CRUDs, dashboards e regras da demonstração.
- `pdf.js`: gerador local de PDFs.
- `server.py`: servidor local, e-mail, CEP e CNPJ.
- `firestore.rules`: ponto de partida conservador para produção.
- `database.sample.json`: exemplo de coleções sem segredos.
- `TESTES_EXECUTADOS.md`: matriz de validação desta entrega.

## Limite da versão local

Esta entrega é um MVP funcional para validação do produto e da experiência. `localStorage` não substitui uma base multiusuário de produção. Para publicação comercial, implemente Firebase Auth, Firestore, Functions, trilha imutável, hashing de senhas/tokens, controle de sessão, backups gerenciados e monitoramento.

### Cloud Functions para e-mail, CEP e CNPJ em produção

Em `STORAGE_MODE: 'firebase'`, o frontend chama Cloud Functions (`sendEmail`, `getEmailStatus`, `lookupCep` e `lookupCnpj`) e não depende do `server.py`. O `server.py` permanece somente para demonstração local (`STORAGE_MODE: 'local'`).

Configure segredos e variáveis de ambiente antes do deploy:

```bash
firebase functions:secrets:set SMTP_PASSWORD
firebase functions:config:set smtp.host="smtp.seudominio.com.br" # legado, se utilizado no seu projeto
```

Para a implementação atual em Functions v2, defina também as variáveis de ambiente não secretas no ambiente de deploy/console Firebase:

```bash
SMTP_HOST=smtp.seudominio.com.br
SMTP_PORT=587
SMTP_SECURITY=starttls
SMTP_USERNAME=usuario@seudominio.com.br
SMTP_SENDER_EMAIL=nao-responda@seudominio.com.br
SMTP_SENDER_NAME="Valora Group"
```

Regras de segurança implementadas nas Functions:

- `sendEmail` exige usuário autenticado e limita envio a `admin_valora`, `empresa_admin` e `gestor_pesquisa`.
- `empresa_admin` e `gestor_pesquisa` só enviam templates `invite` e `result` vinculados à própria empresa.
- `participante` não tem envio livre.
- `SMTP_PASSWORD` é lido via Secret Manager e nunca enviado ao navegador ou gravado no Firestore.
- `getEmailStatus` retorna somente `configured`, `senderName` e `senderEmail` mascarado.
- `lookupCep` valida 8 dígitos, consulta ViaCEP com fallback BrasilAPI e aplica rate limit.
- `lookupCnpj` valida 14 dígitos, exige autenticação, consulta BrasilAPI e retorna apenas dados cadastrais necessários.
