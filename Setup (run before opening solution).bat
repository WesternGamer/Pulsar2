:: This script creates a symlink to the game binaries to account for different installation directories on different systems.

@echo off
set /p path="Please enter the folder location of your SpaceEngineers2.exe: "
cd %~dp0
rmdir SeBinaries > nul 2>&1
mklink /J SeBinaries "%path%"
if errorlevel 1 goto Error
echo Done!

echo You can now open the project without issue.
goto EndFinal

:Error
echo An error occured creating the symlink.
goto EndFinal

:EndFinal
pause
