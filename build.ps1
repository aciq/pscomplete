dotnet build src -c Release
Copy-Item "./src/bin/Release/net6.0/aciq.pscomplete.dll" "./PsComplete/aciq.pscomplete.dll" -Force
Import-Module .\PsComplete\PsComplete.psd1
Install-PsComplete