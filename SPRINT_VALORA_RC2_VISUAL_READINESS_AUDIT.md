# Sprint Valora RC2 — Visual Readiness Audit

## 1. Resumo

A sprint consolidou a etapa visual RC2 no projeto oficial ASP.NET Core MVC/Razor, mantendo a marca em paths oficiais, fallback seguro e validadores para execução com ou sem os binários reais.

## 2. Estado dos assets

Os arquivos `valora-logo-full.jpeg` e `valora-symbol.jpeg` não estão presentes no working tree. A pendência manual está documentada e o modo diagnóstico permite auditar a aplicação sem criar binários.

## 3. Fallback visual

O fallback `Valora Group` foi preservado como elemento premium, acionado por erro de imagem, sem `VG` ou `V` como marca final e sem imagem quebrada.

## 4. Manual de inclusão da logo

`VALORA_BRAND_ASSETS_MANUAL_SETUP.md` documenta regra do Codex, nomes, local, formato, teste, commit, navegação e resolução de imagem quebrada.

## 5. Home pública refinada

A Home mantém hero com logo/fallback, diagnóstico gratuito, CTA WhatsApp, CTA entrar, processo, dimensões, devolutiva, certificado, LGPD e visual executivo.

## 6. Diagnóstico refinado

A tela do diagnóstico gratuito descreve LGPD, progresso, escala 1 a 5, envio com proteção contra duplo clique, erro amigável e redirecionamento sem Firebase.

## 7. Resultado refinado

Resultado público mantém marca/fallback, leitura executiva, dimensões, radar textual, benchmarking, risco, próximo nível e CTAs de e-mail, certificado e WhatsApp.

## 8. Certificado refinado

Certificado e validação pública usam marca/fallback, carregamento via API oficial, código validável e CTAs de validação/WhatsApp.

## 9. Admin refinado

Admin mantém marca/fallback em sidebar e topbar, separação do layout público, Bootstrap/jQuery e scripts de sessão/guards/perfil.

## 10. Menu/perfil validado

A validação `web:admin-menu-profile-access` cobre perfis obrigatórios e permissões declaradas no menu administrativo.

## 11. Checklist visual criado

`VALORA_RC2_VISUAL_HOMOLOGATION_CHECKLIST.md` cobre desktop, tablet, celular, Chrome, Edge, Home, diagnóstico, resultado, certificado, LGPD, contato, WhatsApp, login, admin e menu por perfil.

## 12. Validadores criados

Foi criado `tools/validate-valora-rc2-visual-readiness.js` e o script `web:rc2-visual-readiness` foi adicionado ao `package.json`. O validador de assets passou a ter modo obrigatório e modo diagnóstico.

## 13. Comandos executados

- `VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets`
- `npm run web:rc2-visual-readiness`
- `npm run security:no-service-account-secrets`
- `npm run web:public-legacy-parity`
- `npm run web:valora-insight-public-journey`
- `npm run web:admin-menu-profile-access`
- `npm run backend:sql-schema-validate`
- `npm run backend:domain-entities-validate`
- `npm run backend:official-validate`
- `npm run check:critical`

## 14. Comandos não executados e motivo

- `npm run web:brand-assets` obrigatório sem variável: não executado inicialmente porque os binários reais não existem e o resultado esperado é falha até inclusão manual.
- Comandos `dotnet restore/build/test`: dependem de .NET SDK disponível no ambiente; registrar resultado real na seção de testes da PR.

## 15. Gaps restantes

- Incluir e versionar manualmente os dois JPEGs oficiais.
- Rodar `npm run web:brand-assets` em modo obrigatório após inclusão dos assets.
- Homologar visualmente com navegador real, .NET SDK, PostgreSQL e comparação desktop/mobile.

## 16. Próximo passo recomendado

Adicionar os assets oficiais, executar validação obrigatória e iniciar homologação final para gerar o pacote `0.9.0-rc2`.
