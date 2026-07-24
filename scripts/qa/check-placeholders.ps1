$ErrorActionPreference='Stop'
$matches = Get-ChildItem -Path . -Include *.cshtml,*.html -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj|publish)\\' } | Select-String -Pattern 'Lorem ipsum|Página em construção|stack trace|JSON técnico'
if ($matches) { $matches | ForEach-Object { Write-Host $_ }; throw 'Visible placeholder/error text found.' }
Write-Host 'Placeholder checks passed.'
