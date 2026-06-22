$ErrorActionPreference = "Stop"
function Invoke-Valora($argsLine) {
  Write-Host "Executando: node scripts/go-production.js $argsLine" -ForegroundColor Cyan
  node scripts/go-production.js $argsLine
  if ($LASTEXITCODE -ne 0) { throw "Comando falhou com código $LASTEXITCODE" }
}
while ($true) {
  Clear-Host
  Write-Host "========================================"
  Write-Host " VALORA PULSE - PUBLICADOR DE PRODUÇÃO"
  Write-Host "========================================"
  Write-Host ""
  Write-Host "1. Simular entrada em produção"
  Write-Host "2. Entrar em produção agora"
  Write-Host "3. Entrar em produção importando dados locais"
  Write-Host "4. Apenas importar dados no Firebase"
  Write-Host "5. Apenas gerar build PRD"
  Write-Host "6. Apenas rodar health check"
  Write-Host "7. Restaurar último backup"
  Write-Host "0. Sair"
  Write-Host ""
  $op = Read-Host "Escolha uma opção"
  try {
    switch ($op) {
      "1" { Invoke-Valora "--dry-run" }
      "2" { Invoke-Valora "--apply" }
      "3" { Invoke-Valora "--apply --with-data --open-browser" }
      "4" { Invoke-Valora "--apply --with-data" }
      "5" { npm run build:prod; if ($LASTEXITCODE -ne 0) { throw "Build falhou" } }
      "6" { npm run prod:health; if ($LASTEXITCODE -ne 0) { throw "Health check falhou" } }
      "7" { node scripts/restore-iis-backup.js --latest; if ($LASTEXITCODE -ne 0) { throw "Rollback falhou" } }
      "0" { return }
      default { Write-Host "Opção inválida." -ForegroundColor Yellow }
    }
  } catch {
    Write-Host "Erro: $_" -ForegroundColor Red
  }
  Write-Host ""
  Read-Host "Pressione ENTER para voltar ao menu"
}
