$managed = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed"
$tmp = [System.Reflection.Assembly]::LoadFrom("$managed\Unity.TextMeshPro.dll")

$fa = $tmp.GetType("TMPro.TMP_FontAsset")
Write-Host "=== TMP_FontAsset Methods ==="
$fa.GetMethods([System.Reflection.BindingFlags]"Public,Static") | Where-Object { $_.Name -like "*Create*" } | ForEach-Object {
    $p = ($_.GetParameters() | ForEach-Object { "$($_.ParameterType.Name) $($_.Name)" }) -join ", "
    Write-Host "$($_.ReturnType.Name) $($_.Name)($p)"
}
