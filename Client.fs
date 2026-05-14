namespace pgy_k2wmgh_fsharp_omega

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.UI.Html

[<JavaScript>]
module Client =

    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    // --- State ---
    let rvCurrentPage = Var.Create "home"
    let rvIsHomeActive = rvCurrentPage.View.Map (fun p -> if p = "home" then "nav-link active" else "nav-link")
    let rvIsAdminActive = rvCurrentPage.View.Map (fun p -> if p = "admin" then "nav-link active" else "nav-link")

    // Home page state
    let rvHomeUrl = Var.Create ""
    let rvShortUrl = Var.Create ""
    let rvShowResult = Var.Create false
    let rvIsError = Var.Create false
    let rvCopyBtnText = Var.Create "Copy"
    let rvShortenBtnLoading = Var.Create false
    let rvResultLabel = Var.Create ""

    // Admin page state
    let rvUrlList = ListModel.FromSeq ([] : UrlRecord list)
    let rvRefreshLoading = Var.Create false
    let rvEditingCode = Var.Create ""
    let rvEditUrl = Var.Create ""
    let rvShowConfirm = Var.Create false
    let rvConfirmCode = Var.Create ""
    let rvConfirmUrl = Var.Create ""

    // --- Helpers ---
    let baseUrl =
        let loc = JS.Window.Location
        loc?protocol + "//" + loc?host + "/"

    let showToast (msg: string) (isError: bool) =
        let toastClass = if isError then "toast-error" else "toast-success"
        let div = JS.Document.CreateElement("div")
        div?className <- "toast " + toastClass
        div?textContent <- msg
        let container = JS.Document.QuerySelector(".toast-container")
        container.AppendChild(div) |> ignore
        JS.Window.SetTimeout(fun () ->
            if not (isNull (div?parentNode)) then
                div?parentNode?RemoveChild(div) |> ignore
        , 3000) |> ignore

    let showPage (page: string) =
        rvCurrentPage.Value <- page
        let homeEl = JS.Document.GetElementById("homePage")
        let adminEl = JS.Document.GetElementById("adminPage")
        if not (isNull homeEl) && not (isNull adminEl) then
            homeEl?className <- if page = "home" then "page active" else "page"
            adminEl?className <- if page = "admin" then "page active" else "page"

    let isValidUrl (url: string) =
        url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("ftp://")

    // --- Firestore operations ---
    let refreshUrlList () =
        async {
            rvRefreshLoading.Value <- true
            try
                let! urls = Firestore.getAllUrls ()
                rvUrlList.Clear()
                for url in urls do
                    rvUrlList.Add url
            with ex ->
                showToast ("Failed to load URLs: " + ex.Message) true
            rvRefreshLoading.Value <- false
        }

    let handleShortenUrl () =
        async {
            let url = rvHomeUrl.Value.Trim()
            if System.String.IsNullOrWhiteSpace url then
                rvIsError.Value <- true
                rvResultLabel.Value <- "! Please enter a URL"
                rvShowResult.Value <- true
            elif not (isValidUrl url) then
                rvIsError.Value <- true
                rvResultLabel.Value <- "! URL must start with http:// or https://"
                rvShowResult.Value <- true
            else
                // Check for duplicate URL in existing list
                let duplicate =
                    rvUrlList.Value
                    |> Seq.tryFind (fun r -> r.OriginalUrl = url)
                match duplicate with
                | Some existing ->
                    rvShortUrl.Value <- baseUrl + existing.ShortCode
                    rvIsError.Value <- false
                    rvResultLabel.Value <- "✓ URL already shortened!"
                    rvShowResult.Value <- true
                    showToast "This URL was already shortened!" false
                | None ->
                    rvShortenBtnLoading.Value <- true
                    try
                        let! shortCode = Firestore.addUrl url
                        let shortUrl = baseUrl + shortCode
                        rvShortUrl.Value <- shortUrl
                        rvIsError.Value <- false
                        rvResultLabel.Value <- "✓ Short URL created!"
                        rvShowResult.Value <- true
                        showToast "URL shortened successfully!" false
                    with ex ->
                        rvIsError.Value <- true
                        rvResultLabel.Value <- "! " + ex.Message
                        rvShowResult.Value <- true
                        showToast ("Error: " + ex.Message) true
                    rvShortenBtnLoading.Value <- false
        }

    let handleCopyUrl () =
        let text = rvShortUrl.Value
        if not (System.String.IsNullOrWhiteSpace text) then
            let textarea = JS.Document.CreateElement("textarea")
            textarea?value <- text
            textarea?style?position <- "fixed"
            textarea?style?left <- "-9999px"
            JS.Document.Body.AppendChild(textarea) |> ignore
            textarea?select()
            JS.Document?execCommand("copy") |> ignore
            JS.Document.Body.RemoveChild(textarea) |> ignore
            rvCopyBtnText.Value <- "Copied!"
            showToast "Copied to clipboard!" false
            JS.Window.SetTimeout(fun () -> rvCopyBtnText.Value <- "Copy") |> ignore

    let handleDeleteUrl (code: string) (originalUrl: string) =
        rvConfirmCode.Value <- code
        rvConfirmUrl.Value <- originalUrl
        rvShowConfirm.Value <- true

    let handleConfirmDelete () =
        async {
            let code = rvConfirmCode.Value
            rvShowConfirm.Value <- false
            try
                do! Firestore.deleteUrl code
                rvUrlList.RemoveBy (fun r -> r.ShortCode = code) |> ignore
                showToast ("Deleted " + code) false
            with ex ->
                showToast ("Delete failed: " + ex.Message) true
        }

    let handleStartEdit (code: string) (currentUrl: string) =
        rvEditingCode.Value <- code
        rvEditUrl.Value <- currentUrl

    let handleCancelEdit () =
        rvEditingCode.Value <- ""

    let handleSaveEdit () =
        async {
            let code = rvEditingCode.Value
            let newUrl = rvEditUrl.Value.Trim()
            if System.String.IsNullOrWhiteSpace newUrl then
                showToast "URL cannot be empty" true
            elif not (isValidUrl newUrl) then
                showToast "URL must start with http:// or https://" true
            else
                try
                    do! Firestore.updateUrl code newUrl
                    rvUrlList.UpdateBy (fun r -> if r.ShortCode = code then Some { r with OriginalUrl = newUrl } else None) |> ignore
                    rvEditingCode.Value <- ""
                    showToast "URL updated successfully!" false
                with ex ->
                    showToast ("Update failed: " + ex.Message) true
        }

    // --- Home page results ---
    let resultBoxClass =
        rvShowResult.View.Map (fun show ->
            if show then
                let errClass = if rvIsError.Value then " error" else ""
                "result-box visible" + errClass
            else
                "result-box"
        )

    let shortUrlText =
        rvShortUrl.View.Map (fun url ->
            if System.String.IsNullOrWhiteSpace url then "" else url
        )

    let shortenBtnText =
        rvShortenBtnLoading.View.Map (fun loading ->
            if loading then "Shortening..." else "Shorten"
        )

    let refreshBtnText =
        rvRefreshLoading.View.Map (fun loading ->
            if loading then "⏳ Loading..." else "🔄 Refresh"
        )

    let confirmOverlayClass =
        rvShowConfirm.View.Map (fun show ->
            if show then "confirm-overlay visible" else "confirm-overlay"
        )

    let confirmUrl =
        rvConfirmUrl.View.Map (fun url -> url)

    // --- Admin table ---
    let renderAdminTable () =
        rvUrlList.View.Map (fun items ->
            let itemsList = Seq.toList items
            if List.isEmpty itemsList then
                div [attr.``class`` "empty-state"] [
                    div [attr.``class`` "empty-icon"] [text "🔗"]
                    h3 [] [text "No URLs yet"]
                    p [] [text "Create your first shortened URL on the home page."]
                ]
            else
                table [] [
                    thead [] [
                        tr [] [
                            th [] [text "Short Code"]
                            th [] [text "Original URL"]
                            th [] [text "Created"]
                            th [attr.style "text-align: right"] [text "Actions"]
                        ]
                    ]
                    tbody [] [
                        for item in itemsList ->
                            let shortUrl = baseUrl + item.ShortCode
                            tr [] [
                                td [] [
                                    span [attr.``class`` "code-badge"] [
                                        a [attr.href shortUrl; attr.target "_blank"] [text item.ShortCode]
                                    ]
                                ]
                                td [attr.``class`` "table-url-cell"] [
                                    a [attr.href item.OriginalUrl; attr.target "_blank"] [text item.OriginalUrl]
                                ]
                                td [] [
                                    text (
                                        let dateStr = JS.Eval("new Date(" + string item.CreatedAt + ").toLocaleString()")
                                        string dateStr
                                    )
                                ]
                                td [attr.style "text-align: right"] [
                                    button [
                                        attr.``class`` "btn btn-outline btn-sm"
                                        attr.title "Edit"
                                        on.click (fun _ _ -> handleStartEdit item.ShortCode item.OriginalUrl)
                                    ] [text "✏️ Edit"]
                                    text " "
                                    button [
                                        attr.``class`` "btn btn-danger btn-sm"
                                        attr.title "Delete"
                                        on.click (fun _ _ -> handleDeleteUrl item.ShortCode item.OriginalUrl)
                                    ] [text "🗑️ Delete"]
                                ]
                            ]
                    ]
                ]
        ) |> Doc.EmbedView

    [<SPAEntryPoint>]
    let Main () =

        // Check for redirect (visiting a short URL)
        async {
            let path: string = (JS.Window.Location :> obj)?pathname
            let code = path.Trim('/')
            if not (System.String.IsNullOrWhiteSpace code) && code <> "admin" then
                try
                    let! result = Firestore.getUrlByCode code
                    match result with
                    | Some record ->
                        JS.Window.Location?href <- record.OriginalUrl
                    | None ->
                        showToast ("URL '" + code + "' not found") true
                with ex ->
                    showToast ("Error: " + ex.Message) true
        } |> Async.Start

        // Initial load of URL list
        refreshUrlList () |> Async.Start

        IndexTemplate.Main()
            // Navigation - use classDyn to toggle active class
            .homeClass(attr.classDyn rvIsHomeActive)
            .adminClass(attr.classDyn rvIsAdminActive)
            .RouteHome(fun _ -> showPage "home")
            .RouteAdmin(fun _ -> showPage "admin")

            // Home page
            .HomeUrl(rvHomeUrl)
            .ShortenUrl(fun _ -> handleShortenUrl () |> Async.Start)
            .ShortenBtnContent(shortenBtnText)
            .ResultBoxClass(attr.classDyn resultBoxClass)
            .ResultLabel(rvResultLabel.View)
            .ShortUrlText(shortUrlText)
            .CopyUrl(fun _ -> handleCopyUrl ())

            // Admin page
            .RefreshUrls(fun _ -> refreshUrlList () |> Async.Start)
            .RefreshBtnText(refreshBtnText)
            .AdminTable(renderAdminTable ())

            // Confirm dialog
            .ConfirmOverlayClass(attr.classDyn confirmOverlayClass)
            .ConfirmTitle("Confirm Delete")
            .ConfirmUrl(confirmUrl)
            .ConfirmBtnText("Delete")
            .CancelConfirm(fun _ -> rvShowConfirm.Value <- false)
            .ConfirmAction(fun _ -> handleConfirmDelete () |> Async.Start)

            .Doc()
        |> Doc.RunById "main"