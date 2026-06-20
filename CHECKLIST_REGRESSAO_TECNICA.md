# Checklist de Regressão Técnica — Valora Pulse

Validações obrigatórias antes de qualquer nova feature:

1. [ ] App sai de "Carregando o sistema".
2. [ ] App carrega com localStorage vazio.
3. [ ] App carrega com localStorage antigo.
4. [ ] Botão recriar base local funciona.
5. [ ] Login demo funciona.
6. [ ] Admin abre dashboard.
7. [ ] Admin abre planos.
8. [ ] Empresa abre dashboard.
9. [ ] Participante abre área.
10. [ ] `node --check` passa em todos os arquivos JavaScript.
11. [ ] Console sem `SyntaxError`.
12. [ ] Console sem erro de `state` nulo.
13. [ ] CSP sem inline handler bloqueado.

## Bloqueios obrigatórios

Não avançar para novas funcionalidades enquanto qualquer item acima estiver reprovado.
