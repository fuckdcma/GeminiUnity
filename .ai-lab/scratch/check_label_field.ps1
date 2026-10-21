Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.Common.dll')
$btn = $asm.MainModule.Types | Where-Object { $_.Name -eq 'ButtonElement' }
$lbl = $btn.Fields | Where-Object { $_.Name -eq 'Label' }
Write-Output "Label field: IsPublic=$($lbl.IsPublic), IsPrivate=$($lbl.IsPrivate), FieldType=$($lbl.FieldType.FullName)"
