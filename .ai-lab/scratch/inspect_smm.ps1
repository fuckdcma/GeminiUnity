Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.dll')
$smm = $asm.MainModule.Types | Where-Object { $_.Name -eq 'StartMainMenu' }
$setup = $smm.Methods | Where-Object { $_.Name -eq 'Setup' }
Write-Output "--- StartMainMenu.Setup IL ---"
$setup.Body.Instructions | ForEach-Object { "$($_.OpCode) $($_.Operand)" }
