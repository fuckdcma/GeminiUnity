Add-Type -Path 'UnityLocalizationStudio\bin\Release\net8.0-windows\Mono.Cecil.dll'
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly('C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed\Kitchen.Common.dll')
$menu = $asm.MainModule.Types | Where-Object { $_.Name -like 'Menu`1' }
Write-Output "--- Menu Methods ---"
$menu.Methods | Where-Object { $_.Name -like '*Button*' } | ForEach-Object {
    Write-Output "METHOD: $($_.Name) Parameters: $($_.Parameters.Count)"
    $_.Body.Instructions | ForEach-Object { "  $($_.OpCode) $($_.Operand)" }
}
