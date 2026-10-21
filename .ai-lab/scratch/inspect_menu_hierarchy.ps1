Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.Common.dll')
$smm = $asm.MainModule.Types | Where-Object { $_.Name -eq 'StartMainMenu' }
Write-Output "StartMainMenu BaseType: $($smm.BaseType.FullName)"
$parent = $asm.MainModule.Types | Where-Object { $_.FullName -eq $smm.BaseType.FullName -or $_.Name -eq $smm.BaseType.Name }
Write-Output "Parent BaseType: $($parent.BaseType.FullName)"
$grandparent = $asm.MainModule.Types | Where-Object { $_.FullName -eq $parent.BaseType.FullName -or $_.Name -eq $parent.BaseType.Name }
Write-Output "Grandparent Methods:"
$grandparent.Methods | Where-Object { $_.Name -like '*Button*' } | ForEach-Object {
    Write-Output "METHOD: $($_.Name)"
    $_.Body.Instructions | ForEach-Object { "  $($_.OpCode) $($_.Operand)" }
}
