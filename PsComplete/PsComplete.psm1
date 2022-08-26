# immediately chain into the next argument if its a switch
# or stop if input is expected
function HandleReplacementArgChain($replacement) {

    if ($replacement.ResultType -eq 'ProviderContainer'){

    }
    

    switch ($replacement.ArgumentType) {
        "psobject" {
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert('.');
            Invoke-GuiPsComplete;
        } 
        "switch" { 
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' -');
            Invoke-GuiPsComplete;
        }
        "IDictionary" { 
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' @{ "" = "" }');
            [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($cursorPosition + $replacement.CompletionText.Length + 4);
        }
        "string array" { 
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' ');
        }
        { @("CommandTypes", "ActionPreference") -contains $_ } {
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' ');
            Invoke-GuiPsComplete;
        }
        Default {
            [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' ');
        }
    }
}

function writeDebug($variable) {
    $variable | ConvertTo-Json -Depth 5 > $env:USERPROFILE/sample.json
}

function Invoke-GuiPsComplete() {
    $buffer = ''
    $cursorPosition = 0
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$buffer, [ref]$cursorPosition)
    $completion = TabExpansion2 $buffer $cursorPosition 
    
    if ($completion.CompletionMatches.Count -eq 0) {
        return
    }

    $replacement = 
    Invoke-PsComplete `
        -Content $completion.CompletionMatches `
        -CommandParameter "$buffer" `
        -CommandCursorPosition $cursorPosition 

    # debug
    # @{r=$replacement; r2=$completion} | ConvertTo-Json -Depth 5 > sample.json
    # Write-Warning "`n`n$replacement.ResultType"
    
    if ($replacement) {
        
        switch ($replacement.ExitKey) {
            Tab {
                [Microsoft.PowerShell.PSConsoleReadLine]::Replace($completion.ReplacementIndex, $completion.ReplacementLength, $replacement.CompletionText)
                
                if ($replacement.ResultType -eq 'Command') {
                    [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' -');
                    Invoke-GuiPsComplete;
                }
                elseif ($replacement.ResultType -eq 'ParameterName') {
                    HandleReplacementArgChain $replacement
                }
                elseif ($replacement.ResultType -eq 'ProviderContainer') {
                    if ([System.Environment]::OSVersion.Platform -eq 'Unix') {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('/');
                        Invoke-GuiPsComplete;
                    }
                    else {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('\');
                        Invoke-GuiPsComplete;
                    }
                }
            }
            Enter {
                [Microsoft.PowerShell.PSConsoleReadLine]::Replace($completion.ReplacementIndex, $completion.ReplacementLength, $replacement.CompletionText)
            }
            Escape {
                [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($cursorPosition);
            }
            ## if there is a single option
            None {
                [Microsoft.PowerShell.PSConsoleReadLine]::Replace($completion.ReplacementIndex, $completion.ReplacementLength, $replacement.CompletionText)
                if ($replacement.ResultType -eq 'Command') {
                    [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' -');
                    Invoke-GuiPsComplete;
                }
                elseif ($replacement.ResultType -eq 'ProviderContainer') {
                    if ([System.Environment]::OSVersion.Platform -eq 'Unix') {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('/');
                        Invoke-GuiPsComplete;
                    }
                    else {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('\');
                        Invoke-GuiPsComplete;
                    }
                }
            }
        }
    }
}


function Install-PsComplete() {
    $loadedAssemblies = `
        [System.AppDomain]::CurrentDomain.GetAssemblies() `
        | Where-Object Location `
        | ForEach-Object {$_.GetName().Name};
   
    if (!($loadedAssemblies.Contains('FSharp.Core'))) {
        Import-Module "$PSScriptRoot/FSharp.Core.dll"    
    }
    if (!($loadedAssemblies.Contains('aciq.pscomplete'))) {
        Import-Module "$PSScriptRoot/aciq.pscomplete.dll"   
    }

    Set-PSReadLineKeyHandler -Chord 'Tab' -ScriptBlock { 
        Invoke-GuiPsComplete 
    }
}

# Import-Module '/home/ian/f/publicrepos/aciq.pscomplete/src/bin/Debug/net6.0/aciq.pscomplete.dll' -DisableNameChecking
# Import-Module '/home/ian/f/publicrepos/aciq.pscomplete/src/bin/Release/net6.0/aciq.pscomplete.dll' -DisableNameChecking

# Set-PSReadLineKeyHandler -Chord 'Tab' -ScriptBlock { Invoke-GuiPsComplete }
# Set-PSReadLineKeyHandler -Chord 'Ctrl+q' -ScriptBlock { Invoke-GuiPsComplete }
