@echo off
setlocal

:: === CONFIGURATION ===
set "PROJECT_PATH=E:\Projets\Claudius"
set "BRANCH=main"
set "INTERVAL=60"  :: Temps en secondes entre chaque verification

:LOOP
cd /d "%PROJECT_PATH%"

:: Verifie s’il y a des changements
git status --porcelain > nul
if not errorlevel 1 (
    echo [%date% %time%] Modifications detectees, enregistrement en cours...

    git add -A
    git commit -m "Commit automatique le %date% %time%"
    git push origin %BRANCH%

    echo [%date% %time%] >>>>>>>>>>>>>>>>>> Push effectue.
) else (
    echo [%date% %time%] Aucun changement detecte.
)

:: Pause INTERVAL secondes
timeout /t %INTERVAL% /nobreak > nul

goto LOOP
