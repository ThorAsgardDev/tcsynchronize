
mkdir package
cd package

mkdir "TCSynchronize-1.0.0"

xcopy "..\configuration" "TCSynchronize-1.0.0" /E /Y /I /R
xcopy "..\resources" "TCSynchronize-1.0.0" /E /Y /I /R
xcopy ""..\bin\Release\netcoreapp3.1\win-x64\publish\TCSynchronize.exe" "TCSynchronize-1.0.0" /E /Y /I /R

REM Package ZIP archive
7z a "TCSynchronize-1.0.0.zip" "TCSynchronize-1.0.0" 
