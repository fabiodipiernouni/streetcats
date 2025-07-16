@echo off
setlocal enabledelayedexpansion

REM ========================================
REM STREETCATS - Script di Avvio
REM ========================================
REM Autore: StreetCats Team
REM Descrizione: Script per avviare l'app in diverse modalità
REM Uso: run.bat [Development|Integration|Production]
REM ========================================

echo.
echo ====================================
echo STREETCATS - Launcher Script
echo ====================================
echo.

REM Colori per Windows 11
set "RED=[91m"
set "GREEN=[92m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "MAGENTA=[95m"
set "CYAN=[96m"
set "WHITE=[97m"
set "RESET=[0m"

REM Verifica se è stato passato un parametro
if "%~1"=="" (
    echo %RED%ERRORE: Ambiente non specificato!%RESET%
    echo.
    echo %YELLOW%Uso corretto:%RESET%
    echo    %WHITE%run.bat Development%RESET%   ^(Servizi Mock - Non serve backend^)
    echo    %WHITE%run.bat Integration%RESET%   ^(Backend Locale - Porta 3000^)
    echo    %WHITE%run.bat Production%RESET%    ^(Backend Reale - api.streetcats.it^)
    echo.
    echo %CYAN%Ambienti disponibili:%RESET%
    echo    %GREEN%Development%RESET%  - Demo con dati mock
    echo    %BLUE%Integration%RESET%   - Test con backend locale
    echo    %MAGENTA%Production%RESET%    - Backend di produzione
    echo.
    pause
    exit /b 1
)

REM Normalizza il parametro (case-insensitive)
set "ENV=%~1"
if /i "%ENV%"=="dev" set "ENV=Development"
if /i "%ENV%"=="development" set "ENV=Development"
if /i "%ENV%"=="int" set "ENV=Integration"
if /i "%ENV%"=="integration" set "ENV=Integration"
if /i "%ENV%"=="prod" set "ENV=Production"
if /i "%ENV%"=="production" set "ENV=Production"

REM Verifica che l'ambiente sia valido
if /i not "%ENV%"=="Development" if /i not "%ENV%"=="Integration" if /i not "%ENV%"=="Production" (
    echo %RED%ERRORE: Ambiente '%~1' non riconosciuto!%RESET%
    echo.
    echo %YELLOW%Ambienti validi:%RESET%
    echo    • Development ^(o dev^)
    echo    • Integration ^(o int^)  
    echo    • Production ^(o prod^)
    echo.
    pause
    exit /b 1
)

REM Mostra configurazione ambiente
echo %CYAN%Configurazione Ambiente:%RESET%
echo    Directory: %CD%
echo    Ambiente: %WHITE%%ENV%%RESET%

REM Configurazioni specifiche per ambiente
if /i "%ENV%"=="Development" (
    echo    Modalità: %GREEN%DEVELOPMENT%RESET% ^(Servizi Mock^)
    echo    Backend: %YELLOW%Non richiesto%RESET%
    echo    Logging: %GREEN%Dettagliato%RESET%
    set "ENV_COLOR=%GREEN%"
)

if /i "%ENV%"=="Integration" (
    echo    Modalità: %BLUE%INTEGRATION%RESET% ^(API Reali^)
    echo    Backend: %WHITE%http://localhost:3000/api%RESET%
    echo    Logging: %BLUE%Completo%RESET%
    echo    %YELLOW%AVVISO: Assicurati che il backend sia in esecuzione su porta 3000%RESET%
    set "ENV_COLOR=%BLUE%"
)

if /i "%ENV%"=="Production" (
    echo    Modalità: %MAGENTA%PRODUCTION%RESET% ^(Backend Reale^)
    echo    Backend: %WHITE%https://api.streetcats.it/api%RESET%
    echo    Logging: %MAGENTA%Minimale%RESET%
    echo    %RED%AVVISO: Richiede backend di produzione attivo%RESET%
    set "ENV_COLOR=%MAGENTA%"
)

echo.
echo %CYAN%Avvio tra 3 secondi...%RESET%
timeout /t 3 /nobreak >nul

REM Verifica che dotnet sia installato
echo %CYAN%Verifica prerequisiti...%RESET%
where dotnet >nul 2>&1
if errorlevel 1 (
    echo %RED%ERRORE: .NET SDK non trovato!%RESET%
    echo %YELLOW%Scarica .NET 8 da: https://dotnet.microsoft.com/download%RESET%
    pause
    exit /b 1
)

REM Verifica che il progetto esista
if not exist "StreetCats.Client.csproj" (
    echo %RED%ERRORE: File progetto non trovato!%RESET%
    echo %YELLOW%Assicurati di essere nella directory del progetto StreetCats.Client%RESET%
    pause
    exit /b 1
)

REM Per Integration, verifica connessione backend (opzionale)
if /i "%ENV%"=="Integration" (
    echo %CYAN%Verifica connessione backend locale...%RESET%
    curl -s -o nul -w "%%{http_code}" http://localhost:3000/health 2>nul | findstr "200" >nul
    if errorlevel 1 (
        echo %YELLOW%AVVISO: Backend localhost:3000 non risponde%RESET%
        echo %YELLOW%   Continuando comunque... ^(il backend potrebbe non essere ancora avviato^)%RESET%
    ) else (
        echo %GREEN%OK: Backend localhost:3000 raggiungibile%RESET%
    )
)

echo.
echo ====================================
echo %ENV_COLOR%AVVIO %ENV%%RESET%
echo ====================================

REM Imposta variabile ambiente
set "ASPNETCORE_ENVIRONMENT=%ENV%"

REM Log di avvio
echo %CYAN%Eseguendo: dotnet run%RESET%
echo %YELLOW%ASPNETCORE_ENVIRONMENT=%ENV%%RESET%
echo.

REM Avvia l'applicazione
dotnet run

REM Gestione errori
if errorlevel 1 (
    echo.
    echo %RED%ERRORE durante l'avvio dell'applicazione!%RESET%
    echo.
    echo %YELLOW%Possibili soluzioni:%RESET%
    echo    • Verifica che tutte le dipendenze siano installate: %WHITE%dotnet restore%RESET%
    echo    • Controlla i file di configurazione appsettings.%ENV%.json
    echo    • Verifica i log per errori specifici
    if /i "%ENV%"=="Integration" (
        echo    • Assicurati che il backend sia in esecuzione su localhost:3000
    )
    echo.
    pause
    exit /b 1
)

echo.
echo %GREEN%Applicazione terminata correttamente%RESET%
echo %CYAN%Arrivederci!%RESET%
pause