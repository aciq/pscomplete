# PsComplete
cross-platform custom powershell autocomplete

Rewritten from scratch to make it compatible with mac/linux

## demo  

https://user-images.githubusercontent.com/36763595/201066021-73a4874f-e880-43f1-abaf-13c8965bc5b3.mp4


## installation

- `Install-Module -Name PsComplete`
- `Import-Module -Name PsComplete`
- After that Tab is bound to the expander for this session

completion can also be invoked programmatically with `Invoke-GuiPsComplete`


## features

- Search with regex (`^<start>.*<filter>`)
- Performant up to tens of thousands of completions
- Auto-expands to positional parameters (ex. Get-Process (pos.0))
- Press Tab again to immediately select the next parameter (useful for switches)
- Press Enter to finish command

## one caveat because of missing features in Powershell Core:

- Only works when there is enough free space under the current command (use clear)

<!-- 
- the color Black does not exist in windows, it's the background color. however it does exist on linux
- blank color (-1) only exists on linux, throws an exception on windows
- the coordinate systems of linux pwsh and windows are different (windows coordinates are -1)
- there is no way to access the buffer on linux, but it can be overridden with a new array which is destructive to previous screen contents
- there is no way to fill a rectangle on linux using SetBufferContents 
-->


