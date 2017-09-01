module Github

open System
open System.Text.RegularExpressions
open Octokit
open TelegramAnswers
open TelegramAnswers.Log

let generateTitleId = sprintf "(%i) %s"

let getIssueId id (issues : Issue seq) = 
    issues
    |> Seq.filter (fun x -> Regex.IsMatch(x.Title, sprintf "^\\(%i\\)" id))
    |> Seq.map (fun x -> x.Number)
    |> Seq.tryHead

let makeAgent (config : GithubConfig) = 
    MailboxProcessor<Command>.Start(fun inbox -> 
        let client = GitHubClient(ProductHeaderValue("TA"))
        client.Credentials <- Credentials(config.token)
        LOG "Github :: start listening commands"
        let createIssue id title text = 
            async { 
                LOG(sprintf "Create issue, id = %O, text = %O" id text)
                let issue = generateTitleId id title |> NewIssue
                issue.Body <- text
                issue.Labels.Add("question")
                do! client.Issue.Create
                        (config.repoOwner, config.repoName, issue)
                    |> Async.AwaitTask
                    |> Async.Ignore
            }
        
        let createComment id time text = 
            async { 
                LOG
                    (sprintf "Create comment, id = %O, time = %O, text = %O" id 
                         time text)
                let r = RepositoryIssueRequest()
                r.Since <- DateTimeOffset(time) |> Nullable
                let! issues = client.Issue.GetAllForRepository
                                  (config.repoOwner, config.repoName, r) 
                              |> Async.AwaitTask
                match getIssueId id issues with
                | Some id -> 
                    sprintf "Github :: Create comment for issue with gh-id = %O" 
                        id |> LOG
                    do! client.Issue.Comment.Create
                            (config.repoOwner, config.repoName, id, text)
                        |> Async.AwaitTask
                        |> Async.Ignore
                | None -> LOG "Github :: Warning :: Issue not found"
            }
        
        let rec messageLoop() = 
            async { 
                let! command = inbox.Receive()
                LOG(sprintf "Incoming github command = %O" command)
                match command with
                | CreateIssue(id, title, text) -> do! createIssue id title text
                | CreateComment(id, time, text) -> 
                    do! createComment id time text
                return! messageLoop()
            }
        
        messageLoop())