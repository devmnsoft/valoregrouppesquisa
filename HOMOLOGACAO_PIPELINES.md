# Homologação dos Pipelines CI/CD

## Versão testada

- Produto: Valora Pulse.
- Repositório: `devmnsoft/valoregrouppesquisa`.
- Versão/referência local: branch atual de trabalho, com workflows existentes em `.github/workflows/`.

## Data

2026-06-22.

## Branch

Branch atual do repositório local no momento da homologação.

## Workflows avaliados

- `.github/workflows/pr-validation.yml` — validação de pull requests para `main`.
- `.github/workflows/secure-build.yml` — build seguro em push para `main` e execução manual.
- `.github/workflows/firebase-preview.yml` — deploy de preview em PR interno.
- `.github/workflows/firebase-production-deploy.yml` — deploy controlado de produção por execução manual ou tag `v*`.

## Cenários testados

| Cenário | Comando/Workflow | Resultado esperado | Resultado obtido | Status |
| --- | --- | --- | --- | --- |
| PR segura | `npm run check`, `npm run check:no-dist`, `npm run security:check`, `npm run build:prod`, `node --check dist/assets/app.*.js`, `find dist -name "*.map"`, `npm run postbuild:security` | Passa sem `dist/` versionado, sem secrets, sem source maps e com JS válido | Todos os comandos passaram; `find dist -name "*.map"` retornou vazio | OK |
| PR com `dist/` | `git add -f dist/teste.txt` e `npm run check:no-dist` | Falha com mensagem clara de bloqueio de artefato | Falhou com `A pasta dist/ é artefato de build e não deve ser commitada.` | OK |
| Source map indevido | criar `dist/assets/app.fake.js.map` e executar `npm run postbuild:security` | Falha por detectar `.map` no build | Falhou com `Arquivo proibido no build: dist/assets/app.fake.js.map` | OK |
| Segredo fake | criar arquivo temporário rastreado com `TELEGRAM_BOT_TOKEN=fake_test_token_should_be_blocked` e executar `npm run security:check` | Falha por detectar padrão proibido | Falhou com `Token/segredo suspeito (TELEGRAM_BOT_TOKEN)` | OK |
| CSP insegura | alterar temporariamente `firebase.json` com `script-src *`, `connect-src *` e `unsafe-eval`; executar `npm run security:check` | Falha por CSP insegura | Falhou detectando `script-src *`, `connect-src *` e `unsafe-eval` | OK |
| Build produção | `npm run build:prod` | Gera `dist/index.html`, `dist/assets/app.<hash>.js`, `dist/assets/style.<hash>.css`, sem `.map` e sem conteúdo sensível | Build gerou `dist/` limpo e `npm run postbuild:security` passou | OK |
| Ausência de `dist/` no Git | `git ls-files dist` | Saída vazia | Saída vazia | OK |
| Artefato GitHub Actions | Revisão de `.github/workflows/secure-build.yml` | Quando executado, artifact contém apenas `dist/` gerado pelo build validado | Workflow faz upload de `dist` como `valora-pulse-dist` após checks e bloqueio de `.map`; validação real do artifact depende de run no GitHub Actions | OK com ressalva |
| Deploy preview | Revisão de `.github/workflows/firebase-preview.yml` | Roda apenas em PR interno, faz build antes, usa secrets e publica canal preview | Workflow restringe forks, roda checks/build e publica `pr-<numero>`; execução real depende de secrets de homologação | OK com ressalva |
| Deploy production | Revisão de `.github/workflows/firebase-production-deploy.yml` | Deploy não roda em qualquer branch; exige controle manual/tag e environment production | Workflow só roda em `workflow_dispatch` ou tag `v*`, usa `environment: production`, faz checks/build antes e publica `dist/` | OK |
| Rollback | Revisão de `ROLLBACK_PRODUCAO.md` | Procedimento documenta identificação, rollback Firebase, preview, restauração, aprovação e incidente | Documento cobre histórico do Hosting, workflow manual, canal preview, rotação/revalidação e aprovação | OK |

## Resultado

A homologação local dos controles de CI/CD foi concluída com sucesso. Os cenários negativos foram simulados com arquivos/alterações temporárias e revertidos antes da entrega final.

## Evidências

- `npm run check` passou.
- `npm run check:no-dist` passou no estado limpo.
- `npm run security:check` passou no estado limpo.
- `npm run build:prod` passou e gerou `dist/` local apenas como artefato ignorado pelo Git.
- `node --check dist/assets/app.*.js` passou.
- `find dist -name "*.map"` não retornou arquivos.
- `git ls-files dist` não retornou arquivos versionados.
- Simulação de `dist/teste.txt` rastreado falhou corretamente em `check:no-dist`.
- Simulação de `dist/assets/app.fake.js.map` falhou corretamente em `postbuild:security`.
- Simulação de segredo fake falhou corretamente em `security:check`.
- Simulação de CSP insegura falhou corretamente em `security:check`.

## Problemas encontrados

Nenhum problema bloqueante foi encontrado nos scripts/workflows avaliados. As execuções `npm install` emitiram avisos de ambiente (`http-proxy` e engine do Node local), mas não impediram a homologação.

## Correções aplicadas

- Criado este relatório de homologação.
- Atualizada a documentação operacional para registrar comandos, resultados, regras de deploy controlado e pendências controladas.
- Nenhum script de pipeline precisou ser corrigido, pois os bloqueios esperados funcionaram.

## Pendências

- Executar os workflows no GitHub Actions em PR real para anexar URLs/run IDs como evidência externa.
- Validar o conteúdo do artifact `valora-pulse-dist` baixado diretamente do GitHub Actions após um run real.
- Executar deploy preview real somente quando os secrets de homologação estiverem configurados.
- Executar deploy production real somente com autorização formal, aprovação do environment `production` e release/tag aprovada.
- Se possível, simular rollback em projeto Firebase de homologação antes do próximo go-live.

## Decisão final

Aprovado com ressalvas.

As ressalvas se limitam às validações que dependem de ambiente externo do GitHub/Firebase e autorização de deploy real. Os controles locais, scripts de bloqueio e regras dos workflows foram homologados.
