module aciq.pscomplete.Library

open System.Management.Automation
open System.Text.Json
open Helpers
open System.Management.Automation.Host
open System
open System.Text.RegularExpressions
open System.Linq
open aciq.pscomplete.Helpers

type ExitKey = | None = 0 | Tab = 1 | Enter = 2 | Escape = 3

[<CLIMutable>]
type CompleteOutput = 
    {
        ArgumentType : string
        CompletionText : string
        ResultType : CompletionResultType
        ExitKey : ExitKey
    }

[<OutputType(typeof<CompleteOutput>)>]
[<Cmdlet(VerbsLifecycle.Invoke, "PsComplete")>]
type ConfCmdlet() =
    inherit PSCmdlet()

    [<Parameter(Position = 0, ValueFromPipelineByPropertyName = true)>]
    member val Content = ResizeArray<CompletionResult>() with get,set
    [<Parameter>]
    member val CommandParameter = "" with get,set
    [<Parameter>]
    member val CommandCursorPosition = 0 with get,set
    //
    member val FilterText = "" with get,set
    member val CleanBufferConfig = bool with get,set
    member val FrameH = 0 with get,set
    member val FrameW = 0 with get,set
    member val FrameTop = Unchecked.defaultof<Coordinates> with get,set
    member val Buffer = Unchecked.defaultof<BufferCell [,]> with get,set


    member x.WriteBufferLine (y:int) (buffer:BufferCell[,]) (current:string) =
        let xmax = buffer.GetLength(1) - 1
        let current = current + "                    " // to clear previous text cells
        for i = 0 to min (current.Length - 1) xmax do
            buffer[y,i].Character <- current[i]
          
        
    member x.DrawEmptyFilter (_:PSHostRawUserInterface) (buffer:BufferCell[,])  = 
        let top = Graphics.boxTop 5 $"{x.CommandParameter}[{x.FilterText}]" 
        x.WriteBufferLine 0 buffer top


    member x.RenderState (state:DisplayState)   =
        x.ClearScreen x.Buffer
        let pageLength = x.FrameH - 2 // frames
        let filtered = state |> DisplayState.filteredContent
        
        let pageIndex = state.SelectedIndex / pageLength
        let pageSelIndex = state.SelectedIndex % pageLength
        
        let currPage =
            filtered
            |> Seq.skip (pageIndex * pageLength)
            |> Seq.truncate pageLength
            |> Seq.toList
        
        let completions = currPage |> List.map PsCompletion.toText
        let longest : string = completions |> List.maxBy (fun f -> f.Length)
        let currSelectedText = completions[pageSelIndex]
        
        let topLine = Graphics.boxTop longest.Length $"{x.CommandParameter}[{state.FilterText}]"
        let bottomLine = Graphics.boxBottom longest.Length $"{state.SelectedIndex+1} of {filtered.Length}"
        let content =
            [|
                yield topLine
                for n in completions do
                    yield Graphics.boxCenter longest.Length n
                yield bottomLine
            |]
        content
        |> Array.iteri (fun i f ->
            x.WriteBufferLine (i) x.Buffer f
        )
        x.Host.UI.RawUI.SetBufferContents(x.FrameTop ,x.Buffer)
        
        // color selected line
        
        match Platform with
        | Unix ->  
            x.Host.UI.RawUI.BackgroundColor <- ConsoleColor.Blue
            let newarr = x.Host.UI.RawUI.NewBufferCellArray( Size(Width=currSelectedText.Length,Height=1),bufferCell ' ')
            let txtcontent = $"{currSelectedText}"
            x.WriteBufferLine 0 newarr txtcontent
            x.Host.UI.RawUI.SetBufferContents(Coordinates(1,x.FrameTop.Y+1+pageSelIndex),newarr)
            x.Host.UI.RawUI.BackgroundColor <- ConsoleColor.Black
        | Win ->
            let selectedLine = x.FrameTop.Y+1+pageSelIndex
            let linebuffer =
                x.Host.UI.RawUI.GetBufferContents(
                    Rectangle(
                        left=0,
                        top=selectedLine,
                        right = currSelectedText.Length - 1,
                        bottom=selectedLine
                        )
                    )
            linebuffer    
            |> Array2D.iteri (fun y x f ->
                linebuffer[y,x].BackgroundColor <- ConsoleColor.Blue
            )
            x.Host.UI.RawUI.SetBufferContents(Coordinates(0,selectedLine),linebuffer)

    member x.ClearScreen(buffer:BufferCell[,]) =
        let defaultColor =
            match Platform with 
            | Win -> 0 |> enum<ConsoleColor>
            | Unix ->  -1 |> enum<ConsoleColor>
        x.Host.UI.RawUI.BackgroundColor <- defaultColor
        buffer 
        |> Array2D.iteri (fun x y _ -> buffer[x,y].Character <- ' ')
        x.Host.UI.RawUI.SetBufferContents(x.FrameTop,buffer)

    override this.BeginProcessing() =
        let ui = this.Host.UI.RawUI
        this.FrameTop <- Coordinates(0,ui.CursorPosition.Y + 1 - ui.WindowPosition.Y)
        this.FrameH <- ui.WindowSize.Height - ui.CursorPosition.Y - 1
        this.FrameW <- ui.WindowSize.Width
        //
        this.Buffer <- ui.NewBufferCellArray(Size(this.FrameW,this.FrameH), bufferCell ' ')

    member this.ExitWithWarning(message: string) =
        this.WriteWarning("\n"+message)
        
    member this.ShouldExitEarly() =
        if this.FrameH < 3 || this.FrameW < 1 then 
            this.ExitWithWarning("Window too small to draw completion list, please clear the buffer")
            true
        elif this.Content.Count = 0 then true
        elif this.Content.Count = 1 then
            this.WriteObject  
                {
                    CompletionText = this.Content[0].CompletionText
                    ArgumentType = "" // argument type was never queried
                    ResultType = this.Content[0].ResultType
                    ExitKey = ExitKey.None
                }
            true
        else
        false
        
    member this.GetCompletionAndExit (state:DisplayState) (exitKey:ExitKey) =
        this.ClearScreen this.Buffer
        let filtered = state |> DisplayState.filteredContent
        let completion = filtered[state.SelectedIndex]
        {
            CompletionText = completion.CompletionText
            ArgumentType = completion |> PsCompletion.toText |> PsArgument.getText
            ResultType = completion.ResultType
            ExitKey = exitKey
        }
        |> this.WriteObject
    override this.ProcessRecord() =
        try 
            if this.ShouldExitEarly() then
                this.ExitWithWarning("\n\nExited Early")
            else
            let ui = this.Host.UI.RawUI
            let rec loop (state : DisplayState) =
                this.RenderState state
                let c = ui.ReadKey(options=readkeyopts)
                match c.VirtualKeyCode |> enum<ConsoleKey> with 
                | ConsoleKey.Tab -> this.GetCompletionAndExit state ExitKey.Tab 
                | ConsoleKey.Enter -> this.GetCompletionAndExit state ExitKey.Enter
                | ConsoleKey.Escape -> this.GetCompletionAndExit state ExitKey.Escape                      
                | ConsoleKey.LeftArrow -> loop state                      
                | ConsoleKey.RightArrow -> loop state                   
                | ConsoleKey.UpArrow -> loop (DisplayState.withArrowUp state)
                | ConsoleKey.DownArrow -> loop (DisplayState.withArrowDown state)
                // | ConsoleKey.OemPeriod -> getCompletionAndExit ExitKey.Period 
                // | ConsoleKey.Oem2 -> getCompletionAndExit ExitKey.Slash // forward-slash 
                // | ConsoleKey.Oem5 -> getCompletionAndExit ExitKey.Backslash // backslash 
                | ConsoleKey.Backspace -> loop (DisplayState.withBackspace state)
                | keycode ->
                match int keycode with
                // shift ctrl alt
                | 16 | 17 | 18 -> ()
                | _ -> loop (DisplayState.withFilterChar c.Character state)

            
            let initState =
                {
                    CommandString = this.CommandParameter
                    FilterText = ""
                    SelectedIndex = 0
                    Content = this.Content |> Seq.toList
                }
            
            loop initState
            

        with e -> 
            {
                CompletionText = e.Message+"\n"+e.StackTrace
                ArgumentType = ""
                ExitKey = ExitKey.None
                ResultType = CompletionResultType.Text
            }
            |> this.WriteObject
        ()
            
    override this.EndProcessing() = ()
