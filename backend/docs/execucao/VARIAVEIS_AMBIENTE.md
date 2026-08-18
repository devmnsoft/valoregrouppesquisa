# Variáveis de ambiente

| Variável | Obrigatória | Uso |
|---|---:|---|
| `ASPNETCORE_ENVIRONMENT` | sim | `Development` local; `Production` em produção |
| `ConnectionStrings__DefaultConnection` | sim | PostgreSQL oficial; contém secret e não deve ser logada |
| `Jwt__Secret` | sim | assinatura de tokens; mínimo 32 caracteres em ambiente compartilhado |
| `Api__BaseUrl` | Web | endereço interno da API para o BFF |
| `VALORA_SEED_DEMO` | não | opt-in do seed; aceito somente em Development |
| `Email__Enabled` | não | habilita entrega por e-mail |
| `Email__Smtp__Host` | se e-mail | host SMTP |
| `Email__Smtp__Username` / `Email__Smtp__Password` | se exigido | credenciais SMTP secretas |
| `VALORA_BACKUP_DIR` | não | diretório operacional de backup |
| `Build__Sha` | não | identificador não sensível exibido na saúde |

Use `.env.example` como catálogo, não como secret store. Configurações opcionais ausentes aparecem como atenção/não configurado em Saúde do Sistema.
