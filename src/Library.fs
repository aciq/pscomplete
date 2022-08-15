module aciq.pscomplete.Library

open System.Management.Automation
open Helpers
open System.Management.Automation.Host
open System.Reflection
open System
open System.Linq
open System.Text.RegularExpressions

type ConfTypes =
    | NemoActions = 0 
    | NemoAccels = 1


type t = ValidateEnumeratedArgumentsAttribute

let bufferCell char = BufferCell(
    Character = char,
    BufferCellType = BufferCellType.Complete
)    




        





let topLeftDouble : char = char 9556
let verticalDouble = char 9553
let horisontalDouble = char 9552
let topRightDouble = char 9559
let bottomLeftDouble = char 9562
let bottomRightDouble = char 9562


let drawquery (cmdlet:ConfCmdlet) (ui:PSHostRawUserInterface) (buffer:BufferCell[,])  = 
    let _ : ResizeArray<_> = cmdlet.Content
    let truncated : CompletionResult[] = cmdlet.FilteredContent
    if truncated.Length = 0 then ui.WindowTitle <- $"empty" else

    let comptexts = cmdlet.CompleteTexts()
    let longest : string = cmdlet.LongestCompleteText()
    let st1 = String.replicate 2 $"%c{horisontalDouble}"
    let filtertext =
        $"{topLeftDouble}{st1} [{cmdlet.FilterText}] ".PadRight(longest.Length+1,horisontalDouble) + $"{topRightDouble}"
    writeBufferLine cmdlet 0 buffer filtertext
    for y = 0 to truncated.Length - 1 do
        let tt = $"{verticalDouble}{comptexts[y]}".PadRight(longest.Length+1,' ') + $"{verticalDouble}"
        writeBufferLine cmdlet (1+y) buffer tt //2+
    match cmdlet.Content.Count, comptexts.Count() with 
    | a,b when a > b -> if (cmdlet.FrameH + cmdlet.ScrollY > a + 2) then true else false
    | a,b -> if (cmdlet.FrameH + cmdlet.ScrollY >= b) then true else false
    |> function 
        | true -> 
            let st1 = String.replicate 2 $"%c{horisontalDouble}"
            let filtertext =
                $"{bottomLeftDouble}{st1}     ".PadRight(longest.Length+1,horisontalDouble) + $"{bottomRightDouble}"
            writeBufferLine cmdlet (comptexts.Count()+1) buffer filtertext
        | false -> 
            writeBufferLine cmdlet (comptexts.Count()+1) buffer (String.replicate (longest.Length+1) " " )



let drawemptyfilter (cmdlet:ConfCmdlet) (ui:PSHostRawUserInterface) (buffer:BufferCell[,])  = 
    let st1 = String.replicate 2 $"%c{horisontalDouble}"
    let filtertext =
        $"{topLeftDouble}{st1} [{cmdlet.FilterText}] ".PadRight(5,horisontalDouble) + $"{topRightDouble}"
    writeBufferLine cmdlet 0 buffer filtertext
    

let movePosition (cmdlet:ConfCmdlet) (ui:PSHostRawUserInterface) (coords:Coordinates) (start:BufferCell [,]) (adjustY) = 
    let _ : ResizeArray<_> = cmdlet.Content
    let _ : _[] = cmdlet.FilteredContent
    cmdlet.ClearScreen(start)
    cmdlet.FilterContent()
    setInputs cmdlet (adjustY)
    ui.WindowTitle <- $"selection {cmdlet.Index+1 + cmdlet.ScrollY} of {cmdlet.Content.Count}"
    match cmdlet.FilteredContent.Length with 
    | 0 -> 
        drawemptyfilter cmdlet ui start
        ui.SetBufferContents(coords,start)
    | _ -> 
    
    drawquery cmdlet ui start
    let widest : string = cmdlet.LongestCompleteText()
    let arr2 : BufferCell[,] = start[0..widest.Length,*]
    ui.SetBufferContents(coords,arr2)
    colorBlock cmdlet ui coords.Y arr2
    setcursorPos cmdlet ui (coords.Y) 
    colorSelectedLine cmdlet ui coords.Y 

