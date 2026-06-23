# Resiliência de renderização

A SPA usa `renderGuard()` nas rotas principais e `normalizeStateInPlace()` antes de renderizar. A Home normaliza FAQ com `normalizeFaqItems()` e o ValoraBot usa `asArray()` para `knowledgeBase`.

Teste estados legados com:

```bash
node scripts/validate-render-resilience.js
```

O teste cobre FAQ string, objeto map, `{ items: [...] }`, arrays ausentes e mapas em campos externos.
