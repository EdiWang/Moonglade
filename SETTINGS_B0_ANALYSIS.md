# B0 Settings Behavior and Compatibility Matrices

## Status and Reading Guide

- Date: 2026-11-05, Asia/Taipei.
- Original B0 authorization covered analysis and task records only. The user subsequently authorized B1 and B2 and confirmed D15's ordinary absent-field defaults; consult `SETTINGS_RELIABILITY_PLAN.md` for current implementation authority and progress.
- Source baseline reviewed: `f6407bd24978458af6f09c6b9cb4a22a667096e1`.
- Progress and authority: read `SETTINGS_RELIABILITY_PLAN.md` first. Confirmed user answers override recommendations below. Pending answers are not approval.
- Deliverable status: B0 inventory and startup decisions are complete; the user authorized proceeding to B1 and then B2. Source observations below describe the recorded pre-B1 baseline. B0 itself did not exercise destructive paths, real databases, or external email services; subsequent batch results are recorded in the tracker.

Notation: **Current** describes source behavior, **Confirmed** describes a user decision, and **Proposed** requires approval where it changes behavior. This is a task record, not shipped operational guidance.

## 1. Startup and Recovery Matrix

Current startup calls schema creation, infers newness from an empty settings table, clears application data before seeding, loads settings, writes defaults one group at a time, then builds site icons. These steps do not form one installation transaction. Settings checks alone therefore do not safely distinguish a failed installation from a damaged existing site.

| ID | Scenario | Current behavior / evidence | Target / decision state |
| --- | --- | --- | --- |
| S01 | First creation with no previous application data | Creates schema, clears tables, seeds content, saves settings separately. | Confirmed: preserve first installation and make it safe to retry. Seed data and default settings must commit atomically under the user-approved transaction approach. No clearing of unrelated existing data. |
| S02 | Existing supported schema, all application tables empty | Empty settings triggers seeding. | Confirmed D13: permit first-install initialization after proving every application table empty, including retry on an empty schema after a rolled-back installation. |
| S03 | Existing business data but settings table empty | Treats the database as new and enters the clear-and-reseed path. | Confirmed D5: fail normal initialization, preserve all data, and report manual recovery. Do not reconstruct accounts or run repair writes. |
| S04 | Other settings/data exist but account row is missing | Loader generates shipped account defaults. | Confirmed D5: fail normal initialization before repair writes; retain all rows and request manual recovery. Do not relax this to accommodate old test fixtures. |
| S05 | Account exists; ordinary group is missing | Loader assigns and persists that group's defaults. | Preserve the existing default-initialization path; distinguish a newly introduced group from corrupt existing JSON. |
| S06 | Ordinary group has malformed, incompatible, or null JSON | Replaces the group with defaults and writes its row, with a Warning for existing-row replacement. | Confirmed D2: continue automatic default recovery and Warning logging. Keep unrelated valid groups and timestamps unchanged. |
| S07 | Ordinary JSON is readable but semantically invalid, such as a zero page size | Loaded without shared business validation. | Proposed B3: apply approved semantic rules, then recover the entire invalid ordinary group and warn. List compatibility effects before implementation. |
| S08 | Account or manifest has malformed/incompatible/null JSON | Loader throws; default persistence has not yet begun. | Preserve protected-setting failure behavior. Do not replace credentials or installation history. |
| S09 | Valid other settings and account, but no manifest row | Currently inserts a manifest with unknown version and current time; content remains. | Confirmed by user during B0: preserve compatibility, insert the manifest, and emit Warning. Time represents reconstruction, not a claim about original installation. |
| S10 | Account/manifest is `{}` or partially populated | Non-null deserialization succeeds, even if values are unusable. | Q4 pending: define minimum supported protected shapes without rejecting legitimate old manifests or incomplete authenticator setup. No proposed default-credential recovery. |
| S11 | Successful installation followed by repeated startup | Loads saved groups without normal reseeding. | Confirmed idempotency requirement: preserve existing row identities, values, relationships, installation data, and valid timestamps; do not duplicate example content. |
| S12 | New implementation fails before installation transaction commits | Currently can leave seed content or a subset of settings committed independently. | Confirmed: roll back first-install data/default writes, log the failure, and allow a subsequent startup after dependency recovery. A first-install failure must not create a permanent manual-recovery barrier. |
| S13 | Commit may have succeeded, but its acknowledgment is lost | Existing retry structure has no explicit installation-completion verification. | Required by idempotency: re-read committed state before retrying writes; a completed installation must not be reseeded. Exact implementation remains B1 design work. |
| S14 | An older release already left partial content/settings without reliable installation provenance | Cannot reliably distinguish abandoned bootstrap data from a damaged established site. | Confirmed D14: preserve this state for manual confirmation; never automatically clear it. Safe retries remain required for installations under the new transactional implementation. Never infer ownership from welcome-post titles or low row counts. |
| S15 | A query fails while determining newness | Current check catches the error, assumes existing, and continues toward further initialization. | Proposed: do not classify inaccessible state as empty or authorize writes. Log a safe diagnostic and let startup be retried after dependencies recover. |
| S16 | Noncritical site-icon generation fails after committed installation | Errors are logged; startup can continue. | Preserve the noncritical boundary. Do not claim file generation participates in the database transaction. Subsequent generation remains independently retryable. |

