namespace TelegramAnswers

open System

type GithubConfig = 
    { repoOwner : string
      repoName : string
      token : string }

type TelegramConfig = 
    { token : string
      chat : string }

type Config = 
    { github : GithubConfig
      telegram : TelegramConfig }

type Command = 
    | CreateIssue of int * title : string * body : string
    | CreateComment of int * DateTime * string

type Question = 
    { id : int
      title : string
      text : string }

type Answer = 
    { id : int
      text : string
      time : DateTime
      question : Question }

type UserAction = 
    | Question of Question
    | Answer of Answer
    | Unknown

module String = 
    let startWith (text : string) (prefix : string) = text.StartsWith(prefix)
    let contains (x : string) pattern = x.Contains(pattern)
    
    let limit size text = 
        if String.length text <= size then text
        else text.Substring(0, size - 1) + "…"

module Config = 
    let tryParse argv = 
        match argv with
        | [| tgToken; chat; ghToken; repoOwner; repoName |] -> 
            { github = 
                  { repoOwner = repoOwner
                    repoName = repoName
                    token = ghToken }
              telegram = 
                  { token = tgToken
                    chat = chat } }
            |> Some
        | _ -> None

module Log = 
    let LOG = printfn "LOG :: %s\n"

module Domain = 
    open System.Text.RegularExpressions
    open Telegram.Bot
    open Telegram.Bot.Types
    
    let private clearFromMarker x = Regex.Replace(x, " *#q *", " ")
    
    let parse (upd : Update) = 
        let isQuestion text = String.contains text "#q"
        if isNull upd.Message.Text then Unknown
        else if isNull upd.Message.ReplyToMessage then 
            if isQuestion upd.Message.Text then 
                Question { id = upd.Message.MessageId
                           title = 
                               upd.Message.Text
                               |> clearFromMarker
                               |> String.limit 50
                           text = upd.Message.Text |> clearFromMarker }
            else Unknown
        else if isQuestion upd.Message.ReplyToMessage.Text then 
            Answer 
                { id = upd.Message.MessageId
                  text = upd.Message.Text
                  time = upd.Message.ReplyToMessage.Date
                  question = 
                      { id = upd.Message.ReplyToMessage.MessageId
                        title = 
                            upd.Message.ReplyToMessage.Text
                            |> clearFromMarker
                            |> String.limit 50
                        text = 
                            upd.Message.ReplyToMessage.Text |> clearFromMarker } }
        else Unknown
    
    let actionToCommand a = 
        match a with
        | Question x -> CreateIssue(x.id, x.title, x.text) |> Some
        | Answer x -> CreateComment(x.question.id, x.time, x.text) |> Some
        | _ -> None