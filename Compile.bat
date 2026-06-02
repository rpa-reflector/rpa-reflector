TITLE Compiling
@ECHO off

SET projectFile=%1

SET netVersion=v4.0.30319
IF NOT "%~2" == "" (
    SET netVersion=%2
)

SET configuration=Release
IF NOT "%~3" == "" (
    SET configuration=%3
)

SET logFile=Build.log
IF NOT "%~4" == "" (
    SET logFile=%4
)

CLS

ECHO Starting build
ECHO.
ECHO Build settings:
ECHO   - Project file: %projectFile%
ECHO   - .NET version: %netVersion%
ECHO   - Configuration: %configuration%
ECHO   - Log file: %logFile%
ECHO.

ECHO Cleaning build dirs
RD .\Bin /S /Q
MD .\Bin
RD .\Debug /S /Q
MD .\Debug

ECHO Starting build
CALL "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\msbuild.exe" "%projectFile%" /p:Configuration=%configuration% /l:FileLogger,Microsoft.Build.Engine;logfile=%logFile%

IF %ERRORLEVEL% NEQ 0 (
	ECHO Compile failed, exiting..
	EXIT /b %ERRORLEVEL%
)

ECHO Copying release to .\Bin\

XCOPY .\Fernandez\bin\%configuration%\*.exe .\Bin\
XCOPY .\Fernandez\bin\%configuration%\*.exe.config .\Bin\
XCOPY .\Fernandez\bin\%configuration%\*.exe.manifest .\Bin\
XCOPY .\Fernandez\bin\%configuration%\*.dll .\Bin\
XCOPY .\Fernandez\bin\%configuration%\Fernandez.pdb .\Debug\
exit
