@echo off
title 1 Bullet - Update
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\update.ps1" & echo. & pause & exit /b
