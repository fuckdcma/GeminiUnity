$p = Start-Process -FilePath "C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\PlateUp.exe" -PassThru
Start-Sleep -Seconds 8
if (!$p.HasExited) {
    Stop-Process -Id $p.Id -Force
}
