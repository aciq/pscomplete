module aciq.pscomplete.Helpers

open System
open System.Management.Automation.Host


module Chars =
    let [<Literal>] topLeftDouble = '╔'
    let [<Literal>] verticalDouble = '║'
    let [<Literal>] horizontalDouble = '═'
    let [<Literal>] topRightDouble = '╗'
    let [<Literal>] bottomLeftDouble = '╚'
    let [<Literal>] bottomRightDouble = '╝'
    
    // let hexadecimal = $"\u255d"
    // let hexadecimal = $"\u255a"
    // let hexadecimal = $"\u2557"
    // let hexadecimal = $"\u2554"
    // let hexadecimal = $"\u2551"
    // let hexadecimal = $"\u2550"
    
module Graphics =
    let boxTop length content =
        $"╔══ {content} ".PadRight(length+1,'═') + "╗"

    
    let boxCenter length content = 
        $"║{content}".PadRight(length+1,' ') + "║"
        
    
    let boxBottom length content =
        $"╚══ {content} ".PadRight(length+1,'═') + "╝"
        
        
module PsArgument =
    let getText (v:string) = 
        let argtype = v
        let startidx = argtype.IndexOf(": [")
        if startidx = -1 then "" else
        argtype.Substring(startidx + ": [".Length) |> (fun f -> f[..f.Length-2])
        
        
let bufferCell char = BufferCell(
    Character = char,
    BufferCellType = BufferCellType.Complete,
    ForegroundColor = ConsoleColor.White
    // BackgroundColor = ConsoleColor.Black
)

let winunix ifWin ifUnix = 
    match Environment.OSVersion.Platform with 
    | PlatformID.Win32NT -> ifWin
    | _ -> ifUnix



type PlatformKind = | Win | Unix 
let Platform = 
    match Environment.OSVersion.Platform with 
    | PlatformID.Win32NT -> Win
    | _ -> Unix
    
    