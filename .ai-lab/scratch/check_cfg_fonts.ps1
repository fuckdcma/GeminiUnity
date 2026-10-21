Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
# Check fonts in Font_Mapping_Matrix.csv or Font_Mapping_Config.json
$cfg = Get-Content 'Output\Studio\Assets\Fonts\Font_Mapping_Config.json' -Raw
$matches = [regex]::Matches($cfg, '"OriginalFont":\s*"([^"]+)"')
Write-Output "Unique OriginalFont in config:"
$matches | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique | Select-Object -First 15
