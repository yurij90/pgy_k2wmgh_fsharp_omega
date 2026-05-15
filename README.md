# 🔗 URL Shortener — F# WebSharper

A modern, full-stack URL shortening application built with **F#** and **WebSharper**, deployed on **Firebase**. Shorten long URLs, copy them to clipboard, and manage them from an admin dashboard.

🔗 **Live Demo:** [https://pgy-k2wmgh-fsharp-omega.web.app/](https://pgy-k2wmgh-fsharp-omega.web.app/)

---

## ✨ Features

- **URL Shortening** – Paste a long URL and instantly get a short, shareable link.
- **Duplicate Detection** – If a URL has already been shortened, the existing short code is returned.
- **One-Click Copy** – Copy the shortened URL to your clipboard with a single click.
- **Admin Dashboard** – View, edit, and delete all shortened URLs in one place.
- **Inline Editing** – Edit the original URL directly in the admin table without leaving the page.
- **Delete Confirmation** – Modal confirmation dialog before deleting a URL.
- **Responsive UI** – Fully responsive dark-themed interface built with pure CSS.
- **Toast Notifications** – Animated toast messages for success and error feedback.
- **Redirect Handling** – Visiting a short code automatically redirects to the original URL with a loading animation.

---

## 🧰 Tech Stack

| Technology          | Purpose                              |
|---------------------|--------------------------------------|
| **F#**              | Core application language            |
| **WebSharper 10**   | F#-to-JavaScript compiler & UI framework |
| **WebSharper.UI**   | Reactive UI templating (MVU pattern) |
| **ASP.NET Core**    | Server-side hosting                   |
| **Firebase**        |    |
| ├ Firestore         | NoSQL database for storing URLs      |
| ├ Firebase Hosting  | Static asset & SPA hosting           |
| └ Firebase Auth     | (optional, not used in current build)|
| **Vite**            | Development build tool (HMR)         |
| **ESBuild**         | Production bundling (minification)   |
| **Google Fonts**    | Inter font family                    |

---

## 🏗️ Project Structure

```
pgy_k2wmgh_fsharp_omega/
├── Client.fs                  # Client-side SPA logic (F# / WebSharper)
├── Firestore.fs               # Firebase Firestore data access layer
├── Startup.fs                 # ASP.NET Core startup / server entry point
├── appsettings.json           # ASP.NET Core configuration
├── esbuild.config.mjs         # Production bundler configuration
├── vite.config.js             # Vite dev-server configuration
├── wsconfig.json              # WebSharper compiler configuration
├── firebase.json              # Firebase Hosting & Firestore config
├── firestore.rules            # Firestore security rules
├── firestore.indexes.json     # Firestore composite indexes
├── package.json               # npm dependencies (Firebase SDK, esbuild)
├── pgy_k2wmgh_fsharp_omega.fsproj  # .NET project file
└── wwwroot/                   # Static assets
    ├── index.html             # Main HTML page + inline CSS + Firebase init
    ├── favicon.svg            # Browser tab icon
    └── Scripts/               # Built JS/CSS output
```

---

## 🚀 How to Run Locally

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- A Firebase project with Firestore enabled (or use the existing one)

### 1. Clone & Install Dependencies

```bash
git clone https://github.com/yurij90/pgy_k2wmgh_fsharp_omega.git
cd pgy_k2wmgh_fsharp_omega
npm install
```

### 2. Set Up Firebase

Create a Firebase project, enable **Cloud Firestore**, and replace the Firebase config in `wwwroot/index.html` (lines 898–909) with your own credentials:

```js
var firebaseConfig = {
    apiKey: "YOUR_API_KEY",
    authDomain: "YOUR_PROJECT.firebaseapp.com",
    projectId: "YOUR_PROJECT_ID",
    // ...
};
```

### 3. Run in Development Mode

```bash
dotnet run
```

This starts the ASP.NET Core server with Vite HMR enabled (the `#if DEBUG` block in `Startup.fs` handles this). Open the URL shown in the terminal (typically `https://localhost:5001`).

### 4. Build for Production

```bash
dotnet publish -c Release
```

The output is in `bin/Release/net10.0/publish/wwwroot/`. You can deploy the contents of that folder to Firebase Hosting or any static host.

---

## 🔥 Deployment (Firebase)

The project includes a `firebase.json` and `.firebaserc` for Firebase Hosting. To deploy:

```bash
dotnet publish -c Release
firebase deploy --only hosting
```

The live site is hosted at:  
➡️ **[https://pgy-k2wmgh-fsharp-omega.web.app/](https://pgy-k2wmgh-fsharp-omega.web.app/)**

---

## 🧠 How It Works

1. **Enter a URL** on the home page and click **Shorten**.
2. The client (F# compiled to JavaScript via WebSharper) calls Firestore directly using the Firebase Web SDK.
3. A 6-character alphanumeric short code is generated and stored in the `urls` collection.
4. The short URL is displayed and can be copied with one click.
5. **Visiting a short URL** triggers the redirect logic in `Client.fs` — it reads the path, looks up the Firestore document by short code, and redirects the browser to the original URL.
6. The **Admin Dashboard** lists all URLs with edit and delete capabilities, all handled client-side via reactive views.

### Data Model (Firestore `urls` collection)

| Field        | Type   | Description                    |
|--------------|--------|--------------------------------|
| `shortCode`  | string | 6-char alphanumeric ID (doc ID) |
| `originalUrl`| string | The original long URL          |
| `createdAt`  | number | Unix timestamp (ms)            |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 👤 Author

**Yurij** – [GitHub](https://github.com/yurij90)