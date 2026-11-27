# Moonglade Blog

Moonglade is a self-hosted personal blog built with ASP.NET Core Razor Pages on .NET 10. It supports PostgreSQL (default) and SQL Server.

- Publish HTML or Markdown posts with Mermaid diagrams, drafts, scheduling, categories, and tags. Create standalone pages.
- Manage comments, replies, Webmentions, themes, widgets, menus, images, site verification files, and activity in the admin portal (desktop browsers, 1440 CSS pixels or wider).
- Visitors can switch languages in the footer. Moonglade supports English, Simplified Chinese, Traditional Chinese (Hong Kong terminology), German, Japanese, and French (France). The blog's default language controls blog and post metadata.

## Deployment

> This blogging system must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.

Deploy a published release at the URL root with HTTPS and HTTP/2. Behind a reverse proxy, set `ForwardedHeaders:KnownProxies` to the trusted proxy addresses so the app receives the public scheme and client IP. [Releases](https://github.com/EdiWang/Moonglade/releases)

### Database Upgrades

Fresh installations create the schema and seed initial data automatically. For upgrades, stop all Moonglade hosts, back up the database, test the required scripts on a restored copy, and apply them before starting the new version. [Database execution guide](database/README.md#verification-and-execution)

For 16.9.0 to 16.10.0, run the following scripts in order for your provider. [PostgreSQL scripts](database/PostgreSql) · [SQL Server scripts](database/SqlServer)

1. `3.activity-log-events.sql` replaces legacy activity history and discards its existing entries.
2. `4.seo-settings.sql` copies legacy SEO settings.
3. `5.seo-robots-txt.sql` copies the existing robots.txt content.
4. `6.owner-account.sql` creates owner account storage.
5. `7.owner-account-cutover.sql` transfers the public owner email and removes legacy local credentials.
6. `8.owner-avatar.sql` transfers the owner avatar and Gravatar preference.
7. `9.owner-presentation.sql` transfers the owner name and description.
8. `10.owner-oidc.sql` creates OIDC storage and the Data Protection key table required for local sign-in and antiforgery tokens. Run it even if you use only local sign-in.

For 16.10.0 to 16.11.0, run `11.owner-oidc-binding-only.sql` after the earlier upgrades. It removes the OIDC enable/disable switch. Existing bindings are preserved, including previously disabled bindings, which resume allowing third-party sign-in.

The widget-composer schema also requires `12.widget-regions.sql` after script 11. It replaces widget numeric placement bands with explicit regions and region-local order, and raises the stored title limit to 128. Follow the [widget upgrade and recovery steps](database/README.md#widget-regionorder-upgrade-12) before starting this application against an existing database.

Run the provider's `13.unified-accounts.sql` after script 12 and before starting a version with collaborator accounts. The script preserves the owner identity, credentials, OIDC binding, Data Protection keys and activity history. For rollback, restore the pre-upgrade database backup and its matching application version together. [Unified account upgrade steps](database/README.md#unified-account-upgrade-13)

For databases older than 16.9.0, run the applicable category ID and PostgreSQL naming upgrades (`1` and `2`) first. Keep access restricted until any required owner setup is complete. [First run](#first-run)

### Widget Placement

In `/admin/widgets`, select a widget in the sidebar structure and edit its content inline. Add by type, drag widgets within or between regions, choose placement, or use Move up/Move down within a region. The regions are Before Profile, Before Tags, Before Categories, and After Categories. Built-in anchors reflect existing settings and remain read-only. Edits, removals, and type changes stay in a page draft until Save changes replaces the complete collection. Undo restores staged widget removals; Discard changes restores the loaded baseline. Titles allow 128 characters and each widget content allows 2000 characters.

The isolated, noninteractive preview uses the actual public sidebar templates, current theme and saved custom CSS. It follows draft changes without saving. Invalid fields or preview failures retain the last successful preview with an out-of-date notice; use Refresh preview to retry. Preview immediately reflects saved custom CSS; public tabs retain the existing one-hour stylesheet cache and may need a fresh asset cache for comparison. Link List respects each link's opening switch; legacy links without a stored switch retain their new-tab default.

### Widgets API

Administrator-only `GET /api/widgets` returns an uncached complete `{ "widgets": [...] }` collection, including disabled widgets in canonical sidebar order. Antiforgery-protected `PUT /api/widgets` atomically replaces that collection and returns the canonical saved envelope. `POST /api/widgets/preview` accepts the same editable envelope and returns uncached HTML without saving widgets, settings or activity. Other APIs are unchanged; the old widget list/detail and individual create/update/delete routes are retired.

Every entry must explicitly include all seven editable fields:

```json
{
    "widgets": [
        {
            "id": "10000000-0000-0000-0000-000000000001",
            "title": "Links",
            "widgetType": "LinkList",
            "region": "AfterCategories",
            "sortOrder": 0,
            "isEnabled": true,
            "contentCode": "[]"
        }
    ]
}
```

IDs must be unique nonempty GUIDs and stay stable across retries. Types are `LinkList`, `ImageLink` and `ButtonLink`; `contentCode` is the corresponding JSON string, limited to 2000 characters per widget. Empty link lists are valid; images require an image URL and button lists contain one to three buttons. Titles are required, trimmed and limited to 128 characters. Missing/null/incomplete envelopes, invalid values or malformed type content return HTTP 400 with field errors such as `Widgets[index].ContentCode[item].Name`, before mutation. There is no widget/link count cap beyond existing host request limits.

Regions are `BeforeProfile`, `BeforeTags`, `BeforeCategories` and `AfterCategories`; submitted `sortOrder` is an integer from 0 through 2147483647. Within each region, submitted array position breaks order ties; saved orders become contiguous from zero. `displayOrder` is retired. An explicit empty array deletes every widget. Omitted IDs are deleted, absent IDs are created/restored and matching IDs are updated. The later successful whole-page save replaces earlier additions and deletions. Surviving IDs retain creation UTC; new/restored IDs receive current server UTC. Responses also include server-owned `contentType` and `createdTimeUtc`, which clients cannot overwrite.

An uncertain save retains the draft and requires explicit whole-page overwrite confirmation before retry. Cache invalidation failure after commit returns HTTP 500 Problem Details with `committed: true` and `savedCollection`; the editor reports Saved, blocks edits/resubmission and offers confirmed reload. Activity dispatch failures preserve successful persistence. Public widget caches remain per-instance. Follow the [database upgrade and recovery guide](database/README.md#widget-regionorder-upgrade-12) when installing the matching schema and application.

### Quick Local Deployment (Docker)

Copy the environment template to a local `.env`, set `POSTGRES_PASSWORD`, and run the command below. [Environment template](.env.example)

```bash
docker compose up -d
```

Compose starts PostgreSQL and configures the web container. Keep the database volume and both image volumes when replacing containers. Open `http://localhost:8080` for owner setup. [First run](#first-run)

## Development

Install the .NET 10 SDK and start PostgreSQL or SQL Server. The solution is `src/Moonglade.slnx`.

### Database Setup

Set `ConnectionStrings:DatabaseProvider` to `PostgreSql` or `SqlServer`. The default is PostgreSQL at `localhost:5432`. Set `ConnectionStrings:MoongladeDatabase` or override it with `ConnectionStrings__MoongladeDatabase`.

### Build & Run

```bash
dotnet run --project src/Moonglade.Web/Moonglade.Web.csproj
```

In PowerShell, `./start.ps1` uses the Development launch profile. Open `https://localhost:10210`. [Test commands](src/Tests/README.md#run-tests) · [Engineering reference](docs/agent-reference.md)

### First Run

Restrict access until setup is complete. Anyone who reaches an uninitialized site can claim it at `/auth/setup-account`. Enter the owner's email and password and verify the authenticator code when TOTP is required. Setup signs the owner in.

Passwords must contain at least 8 printable ASCII characters, including an English letter and a digit. Uppercase/lowercase combinations and symbols are optional; all printable ASCII symbols are accepted. Spaces and other whitespace are not allowed. Account setup, password changes, and authenticator resets use the same rule.

Pending setup redirects page requests to the setup page and returns HTTP 503 for business APIs, feeds, and protocol endpoints. Setup assets and health endpoints remain available. Hosts cache confirmed setup, so stop all hosts before restoring or replacing the database.

## Configuration

Configure host options in the settings file or with environment variables (`:` becomes `__`). Manage blog preferences at `/admin/settings` and the owner account at `/admin/account`. [Host settings](src/Moonglade.Web/appsettings.json)

### Saving Blog Settings

Switches and selections save immediately. Use **Save** for edits in **Change** dialogs and on the menu page. In script editors, **Clear** empties the editor and **Save** applies it.

| Setting | When it takes effect |
| --- | --- |
| Rendering, theme, and feed options | On subsequent requests, subject to external cache lifetimes. |
| FOAF and sitemap switches | On subsequent requests. Disabled endpoints return HTTP 404. |
| Watermark and original-image retention | On future uploads. |
| Notification switches | On future enqueue decisions. Queued email retains its recipient and content. |
| Notification sender display name | On subsequent delivery attempts, including for queued email. |
| Scheduled publishing | Saving wakes the worker. In-flight publishing completes. |

Coordinate runtime settings and cache invalidation across replicas. Both are process-local.

### Default Post Editor

Choose HTML (default) or Markdown for new posts under **Settings > Content > Default editor**. Existing posts retain their content type.

### Authentication

The owner and up to two collaborators use the same email and password sign-in. TOTP is required by default. Set `Authentication:Totp:Required` to `false` to disable TOTP during local sign-in, including for accounts with an enrolled authenticator. Sign-in and authenticator screens use the desktop layout. `Authentication:LocalAccountRateLimit` defaults to 10 attempts per minute, per client IP during setup and per account/IP during sign-in, TOTP verification, collaborator password changes and collaborator management.

Use **Remember this device** only on trusted devices. The browser's protected HTTPS cookie skips TOTP for 30 days and survives sign-out. Sign-in still requires the password. Changing the password or email, resetting the authenticator, or clearing the cookie ends device trust.

The owner manages collaborators under **Account Settings → Collaborators**. Creation and changes require fresh owner password and applicable TOTP proof. Give the collaborator their temporary password securely. It has no time-based expiry and must be changed before they can manage content. Account and credential changes revoke the affected collaborator's prior sessions and remembered devices. A disabled account still occupies a slot. Deleting it frees the slot and preserves its activity history. Collaborators can change their own password from the account menu. The owner handles lost passwords, lost authenticators and email changes.

Collaborators can manage all posts, pages, categories, tags and built-in comments, including scheduling, restoring recycled content, uploading article images and viewing commenter email addresses and IPs. The owner manages settings, menus, widgets, site assets, activity history, exports and account administration. Permanent deletion and emptying of post and page recycle bins are owner-only.

Treat collaborators as trusted publishers. They can use the existing HTML, embeds and per-page CSS. Content served on the blog origin can interact with a viewer's signed-in session. Give accounts only to people you trust with that publishing capability.

In the owner's **Account Settings**, email changes and authenticator resets require the current password. Email changes revoke sessions and retain TOTP. An authenticator reset ends local sessions so the owner can re-enroll. A password change refreshes the current session.

Data Protection stores keys in `DataProtectionKeys` (`data_protection_keys` on PostgreSQL). The stable `Moonglade` application name lets cookies, antiforgery tokens, and temporary proofs work across container replacements and replicas sharing the database. Sign in again after the first upgrade from container-local keys.

Under **Account Settings → Third-party sign-in**, configure one public HTTPS OpenID Connect provider, register the site's `/signin-oidc` callback, and confirm the owner binding. Choose a provider that honors `prompt=login` for reauthentication on each sign-in. A bound identity can sign in as the owner. Unbinding the account or deleting the provider revokes its Moonglade sessions. Unbinding retains the provider configuration for binding another identity.

Preserve the key table when backing up or restoring the database. It also holds the keys needed to decrypt the OIDC client secret under `Moonglade.OidcCredentials`. If the OIDC key is lost, save the client secret again in Account Settings. Restrict database and backup access and use encryption at rest to protect the stored keys.

### Comments

Configure review, keyword moderation, and Markdown links at `/admin/settings/comment`. Links appear as Markdown text by default on public and admin pages and in new emails. Enabled links use URL validation and `rel="nofollow ugc noopener noreferrer"`.

| Host option | Default behavior |
| --- | --- |
| `CommentRateLimit` | Allows five submissions per client IP and post in a ten-minute fixed window. |
| `CommentSubmissionGuard` | Checks a honeypot and rejects forms submitted before three seconds or after 240 minutes. Set `MaxFormAgeMinutes` to `0` to disable the age limit. |

Each section has an `Enabled` switch.

### Image Storage

Configure two writable, durable, absolute filesystem roots that do not overlap. Use shared storage for replicas.

| Option | Purpose |
| --- | --- |
| `ImageStorage:FileSystemPath` | Public processed images served at `/image/{filename}`. Compose mounts `/app/images`. |
| `ImageStorage:OriginalFileSystemPath` | Private originals when retention is enabled. Compose mounts `/app/images-origin`. |

Empty paths use `moonglade/images` and `moonglade/images-origin` under the user profile. Deploy with explicit persistent paths. Moonglade validates upload content and sanitizes SVGs.

With CDN delivery enabled, posts and feeds use CDN URLs and `/image/{filename}` redirects to the CDN. Expose only the public root, preserve filenames and media types, and publish only complete files. The owner avatar is served from account storage at `/avatar`. [Cloudflare R2 mount example](docs/cloudflare-r2-image-storage.md)

### Security HTTP Headers

Moonglade emits `X-Content-Type-Options: nosniff`. Set `EnableCSP` to `true` and supply `CSPValue` to add a Content Security Policy.

With OIDC and a CSP `form-action` directive, allow the provider's authorization endpoint origin alongside `'self'` so the browser can follow the redirect during account binding.

### Email Notifications

Configure SMTP in `Email` and restart. At `/admin/settings/notification`, set the sender display name, enable notifications, and send a test email. A blank display name blocks test sends and pauses queued delivery until a name is saved.

| Option | Purpose |
| --- | --- |
| `Enabled` | Enables email capability by default. Set to `false` to disable it explicitly. |
| `SmtpServer`, `SmtpPort` | SMTP endpoint. Port 465 uses implicit TLS. On other ports, `EnableSsl=true` requires STARTTLS and `false` uses STARTTLS when offered and otherwise allows an unencrypted connection. |
| `SmtpUserName`, `SmtpPassword` | SMTP credentials. Supply the password through deployment secrets, for example `Email__SmtpPassword`. |
| `SmtpSenderAddress` | Bare email address used in the sender headers and SMTP envelope. Blank values use `SmtpUserName`. |

Disabled or invalid SMTP configuration leaves the site online and pending mail in the database outbox. New notifications and test sends are unavailable until valid configuration is applied and the app restarts. Delivery is at least once and may duplicate a notification after interruption.

## Protocols & Standards

| Endpoint | Purpose |
| --- | --- |
| `/rss`, `/atom`, `/opml` | Feeds and subscriptions |
| `/opensearch` | OpenSearch discovery |
| `/foaf.xml` | FOAF profile |
| `/webmention` | Webmention reception |
| `/sitemap.xml`, `/robots.txt` | Crawler discovery and directives |

Incoming and outgoing Webmentions require public HTTP or HTTPS addresses. Site verification files are served at the URL root. SEO settings also control metadata and IndexNow.

## Health Checks

- `GET /health` checks application liveness. Compose uses this endpoint for the web container.
- `GET /health/ready` checks readiness, including database connectivity.
