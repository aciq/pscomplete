dotnet build src -c Release
Copy-Item "$PSScriptRoot/src/bin/Release/net7.0/aciq.pscomplete.dll" "./PsComplete/aciq.pscomplete.dll" -Force
Import-Module $PSScriptRoot\PsComplete\PsComplete.psd1