Normal initialization failure currently produces an error-only host rather than necessarily terminating the process (`src/Moonglade.Setup/WebApplicationExtensions.cs`). The confirmed requirement prevents normal blog initialization and unsafe writes; changing process-exit/diagnostic-host behavior is outside B0 and should not happen incidentally.

An emptiness check must cover all 19 mapped application sets, not only posts and settings: Category, Comment, CommentReply, Post, PostCategory, PostTag, PostView, PostViewDaily, Tag, BlogPage, Mention, BlogTheme, StyleSheet, BlogAsset, BlogConfiguration, Widget, ActivityLog, EmailOutboxMessage, and SiteVerificationFile. A site with only pages, assets, custom themes, widgets, pending mail, or verification files is not an empty application database. A failed query or missing table is not proof of emptiness. Both supported provider contexts must be considered.

## 2. First-Installation Idempotency: Approved Direction and Limits

The user explicitly rejected a policy that permanently blocks a first-install retry after dependency failure. The user approved one database transaction for seed rows and all default-setting rows, rollback on pre-commit failure, and retry after dependency recovery. A persistent multi-step installer has not been approved.

Implementation constraints to carry into B1:

- Schema creation and data installation are different boundaries. Do not add schema migrations or a new persistent installer marker without approval. Check actual schema-creation failure behavior on both providers before claiming recovery from every possible creation failure.
- Both providers already enable EF execution-strategy retries. The installation transaction must be executed as a replayable unit using the provider strategy, not as a bare transaction wrapped around independently retried commands.
- `Seed.SeedAsync` currently retries recursively using the same context and then rethrows; adding an outer transaction without reviewing this path is insufficient. Each retry must avoid residual tracked inserts and duplicate seeds. Keep cancellation and useful diagnostics.
- A lost commit acknowledgment must not cause a blind second insertion. Verify database state using a clean read/context as appropriate; a full committed installation should load normally. Do not infer success merely from one ordinary setting.
- Do not publish rolled-back startup settings as a successful running configuration. A failed attempt must not make normal endpoints/background work consume an uncommitted account/configuration. This does not authorize the optional general immutable-snapshot refactor.
- Do not reset category identity allocators to hide gaps from failed attempts. Database-generated identities remain supported; idempotency means no duplicate committed installation, not reusable identity numbers.
- Site-icon filesystem work stays outside the installation-data transaction. The transaction does not make file operations atomic.
- An unrecoverable external dependency can fail the current startup with console diagnostics, but must not prevent a later safe retry. Automatic indefinite background installation retries or serving a partially configured blog are not approved requirements.

