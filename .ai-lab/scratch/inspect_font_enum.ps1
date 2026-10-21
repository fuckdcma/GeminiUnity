Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.GameData.dll')
$fnt = $asm.MainModule.Types | Where-Object { $_.Name -eq 'Font' }
Write-Output "--- KitchenData.Font FIELDS ---"
$fnt.Fields | ForEach-Object { "$($_.Name) = $($_.Constant)" }
