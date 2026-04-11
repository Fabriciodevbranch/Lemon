# Lemon Writer

A cozy writer platform with built-in version control — designed for writers, not developers.

## What is Lemon Writer?

Lemon Writer is a full-stack writing platform that gives authors powerful versioning tools without the complexity of traditional version control systems. Think of it as your writing workspace with a built-in "time machine."

**Writer-friendly features** (no developer jargon):
- 📸 **Snapshots** — Save a version of your work at any point ("Take a Snapshot")
- 📜 **Timeline** — Visual history of all your versions, with side-by-side comparison
- ↩️ **Restore** — Jump back to any previous version of your chapter
- 🔍 **Grab Content** — Pick specific paragraphs from an older version and apply them to your current work
- 📝 **Drafts** — Work on alternate versions of a chapter without affecting your main work; publish a draft when you're happy with it
- 📚 **Library** — Organize your books, series, and chapters
- 📖 **Rich Editor** — Full formatting support including images, headers, lists, quotes, and more
- 📤 **Export** — Export your book as EPUB or PDF
- 🔐 **Login** — Sign in with Google or email/password

## Project Structure

```
Lemon/
├── BackEnd/               .NET 8 backend API
│   ├── src/
│   │   ├── LemonWriter.Domain/         Domain entities & events
│   │   ├── LemonWriter.Application/    CQRS commands, queries, behaviors
│   │   ├── LemonWriter.Infrastructure/ EF Core, gRPC services, repositories
│   │   └── LemonWriter.API/            ASP.NET Core Web API entry point
│   ├── tests/
│   │   └── LemonWriter.Application.Tests/
│   └── proto/             gRPC contracts (books, chapters, drafts, users)
│
└── FrontEnd/              Angular 20 frontend
    └── lemon-writer/
        └── src/app/
            ├── core/      Auth, HTTP services, models
            ├── features/  Library, Editor, Timeline, Drafts, Settings
            └── shared/    Navbar, sidebar, theme picker
```

## BackEnd

- **Architecture**: Clean Architecture + CQRS (MediatR) + Event-Driven
- **Transport**: REST API + gRPC (Grpc.AspNetCore)
- **Database**: EF Core 8 with SQLite (swap to PostgreSQL for production)
- **Auth**: JWT Bearer + Google OAuth 2.0
- **Observability**: OpenTelemetry (traces, metrics, logs via OTLP)
- **Logging**: Serilog with structured logging
- **Docs**: Swagger/OpenAPI

### Running the API

```bash
cd BackEnd
dotnet restore
dotnet run --project src/LemonWriter.API
```

The API will be available at `http://localhost:5000` with Swagger at `http://localhost:5000/swagger`.

### Configuration

Copy `appsettings.json` and create `appsettings.Development.json` with your values:

```json
{
  "ConnectionStrings": { "DefaultConnection": "Data Source=lemon.db" },
  "Jwt": { "Key": "your-secret-key", "Issuer": "LemonWriter", "Audience": "LemonWriter" },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." }
  }
}
```

## FrontEnd

- **Framework**: Angular 20 (standalone components)
- **UI Library**: Angular Material
- **Editor**: Quill (via ngx-quill) with full formatting + image support
- **Auth**: JWT storage + Google OAuth redirect
- **Themes**: 3 built-in cozy themes + custom theme builder

### Themes

| Theme | Feel |
|-------|------|
| 🕯️ **Cozy Amber** | Warm candlelight, parchment tones |
| 🌿 **Cozy Forest** | Earthy greens, nature-inspired |
| 🌙 **Cozy Night** | Dark mode with warm amber accents |
| 🎨 **Custom** | Define your own colors |

### Running the Frontend

```bash
cd FrontEnd/lemon-writer
npm install
ng serve
```

The app will be available at `http://localhost:4200`.

Update `src/environments/environment.ts` to point to your backend:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api',
  googleClientId: 'YOUR_GOOGLE_CLIENT_ID'
};
```

## Running Tests

```bash
# Backend tests
cd BackEnd
dotnet test

# Frontend tests
cd FrontEnd/lemon-writer
ng test
```
