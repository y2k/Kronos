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

let help = 
    "dotnet run <telegram token> <telegram chat title> <github token> <github owner> <github repository name>"

[<EntryPoint>]
let main argv = 
    match Config.tryParse argv with
    | Some config -> 
        let agent = Github.makeAgent config.github
        Telegram.repl config.telegram (handleUpdate agent)
    | None -> printf "%s" help
    0