let getArgumentText (v:string) = 
    let argtype = v
    let startidx = argtype.IndexOf(": [")
    if startidx = -1 then "" else
    argtype.Substring(startidx + ": [".Length) |> (fun f -> f[..f.Length-2])

[<CLIMutable>]
type CompleteOutput = 
    {
        ArgumentType : string
        CompletionText : string
        ResultType : CompletionResultType
        Continue : bool
    }

[<OutputType(typeof<CompleteOutput>)>]
[<Cmdlet("q", "Conf")>]
type ConfCmdlet() =
    inherit PSCmdlet()

    [<Parameter(Position = 0, ValueFromPipelineByPropertyName = true)>]

    member val Content = ResizeArray<CompletionResult>() with get,set
    member val FilterText = "" with get,set
    member val CleanBufferConfig = bool with get,set
    member val FrameH = 0 with get,set
    member val FrameW = 0 with get,set
    member val FrameTop = Unchecked.defaultof<Coordinates> with get,set
    member val Index = 0 with get,set
    member val ScrollY = 0 with get,set
    member val FilteredContent = [||] with get,set


    member x.SetCursorPos (ui:PSHostRawUserInterface) yroot =
      ui.CursorPosition <- Coordinates(X=0,Y=yroot+2+x.Index)


    member x.WriteBufferLine (y:int) (buffer:BufferCell[,]) (current:string) =
        let xmax = buffer.GetLength(1) - 1
        let current = current + "                    "
        for i = 0 to min (current.Length - 1) xmax do
            buffer[y,i].Character <- current[i]

    member x.ColorBlock (ui:PSHostRawUserInterface) yroot (block: BufferCell[,]) =
        let _ : CompletionResult[] = x.FilteredContent
        let longest = x.LongestCompleteText()
        ui.BackgroundColor <- ConsoleColor.Black
        let colorArray = block[0..x.FilteredContent.Length,0..longest.Length]
        for i = 0 to x.FilteredContent.Length do
            ui.SetBufferContents(Coordinates(0,yroot+i),colorArray[i..i,0..longest.Length+10])
        ()

    member x.ColorSelectedLine (ui:PSHostRawUserInterface) yroot =
        let _ : CompletionResult[] = x.FilteredContent
        ui.BackgroundColor <- ConsoleColor.Blue
        let len = min 20 x.FilteredContent[x.Index].ListItemText.Length
        let newarr = ui.NewBufferCellArray( Size(Width=len,Height=1),bufferCell ' ')
        let txtcontent = $"{x.CompleteTexts()[x.Index]}"
        x.WriteBufferLine 0 newarr txtcontent
        ui.SetBufferContents(Coordinates(1,yroot+1+x.Index),newarr)
        ui.BackgroundColor <- ConsoleColor.Black
        ()

    member x.SetInputs (adjust:int) = 
        
        match x.Index + adjust with 
        | -1 -> 
            if x.ScrollY > 0 then
                x.ScrollY <- x.ScrollY - 1
                x.FilterContent()
        | n when n >=(x.FilteredContent.Length ) -> 
            if n + x.ScrollY < x.Content.Count then
                x.ScrollY <- x.ScrollY + 1
                x.FilterContent()
        | _ -> x.Index <- (x.Index + adjust)



    member x.LongestCompleteText() : string =
        x.CompleteTexts() |> Array.maxBy (fun (f:string) -> f.Length)
    member x.CompleteTexts() : string[] =
        [|
            for t in x.FilteredContent do
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
        |> Seq.where (fun f -> f.CompletionText.Contains(x.FilterText,StringComparison.OrdinalIgnoreCase) )
        |> Seq.toArray
        |> (fun f -> 
            if f.Length > 0 then 
                f |> Seq.skip x.ScrollY 
            else f 
        )
        |> Seq.truncate (x.FrameH - 3)
        |> Seq.toArray
        |> (fun f -> x.FilteredContent <- f)


        
    member x.ClearScreen(buffer:BufferCell[,]) =
        x.Host.UI.RawUI.BackgroundColor <- -1 |> enum<ConsoleColor>
        buffer 
        |> Array2D.iteri (fun x y f -> buffer[x,y].Character <- ' ')
        x.Host.UI.RawUI.SetBufferContents(x.FrameTop,buffer)

    override this.BeginProcessing() = this.WriteVerbose "Begin!"

    override this.ProcessRecord() =
        try 
            this.FilterContent()
            if this.Content.Count = 0 then ()
            // if one item then return and continue
            if this.Content.Count = 1 then 
                this.WriteObject  
                    {
                        CompletionText = this.Content[0].CompletionText
                        ArgumentType = 
                            try this.CompleteTexts()[0] |> getArgumentText
                            with e -> ""
                        ResultType = this.Content[0].ResultType
                        Continue = true
                    }
            else

            let ui = this.Host.UI.RawUI
            this.FrameTop <- Coordinates(0,ui.CursorPosition.Y + 1 - ui.WindowPosition.Y)
            this.FrameH <- ui.WindowSize.Height - ui.CursorPosition.Y - 3
            this.FrameW <- ui.WindowSize.Width

            if this.FrameH < 0 then () else
            if this.FrameW < 0 then () else
            
            let buf charray = 
                ui.NewBufferCellArray( Size(this.FrameW,this.FrameH), bufferCell charray)

            let start = buf ' '
            this.FilterContent()
            drawquery this ui start
            ui.SetBufferContents(this.FrameTop,start) // 2d array
            
            let readkeyopts = 
                ReadKeyOptions.NoEcho 
                ||| ReadKeyOptions.AllowCtrlC
                ||| ReadKeyOptions.IncludeKeyDown

            // let clearBuffer() =
            //     start 
            //     |> Array2D.iteri (fun x y f -> start[x,y].Character <- ' ')
            //     ui.SetBufferContents(this.FrameTop,start)

            let movepos (by:int) = movePosition this ui this.FrameTop start by    

            movepos 0      

            

            

            let rec loop() = 
                let c = ui.ReadKey(options=readkeyopts)
                
                match c.VirtualKeyCode |> enum<ConsoleKey> with 
                | ConsoleKey.Tab ->
                    let argtype = this.CompleteTexts()[this.Index] |> getArgumentText
                    {
                        CompletionText = this.FilteredContent[this.Index].CompletionText
                        ArgumentType = argtype 
                        ResultType = this.FilteredContent[this.Index].ResultType
                        Continue = true
                    }
                    |> this.WriteObject
                    this.ClearScreen(start) |> ignore
                | ConsoleKey.Enter -> 
                    {
                        CompletionText = this.FilteredContent[this.Index].CompletionText
                        ArgumentType = this.CompleteTexts()[this.Index] |> getArgumentText
                        Continue = false
                        ResultType = this.FilteredContent[this.Index].ResultType
                    }
                    |> this.WriteObject
                    this.ClearScreen(start) |> ignore
                | ConsoleKey.Escape -> this.ClearScreen(start) |> ignore
                | ConsoleKey.UpArrow -> movepos -1 |> ignore ;(*up arrow*) loop()
                | ConsoleKey.DownArrow -> movepos +1 |> ignore;(*down arrow*) loop()
                | ConsoleKey.OemPeriod -> (*dot*) loop()
                | ConsoleKey.Backspace -> 
                    this.Index <- 0
                    this.ScrollY <- 0
                    this.FilterText <- this.FilterText[..this.FilterText.Length - 2]
                    movepos 0 |> ignore
                    loop()
                | _ -> 
                match c.Character with 
                | c -> 
                    // clearBuffer()
                    this.FilterText <- $"{this.FilterText}%c{c}"
                    this.Index <- 0
                    this.ScrollY <- 0
                    movepos 0 |> ignore
                    loop ()

            loop()

        with e -> 
            {
                CompletionText = e.Message+"\n"+e.StackTrace
                ArgumentType = ""
                Continue = false
                ResultType = this.Content[0].ResultType
            }
            |> this.WriteObject
            
        

    override this.EndProcessing() = this.WriteVerbose "End!"
