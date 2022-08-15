module aciq.pscomplete.Helpers

open System


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