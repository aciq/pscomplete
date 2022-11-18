namespace aciq.pscomplete

open System.Management.Automation
open System.Linq
open System.Runtime.CompilerServices
open System.Text.RegularExpressions

type DisplayState =
    {
        CommandString: string
        mutable FilterText: string
        mutable SelectedIndex: int
        Content: CompletionResult ResizeArray
        FilteredCache: CompletionResult ResizeArray
        PageLength: int
    }
    with
        // [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        // member this.RegexFilter() =
        //     let cmd = this.CommandString.Split(" ").Last()
        //     match cmd with
        //     | "" -> $".*{this.FilterText}"
        //     // folders
        //     | _ when cmd.EndsWith "\\" -> $".*{this.FilterText}"
        //     | _ when cmd.EndsWith "/" -> $".*{this.FilterText}" 
        //     // various start symbols that break search
        //     | _ when
        //         let startSymbols = [|'$';'.';'\'';'"';'~'|]
        //         startSymbols |> Array.exists cmd.StartsWith
        //         -> $".*{this.FilterText}"
        //     // in other cases search with regex
        //     | _ ->
        //         let start = cmd.TrimStart([| '-'; '.'; '[' |]) |> Regex.Escape
        //         $"^{start}.*{this.FilterText}"
        
        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member this.TryGoUpBy (x:int) =
            if this.SelectedIndex - x >= 0 then
                this.SelectedIndex <- this.SelectedIndex - x
                
        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member this.TryGoDownBy (x:int) =
            if this.SelectedIndex + x < this.FilteredCache.Count then
                this.SelectedIndex <- this.SelectedIndex + x
                
        
module DisplayState =
    open System
    let filterCacheInPlace (state: DisplayState) =
        let temp = state.FilteredCache.ToArray()
        state.FilteredCache.Clear()
        temp
        |> Seq.where (fun f -> f.ListItemText.Contains(state.FilterText,StringComparison.OrdinalIgnoreCase))
        |> state.FilteredCache.AddRange
        state
    let filterInPlace (state: DisplayState) =
        state.FilteredCache.Clear()
        state.Content
        |> Seq.where (fun f -> f.ListItemText.Contains(state.FilterText,StringComparison.OrdinalIgnoreCase))
        |> state.FilteredCache.AddRange
        state
     
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let arrowRightInplace (state:DisplayState) =
        state.TryGoDownBy(state.PageLength)
        state
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let arrowDownInplace (state:DisplayState) =
        state.TryGoDownBy(1)
        state
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let arrowLeftInplace (state:DisplayState) =
        state.TryGoUpBy(state.PageLength)
        state
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let arrowUpInplace (state:DisplayState) =
        state.TryGoUpBy(1)
        state
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let backspaceInplace (state:DisplayState) =
        state.SelectedIndex <- 0
        state.FilterText <- state.FilterText[.. state.FilterText.Length - 2]
        state
        
    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let addFilterCharInplace (c:char) (state:DisplayState) =
        state.SelectedIndex <- 0
        state.FilterText <- $"%s{state.FilterText}%c{c}"
        state
        