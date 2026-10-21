Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.GameData.dll')
$gl = $asm.MainModule.Types | Where-Object { $_.Name -eq 'GlobalLocalisation' }
Write-Output "--- GlobalLocalisation FIELDS ---"
$gl.Fields | ForEach-Object { "$($_.Name) : $($_.FieldType.FullName)" }
Write-Output "--- GlobalLocalisation PROPERTIES ---"
$gl.Properties | ForEach-Object { "$($_.Name) : $($_.PropertyType.FullName)" }
