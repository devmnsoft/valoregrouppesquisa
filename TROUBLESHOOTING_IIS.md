# Troubleshooting IIS

## Erro: `spawnSync git ENOENT`

Esse erro indica que o Git não está instalado ou não está disponível no `PATH` do Windows.

Correção:

```powershell
git --version
winget install --id Git.Git -e --source winget
```

Depois, confirme que este caminho está no `PATH` do Windows:

```text
C:\Program Files\Git\cmd
```

O `npm run security:check` roda em modo fallback local quando Git não existe fora de CI, procurando secrets e CSP insegura nos arquivos locais. Sem Git, ele não consegue confirmar exatamente quais arquivos estão versionados nem validar `git ls-files dist`.

Alternativa emergencial, não recomendada para produção:

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply --skip-security-check
```

Ao usar `--skip-security-check`, o publicador não pula a validação automaticamente: a opção precisa ser explícita e o relatório registra `ATENÇÃO: security-check foi pulado nesta publicação.`
