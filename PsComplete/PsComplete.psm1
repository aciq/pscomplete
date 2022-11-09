# immediately chain into the next argument if its a switch
# or stop if input is expected
using namespace System.Management.Automation
function HandleReplacementArgChain($replacement) {

    if ($replacement.ResultType -eq 'ProviderContainer') {
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
    $variable | ConvertTo-Json -Depth 5 > $env:HOME/Desktop/sample.json
}

## Command Helpers
function CommandGetType {
    [OutputType([System.Management.Automation.CommandTypes])]
    Param ([CommandInfo]$commandinfo)   
    $commandinfo 
    | Select-Object -First 1 -ExpandProperty CommandType
}
function GetPositionParameters {
    [OutputType([CommandParameterInfo])]
    Param ($parameters)   
    $parameters
    | Where-Object -Property Position -NE ([Int32]::MinValue)
    | Sort-Object -Property Position
}

$PsCompleteSettings = @{
    ExpandPositionParameters = $true
}




function HandleCompletionCommand($commandname) {
    
    $command = Get-Command $commandname
    [System.Management.Automation.CommandTypes] $commandType = CommandGetType $command
                    
    switch ($commandType) {
        ([System.Management.Automation.CommandTypes]::Application) { 
            ## e.g. apt, cmd, /bin/sh
        }
        ([System.Management.Automation.CommandTypes]::Cmdlet) {
            $params = $command.ParameterSets[0].Parameters
            $posParameters = GetPositionParameters($params)
            writeDebug $posParameters.Count
                            
            if ($posParameters.Length -gt 0) {
                $p1 = $posParameters[0]
                [System.Type] $p1Type = $p1.ParameterType
                [Microsoft.PowerShell.PSConsoleReadLine]::Insert(' ')
                if ($PsCompleteSettings.ExpandPositionParameters) {
                    switch ($p1Type.FullName) {
                                        ("System.String[]") { 
                            [Microsoft.PowerShell.PSConsoleReadLine]::Insert("''")
                            $cmdlen = "$replacement.CompletionText".Length + 1
                            [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($cmdlen)
                        }
                        Default {}
                    }
                    Invoke-GuiPsComplete
                }
            }
                            
        }
        ([System.Management.Automation.CommandTypes]::Function) {
            writeDebug "function"
        }
                        
        Default {
            writeDebug "got defualt"
        }
    }
}

function Invoke-GuiPsComplete() {
    $buffer = ''
    $cursorPosition = 0
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$buffer, [ref]$cursorPosition)
    if ($buffer -eq '') { return }
    $completion = TabExpansion2 $buffer $cursorPosition 
    
    if ($completion.CompletionMatches.Count -eq 0) {
        return
    }

    $replacement = 
    Invoke-PsComplete `
        -Content $completion.CompletionMatches `
        -CommandParameter "$buffer" `
         

    # debug
    # writeDebug @{r=$replacement; r2=$completion}
    # Write-Warning "`n`n$replacement.ResultType"
    
    if ($replacement) {
        switch ($replacement.ExitKey) {
            Tab {
                [Microsoft.PowerShell.PSConsoleReadLine]::Replace($completion.ReplacementIndex, $completion.ReplacementLength, $replacement.CompletionText)

                if ($replacement.ResultType -eq 'Command') {
                    HandleCompletionCommand $replacement.CompletionText
                }
                elseif ($replacement.ResultType -eq 'ParameterName') {
                    HandleReplacementArgChain $replacement
                }
                elseif ($replacement.ResultType -eq 'ProviderContainer') {
                    if ([System.Environment]::OSVersion.Platform -eq 'Unix') {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('/');
                    }
                    else {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('\');
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
                    HandleCompletionCommand $replacement.CompletionText
                }
                elseif ($replacement.ResultType -eq 'ProviderContainer') {
                    if ([System.Environment]::OSVersion.Platform -eq 'Unix') {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('/');
                    }
                    else {
                        [Microsoft.PowerShell.PSConsoleReadLine]::Insert('\');
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
    | ForEach-Object { $_.GetName().Name };
   
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

Install-PsComplete

# Import-Module '/home/ian/f/publicrepos/aciq.pscomplete/src/bin/Debug/net6.0/aciq.pscomplete.dll' -DisableNameChecking
# Import-Module '/home/ian/f/publicrepos/aciq.pscomplete/src/bin/Release/net6.0/aciq.pscomplete.dll' -DisableNameChecking

# Set-PSReadLineKeyHandler -Chord 'Tab' -ScriptBlock { Invoke-GuiPsComplete }
# Set-PSReadLineKeyHandler -Chord 'Ctrl+q' -ScriptBlock { Invoke-GuiPsComplete }
