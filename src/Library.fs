module aciq.pscomplete.Library

open System.Management.Automation
open Helpers
open System.Management.Automation.Host
open System
open System.Text.RegularExpressions
open System.Linq

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
    member val CommandString = "" with get,set
    [<Parameter>]
    member val CommandCursorPosition = 0 with get,set
    
    member val DefaultBgColor = Unchecked.defaultof<ConsoleColor> with get,set
    member val FilterText = "" with get,set
    member val CleanBufferConfig = bool with get,set
    member val FrameH = 0 with get,set
    member val FrameW = 0 with get,set
    member val FrameTop = Unchecked.defaultof<Coordinates> with get,set
    member val Index = 0 with get,set
    member val ScrollY = 0 with get,set
    member val Buffer = Unchecked.defaultof<BufferCell [,]> with get,set
    member val FilteredContent : CompletionResult[] = [||] with get,set
    member val VisibleContent : CompletionResult[] = [||] with get,set

    member x.SetCursorPos (ui:PSHostRawUserInterface) yroot =
        match Platform with 
        | Win -> 
            // the y coord is -1 on win for some reason
            ui.CursorPosition <- Coordinates(X=1,Y=yroot+2+x.Index - 1) 
        | Unix -> 
            ui.CursorPosition <- Coordinates(X=0,Y=yroot+2+x.Index)
        
            

    member x.WriteBufferLine (y:int) (buffer:BufferCell[,]) (current:string) =
        let xmax = buffer.GetLength(1) - 1
        let current = current + "                    "
        for i = 0 to min (current.Length - 1) xmax do
            buffer[y,i].Character <- current[i]
          


    member x.ColorBlockInside (ui:PSHostRawUserInterface) yroot (block: BufferCell[,]) =
        let xstart = 1
        let ystart = 1
        let longest = x.LongestCompleteText()
        ui.BackgroundColor <- ConsoleColor.Black
        let colorArray = block[xstart..x.VisibleContent.Length,ystart..longest.Length]
        colorArray |> Array2D.iteri (fun x y f -> colorArray[x,y].BackgroundColor <- ConsoleColor.Black )
        for i = 0 to x.VisibleContent.Length do
            ui.SetBufferContents(Coordinates(xstart,yroot+i+ystart),colorArray[i..i,0..longest.Length+10])
        ()
        
    member x.MoveAndRender (ui:PSHostRawUserInterface) (coords:Coordinates) (start:BufferCell [,]) adjustY = 
        x.ClearScreen(start)
        x.FilterContent()
        x.UpdateIndex adjustY
        match x.VisibleContent.Length with 
        | 0 -> 
            x.DrawEmptyFilter ui start
            ui.SetBufferContents(coords,start)
        | _ -> 

        x.DrawQueryBox ui start
        let widest : string = x.LongestCompleteText()
        let innerBox : BufferCell[,] = start[0..widest.Length,*]
        // let seltext = x.VisibleContent[x.Index].ListItemText
        let seltext = x.CompleteTexts()[x.Index]
        x.SetCursorPos ui coords.Y 
        match Environment.OSVersion.Platform with 
        | PlatformID.Win32NT -> 
            // black does not exist in win32, black just means default color
            // innerBox[1..x.VisibleContent.Length,1..widest.Length]
            // |> Array2D.iteri (fun y x f -> 
            //     innerBox[y+1,x+1].BackgroundColor 
            //         <- ConsoleColor.Black
            // )
            // color selected cells
            for i = 1 to seltext.Length do
                innerBox[x.Index + 1, i].BackgroundColor 
                    <- ConsoleColor.Blue
                innerBox[x.Index + 1, i].ForegroundColor 
                    <- ConsoleColor.Black
            ui.SetBufferContents(coords,innerBox)
        | _ -> 
            ui.SetBufferContents(coords,innerBox)
            x.ColorSelectedLine ui coords.Y 
        
        

        
    member x.DrawEmptyFilter (_:PSHostRawUserInterface) (buffer:BufferCell[,])  = 
        let top = Graphics.boxTop 5 $"{x.CommandString}[{x.FilterText}]" 
        x.WriteBufferLine 0 buffer top


    member x.ColorSelectedLine (ui:PSHostRawUserInterface) yroot =
        ui.BackgroundColor <- ConsoleColor.Blue
        let len = min 25 x.FilteredContent[x.Index].ListItemText.Length
        let newarr = ui.NewBufferCellArray( Size(Width=len,Height=1),bufferCell ' ')
        let txtcontent = $"{x.CompleteTexts()[x.Index]}"
        x.WriteBufferLine 0 newarr txtcontent
        ui.SetBufferContents(Coordinates(1,yroot+1+x.Index),newarr)
        ui.BackgroundColor <- ConsoleColor.Black
       

    member x.UpdateIndex (adjust:int) = 
        match x.Index + adjust with 
        | -1 -> 
            if x.ScrollY > 0 then
                x.ScrollY <- x.ScrollY - 1
                x.FilterContent()
        | n when n >= x.VisibleContent.Length -> 
            if n + x.ScrollY < x.Content.Count then
                x.ScrollY <- x.ScrollY + 1
                x.FilterContent()
        | _ -> x.Index <- (x.Index + adjust)


    member x.DrawQueryBox (_:PSHostRawUserInterface) (buffer:BufferCell[,])  = 
        let comptexts = x.CompleteTexts()
        let longest : string = x.LongestCompleteText()
        let filtertext =
            Graphics.boxTop longest.Length $"{x.CommandString}[{x.FilterText}]" 
        x.WriteBufferLine 0 buffer filtertext
        for y = 0 to x.VisibleContent.Length - 1 do
            Graphics.boxCenter longest.Length comptexts[y] 
            |> x.WriteBufferLine (1+y) buffer 


        let bottomcontent = $"{x.Index + x.ScrollY + 1} of {x.FilteredContent.Length}"
        let filtertext = Graphics.boxBottom longest.Length bottomcontent
        x.WriteBufferLine (x.VisibleContent.Length+1) buffer filtertext
        
    member x.LongestCompleteText() : string =
        if x.CompleteTexts().Length = 0 then "" else
        x.CompleteTexts()
        |> Array.maxBy (fun (f:string) -> f.Length)
        |> (fun f -> f.PadRight(f.Length + 3,' '))
    member x.CompleteTexts() : string[] =
        [|
            for t in x.VisibleContent do
                let tt = 
                    t.ToolTip
                    |> (fun f -> f.Replace("[]"," array"))
                    |> (fun f -> 
                        
                        if f.StartsWith "[" then ": " + f[..f.IndexOf("]")] 
                        elif f.StartsWith "\n" then 
                            Regex.Matches(f,"\[-").Count
                            |> (fun f -> $": %i{f}" )
                        else ""
                    )
                let linecontent = $"{t.ListItemText} {tt}" 
                yield linecontent
        |]
        

    member x.FilterContent() = 
        x.Content
        |> Seq.where (fun f ->
            let regexfilter =
                let cmd = x.CommandString.Split(" ").Last()
                if cmd = "" then $".*{x.FilterText}" else
                match cmd.First(), cmd.Last() with
                | '$', _ -> $".*{x.FilterText}"
                // folders
                | _, '\\' | _, '/' -> $"{x.FilterText}"
                | _ -> 
                    let start = cmd.TrimStart([|'-';'.';'['|]) |> Regex.Escape
                    $"^{start}.*{x.FilterText}"
            
            try Regex.IsMatch(
                f.ListItemText,
                regexfilter,
                RegexOptions.IgnoreCase)
            with e-> false
        )
        |> (fun f -> if Seq.isEmpty f then [||] else Seq.toArray f)
        |> (fun f ->
            x.FilteredContent <- f
            x.VisibleContent <-
                if f.Length = 0 then f
                else 
                    f |> Seq.skip x.ScrollY
                    |> Seq.truncate (x.FrameH - 3)
                    // |> Seq.truncate (x.FrameH - 3)
                    |> Seq.toArray
        )
        

    member x.ClearScreen(buffer:BufferCell[,]) =
        // x.Host.UI.RawUI.BackgroundColor <- -1 |> enum<ConsoleColor>
        x.Host.UI.RawUI.BackgroundColor <- x.DefaultBgColor
        buffer 
        |> Array2D.iteri (fun x y _ -> buffer[x,y].Character <- ' ')
        x.Host.UI.RawUI.SetBufferContents(x.FrameTop,buffer)

    override this.BeginProcessing() = this.WriteVerbose "Begin!"

    
    member this.ExitWithWarning(message: string) =
        this.WriteWarning("\n"+message)
    override this.ProcessRecord() =
        try 
            this.DefaultBgColor <- this.Host.UI.RawUI.BackgroundColor
            this.FilterContent()
            if this.Content.Count = 0 then ()
            if this.Content.Count = 1 then 
                this.WriteObject  
                    {
                        CompletionText = this.Content[0].CompletionText
                        ArgumentType = "" // argument type was never queried
                        ResultType = this.Content[0].ResultType
                        ExitKey = ExitKey.None
                    }
            else
            let ui = this.Host.UI.RawUI
            this.FrameTop <- Coordinates(0,ui.CursorPosition.Y + 1 - ui.WindowPosition.Y)
            this.FrameH <- ui.WindowSize.Height - ui.CursorPosition.Y - 1 
            this.FrameW <- ui.WindowSize.Width
            
            if this.FrameH < 1 || this.FrameW < 1 then 
                this.ExitWithWarning("Window too small to draw completion list, please clear the buffer")
            else
            
            let init() =
                this.Buffer <- ui.NewBufferCellArray(Size(this.FrameW,this.FrameH), bufferCell ' ')
                this.FilterContent()
                this.DrawQueryBox ui this.Buffer
                ui.SetBufferContents(Coordinates(0,ui.CursorPosition.Y + 1 - ui.WindowPosition.Y),this.Buffer) 
                
            init()
            
                
            let readkeyopts = 
                ReadKeyOptions.NoEcho 
                // ||| ReadKeyOptions.AllowCtrlC
                ||| ReadKeyOptions.IncludeKeyDown

            let movepos (by:int) = this.MoveAndRender ui this.FrameTop this.Buffer by    
            movepos 0      
            
            let getCompletionAndExit exitKey =
                try 
                    if this.VisibleContent.Length <= this.Index then ()
                    this.ClearScreen(this.Buffer) 
                    {
                        CompletionText = this.VisibleContent[this.Index].CompletionText
                        ArgumentType = this.CompleteTexts()[this.Index] |> PsArgument.getText
                        ResultType = this.VisibleContent[this.Index].ResultType
                        ExitKey = exitKey
                    }
                    |> this.WriteObject
                with e -> ()

            let rec loop() = 
                let c = ui.ReadKey(options=readkeyopts)
                match c.VirtualKeyCode |> enum<ConsoleKey> with 
                | ConsoleKey.Tab -> getCompletionAndExit ExitKey.Tab 
                | ConsoleKey.Enter -> getCompletionAndExit ExitKey.Enter
                | ConsoleKey.Escape -> getCompletionAndExit ExitKey.Escape                      
                | ConsoleKey.LeftArrow -> loop()                      
                | ConsoleKey.RightArrow -> loop()                      
                | ConsoleKey.UpArrow -> movepos -1; loop()
                | ConsoleKey.DownArrow -> movepos +1 ; loop()
                // | ConsoleKey.OemPeriod -> getCompletionAndExit ExitKey.Period 
                // | ConsoleKey.Oem2 -> getCompletionAndExit ExitKey.Slash // forward-slash 
                // | ConsoleKey.Oem5 -> getCompletionAndExit ExitKey.Backslash // backslash 
                | ConsoleKey.Backspace -> 
                    this.Index <- 0
                    this.ScrollY <- 0
                    this.FilterText <- this.FilterText[..this.FilterText.Length - 2]
                    movepos 0 
                    loop()
                | _ -> 
                match c.Character with 
                | c -> 
                    // clearBuffer()
                    this.FilterText <- $"{this.FilterText}%c{c}"
                    this.Index <- 0
                    this.ScrollY <- 0
                    movepos 0 
                    loop ()

            loop()

        with e -> 
            {
                CompletionText = e.Message+"\n"+e.StackTrace
                ArgumentType = ""
                ExitKey = ExitKey.None
                ResultType = CompletionResultType.Text
            }
            |> this.WriteObject
            
        

    override this.EndProcessing() = this.WriteVerbose "End!"
