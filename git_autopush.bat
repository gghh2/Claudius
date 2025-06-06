@echo off
setlocal

:: === CONFIGURATION ===
set "PROJECT_PATH=E:\Projets\Claudius"
set "BRANCH=main"
set "INTERVAL=60"  :: Temps en secondes entre chaque vérification

:LOOP
cd /d "%PROJECT_PATH%"

:: Vérifie s’il y a des changements
git status --porcelain > nul
if not errorlevel 1 (
    echo [%date% %time%] 🔍 Modifications détectées, enregistrement en cours...

    git add -A
    git commit -m "⏱️ Commit automatique le %date% %time%"
    git push origin %BRANCH%

    echo [%date% %time%] ✅ Push effectué.
) else (
    echo [%date% %time%] 🟢 Aucun changement détecté.
)

:: Pause INTERVAL secondes
timeout /t %INTERVAL% /nobreak > nul

goto LOOP
