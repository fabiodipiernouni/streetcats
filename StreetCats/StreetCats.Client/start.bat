@echo off
setlocal

echo ========================================
echo 🐱 STREETCATS - Menu di Avvio
echo ========================================
echo.
echo Seleziona l'ambiente di avvio:
echo.
echo [1] 🧪 Development  - Servizi Mock (Demo)
echo [2] 🔄 Integration  - Backend Locale (localhost:3000)
echo [3] 🚀 Production   - Backend Reale (api.streetcats.it)
echo [4] ❌ Esci
echo.

:menu
set /p choice="Inserisci la tua scelta (1-4): "

if "%choice%"=="1" (
    echo.
    echo 🧪 Avvio modalità Development...
    call run.bat Development
    goto end
)

if "%choice%"=="2" (
    echo.
    echo 🔄 Avvio modalità Integration...
    call run.bat Integration
    goto end
)

if "%choice%"=="3" (
    echo.
    echo 🚀 Avvio modalità Production...
    call run.bat Production
    goto end
)

if "%choice%"=="4" (
    echo.
    echo 👋 Arrivederci!
    goto end
)

echo.
echo ❌ Scelta non valida! Riprova.
echo.
goto menu

:end
echo.
pause