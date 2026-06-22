$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $Root
function Run-Node($ArgsLine) {
  Write-Host "`n> node $ArgsLine`n" -ForegroundColor Cyan
  $nodeArgs = $ArgsLine -split ' '
  & node @nodeArgs
  if ($LASTEXITCODE -ne 0) { Write-Host "`nFalhou com código $LASTEXITCODE" -ForegroundColor Red }
}
function Ask-DataFile() { Read-Host "Informe o caminho do export JSON (.\exports\valora-prd-export.json)" }
do {
  Clear-Host
  Write-Host "========================================"
  Write-Host " PUBLICADOR VALORA PULSE - PRD / IIS"
  Write-Host "========================================"
  Write-Host "1. Simular publicação"
  Write-Host "2. Publicar em produção"
  Write-Host "3. Gerar pacote IIS sem copiar"
  Write-Host "4. Publicar levando dados locais para Firebase"
  Write-Host "5. Rodar health check da PRD"
  Write-Host "6. Restaurar último backup"
  Write-Host "7. Abrir pasta de relatórios"
  Write-Host "0. Sair"
  $op = Read-Host "Escolha uma opção"
  switch ($op) {
    "1" { Run-Node "scripts/publish-iis-prd.js --dry-run" }
    "2" { Run-Node "scripts/publish-iis-prd.js --apply" }
    "3" { Run-Node "scripts/publish-iis-prd.js --package-only" }
    "4" { $f = Ask-DataFile; Run-Node "scripts/publish-iis-prd.js --apply --with-data --data-file $f" }
    "5" { Run-Node "scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase" }
    "6" { Run-Node "scripts/restore-iis-backup.js --latest" }
    "7" { New-Item -ItemType Directory -Force -Path "publish\reports" | Out-Null; Invoke-Item "publish\reports" }
    "0" { return }
    default { Write-Host "Opção inválida." -ForegroundColor Yellow }
  }
  Read-Host "Pressione ENTER para continuar"
} while ($true)
Read-Host "Pressione ENTER para sair"
