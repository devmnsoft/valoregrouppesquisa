$ErrorActionPreference='Stop'
$matches = Get-ChildItem -Path . -Include *.cshtml,*.html -Recurse -File | Select-String -Pattern 'TODO|FIXME'
if ($matches) { $matches | ForEach-Object { Write-Host $_ }; throw 'Visible TODO/FIXME found.' }
Write-Host 'Visible TODO checks passed.'
