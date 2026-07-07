# Sprint Valora Brand Assets Applied Audit

## 1. Resumo

A sprint aplicou as referências aos assets oficiais de marca já versionados no Web ASP.NET oficial (`backend/Valora.Web`) e nos scripts SQL oficiais, sem gerar, converter ou recriar arquivos binários. O validador de branding foi criado e conectado ao `package.json`.

## 2. Assets utilizados

- Logo completa: `/img/brand/valora-logo-full.jpeg`.
- Símbolo: `/img/brand/valora-symbol.jpeg`.

Observação: os caminhos foram aplicados no código conforme solicitado, porém os arquivos binários não estão presentes no working tree desta execução. O validador apontou essa ausência.

## 3. Locais onde a logo completa foi aplicada

- Hero da Home pública.
- Página pública de certificado.
- Scripts SQL oficiais de branding (`logo_url`).

## 4. Locais onde o símbolo foi aplicado

- Topbar pública.
- Favicon e apple touch icon do layout público.
- Sidebar/admin topbar.
- Resultado público.
- Validação de certificado.
- Scripts SQL oficiais de branding (`symbol_url`).

## 5. Ajustes na topbar pública

- A marca textual `VG` foi substituída pelo símbolo oficial com texto institucional `Valora Group` e subtítulo `Governance, Controller & Advisory`.
- O menu público foi preservado com Início, Diagnóstico gratuito, Como funciona, Planos, LGPD, Contato, Entrar e WhatsApp.

## 6. Ajustes no admin

- A sidebar e a topbar administrativa deixaram de usar `V` textual.
- O símbolo oficial foi aplicado com a classe `admin-brand-symbol`.
- O texto administrativo foi atualizado para `Valora Group` e `Painel administrativo`.
- O layout admin passou a referenciar favicon/apple touch icon e `valora-admin.css`.

## 7. Ajustes no resultado/certificado

- O resultado público recebeu cabeçalho visual com símbolo da Valora Group.
- O certificado público recebeu cabeçalho visual com logo completa.
- A validação de certificado recebeu cabeçalho visual com símbolo da Valora Group.

## 8. Ajustes no SQL de branding

- `organization_branding` passou a declarar `symbol_url` e `secondary_color` nos scripts completos.
- Foram adicionados comandos idempotentes `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` para `symbol_url` e `secondary_color`.
- Foram adicionados valores oficiais: `logo_url`, `symbol_url`, `primary_color` e `secondary_color` sem quebrar bancos existentes.

## 9. Validador criado

- Criado `tools/validate-valora-brand-assets.js`.
- Adicionado script `web:brand-assets` ao `package.json`.
- O validador confere existência dos assets, remoção de marcas textuais antigas, uso de favicon/símbolo, uso da logo na Home, paths SQL e ausência de imagem externa como logo.
- O validador também bloqueia arquivos JSON de service account em áreas oficiais.

## 10. Comandos executados

- `npm run web:brand-assets` — falhou porque os arquivos binários esperados não existem no working tree desta execução.
- `npm run security:no-service-account-secrets` — passou.
- `npm run web:public-legacy-parity` — passou.
- `npm run web:valora-insight-public-journey` — passou.
- `npm run backend:sql-schema-validate` — passou.
- `npm run backend:domain-entities-validate` — passou com avisos pré-existentes de linhas longas/classes em uma linha.
- `npm run backend:official-validate` — passou.
- `npm run check:critical` — passou.
- `dotnet restore backend/Valora.sln` — não executado, pois o SDK .NET não está instalado no ambiente.
- `dotnet build backend/Valora.sln` — não executado, pois o SDK .NET não está instalado no ambiente.
- `dotnet test backend/Valora.sln` — não executado, pois o SDK .NET não está instalado no ambiente.

## 11. Comandos não executados e motivo

- Os comandos .NET não foram executados porque `dotnet` não está disponível no PATH do container.

## 12. Gaps restantes

- Os arquivos `backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg` e `backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg` precisam estar presentes no repositório/ambiente para `npm run web:brand-assets` passar.
- Como solicitado, nenhum binário foi recriado, convertido ou adicionado por esta execução.

## 13. Próximo passo recomendado

Confirmar que os dois JPEGs oficiais foram versionados no branch atual e executar novamente `npm run web:brand-assets` antes do merge/release.
