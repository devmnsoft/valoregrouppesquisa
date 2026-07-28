# Homologação da Fase 02E

## Roteiro

1. Configurar `Api:BaseUrl`, Data Protection persistente, HTTPS e banco PostgreSQL descartável.
2. Executar restore/build/test e aplicar o banco canônico duas vezes.
3. Confirmar que respostas de `/bff/auth/*` não contêm `accessToken` nem `refreshToken`.
4. Validar login, cadastro, recuperação, logout e expiração em dois navegadores.
5. Conferir cookie Secure/HttpOnly, antiforgery, ausência de storage de autenticação e revogação na API.
6. Executar o validador premium, Playwright e axe nas resoluções previstas.

## Rollback

Reverter o commit da fase. Não há migration destrutiva nesta entrega. Sessões em memória desaparecem ao reiniciar o Web, exigindo novo login.
