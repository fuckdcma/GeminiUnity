$managed = "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp_Data\Managed"
[System.Reflection.Assembly]::LoadFrom("$managed\UnityEngine.CoreModule.dll") | Out-Null
[System.Reflection.Assembly]::LoadFrom("$managed\UnityEngine.UI.dll") | Out-Null
$tmp = [System.Reflection.Assembly]::LoadFrom("$managed\Unity.TextMeshPro.dll")

$ugui = $tmp.GetType("TMPro.TextMeshProUGUI")
Write-Host "=== TextMeshProUGUI Declared Methods ==="
$ugui.GetMethods([System.Reflection.BindingFlags]"Public,NonPublic,Instance,DeclaredOnly") | Where-Object { $_.Name -like "*Enable*" -or $_.Name -like "*Awake*" -or $_.Name -like "*Start*" } | ForEach-Object {
    Write-Host "$($_.DeclaringType.FullName):: $($_.ToString())"
}

$tm = $tmp.GetType("TMPro.TextMeshPro")
Write-Host "`n=== TextMeshPro Declared Methods ==="
$tm.GetMethods([System.Reflection.BindingFlags]"Public,NonPublic,Instance,DeclaredOnly") | Where-Object { $_.Name -like "*Enable*" -or $_.Name -like "*Awake*" -or $_.Name -like "*Start*" } | ForEach-Object {
    Write-Host "$($_.DeclaringType.FullName):: $($_.ToString())"
}

$kd = [System.Reflection.Assembly]::LoadFrom("$managed\Kitchen.GameData.dll")
Write-Host "`n=== KitchenData Types ==="
$kd.GetTypes() | Where-Object { $_.Name -like "*Local*" -or $_.Name -like "*String*" -or $_.Name -like "*Text*" -or $_.Name -like "*Lang*" } | ForEach-Object {
    Write-Host "Type: $($_.FullName)"
    $_.GetMethods([System.Reflection.BindingFlags]"Public,NonPublic,Static,Instance,DeclaredOnly") | ForEach-Object {
        Write-Host "  Method: $($_.ToString())"
    }
}
