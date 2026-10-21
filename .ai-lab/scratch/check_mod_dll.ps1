Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('Output\Studio\VietnameseLocalizationMod.dll')
$p = $asm.MainModule.Types | Where-Object { $_.Name -eq 'VietnamesePlugin' }
$m = $p.Methods | Where-Object { $_.Name -eq 'InjectGameDataDirectly' }
Write-Output "--- InjectGameDataDirectly in Output\Studio\VietnameseLocalizationMod.dll ---"
$m.Body.Instructions | ForEach-Object { "$($_.OpCode) $($_.Operand)" }
