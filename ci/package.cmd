
mkdir package
cd package

REM Package ZIP archive
7z a "TCSynchronize-1.0.0.zip" "..\configuration" "..\resources" "..\bin\Release\netcoreapp3.1\win-x64\publish\TCSynchronize.exe"
