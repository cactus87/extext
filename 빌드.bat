@echo off
chcp 65001 >nul
echo ========================================
echo TextExpander - 타임스탬프 빌드
echo ========================================
echo.

cd /d "%~dp0src"

REM publish 폴더 생성 (없으면 생성)
if not exist "%~dp0publish" mkdir "%~dp0publish"

REM 타임스탬프 생성
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value') do set datetime=%%I
set timestamp=%datetime:~0,8%_%datetime:~8,6%

set "outputPath=%~dp0publish\%timestamp%"

echo 빌드 중: %outputPath%
echo.

dotnet publish TextExpander.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o "%outputPath%"

if %ERRORLEVEL% equ 0 (
    echo.
    echo ✅ 빌드 성공!
    echo 📁 위치: %outputPath%
    echo 🚀 실행: %outputPath%\TextExpander.exe
    echo.
    
    REM 탐색기로 폴더 열기
    explorer "%outputPath%"
    
    REM 실행 옵션
    set /p run="바로 실행하시겠습니까? (Y/N): "
    if /i "%run%"=="Y" (
        start "" "%outputPath%\TextExpander.exe"
    )
) else (
    echo.
    echo ❌ 빌드 실패!
    pause
)

