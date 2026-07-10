# Auditoria final de regressão legacy — Valora Insight™

| Item | Status | Arquivo corrigido | Evidência |
|------|--------|------------------|-----------|
| 1. Link de resultado por e-mail | OK | `functions/index.js`, `communication-gateway/src/templates/result-email-template.js` | `sendResultEmail` rotaciona token com `rotateResultTokenForResponse`, monta URL com `buildPublicResultUrl(response.id,resultToken)` e o template mantém apenas o botão de resultado, sem certificado. Validado por `npm run legacy:final-regression` e `node --check functions/index.js`. |
| 2. Link de resultado por WhatsApp | OK | `functions/index.js`, `app.js` | `adminCreateResultShareLink` rotaciona token e retorna URL pública com `rt`; `shareResultWhatsapp` usa essa callable na área admin. Validado por `npm run legacy:final-regression`. |
| 3. Link de pesquisa por WhatsApp | OK | `functions/index.js`, `app.js` | `preparePublicSurveyLink` gera token novo, salva apenas hash, renova validade em 180 dias e `shareSurveyWhatsapp` usa a URL retornada. Validado por `npm run legacy:final-regression`. |
| 4. getPublicResult | OK | `functions/index.js` | `getPublicResult` valida somente `responseId + rt`, sem Firebase Auth, e remove `resultTokenHash` antes do retorno. Validado por `node --check functions/index.js`. |
| 5. resultToken/resultTokenHash | OK | `functions/index.js` | `submitSurveyResponse` salva `resultTokenHash` e retorna `resultToken` real; rotinas de reparo/rotação geram novo par. Validado por `npm run check`. |
| 6. sendResultEmail | OK | `functions/index.js`, `communication-gateway/src/templates/result-email-template.js` | Reenvio por `responseId` rotaciona token; template não injeta link de certificado. Validado por `npm run legacy:final-regression`. |
| 7. adminCreateResultShareLink | OK | `functions/index.js` | Callable exige admin, valida escopo da resposta, rotaciona token e usa `buildPublicResultUrl`. Validado por `npm run legacy:final-regression`. |
| 8. preparePublicSurveyLink | OK | `functions/index.js` | Sempre gera novo `publicToken`, salva `publicTokenHash`, expiração de 180 dias, `status: active`, `visibility: public`, `allowRepeat/showResult`. Validado por `npm run legacy:final-regression`. |
| 9. requestNewResultLink | OK | `functions/index.js` | Participante com e-mail correspondente recebe novo link por rotação de token, compatível com respostas antigas. Validado por `npm run legacy:final-regression`. |
| 10. #admin/responses actions | OK | `app.js` | Desktop e mobile exibem Ver resultado, Baixar relatório, Enviar WhatsApp, Reenviar e-mail, Anonimizar e Excluir; aliases admin têm handlers em `createActions`. Validado por `npm run check`. |
| 11. PDF safe text | OK | `app.js`, `report-service.js` | Validador final exige `toPdfSafeText`, `radarBarPdfSafe`, ausência de blocos Unicode e artefatos de interrogação no PDF. Validado por `npm run legacy:final-regression`. |
| 12. PDF sem texto cortado | OK | `app.js` | Fluxo de PDF usa helpers de quebra/paginação (`writePdfWrappedText`, `writePdfSection`, `ensurePdfPageSpace`). Validado por `npm run legacy:final-regression`. |
| 13. Benchmarking estrutural | OK | `app.js` | Devolutiva contém benchmarking qualitativo/referencial com disclaimer contra ranking/certificação externa. Validado por scripts de regressão no build. |
| 14. Certificado removido | OK | `app.js`, `communication-gateway/src/templates/result-email-template.js` | `CERTIFICATE_FEATURE_ENABLED=false`, funções antigas são no-op e template de e-mail não renderiza certificado. Validado por `npm run legacy:final-regression`. |
| 15. Login copy | OK | `app.js` | Login mostra `Entrar no Valora Pulse™` e `Acesse a gestão do Valora Insight™.`; `goLogin` limpa query pública e renderiza sem F5. Validado por `npm run legacy:final-regression`. |
| 16. WhatsApp CTA | OK | `app.js` | CTA oficial preservado como `Fale com o Valora Group`; número oficial e helpers de WhatsApp validados. |
| 17. Mobile global | OK | `style.css` | Regras globais de `box-sizing`, `overflow-x:hidden`, mídias responsivas e tabelas com scroll foram preservadas. Validado no build final. |
| 18. Mobile admin responses | OK | `app.js`, `style.css` | `renderAdminResponsesMobileCards` renderiza cards com todas as ações e CSS oculta tabela no mobile. Validado por `npm run legacy:final-regression`. |
| 19. Public branding | OK | `app.js` | Devolutiva pública usa Valora Insight™ e não trata Valora Pulse™ como nome da devolutiva. Validado por scripts de branding no build. |
| 20. Planos sem certificado | OK | `app.js` | Cards de planos não renderizam `Certificado simples`. Validado por `npm run legacy:final-regression`. |
| 21. Notificações | OK | `app.js` | Participante público não recebe módulos administrativos; ações públicas liberam UI sem exigir auth. Validado por `npm run check`. |
| 22. Build final | OK | `dist/`, `scripts/validate-final-regression-lockdown.js` | `npm run build:prod` gerou `app.f27eb41d1e49.js` e `style.8812bee5fd17.css`; validador final passou após o build. |

## Testes manuais finais

| Teste | Status | Evidência |
|------|--------|-----------|
| Teste 1 — E-mail | Bloqueado no ambiente local | Não há navegador/e-mail real/Firebase CLI disponível nesta sessão; fluxo foi coberto por validação estática e `node --check functions/index.js`. |
| Teste 2 — WhatsApp resultado | Bloqueado no ambiente local | Sem navegador real; código admin foi validado por `npm run legacy:final-regression` e `npm run check`. |
| Teste 3 — WhatsApp pesquisa | Bloqueado no ambiente local | Sem celular/navegador real; expiração e geração de token foram validadas em `functions/index.js` e pelo script final. |
| Teste 4 — Relatório | Bloqueado no ambiente local | Sem renderização manual de PDF; validadores verificaram texto seguro, ausência de artefatos e helpers de paginação. |
| Teste 5 — Certificado | OK por busca automatizada | `npm run legacy:final-regression` garante ausência de textos de certificado no fonte operacional e no `dist`. |
| Teste 6 — Login | OK por validação automatizada | `npm run legacy:final-regression` valida copy e handlers `goLogin/openLogin/login`. |
| Teste 7 — Mobile | OK por validação automatizada | `style.css`, `renderAdminResponsesMobileCards` e build final foram validados. |

## Comandos executados

- `cd functions && npm install`
- `cd functions && npm ci --dry-run && node --check index.js && node --check utils/telegram.js`
- `npm run check && npm run legacy:final-regression && npm run build:prod && npm run legacy:final-regression`
- `firebase deploy --only functions:getPublicResult,functions:submitSurveyResponse,functions:sendResultEmail,functions:adminCreateResultShareLink,functions:requestNewResultLink,functions:preparePublicSurveyLink,getParticipantResultsByPassword --project gestordepesquisa` — bloqueado porque `firebase` não está instalado no ambiente.
- `firebase deploy --only hosting --project gestordepesquisa` — bloqueado porque `firebase` não está instalado no ambiente.
