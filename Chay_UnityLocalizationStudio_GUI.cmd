@echo off
chcp 65001 > nul
title Unity Localization Studio - Professional Publisher Edition
echo ======================================================================
echo    KHỞI ĐỘNG UNITY LOCALIZATION STUDIO (GIAO DIỆN ĐỒ HỌA GUI)
echo ======================================================================
echo.
echo Đang mở ứng dụng...
start "" "%~dp0Output\Studio\UnityLocalizationStudio.exe"
exit
