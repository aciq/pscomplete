module aciq.pscomplete.Helpers

open System
open System.Management.Automation
open System.Management.Automation.Host
open System.Text.Json
open System.Linq
open System.Text.RegularExpressions

module Chars =
    [<Literal>]
    let topLeftDouble = '╔'

    [<Literal>]
    let verticalDouble = '║'

    [<Literal>]
    let horizontalDouble = '═'

    [<Literal>]
    let topRightDouble = '╗'

    [<Literal>]
    let bottomLeftDouble = '╚'

    [<Literal>]
    let bottomRightDouble = '╝'


module Graphics =
    let boxTop length content = $"╔══ {content} ".PadRight(length + 1, '═') + "╗"


    let boxCenter length content = $"║{content}".PadRight(length + 1, ' ') + "║"


    let boxBottom length content = $"╚══ {content} ".PadRight(length + 1, '═') + "╝"


module PsArgument =
    let getText (v: string) =
        let argtype = v
        let startidx = argtype.IndexOf(": [")

        if startidx = -1 then
            ""
        else
            argtype.Substring(startidx + ": [".Length)
            |> (fun f -> f[.. f.Length - 2])


let bufferCell char =
    BufferCell(Character = char, BufferCellType = BufferCellType.Complete, ForegroundColor = ConsoleColor.White)


type PlatformKind =
    | Win
    | Unix

let Platform =
    match Environment.OSVersion.Platform with
    | PlatformID.Win32NT -> Win
    | _ -> Unix



type DisplayState =
    {
        CommandString: string
        FilterText: string
        SelectedIndex: int
        Content: CompletionResult ResizeArray
        FilteredCache: CompletionResult ResizeArray
        PageLength: int
    }

module DisplayState =

    let regexfilter (state: DisplayState): string =
        let cmd = state.CommandString.Split(" ").Last()
        if cmd = "" then
            $".*{state.FilterText}"
        else
            match cmd.First(), cmd.Last() with
            // folders
            | _, '\\'
            | _, '/' -> $"{state.FilterText}"
            | '$',_ | '.',_ | '\'',_ | '"',_ | '~',_ -> $".*{state.FilterText}"
            | _ ->
                let start =
                    cmd.TrimStart([| '-'; '.'; '[' |]) |> Regex.Escape
                $"^{start}.*{state.FilterText}"

    let filterExistingInPlace (state: DisplayState) =
        let filter = regexfilter state
        let temp = state.FilteredCache.ToArray()
        state.FilteredCache.Clear()
        temp
        |> Seq.where (fun f -> Regex.IsMatch(f.ListItemText, filter, RegexOptions.IgnoreCase))
        |> state.FilteredCache.AddRange
        state
    let filterInPlace (state: DisplayState) =
        let filter = regexfilter state
        state.FilteredCache.Clear()
        state.Content
        |> Seq.where (fun f -> Regex.IsMatch(f.ListItemText, filter, RegexOptions.IgnoreCase))
        |> state.FilteredCache.AddRange
        state
        
        
    let withArrowRight (state:DisplayState) =
        let state' =
            match state.FilterText with
            | "" -> state
            | _ -> state |> filterExistingInPlace

        match state.SelectedIndex + state.PageLength >= state'.FilteredCache.Count with
        | true -> state
        | false ->
            { state with
                SelectedIndex = state.SelectedIndex + state.PageLength
            }
    let withArrowDown (state:DisplayState) =
        let state' =
            match state.FilterText with
            | "" -> state
            | _ -> state |> filterExistingInPlace

        match state.SelectedIndex = state'.FilteredCache.Count - 1 with
        | true -> state
        | false ->
            { state with
                SelectedIndex = state.SelectedIndex + 1
            }
    let withArrowLeft state =
        match state.SelectedIndex - state.PageLength < 0 with
        | true -> state
        | false ->
            { state with
                SelectedIndex = state.SelectedIndex - state.PageLength
            }
    let withArrowUp state =
        match state.SelectedIndex with
        | 0 -> state
        | n ->
            { state with
                SelectedIndex = state.SelectedIndex - 1
            }

    let withBackspace state =
        { state with
            SelectedIndex = 0
            FilterText = state.FilterText[.. state.FilterText.Length - 2]
        }

    let withFilterChar c state =
        { state with
            SelectedIndex = 0
            FilterText = $"{state.FilterText}%c{c}"
        }


type PsCompletion() =

    /// tooltips enclosed in [] are displayed 
    /// (usually contains argument type e.g [string array])
    static member toText(res: CompletionResult) =
        let typeinfo =
            res.ToolTip
            |> (fun f -> f.Replace("[]", " array"))
            |> (fun f ->
                if f.StartsWith "[" then
                    ": " + f[.. f.IndexOf("]")]
                elif f.StartsWith "\n" then
                    Regex.Matches(f, "\[-").Count
                    |> (fun f -> $": %i{f}")
                else
                    "")

        $"{res.ListItemText} {typeinfo}"

let readkeyopts =
    ReadKeyOptions.NoEcho
    ||| ReadKeyOptions.AllowCtrlC
    ||| ReadKeyOptions.IncludeKeyDown


// let saveDebugState state =
//    state |> JsonSerializer.Serialize
//    |> (fun f -> System.IO.File.WriteAllText(@"C:\Users\kast\dev\s.json",f))