Reference checked during B0: EF documents explicit transaction/retry coordination and uncertain commit outcomes in [Connection Resiliency](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency), and multi-save atomicity in [Transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions). These support the proposed mechanics; they do not prove the repository's implementation correct.

## 3. Settings Group Catalogue

For this table, unlisted reference values default to null, booleans to false, and integers/enums to zero unless a property initializer says otherwise. Values below describe code defaults, never actual installation data. Account hashes, salts, and secrets are deliberately omitted.

| Group | Whole-group defaults / important initialized values | Current entry points and principal readers |
| --- | --- | --- |
| GeneralSettings | Shipped owner/site/profile metadata; widgets profile/category/subscription enabled; HotTagAmount=10; DefaultLanguageCode=en-us; SideBarOption=Right; copyright uses current UTC year. | POST general; image/avatar paths also update AvatarUrl. Read by layouts/profile/metadata, post editor defaults, tags, mail recipients, feeds, manifest, FOAF, OpenSearch, sitemap. |
| ContentSettings | DefaultEditor=Html (1), PostListPageSize=10, PostTitleAlignment=Left (0), CalloutSectionHtmlPitch=empty, DocumentOutline=true, EnableViewCount=true, MaximumPageNumbersToDisplay=5. Other switches false. | POST content; reads in article/list/search/category/tag/featured pages, post editor and view-count handling. |
| CommentSettings | EnableComments=true, EnableGravatar=true, RequireCommentReview=true; Markdown links and word filtering disabled; CloseCommentAfterDays=0; provider BuiltIn, order OldToNew, filter Mask; shipped keywords. | POST comment; reads in comment controller/feature handler, local moderation provider, public/admin comment rendering, generated comment mail. |
| NotificationSettings | Sending and both notification switches false; EmailDisplayName=Moonglade. | POST notification; controllers/events determine future enqueueing; EmailSettings singleton captures display name; capability state depends on separate deployment configuration. |
| FeedSettings | FeedItemCount=20; UseFullContent=false. | POST subscription; SyndicationDataSource; public RSS/Atom response caches. Explicit count 0 means unlimited. |
| ImageSettings | IsWatermarkEnabled=true, KeepOriginImage=false, WatermarkFontSize=20, WatermarkText=Moonglade, WatermarkColorA=128, WatermarkSkipPixel=40000; CDN disabled. | POST watermark (image settings); upload transformation/original retention; image redirects; Razor and feed content URL rewriting; avatar/CDN event path. |
| AppearanceSettings | ThemeId=100, EnableCustomCss=false, CssCode=empty. | POST appearance; shared theme cache used by public/admin/auth pages; /custom.css reads current code. Unknown custom theme IDs currently fall back to the default theme. |
| AdvancedSettings | Webmention, OpenSearch, FOAF, OPML, sitemap, scheduler, comment external-link warning, and JSON-LD enabled; post external-link warning disabled. Robots/scripts initially null. | POST advanced; startup route mapping, per-request protocol gates, post-management events, layouts, background scheduler. |
| CustomMenuSettings | Enabled; one About menu pointing to /page/about, icon bi-star, display order 1, no submenus. | GET/POST custom-menu; MenuViewComponent and menu Razor. A plain Menu instance instead initializes icon-file-text2 and an empty submenu list. |
| LocalAccountSettings | Shipped bootstrap credentials for genuine installation only; empty TOTP secret; TOTP disabled. Exact credential material is not reproduced here. | Password update, authenticator reset/setup; login and verification pages. Not an ordinary group eligible for credential reset on corruption. |
| SystemManifestSettings | Existing-install fallback: VersionString=0.0.0.0, InstallTimeUtc=current UTC time. New install: current app version and current UTC time. | Startup loader/default persistence; no normal runtime writer found outside configuration infrastructure. Existing valid values take precedence. |

All ordinary groups use `UpdateConfigurationCommand.Create` through the settings controller. Additional writers are `SaveAssetToCdnHandler`, image-settings paired saves, and `SetupAuthenticatorModel`. All groups load through `BlogConfig.LoadFromConfig` and `ConfigInitializer`. No administrator-editable manifest endpoint was found.

