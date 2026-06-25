# Admin > Comunicações

A tela mostra Data, Empresa, Pesquisa, Participante, E-mail, Canal, Status, Tentativas, Último erro e Ações.

## Ações
- Reenviar resultado: chama `resendResultEmail(responseId)`.
- Copiar link do resultado.
- Ver resposta.
- Ver histórico/status.

## Permissões
- `admin_valora`: vê tudo.
- `empresa_admin` e gestores autorizados: veem somente a própria empresa.
- `participante`: não acessa.

## Configurações > Comunicação
Mostra status online/offline, e-mail configurado/não configurado, SMTP ativo/falha e permite Enviar e-mail de teste pelo gateway. Segredos como host, senha e tokens não são exibidos.
