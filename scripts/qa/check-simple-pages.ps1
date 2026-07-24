$ErrorActionPreference='Stop'
$critical = Get-ChildItem -Path . -Filter Simple.cshtml -Recurse -File | Where-Object { $_.FullName -match 'SuperAdmin|Payments|Overdue|Subscriptions|Clients' }
if ($critical) { $critical | ForEach-Object FullName; throw 'Critical Simple.cshtml found.' }
Write-Host 'Simple page checks passed.'
