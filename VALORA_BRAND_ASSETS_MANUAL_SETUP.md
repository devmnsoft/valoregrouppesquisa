# Inclusão manual dos assets oficiais da marca Valora

O Codex não cria, converte, anexa nem versiona arquivos binários de marca. A aplicação possui fallback visual seguro, mas os binários oficiais devem ser adicionados manualmente por uma pessoa autorizada.

## Onde colocar

Copie os arquivos para `backend/Valora.Web/wwwroot/img/brand` com estes nomes obrigatórios:

- `valora-logo-full.jpeg`
- `valora-symbol.jpeg`

Recomendação: logo completa em JPEG otimizado com largura entre 800 e 1600 px; símbolo quadrado com pelo menos 512 x 512 px.

## Como testar

```bash
VALORA_ALLOW_MISSING_BRAND_ASSETS=true npm run web:brand-assets
npm run web:brand-assets
npm run web:visual-homologation
```

No navegador, abra a Home, diagnóstico, resultado, certificado, login e admin. Confirme que a imagem real aparece. Se ela não existir, o fallback institucional “Valora Group” deve aparecer sem imagem quebrada.

## Commit manual

```bash
git add backend/Valora.Web/wwwroot/img/brand/valora-logo-full.jpeg
git add backend/Valora.Web/wwwroot/img/brand/valora-symbol.jpeg
git commit -m "Add official Valora Group brand assets"
git push
```
