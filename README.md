# PsComplete
cross-platform powershell autocomplete

- Rewritten from scratch to make it compatible with mac/linux

- This is the only powershell tab-expansion compatible with all platforms, as all the other ones use windows-only functions to access the buffer array.

## demo  

![](_resources/pscompletewin.gif)

## installation

- `Install-Module -Name PsComplete`
- `Install-PsComplete`
- After that Tab is bound to the expander for this session

optionally add Install-PsComplete to profile (`Invoke-Item $PROFILE`) to have it always on


## features

- Search with regex (`^<start>.*<filter>`)
- Performant up to tens of thousands of completions
- Press Tab again to immediately select the next parameter (useful for switches)
- Press Enter to finish command

## caveats

Since powershell core hasn't implemented some functions on linux/mac and the windows version is not too similar to the linux one internally, this introduces some caveats which i will not solve.

namely:
- Only works when there is enough free space under the current command (use clear)
- Only continuous blocks can be colored with reasonable performance, so i did not use many coloring effects

<!-- 
- the color Black does not exist in windows, it's the background color. however it does exist on linux
- blank color (-1) only exists on linux, throws an exception on windows
- the coordinate systems of linux pwsh and windows are different (windows coordinates are -1)
- there is no way to access the buffer on linux, but it can be overridden with a new array which is destructive to previous screen contents
- there is no way to fill a rectangle on linux using SetBufferContents 
-->


