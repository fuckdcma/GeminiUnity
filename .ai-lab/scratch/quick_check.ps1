Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
# Check in asset verified or raw files
$csv = Import-Csv 'Output\Audit_Verification\PlateUp_EN_2243_Asset_Verified.csv' | Select-Object -First 5
Write-Output "Checked CSV"
