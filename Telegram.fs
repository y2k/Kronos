module Telegram

open Telegram.Bot
open Telegram.Bot.Types
open TelegramAnswers
open TelegramAnswers.Log

let filterMessages (updates : Update seq) chatTitle = 
    updates 
    |> Seq.filter 
           (fun x -> not (isNull x.Message) && x.Message.Chat.Title = chatTitle)

let repl config action = 
    async { 
        let bot = TelegramBotClient(config.token)
        let mutable offset = 0
        LOG "Telegram :: start listening"
        while true do
            try 
                let! updates = bot.GetUpdatesAsync(offset) |> Async.AwaitTask
                offset <- updates
                          |> Seq.map (fun x -> x.Id + 1)
                          |> Seq.fold (fun a x -> max a x) offset
                let chatUpdates = filterMessages updates config.chat
                for update in chatUpdates do
                    try 
                        sprintf "Telegram update = %O, offset = %O, chat = %O" 
                            update.Message.Text offset update.Message.Chat.Title 
                        |> LOG
                        do! action update
                    with e -> LOG(sprintf "Telegram error = %O" e)
            with e -> LOG(sprintf "Telegram error = %O" e)
    }
    |> Async.RunSynchronously