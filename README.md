# Moonglade Blog

Moonglade is a self-hosted personal blog built with ASP.NET Core Razor Pages on .NET 10. It supports PostgreSQL (default) and SQL Server.

- Publish HTML or Markdown posts with Mermaid diagrams, scheduled publishing, categories, and tags. Create standalone pages.
- Manage comments, themes, menus, widgets, and images from a desktop browser at 1440 CSS pixels or wider.
- Offer RSS and Atom feeds, Webmentions, and six interface languages.

## Quick Start with Docker

Copy `.env.example` to `.env`, set `POSTGRES_PASSWORD`, and start Compose from the repository root. [Environment template](.env.example)

```bash
docker compose up -d
```

Open `http://localhost:8080` for [first-run setup](#first-run). Compose starts Moonglade and PostgreSQL. Keep the database and both image volumes when replacing containers.

## First Run

Restrict access until setup is complete. Anyone who can reach an uninitialized site can claim it at `/auth/setup-account`. Set the owner's email and password, then verify the authenticator code when TOTP is required. [Setup behavior and password requirements](docs/operations.md#first-run)

## Deployment

> This blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.

Deploy a published release at the URL root with HTTPS and HTTP/2. Behind a reverse proxy, configure `ForwardedHeaders:KnownProxies` with the trusted proxy addresses. Use separate persistent paths for public images and private originals. [Published releases](https://github.com/EdiWang/Moonglade/releases) · [Image storage](docs/operations.md#image-storage)

### Database Upgrades

Fresh installations create and seed the database automatically. For upgrades, stop all hosts, back up the database, verify the applicable provider scripts on a restored copy, then apply them before starting the new version. For rollback, restore the backup and its matching application version together. [Script order and recovery](database/README.md#upgrade-order)

## Configuration

Set host options in `appsettings.json` or with environment variables (`:` becomes `__`). Manage blog preferences at `/admin/settings`. [Host settings](src/Moonglade.Web/appsettings.json) · [Operations guide](docs/operations.md)

### Authentication

The owner and up to two collaborators sign in with email and password. TOTP is required by default. The owner manages collaborators and optional OpenID Connect sign-in at `/admin/account`. [Account configuration and permissions](docs/operations.md#authentication)

## Development

Install the .NET 10 SDK and start PostgreSQL or SQL Server. The solution is `src/Moonglade.slnx`. Set `ConnectionStrings:DatabaseProvider` to `PostgreSql` or `SqlServer` and set `ConnectionStrings:MoongladeDatabase` or the `ConnectionStrings__MoongladeDatabase` environment variable.

### Build & Run

```bash
dotnet run --project src/Moonglade.Web/Moonglade.Web.csproj
```

In PowerShell, you can also run `./start.ps1`. Open `https://localhost:10210`. [PowerShell launcher](start.ps1) · [Test commands](src/Tests/README.md#run-tests) · [Engineering reference](docs/agent-reference.md)

## License

[GPL-3.0](LICENSE)
