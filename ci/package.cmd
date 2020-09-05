
mkdir package
cd package

set ArtifactName=TCSynchronize-1.0.0

mkdir "%ArtifactName%"

xcopy "..\configuration" "%ArtifactName%\configuration" /E /Y /I /R
xcopy "..\resources" "%ArtifactName%\resources" /E /Y /I /R
xcopy ""..\bin\Release\netcoreapp3.1\win-x64\publish\TCSynchronize.exe" "%ArtifactName%\TCSynchronize.exe" /E /Y /I /R

REM Package ZIP archive
7z a "%ArtifactName%.zip" "%ArtifactName%"
