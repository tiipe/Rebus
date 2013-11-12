@echo off
set currentDir=%~dp0%
set rootDir=%currentDir%
set srcDir=%rootDir%\src
set toolsDir=%rootDir%\tools
set packagesDir=%rootDir%\src\packages
set scriptsDir=%rootDir%\scripts

set fakeDir=%packagesDir%\FAKE.1.74.283.0\tools
set fake=%fakeDir%\fake.exe

"%toolsDir%\NuGet\NuGet.exe" restore "%srcDir%\.nuget\packages.config" -SolutionDirectory "%srcDir%"

"%fake%" "%rootDir%\scripts\build.fsx"
