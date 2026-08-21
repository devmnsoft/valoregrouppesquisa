# Runbook Release Candidate — Valora Insight™ Profissional

> Revisão: 20/08/2026. Este documento é o ponto único de entrada para preparar, demonstrar e promover a RC. Ele não substitui backup, restore nem aprovação humana de produção.

## 1. Pré-requisitos e execução local

Instale .NET SDK 10, Node.js LTS, PostgreSQL 16+ (`psql`) e, para E2E visual, Playwright/Chromium. A partir da raiz:

```bash
cp backend/.env.example backend/.env # nunca versionar o arquivo preenchido
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=valora_dev;Username=postgres;Password=...'
backend/database/postgresql/apply-local.sh
dotnet run --project backend/Valora.Api
dotnet run --project backend/Valora.Web
```

A Web usa a API configurada em `Api__BaseUrl`. Configure ainda `Jwt__SigningKey` (mínimo de 32 caracteres aleatórios), `App__PublicBaseUrl`, `App__WebBaseUrl`, `ASPNETCORE_ENVIRONMENT` e as opções SMTP descritas abaixo. Use secrets do orquestrador, nunca `appsettings*.json`, para senhas e chaves.

## 2. Banco limpo, idempotência e demo

O bootstrap canônico é `database/postgresql/script_completo.sql`. O wrapper recusa demo fora de Development:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export VALORA_SEED_DEMO=true
backend/database/postgresql/apply-local.sh
backend/database/postgresql/apply-local.sh # segunda execução obrigatória
```

A massa é sintética, marcada `[DEMO]`, idempotente e cria a organização e estrutura de apresentação. Login local: `admin.demo@valora.local`; troque a senha antes de qualquer ambiente compartilhado. Em Production mantenha `VALORA_SEED_DEMO=false` e `App__EnableDemoSeed=false`. Para criar o primeiro administrador real, use o bootstrap administrativo documentado no script canônico/endpoint de administração, com senha temporária transmitida por canal seguro e troca obrigatória.

## 3. SMTP e URLs públicas

Configure `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__FromEmail`, `Smtp__FromName`, `Smtp__EnableSsl=true` e `Email__Enabled=true`. Valide SPF, DKIM e DMARC no provedor. `App__PublicBaseUrl` e `App__WebBaseUrl` devem ser HTTPS, sem barra final, apontar ao domínio público correto e jamais a localhost em produção. Faça envio de teste antes de publicar convites.

## 4. Gate automatizado

Linux/macOS:

```bash
backend/scripts/qa/run_release_checks.sh
```

Windows PowerShell: `backend/scripts/qa/run_release_checks.ps1`. O gate audita ações vazias/implementações incompletas em produção, restaura, compila com warnings como erro, testa, valida contratos SQL e publica API/Web. Falha significa **bloqueio da RC**, nunca aceite parcial.

## 5. Roteiro E2E manual mínimo

Use uma base descartável com a demo. Em cada passo registre horário, usuário, URL, status HTTP, correlation ID e captura esperada.

1. **Login:** autenticar como admin; esperado: dashboard premium, identidade e menu completo, sem mensagem técnica.
2. **Administração:** criar organização piloto, atribuir plano, criar gestor e unidade/área; esperado: confirmação e registro de auditoria.
3. **Diagnóstico:** criar a partir do template oficial, importar apenas respondentes fictícios, publicar e copiar link; esperado: status publicado e CTA funcional.
4. **Público:** abrir o link em janela anônima/mobile, responder e enviar; esperado: validação inline, confirmação única e nenhum dado de outro tenant.
5. **Processamento:** acompanhar job até concluído; esperado: score derivado das respostas, evidências e retry controlado em falha.
6. **Entregáveis:** abrir dashboard/resultado, gerar relatório, emitir certificado e exportar Excel/JSON; esperado: arquivos não vazios e valores coerentes com o resultado.
7. **Validação pública:** validar o código do certificado em janela anônima; esperado: identidade, situação e data, sem payload técnico.
8. **Auditoria:** filtrar pelo correlation ID; esperado: criação, publicação, resposta, processamento e entregáveis rastreáveis.
9. **Erros:** visitar rota inexistente e simular 403; esperado: páginas amigáveis, status corretos e referência de suporte, nunca stack trace.
10. **Responsividade:** repetir menu, tabela, modal e formulário em 1440×900, 1024×768, 768×1024 e 390×844; esperado: foco visível, teclado, sem corte horizontal.

Capturas esperadas: `01-login.png`, `02-dashboard.png`, `03-diagnostico-publicado.png`, `04-resposta-mobile.png`, `05-resultado.png`, `06-relatorio.png`, `07-certificado.png`, `08-auditoria.png`. Não capture senhas, tokens ou dados pessoais.

## 6. Guia funcional resumido

- **Organização:** Administração → Dados da Organização; complete identidade, estrutura, marca, responsáveis e plano.
- **Diagnóstico:** Diagnósticos → Novo Diagnóstico; selecione template oficial, público, período e canais antes de publicar.
- **Convites/respostas:** Campanhas envia convites; Respostas acompanha adesão. Reenvie somente para pendentes e respeite consentimento/LGPD.
- **Dashboard:** leia score, dimensões, evidências e evolução no contexto do período e amostra; ausência de dados deve permanecer explícita, nunca preenchida artificialmente.
- **Relatório:** Resultados → Relatórios; gere após processamento concluído e confira versão/metodologia.
- **Certificado:** Resultados → Certificados; emita apenas a partir de resultado elegível e valide pelo código público.
- **Planos/permissões:** Planos e Uso mostra limites; Central de Acessos aplica papel e escopo mínimos. `admin_valora` é operação de plataforma e não deve ser usado no cotidiano do tenant.

## 7. Checklist de produção

- [ ] Branch protegida, revisão aprovada, commit/tag RC identificados.
- [ ] Gate automatizado passou em máquina com SDK e dependências oficiais.
- [ ] Banco descartável subiu do zero e o script passou duas vezes.
- [ ] Backup restaurável e rollback cronometrado foram comprovados.
- [ ] Secrets rotacionados; demo, Swagger e diagnóstico de desenvolvimento desativados.
- [ ] HTTPS/HSTS, cookies seguros, CORS/CSP, rate limit e headers verificados.
- [ ] Migrações aplicadas antes do tráfego, sem operação destrutiva não aprovada.
- [ ] SMTP, URLs públicas, health/readiness e workers verificados sem erro recorrente.
- [ ] E2E principal e matriz de permissões passaram com evidências.
- [ ] 400/401/403/404/500 retornam status correto e não exibem stack trace.
- [ ] Desktop/tablet/mobile, teclado, foco, labels e contraste homologados.
- [ ] Observabilidade, alertas, retenção LGPD, suporte e responsáveis de plantão definidos.
- [ ] Smoke test pós-deploy passou; decisão go/no-go registrada.

## 8. Go/no-go e rollback

Qualquer erro de compilação/teste, 500 no fluxo principal, rota de menu quebrada, vazamento entre tenants, dado inventado, falha de idempotência, job em loop, segredo exposto ou restore não comprovado é **no-go**. Interrompa tráfego, preserve correlation IDs/logs, execute o plano de rollback, restaure somente com dupla confirmação e publique incidente sem conteúdo sensível.
