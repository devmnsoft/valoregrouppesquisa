$ErrorActionPreference='Stop'
Get-ChildItem -Path . -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj|publish|.git)\\' -and $_.Length -gt 25MB } | ForEach-Object { throw "Large binary-like asset: $($_.FullName)" }
Write-Host 'Asset checks passed.'
