# Paridade de jornada pública legado → ASP.NET

| Jornada legado | Arquivo legado | Rota nova | View nova | API usada | Status |
|---|---|---|---|---|---|
| Home pública | `index.html`, `app.js` | `/` | `Views/Home/Index.cshtml` | navegação pública/API sob demanda | migrado |
| Diagnóstico gratuito | `app.js`, `repository.js` | `/diagnostico-gratuito` | `Views/PublicPages/FreeDiagnostic.cshtml` | `/api/free-diagnostics` | migrado |
| Pesquisa pública | `app.js`, `api-repository.js` | `/pesquisa/{surveyId}` | `Views/PublicSurvey/Take.cshtml` | `/api/public/surveys/{surveyId}` | migrado |
| Resposta | `app.js` | `/pesquisa/{surveyId}/responder` | `Views/PublicSurvey/Take.cshtml` | `/api/public/surveys/{surveyId}/responses` | migrado |
| Resultado | `report-service.js` | `/resultado/{responseId}` | `Views/Results/Public.cshtml` | `/api/public/results/{responseId}` | migrado |
| Envio de e-mail | `notification-service.js` | `/resultado/{responseId}/email` | `Views/Results/Public.cshtml` | endpoint oficial de comunicação quando disponível | parcial |
| Certificado | `pdf.js`, `report-service.js` | `/certificado/{certificateId}` | `Views/Certificates/Details.cshtml` | `/api/certificates` | migrado |
| Validar certificado | `pdf.js` | `/certificado/validar/{codigo}` | `Views/Certificates/Validate.cshtml` | `/api/certificates/validate` | migrado |
| WhatsApp | `app.js` | `/whatsapp` | `Views/PublicPages/WhatsApp.cshtml` | não aplicável; link externo sem dado sensível | migrado |
| LGPD | `app.js` | `/lgpd`, `/lgpd/solicitacao` | `Views/Lgpd/*.cshtml` | endpoint LGPD/communications quando disponível | migrado |
| Login | `app.js` | `/entrar` | redirect `/Account/Login` | `/api/auth` | migrado |
| Planos | `app.js` | `/planos` | `Views/Plans/Index.cshtml` | `/api/plans` quando dinâmico | migrado |
| Contato | `app.js` | `/contato` | `Views/PublicPages/Contact.cshtml` | `/api/communications` | migrado |
| ValoraBot | `chatbot-service.js` | todas públicas | `Shared/Public/_PublicBotPanel.cshtml` | pendente sem IA/Firebase | parcial |
| Footer | `index.html` | todas públicas | `Shared/Public/_PublicFooter.cshtml` | não aplicável | migrado |
| Mobile | `style.css` | todas públicas | layout/CSS público | não aplicável | migrado |
