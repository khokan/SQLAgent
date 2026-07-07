@echo off
REM ==========================================
REM Ollama Setup and Run Script for Windows
REM ==========================================

setlocal enabledelayedexpansion

set OLLAMA_PATH=C:\Users\Administrator\AppData\Local\Programs\Ollama\ollama.exe
set MODEL=mistral

echo.
echo ========================================
echo    Ollama Setup Helper Script
echo ========================================
echo.

REM Check if Ollama exists
if not exist "%OLLAMA_PATH%" (
    echo [ERROR] Ollama not found at: %OLLAMA_PATH%
    echo Please install Ollama from: https://ollama.ai
    pause
    exit /b 1
)

echo [OK] Ollama found: %OLLAMA_PATH%
echo.

REM Show menu
:menu
echo Please choose an option:
echo.
echo 1) Check Ollama version
echo 2) List downloaded models
echo 3) Start Ollama service (localhost:11434)
echo 4) Download Mistral model
echo 5) Download Llama2 model
echo 6) Run interactive chat
echo 7) Exit
echo.

set /p choice="Enter your choice (1-7): "

if "%choice%"=="1" goto version
if "%choice%"=="2" goto list
if "%choice%"=="3" goto serve
if "%choice%"=="4" goto mistral
if "%choice%"=="5" goto llama2
if "%choice%"=="6" goto chat
if "%choice%"=="7" goto exit

echo [ERROR] Invalid choice. Please try again.
echo.
goto menu

:version
echo.
"%OLLAMA_PATH%" --version
echo.
pause
goto menu

:list
echo.
echo Listing downloaded models...
echo.
"%OLLAMA_PATH%" list
echo.
pause
goto menu

:serve
echo.
echo ========================================
echo Starting Ollama Service...
echo ========================================
echo.
echo Ollama will run on: http://localhost:11434
echo.
echo To test in another terminal, run:
echo   curl http://localhost:11434/api/tags
echo.
echo Press Ctrl+C to stop the service.
echo.
"%OLLAMA_PATH%" serve
goto menu

:mistral
echo.
echo ========================================
echo Downloading Mistral model (~4GB)...
echo ========================================
echo This may take 5-15 minutes depending on internet speed.
echo.
"%OLLAMA_PATH%" pull mistral
echo.
if errorlevel 1 (
    echo [ERROR] Failed to download Mistral
) else (
    echo [OK] Mistral downloaded successfully!
)
echo.
pause
goto menu

:llama2
echo.
echo ========================================
echo Downloading Llama2 model (~4GB)...
echo ========================================
echo This may take 5-15 minutes depending on internet speed.
echo.
"%OLLAMA_PATH%" pull llama2
echo.
if errorlevel 1 (
    echo [ERROR] Failed to download Llama2
) else (
    echo [OK] Llama2 downloaded successfully!
)
echo.
pause
goto menu

:chat
echo.
echo ========================================
echo Starting Interactive Chat
echo ========================================
echo.
echo Checking for available models...
"%OLLAMA_PATH%" list
echo.
set /p model="Enter model name (default: mistral): "
if "!model!"=="" set model=mistral
echo.
"%OLLAMA_PATH%" run !model!
echo.
pause
goto menu

:exit
echo.
echo Goodbye! Remember to start Ollama before using SQLAgent.
echo Run this script and choose option 3 to start Ollama.
echo.
pause
exit /b 0

endlocal
