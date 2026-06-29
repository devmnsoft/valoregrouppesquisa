# Política sem resposta demo em produção

Sprint 49 consolidou validações de CSP publicada, readiness do Firebase Hosting e fluxo de e-mail com responseId/resultToken reais. Bootstrap da home legada foi movido para assets locais de texto em `vendor/bootstrap/`, evitando dependência de `cdn.jsdelivr.net` na home antiga.

## Diagnóstico e decisão
- CSP local: `firebase.json` contém `Content-Security-Policy`, `script-src-elem`, `style-src-elem`, `object-src 'none'`, `frame-ancestors 'self'` e `connect-src` com `https://api.valoragroup.mnsoft.com.br`.
- CSP publicada: deve ser validada com `npm run security:csp-live`, que executa GET e HEAD no domínio de `VALORA_PUBLIC_URL` e falha se o header real divergir.
- Projeto/canal/cache: a automação agora valida `hosting.public=dist`, workflow de deploy, assets versionados, ausência de sourcemaps e header real pós-deploy; qualquer problema de projeto/canal/cache fica bloqueado no gate live.
- Não há meta CSP no `index.html`; a política efetiva deve vir do Firebase Hosting.

## Perguntas objetivas da auditoria
1. Qual CSP está no firebase.json? A política em `firebase.json` com default self, scripts/styles, connect-src da API, object none e frame ancestors self.
2. Qual CSP está sendo recebida no domínio publicado? Validada em tempo real por `security:csp-live` no ambiente alvo.
3. O domínio publicado está usando o Firebase project correto? Bloqueado por `.firebaserc`, `FIREBASE_PROJECT_ID` e readiness.
4. O deploy foi executado no projeto correto? O workflow exige `FIREBASE_PROJECT_ID` explícito.
5. O deploy foi executado no canal correto? O comando usa hosting production padrão do projeto informado.
6. O dist contém index.html atualizado? Validado por readiness após build.
7. O dist contém versão correta dos assets? Validado por hashes `assets/app.<hash>.js` e `assets/style.<hash>.css`.
8. O header real contém cdn.jsdelivr.net? Não é mais necessário para a home legada porque Bootstrap ficou local.
9. O header real contém api.valoragroup.mnsoft.com.br? Obrigatório em `connect-src`.
10. O header real contém script-src-elem? Obrigatório.
11. O header real contém style-src-elem? Obrigatório.
12. Existe cache antigo? O HTML usa no-store; divergência será capturada pelo header live.
13. Existe Service Worker antigo? Não foi encontrado cadastro novo nesta sprint; validar no navegador se houver cache legado.
14. Existe CDN/reverse proxy na frente? Se houver, `security:csp-live` mede o header final recebido pelo cliente.
15. Existe algum meta CSP no HTML sobrescrevendo header? Não.
16. O app ainda chama resp_demo em produção? Gate novo bloqueia.
17. O envio de e-mail usa responseId real? Sim, endpoint oficial com `encodeURIComponent(responseId)`.
18. O fluxo de resultado por e-mail está testado em produção/staging? Há Playwright live dedicado e validador de fluxo real.

## Fluxo de e-mail real
- Endpoint oficial: `POST /communications/result/{responseId}/send-email`.
- Payload obrigatório: `to`, `resultToken`, `subject`, `message` e `includeCertificate`.
- IDs de demonstração são proibidos no fluxo produtivo por `npm run email:no-demo-response`.
- Falhas de rede/CSP/API preservam resposta, resultado e certificado, registram comunicação com status controlado e exibem mensagem amigável.

## Operação
- Painel de comunicações exibe falhas por CSP/rede, SMTP, configuração e token inválido, reenvios pendentes, última tentativa e ações de fila/reenvio sem expor senha SMTP, tokens puros, hashes, stack trace ou corpo completo.

## Como validar
1. `npm run security:csp`
2. `npm run build:prod`
3. `npm run deploy:firebase-readiness`
4. `VALORA_PUBLIC_URL=https://valoragroup.mnsoft.com.br npm run security:csp-live`
5. `VALORA_API_URL=https://api.valoragroup.mnsoft.com.br npm run prod:smoke`
6. `npm run email:no-demo-response`
7. `npm run email:real-response-flow`
8. `npm run e2e:csp-email-live`

## Limitações conhecidas
- O validador live confirma o header publicado no domínio informado; ele não executa deploy por conta própria.
- O smoke remoto depende de disponibilidade da API pública e DNS.
- O teste Playwright live evita screenshots, vídeo e trace; a simulação de envio só ocorre quando há resposta real disponível no estado carregado.
