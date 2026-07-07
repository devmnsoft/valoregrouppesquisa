# Sprint Valora RC2 — Diagnóstico Visual da Marca

## Estado dos assets

- `backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg`: **não encontrado no working tree**.
- `backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg`: **não encontrado no working tree**.
- Codex não criou, converteu nem anexou binários. A inclusão permanece como pendência manual.

## Uso atual da marca

- Logo completa: Home pública e certificado público usam `/img/brand/valora-logo-full.jpeg` com fallback textual premium.
- Símbolo: topbar pública, sidebar admin, topbar admin, resultado público e validação de certificado usam `/img/brand/valora-symbol.jpeg` com fallback textual.
- Fallback textual: `Valora Group`, sem `VG` ou `V` solto, acionado por `onerror` com `brand-fallback-active`.

## Avaliação visual e funcional

- Fallback: seguro, sem imagem quebrada, com cor institucional, tipografia limpa e aparência executiva.
- Home pública: posicionada como jornada comercial premium com diagnóstico gratuito, CTA WhatsApp, entrada, processo, dimensões, devolutiva, certificado e LGPD.
- Resultado/certificado: usam marca ou fallback, não exibem JSON bruto, token ou hash.
- Admin: usa símbolo/fallback no menu lateral e topbar, com layout Bootstrap/jQuery do projeto oficial.
- Menus/login/perfil: área administrativa permanece separada do layout público e validada por scripts de sessão, guards e perfil.
- SQL: paths oficiais de marca estão previstos nos scripts SQL raiz e PostgreSQL.

## Estado dos validadores antes da sprint

- `web:brand-assets` falhava quando os JPEGs reais não estavam presentes.
- Era necessário distinguir modo obrigatório e modo diagnóstico para permitir auditoria sem versionar binários.

## Gaps restantes

- Adicionar manualmente os dois JPEGs oficiais.
- Executar homologação real com .NET SDK, PostgreSQL e navegador após inclusão dos binários.
- Rodar validação visual desktop/mobile com os assets reais versionados.

## Plano objetivo da sprint

1. Manter os paths oficiais de JPEG e remover dependência de formatos alternativos.
2. Garantir fallback premium em público, admin, resultado e certificado.
3. Atualizar `web:brand-assets` com modo obrigatório e diagnóstico.
4. Criar manual de inclusão dos assets e checklist de homologação visual.
5. Criar validador RC2 visual readiness.
6. Atualizar documentação de RC2 e registrar pendências para homologação final.
