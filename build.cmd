@echo off
setlocal
where /q pwsh
if errorlevel 0 goto RUN_PWSH_CORE
where /q powershell
if errorlevel 0 goto RUN_PWSH
echo Please install PowerShell or PowerShell Core.
exit /b

:RUN_PWSH_CORE
pwsh build.ps1
exit /b

:RUN_PWSH
powershell -NoProfile -ExecutionPolicy Bypass -Command "[System.Threading.Thread]::CurrentThread.CurrentCulture = ''; [System.Threading.Thread]::CurrentThread.CurrentUICulture = ''; try { & '%~dp0build.ps1' %*; exit $LastExitCode } catch { Write-Host $_; exit 1 }"
exit /b