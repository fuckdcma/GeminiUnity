$managed = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed"
$tmp = [System.Reflection.Assembly]::LoadFrom("$managed\Unity.TextMeshPro.dll")

$settings = $tmp.GetType("TMPro.TMP_Settings")
Write-Host "=== TMP_Settings Properties ==="
$settings.GetProperties([System.Reflection.BindingFlags]"Public,Static") | ForEach-Object {
    Write-Host "$($_.PropertyType.Name) $($_.Name)"
}
