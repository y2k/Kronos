open TelegramAnswers
open Log

let handleUpdate (agent : MailboxProcessor<Command>) 
    (update : Telegram.Bot.Types.Update) = 
    async { 
        let optCommand = 
            update
            |> Domain.parse
            |> Domain.actionToCommand
        match optCommand with
        | Some cmd -> 
            LOG(sprintf "Send command to github agent = %O" cmd)
            agent.Post cmd
        | _ -> ()
    }

[<EntryPoint>]
let main argv = 
    let config = Config.tryParse argv |> Option.get
    let agent = Github.makeAgent config.github
    Telegram.repl config.telegram (handleUpdate agent)
    0