$managed = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed"
$kd = [System.Reflection.Assembly]::LoadFrom("$managed\Kitchen.GameData.dll")

$dl = $kd.GetType("KitchenData.DictionaryLocalisation")
Write-Host "=== KitchenData.DictionaryLocalisation ==="
$dl.GetMethods([System.Reflection.BindingFlags]"Public,NonPublic,Instance,Static,DeclaredOnly") | ForEach-Object {
    $p = ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
    Write-Host "$($_.ReturnType.Name) $($_.Name)($p)"
}

$gl = $kd.GetType("KitchenData.GlobalLocalisation")
Write-Host "`n=== KitchenData.GlobalLocalisation ==="
$gl.GetMethods([System.Reflection.BindingFlags]"Public,NonPublic,Instance,Static,DeclaredOnly") | ForEach-Object {
    $p = ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
    Write-Host "$($_.ReturnType.Name) $($_.Name)($p)"
}
