# Inclusão manual dos assets oficiais Valora Group

## Regra operacional

O Codex não manipula, cria, converte nem anexa binários de marca. Os arquivos oficiais devem ser fornecidos e versionados manualmente por uma pessoa autorizada pela Valora Group.

## Onde colocar

Coloque exatamente estes arquivos no projeto Web oficial:

- `backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg`
- `backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg`

## Formatos aceitos

- JPEG real, com extensão `.jpeg`.
- Não usar SVG falso, PNG renomeado, WebP, URL externa ou imagem gerada.
- Não alterar os nomes sem atualizar validadores e SQL.

## Como testar

```bash
npm run web:brand-assets
VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets
npm run web:rc2-visual-readiness
```

O modo obrigatório falha se os JPEGs não existirem. O modo diagnóstico apenas avisa sobre ausência dos binários, mas continua falhando para secrets, imagem externa, `VG`/`V` como marca final ou paths inseguros.

## Como commitar manualmente

```bash
git add backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg
git add backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg
git commit -m "Add official Valora Group brand assets"
git push
```

## Como ver no navegador

1. Inicie a Web oficial em `backend/Valora.Web`.
2. Abra `/`, `/diagnostico-gratuito`, `/resultado/{id}`, `/certificado/{id}`, `/certificado/validar` e uma rota administrativa após login.
3. Confirme que a logo aparece sem distorção em desktop e mobile.
4. Remova temporariamente um arquivo em ambiente local para confirmar que o fallback `Valora Group` aparece sem imagem quebrada.

## Resolução de imagem quebrada

- Confirme o nome exato e a extensão `.jpeg`.
- Confirme se o arquivo está em `backend/Valora.Web/wwwroot/img/brand`.
- Rode `npm run web:brand-assets` para detectar paths inválidos.
- Limpe cache do navegador ou faça hard refresh.