## 4. Complete Top-Level Default Divergence Inventory

These are the differences found between a missing group (uses `DefaultValue`) and an existing object with the named field absent (uses a new object's property initialization). This comparison concerns top-level settings fields; explicit saved values must never be overwritten merely to align defaults.

| Group / fields | Field absent in an existing object | Whole group missing | Compatibility effect / recommendation |
| --- | --- | --- | --- |
| General: MetaKeyword, LogoText, Copyright, SiteTitle, OwnerName, OwnerEmail, Description | null | Shipped metadata; copyright includes current year | B2 decision required: the new default source would fill only absent fields; required null fields may otherwise cause B3 to reset the whole group. Do not overwrite supplied metadata. |
| Content: CalloutSectionHtmlPitch | null | Empty string | Mostly representation; confirm as part of the single-source policy. |
| Comment: EnableGravatar, RequireCommentReview | false | true | Behavioral: old partial JSON could begin showing avatars or requiring approval after alignment. Ask before changing effective missing-field defaults. Explicit false stays false. |
| Notification: EmailDisplayName | null | Moonglade | Affects sender-name fallback and its captured singleton value. Ask before changing absent-field behavior. |
| Feed: FeedItemCount | 0 (unlimited) | 20 | Behavioral: an old object without count could change from unlimited to 20. Explicit 0 remains unlimited. |
| Image: IsWatermarkEnabled, WatermarkFontSize, WatermarkText | false, 0, null | true, 20, Moonglade | Behavioral and validation impact: watermarking could turn on for a partial object. Explicit disabled remains disabled. |
| Appearance: CssCode | null | Empty string | Mostly representation. |
| Advanced | No difference found | Same property initializers | Preserve values and defaults. |
| CustomMenu: IsEnabled, Menus | false, empty array | true, one About menu | Behavioral: an existing partial menu object could gain a menu. Explicit false or an explicitly empty list must remain intact. |
| Account: Username, PasswordHash, PasswordSalt | null | Shipped bootstrap credentials | Never solve this protected-state difference by defaulting old missing credential fields. Define supported protected shapes in Q4. |
| Manifest: VersionString, InstallTimeUtc | null, minimum DateTime | Unknown/current version and current UTC time, according to install context | Do not manufacture historical values for an existing partial manifest without an approved rule. The confirmed missing-row reconstruction rule does not automatically authorize rewriting partial rows. |

Q2 was explicitly confirmed during B2 as D15: use the existing whole-group defaults for genuinely absent ordinary fields, preserve every explicit value (including false/zero/null/empty arrays), and do not rewrite valid stored JSON merely to normalize representation. The user accepted the ordinary behavioral changes listed above. The table remains a record of the pre-B2 differences; account/manifest handling is separate and protected-shape validation remains Q4.

## 5. Current Validation and Proposed Gates

These are observed constraints, not blanket approval to apply request metadata to all historical persisted objects. Current startup loading and the shared update handler do not run a complete semantic-validation pipeline. [DataType] metadata on URLs/email must not be mistaken for a complete business validator.

| Group | Current HTTP/model constraints | B3 compatibility gate |
| --- | --- | --- |
| General | Required metadata/name/title/email/description/language; lengths: MetaKeyword and Description 256, CanonicalPrefix and Copyright and OwnerEmail and AvatarUrl 64, LogoText and SiteTitle 16, OwnerName and Pronouns 32; HotTagAmount 0-20; copyright character expression; locale expression. | Generated CDN avatar URLs can exceed 64 even with an accepted endpoint. Reconcile generated-field ownership/length before shared validation. Do not add URL/email/culture policies silently. |
| Content | DefaultEditor has EnumDataType; PostListPageSize 5-30; MaximumPageNumbersToDisplay 2-10; CalloutSectionHtmlPitch max 2048. | Title-alignment enum has no equivalent declared enum validator; footer HTML has no declared length limit. Preserve the supported editor values 1 and 2. |
| Comment | CloseCommentAfterDays 0-65536; third-party markup max 1024; keyword string max 4096. | Provider/order/filter enums need a defined-value decision. Keep 0 as the current no-age-limit representation. Do not alter moderation policy while adding validation. |
| Notification | EmailDisplayName required, max 64. | Empty legacy names currently fall back to deployment SenderDisplayName in the mail singleton; do not erase that compatibility by applying Required mechanically. |
| Feed | No declared count range. | Recommend nonnegative count with 0 remaining unlimited; any positive upper bound is a new policy and needs approval. |
| Image | Font 8-32; watermark text required/max 32; alpha 0-255; pixel threshold 0-int.MaxValue; endpoint max 128 and HTTPS required when CDN enabled. | Existing IValidatableObject mutates EnableCDNRedirect during validation. Remove mutation in a later implementation; define disabled-watermark and generated-avatar treatment without changing accepted values by accident. |
| Appearance | CSS max 10240 characters; controller requires nonblank CSS when enabled. | Serving limits custom CSS to 10240 UTF-8 bytes, which differs from character count. Existing theme fallback must not accidentally become a rejection of missing custom theme IDs. Ask before changing either policy. |
| Advanced | Robots max 1024, head/footer script fields max 4096; controller restricts nonblank script content to script blocks. | Direct writes/load bypass controller-only rules. Reuse approved restrictions without expanding what administrators are allowed to render. |
| CustomMenu | Request MenuJson max 1024; enabled requests require content; malformed/null parsed arrays rejected. Domain Menus has max 5, but parsed objects are not revalidated here. | Define limits and null handling for menus/submenus; do not invent title/URL lengths or submenu caps. Disabled/empty-menu requests need compatible handling. |
| Account | No data annotations on persisted account model. Separate password-change/reset request models have their own validation and old-password checks. | Do not apply password-change request restrictions to historical persisted accounts. Recommend rejecting unusable credentials and enabled TOTP without a valid secret; retain legitimate disabled/setup states. Exact rules pending Q4. |
| Manifest | No declared semantic validation; valid old version strings are preserved. | Existing tests preserve historical manifests, but null-version test input actually omits the entire row. Do not claim it proves a partial persisted manifest valid. Define partial-row support separately. |

Whole-group semantic recovery can discard otherwise usable sibling values. Before implementing B3, show examples such as a valid site title plus an invalid page-size field in its own group, a long generated avatar URL, missing optional watermark text while disabled, and a legacy blank email display name. User approval of automatic defaults is not approval to invent stricter rules that classify those values as corrupt.

## 6. Activation, Caches, and Writer Coverage

| Settings / field family | Actual reader behavior | Proposed B4 treatment / open point |
| --- | --- | --- |
| General rendering, profile, tags, default author, language metadata | Usually reads current settings on the next request/editor initialization; feed XML and canonical sitemap output can remain cached. Layouts currently also fill an empty AvatarUrl by mutating the singleton without persistence. | Invalidate affected server outputs after commit; preserve existing author/content on already-created posts. Record the layout mutation as deferred C1/C3 impact, not permission for broad refactoring. |
| Content display and default editor | Razor/current request/editor initialization; post cache stores entities rather than whole rendered HTML. | Confirm changes on new requests and new posts. Do not change existing post content types or flush every cache without a dependency reason. |
| Comment approval, moderation, ordering, link rendering | Future submissions/current render paths consult current settings. Previously approved comments and generated outbox bodies are not automatically rewritten. | Keep future-operation semantics; do not add historical re-moderation or rewrite queued mail. |
| Notification enable/which-event flags | Controllers/events consult live settings before new enqueueing. Existing queued messages are retained and delivery does not become retroactively canceled. | Label the switches accurately; do not silently change outbox retention/delivery policy. |
| Notification EmailDisplayName | Captured when the singleton EmailSettings is first resolved, and then used by the singleton sender. | Not reliably live after save. Recommend documenting restart-required behavior to minimize scope; dynamic updating would require a separate user choice. |
| Feed count/full-content flag | RSS/Atom results cached for one hour, including category variants. | Invalidate affected feed generations after successful save. Full-content generation currently selects ContentAbstract before formatting; track the source-selection concern separately rather than assuming cache work fixes it. |
| Image watermark/original-retention options | Read during subsequent upload processing. | Existing stored image bytes are not reprocessed. Verify future uploads only unless scope changes. |
| Image CDN and avatar addresses | Current render/redirect paths read settings; feed XML is cached; redirect/browser/CDN responses may be retained downstream. | Invalidate affected server feeds; keep paired image/general saves and avatar writers in scope. Do not promise downstream cache eviction or file/database atomicity. |
| Appearance theme | Shared General/theme cache, normally 20 minutes; appearance endpoint removes it through a filter. | Centralize successful-save invalidation and cover auth/admin/public readers. The existing filter also runs on failed actions; ensure failure tests distinguish harmless invalidation from correct post-commit effects. |
| Appearance custom CSS | /custom.css reads current settings, emits ETag, and permits one-hour client caching; server applies a UTF-8 byte limit. | Preserve cache validators and explicitly distinguish browser freshness from server generation. No automatic global client-cache purge. |
| Advanced FOAF / sitemap | Endpoint existence selected at startup. FOAF links can reflect current settings sooner than endpoint registration. | Keep restart-required contract for both, with clear save/restart wording. Do not silently make endpoints dynamic. |
| Advanced OpenSearch / OPML / robots / scripts / external-link warnings / JSON-LD / Webmention | Primarily per-request or per-event reads. OpenSearch and FOAF advertise one-hour downstream caching; robots advertises one day. | Keep next-request/event semantics and external-cache limits. Preserve protocol URLs and security. |
| Advanced scheduler | Flag checked at top of loop. Disabled loop sleeps five minutes. A worker already waiting for a scheduled time does not recheck the flag before invoking publication. | Q5 subdecision: choose whether to preserve/document delayed behavior or make disabling prevent the next pending publication. This is a real behavioral distinction; do not silently decide it during cache work. |
| Custom menu | Component reads current menu settings; no separate menu response cache found. | Verify next render; parsed-object validation belongs to B3. |
| Account / manifest | Account read by login/authenticator paths; current reset signs out relevant cookies. Manifest used by startup infrastructure. | Existing cookie/session policy is not changed merely because credentials are saved. Manifest is not a normal settings page. |

Writer checklist for B3-B5: ordinary controller actions; General's avatar preservation; Image's paired ImageSettings/GeneralSettings update; SaveAssetToCdnHandler's GeneralSettings update; password update; authenticator reset; SetupAuthenticatorModel's secret-generation/enabling saves; startup default persistence as a distinct path. Keep successful database saves and post-save audit outcomes separate.

## 7. Proposed B1 Acceptance Cases

All cases below are test/acceptance plans, not results. Fault injection belongs only in disposable databases or controlled copies.

| Case | Expected outcome / evidence |
| --- | --- |
| A01 | Clean installation commits one coherent seed and all 11 settings groups; account and new-install manifest are usable. |
| A02 | Restart twice after success: no duplicate example rows, no changed content/IDs/valid timestamps, no changed install metadata. |
| A03 | Existing post/comment/page/asset data with no settings: initialization fails before data/default writes; compare exact retained values and relationships. |
| A04 | Existing data/settings but missing account: same preservation guarantee; no default-account generation. |
| A05 | Valid account and ordinary settings but no manifest: preserve content and valid timestamps, reconstruct only the missing manifest, warn, and label the time as reconstruction. |
| A06 | Each other mapped set is the sole nonempty set: it must prevent classification as a wholly empty application database; no clearing. |
| A07 | Existing wholly empty supported schema: permit initialization under D13 only after checking every application set; retry after a rolled-back new installation must succeed once dependencies recover. |
| A08 | Inject a failure after seed SaveChanges and during early/middle/late default-setting writes: the installation data transaction does not leave partial committed installation rows; restore the dependency and restart successfully. |
| A09 | Failure before commit and process restart: existing empty schema does not permanently block reinstallation; no duplicate content. Do not require identity allocators to reuse failed-attempt values. |
| A10 | Commit acknowledgment lost: reconstruct state through clean verification, skip reseeding when the installation committed, and do not generate duplicates on provider retry or restart. |
| A11 | Existing initialized database with one corrupt ordinary group and valid protected groups: repair that group only; do not run install seeding. |
| A12 | Query/schema-access failure: no inference of empty state, no destructive write, safe console diagnostic, later retry possible after recovery. |
| A13 | Site-icon generation fails after installation commits: preserve existing noncritical behavior; do not roll back or duplicate committed seed data. |
| A14 | Protected malformed JSON: failure before repair writes. Ambiguous older partial-install state is preserved for manual confirmation under D14, with no automatic clearing. |

The current `StartUpInitializerTests.InitStartUpAsync_ExistingDatabase_PreservesContentAndManifest` seeds ContentSettings and an optional manifest but no account row. Under D5, its fixture must gain a valid account before it can continue proving missing-manifest/content compatibility. Do not preserve account auto-recreation merely to keep this fixture passing. Separate focused tests should exercise the new missing-account rejection.

B1 requires Setup tests, relevant Configuration tests, Web build, and actual relational transaction/retry coverage on PostgreSQL and SQL Server. InMemory cannot establish rollback or provider retry correctness. If an environment is unavailable, record the gap and obtain an explicit acceptance decision; do not label the relational cases passed.

## 8. B0 Questions, Evidence, and Handoff

| Question | Status, including subsequent user decisions |
| --- | --- |
| Q1: missing manifest with valid other settings/account | Answered: reconstruct it without touching business data and log Warning; version unknown and time means reconstruction. |
| Q2: ordinary absent-field default alignment | Resolved during B2 as D15: absent ordinary fields use whole-group defaults; explicit values are retained; protected state is excluded. |
| Q3: new semantic constraints | Resolved during B3 as D16-D17: shared existing constraints plus the approved enum/feed/menu rules and compatibility exceptions; ordinary invalid state recovers the whole group. |
| Q4: minimum/legacy protected shapes | Resolved during B3 as D18: nonblank credential fields, parseable TOTP with enabled-secret requirement and disabled setup support, non-minimum UTC manifest time, and arbitrary/empty version strings preserved. Invalid protected state stops before repairs. |
| Q5: activation classifications | Observed in section 6; EmailDisplayName and scheduler behavior need explicit B4 choices. Does not block B1. |
| S02: pre-existing wholly empty schema | Answered: initialization is allowed after all application tables are confirmed empty (D13). |
| First-install dependency failure | Answered: installation must be idempotent and retryable after the dependency is fixed; console errors are sufficient for the failed attempt. |
| Atomic transaction direction | Answered: use a database transaction; roll back pre-commit failure and retry. |
| S14: older partial-install compatibility | Answered: preserve ambiguous old partial state for manual confirmation; do not automatically clear it (D14). |

Evidence collected: source/model annotations, request handlers, all 11 group defaults, provider registration/retry options, startup/seed/default-write boundaries, mapped application sets, existing startup/configuration tests, frontend read paths, protocol/cache handlers, email singleton construction, scheduler loop, and official EF transaction/retry guidance.

No application tests, browser session, real database connection, SMTP send, seeding, cleanup, or destructive operation was run for B0. The matrices establish source observations and proposed acceptance cases, not runtime proof. Existing earlier test counts remain historical only.

Handoff update: B1 is commit `7f0394673`, B2 is `d51230912`, and the user explicitly authorized B3 and resolved Q3-Q4 as D16-D18. Follow current B3 verification/commit/acceptance status in `SETTINGS_RELIABILITY_PLAN.md`. Q5 remains open for B4; B4 and later batches are not authorized. Earlier source matrices remain the historical B0 baseline.
