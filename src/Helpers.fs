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
        Content: CompletionResult list
    }

module DisplayState =

    let filteredContent (state: DisplayState) =
        let regexfilter =
            let cmd = state.CommandString.Split(" ").Last()

            if cmd = "" then
                $".*{state.FilterText}"
            else
                match cmd.First(), cmd.Last() with
                // folders
                | _, '\\'
                | _, '/' -> $"{state.FilterText}"
                | _ ->
                    let start =
                        cmd.TrimStart([| '-'; '.'; '[' |]) |> Regex.Escape

                    $"^{start}.*{state.FilterText}"

        state.Content
        |> List.where (fun f -> Regex.IsMatch(f.ListItemText, regexfilter, RegexOptions.IgnoreCase))

    let withArrowDown state =
        let filtered = state |> filteredContent

        match state.SelectedIndex = filtered.Length - 1 with
        | true -> state
        | false ->
            { state with
                SelectedIndex = state.SelectedIndex + 1
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


//let saveDebugState state =
//    state |> JsonSerializer.Serialize
//    |> (fun f -> System.IO.File.WriteAllText(@"C:\Users\kast\dev\s.json",f))
