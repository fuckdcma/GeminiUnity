$bytes = [System.IO.File]::ReadAllBytes('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\resources.assets')
$str = [System.Text.Encoding]::UTF8.GetString($bytes)
$matches = [regex]::Matches($str, '([A-Za-z0-9_\- ]+ SDF)')
Write-Output "Found SDF Fonts in resources.assets:"
$matches | ForEach-Object { $_.Value } | Select-Object -Unique | Select-Object -First 20
