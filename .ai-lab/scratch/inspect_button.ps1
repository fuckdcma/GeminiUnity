Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.Common.dll')
$btn = $asm.MainModule.Types | Where-Object { $_.Name -eq 'ButtonElement' }
$setLabel = $btn.Methods | Where-Object { $_.Name -eq 'SetLabel' }
Write-Output "--- SetLabel IL ---"
$setLabel.Body.Instructions | ForEach-Object { "$($_.OpCode) $($_.Operand)" }

$setStyle = $btn.Methods | Where-Object { $_.Name -eq 'SetStyle' }
Write-Output "--- SetStyle IL ---"
$setStyle.Body.Instructions | ForEach-Object { "$($_.OpCode) $($_.Operand)" }
