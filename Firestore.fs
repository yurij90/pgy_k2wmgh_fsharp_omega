namespace pgy_k2wmgh_fsharp_omega

open WebSharper.JavaScript
open WebSharper

[<AutoOpen>]
[<JavaScript>]
module FirestoreTypes =

    type UrlRecord = {
        ShortCode: string
        OriginalUrl: string
        CreatedAt: float
    }

[<JavaScript>]
module Firestore =

    let private getDb () : obj =
        JS.Window?firebase?firestore()

    let private now () : float =
        JS.Eval("Date.now()") :?> float

    let generateShortCode () =
        let chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
        let r = System.Random()
        System.String(Array.init 6 (fun _ -> chars.[r.Next(chars.Length)]))

    let addUrl (originalUrl: string) : Async<string> =
        Async.FromContinuations(fun (ok, err, _) ->
            let db = getDb ()
            let shortCode = generateShortCode ()
            let docRef = db?collection("urls")?doc(shortCode)
            let data = New [
                "originalUrl", originalUrl
                "shortCode", shortCode
                "createdAt", now ()
            ]
            let promise = docRef?set(data)
            promise?``then``(fun () -> ok shortCode) |> ignore
            promise?catch(fun e -> err (System.Exception(string e))) |> ignore
        )

    let getUrlByCode (shortCode: string) : Async<UrlRecord option> =
        Async.FromContinuations(fun (ok, err, _) ->
            let db = getDb ()
            let docRef = db?collection("urls")?doc(shortCode)
            let promise = docRef?get()
            promise?``then``(fun (snapshot: obj) ->
                if snapshot?exists then
                    let data = snapshot?data()
                    let record = {
                        ShortCode = shortCode
                        OriginalUrl = unbox (data?originalUrl)
                        CreatedAt = if not (isNull (data?createdAt)) then unbox (data?createdAt) else 0.0
                    }
                    ok (Some record)
                else
                    ok None
            ) |> ignore
            promise?catch(fun e -> err (System.Exception(string e))) |> ignore
        )

    let getAllUrls () : Async<UrlRecord list> =
        Async.FromContinuations(fun (ok, err, _) ->
            let db = getDb ()
            let collection = db?collection("urls")
            let promise = collection?get()
            promise?``then``(fun (snapshot: obj) ->
                let docs = snapshot?docs
                let length = int (unbox<float> (docs?length))
                let mutable results: UrlRecord list = []
                for i = 0 to length - 1 do
                    let doc = docs?(string i)
                    let data = doc?data()
                    let record = {
                        ShortCode = unbox (doc?id)
                        OriginalUrl = unbox (data?originalUrl)
                        CreatedAt = if not (isNull (data?createdAt)) then unbox (data?createdAt) else 0.0
                    }
                    results <- record :: results
                ok (List.rev results)
            ) |> ignore
            promise?catch(fun e -> err (System.Exception(string e))) |> ignore
        )

    let updateUrl (shortCode: string) (newUrl: string) : Async<unit> =
        Async.FromContinuations(fun (ok, err, _) ->
            let db = getDb ()
            let docRef = db?collection("urls")?doc(shortCode)
            let data = New [ "originalUrl", newUrl ]
            let promise = docRef?update(data)
            promise?``then``(fun () -> ok ()) |> ignore
            promise?catch(fun e -> err (System.Exception(string e))) |> ignore
        )

    let deleteUrl (shortCode: string) : Async<unit> =
        Async.FromContinuations(fun (ok, err, _) ->
            let db = getDb ()
            let docRef = db?collection("urls")?doc(shortCode)
            let promise = docRef?delete()
            promise?``then``(fun () -> ok ()) |> ignore
            promise?catch(fun e -> err (System.Exception(string e))) |> ignore
        )