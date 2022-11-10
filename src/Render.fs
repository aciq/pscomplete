module aciq.pscomplete.Render

open System
open System.Management.Automation
open System.Management.Automation.Host
open System.Threading
open System.Threading.Tasks
open aciq.pscomplete.Helpers

type ExitKey =
    | None = 0
    | Tab = 1
    | Enter = 2
    | Escape = 3


type LoopContext =
    | Arrow
    | Input
    | InputAdded

let debounce fn (millis: int) =
    let last = 0
    (fun arg ->
        let current = Interlocked.Increment(ref last)
        Task.Delay(millis).ContinueWith(fun task ->
            if current = last then fn (arg)
            task.Dispose()
        )
    )

type LoopArgs =
    {
        InitState : DisplayState
        Ui : PSHostRawUserInterface
        ExitCommand: DisplayState -> ExitKey ->  unit
    }

let startLoop (args:LoopArgs) (beforeKey:DisplayState * LoopContext ->unit) =
    let rec loop (ctx:LoopContext) (state: DisplayState) =
        beforeKey(state,ctx)
        
        let c = args.Ui.ReadKey(options = readkeyopts)
        match c.VirtualKeyCode |> enum<ConsoleKey> with
        | ConsoleKey.Tab -> args.ExitCommand state ExitKey.Tab
        | ConsoleKey.Enter -> args.ExitCommand state ExitKey.Enter
        | ConsoleKey.Escape -> args.ExitCommand state ExitKey.Escape
        | ConsoleKey.LeftArrow -> loop LoopContext.Arrow (DisplayState.withArrowLeft state)
        | ConsoleKey.RightArrow -> loop LoopContext.Arrow (DisplayState.withArrowRight state)
        | ConsoleKey.UpArrow -> loop LoopContext.Arrow (DisplayState.withArrowUp state)
        | ConsoleKey.DownArrow -> loop LoopContext.Arrow (DisplayState.withArrowDown state)
        // | ConsoleKey.OemPeriod -> getCompletionAndExit ExitKey.Period
        // | ConsoleKey.Oem2 -> getCompletionAndExit ExitKey.Slash // forward-slash
        // | ConsoleKey.Oem5 -> getCompletionAndExit ExitKey.Backslash // backslash
        | ConsoleKey.Backspace -> loop LoopContext.Input (DisplayState.withBackspace state)
        | keycode ->
            match int keycode with
            // shift ctrl alt
            | 16
            | 17
            | 18 -> ()
            | _ -> loop LoopContext.Input (DisplayState.withFilterChar c.Character state)
    
    loop LoopContext.Input args.InitState