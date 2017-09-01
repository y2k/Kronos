module Telegram

open Telegram.Bot
open Telegram.Bot.Types
open TelegramAnswers
open TelegramAnswers.Log

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
                let chatUpdates = 
                    updates 
                    |> Seq.filter (fun x -> x.Message.Chat.Title = config.chat)
                for update in chatUpdates do
                    try 
                        LOG
                            (sprintf 
                                 "Telegram update = %O, offset = %O, chat = %O" 
                                 update.Message.Text offset 
                                 update.Message.Chat.Title)
                        do! action update
                    with e -> LOG(sprintf "Telegram error = %O" e)
            with e -> LOG(sprintf "Telegram error = %O" e)
    }
    |> Async.RunSynchronously