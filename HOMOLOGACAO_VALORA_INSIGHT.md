# Homologação Valora Insight™

Checklist operacional para validar a Devolutiva Inteligente Valora Insight™ em ambiente local, homologação e PRD.

## Checklist de aceite

- [ ] 5 dimensões
- [ ] 25 perguntas
- [ ] 125 pontos
- [ ] ESG fora do diagnóstico principal
- [ ] Devolutiva executiva completa
- [ ] Radar textual
- [ ] Benchmarking qualitativo
- [ ] Verdade estratégica
- [ ] Risco se nada mudar
- [ ] Próximo nível
- [ ] CTA
- [ ] PDF/relatório
- [ ] Admin > Respostas
- [ ] Área do participante
- [ ] Sem NaN
- [ ] Sem undefined
- [ ] Sem linguagem genérica
- [ ] Health check PASS

## Como validar

1. Execute `npm run check`.
2. Execute `node scripts/validate-valora-insight.js`.
3. Execute `npm run build:prod`.
4. Publique no IIS e rode `node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa`.

## Cenário obrigatório 72/125

Use uma resposta com pontuação por dimensão `18, 12, 16, 14, 12`. O total deve ser `72 / 125` e o nível deve ser `Em estruturação`.

## Publicação IIS

```powershell
robocopy C:\DBBACK\valoregrouppesquisa\dist C:\inetpub\wwwroot\valoragroup /MIR
Copy-Item C:\DBBACK\valoregrouppesquisa\templates\iis\web.config C:\inetpub\wwwroot\valoragroup\web.config -Force
iisreset
```
