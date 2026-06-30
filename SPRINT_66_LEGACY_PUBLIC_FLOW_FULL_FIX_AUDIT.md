# Sprint 66 — Auditoria do fluxo legado

## Respostas objetivas
1. A pesquisa pública é renderizada por `renderTakeSurvey`, com shell em `renderPublicSurveyShell`.
2. A resposta pública é enviada por `submitSurvey`, que chama `submitPublicSurveyResponse`/`submitPublicSurveyAuto`.
3. Em produção, o provider é `auto`: tenta API externa e faz fallback para Cloud Function; fallback local só fica para modo não Firebase/local.
4. O erro raiz identificado era a validação/submissão pública convertendo falhas de provider em mensagem genérica sem código; agora o detalhe sanitizado é exibido.
5. O fallback API -> Cloud Functions existe em `submitPublicSurveyAuto`.
6. `responseId` real é exigido/retornado; respostas demo são rejeitadas no envio de e-mail.
7. `resultToken` real é exigido/retornado e validado por hash nas Functions.
8. O e-mail é chamado após o submit quando o participante marca envio.
9. O e-mail usa `responseId` real.
10. O e-mail usa `resultToken` válido para fluxo público.
11. O certificado é gerado após resultado via funções de certificado do legado.
12. O certificado pode ser baixado/imprimido por PDF/print.
13. A aba Planos usa Firestore quando disponível.
14. Se Firestore não retornar planos, o fallback oficial local cobre os 5 planos.
15. A home pública não renderiza grid inicial de planos; mantém CTA discreto do diagnóstico.
16. O token público usa `publicToken`, `token` ou `accessToken`.
17. Hash interno não deve aparecer na URL.
18. Pesquisa gratuita oficial não é bloqueada por expiração vencida se não estiver revogada.
19. O bridge mobile é carregado após `app.js`.
20. Clique no Menu abre a sidebar via delegation com capture.
21. Bloqueios restantes de produção: configurar secret `SMTP_PASSWORD`, publicar Functions/Hosting, validar regras e executar smoke controlado.

## Diagnóstico real
A mensagem genérica escondia falhas da cadeia API externa/Cloud Function e não preservava um código operacional sanitizado. A correção mantém provider `auto`, adiciona idempotência, força fallback e registra erro seguro no diagnóstico.
