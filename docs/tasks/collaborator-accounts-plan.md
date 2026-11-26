# Collaborator Accounts Plan and Progress

## Current Status

- B1 through B3 are accepted. B4 developer verification is complete on branch `codex/collaborator-accounts`. B4 owner acceptance is pending.
- Planning baseline is `fc2050cc6`. Recheck the current branch, working tree and next migration number when implementation starts.
- Use this file as the source of truth for the current requirements, batch status and acceptance evidence. Replace superseded decisions and obsolete blocker details. Keep conversation records and rejected alternatives out of this file.

| Batch | Scope | State | Commit | Verification and acceptance |
| --- | --- | --- | --- | --- |
| B1 | Unified account store and two-provider upgrade | Accepted on 2026-11-26 | `c8ffabb7300bb0d0cedfa22f21070c9272de6004` | Passed both providers and development owner sign-in |
| B2 | Authorization, content access and activity attribution | Accepted on 2026-11-26 | `cc5eecc8cd561a0a32ae1080c9fd0ccaa2fbdc4d` | Developer verification passed. Owner accepted B2 |
| B3 | Shared sign-in and credential lifecycle | Accepted on 2026-11-26 | `57b09f3e1481c62e2f07cf6349aade9db492d7ab`, `79a99b448e67488c6335ac347bac519e224a9866` | Developer verification and owner acceptance passed |
| B4 | Owner account management and collaborator UI | Awaiting owner acceptance | `Add owner management and collaborator navigation` | Developer verification passed. See B4 acceptance steps |
| B5 | Release verification and operator documentation | Awaiting B4 acceptance | Pending | Pending |

## Confirmed Behavior

### Accounts and sign-in

- A site has one owner and zero to two collaborator accounts in a unified account table. The owner keeps the existing authority, identity, public profile and OpenID Connect binding. Both collaborators have the same content permissions across the whole site, including content owned or created by someone else.
- The owner creates, disables, re-enables and deletes collaborators, changes their email addresses, and resets their passwords and authenticators. Disabled accounts still occupy a slot. Deletion releases the slot and retains the account's historical activity. The owner account is outside this management lifecycle.
- The owner sets a temporary password on creation and password reset. It has no time-based expiration and can only enter a restricted password-change flow. The collaborator chooses a permanent password before receiving content access. Expiring intermediate authentication tickets still apply. The owner handles forgotten passwords, email changes and lost authenticators. Collaborators can change their own passwords.
- Both roles enter through the existing email sign-in page and follow the site's existing TOTP setting and remembered-device rules. OpenID Connect sign-in and management are owner-only. Account management mutations require fresh owner password proof and applicable TOTP proof, including when the owner's session began through OpenID Connect.
- Sign-in, mandatory password change, and the authenticator enrollment and verification screens use desktop layouts.
- Each account has a stable identity and its own session revision. Password resets, email changes, authenticator resets, disablement and deletion revoke that account's prior application sessions, pending authentication and remembered-device trust on the next validation. Re-enabling or creating another account in a freed slot does not revive earlier credentials or cookies. Preserve the existing owner session behaviors for owner self-service operations.
- Account email is unique after normalization for all three slots. Account management and role decisions use the persisted identity rather than user-supplied role or target claims.

### Permission matrix

| Surface | Owner | Collaborator |
| --- | --- | --- |
| Posts and drafts | Full existing access | List, preview, create, edit, manually publish, schedule, cancel, reschedule, unpublish, recycle and restore any post |
| Pages | Full existing access | List, preview, create, edit, publish, unpublish, recycle and restore any page. Use the existing HTML, embedding and per-page CSS editing features |
| Post and page recycle bins | View, restore, permanently delete and empty | View and restore |
| Categories and tags | Full existing access | List, create, edit and delete using the existing rules |
| Built-in comments | Full existing access | List, view visitor email and IP, approve or withdraw approval, reply and delete, including the existing supported selection actions |
| Article images | Existing upload and public delivery | Use the existing article editor upload. Public delivery follows existing access rules |
| Personal credentials | Existing owner account settings and security flows | Change own password and complete applicable sign-in, forced password change and authenticator enrollment flows |
| System and site management | Owner-only | Access denied with a collaborator session |
| Public routes and anonymous protocol entry points | Existing public behavior | Existing public behavior |

System and site management includes Dashboard and analytics, site-wide settings and CSS, navigation menus, widgets, themes, Webmention administration, activity history, data export, site verification and assets, owner profile and avatar settings, OpenID Connect configuration and collaborator administration. Preserve the existing public Webmention receiver, public comment submission, public assets, feeds, health and static-file behavior.

### Content editing, privacy and activity

- Existing page and article editing, embedding, HTML rendering, previews and per-page CSS behavior apply to collaborators. Preserve legacy content and existing owner capabilities. The content workflows use the same save behavior for both roles, with existing post status and database concurrency checks. Browser form-version conflict detection is outside this release.
- Treat collaborator accounts as trusted content publishers. The application denies direct owner-only operations under collaborator credentials. Existing raw content rendered on the blog origin can execute in a viewer's browsing context and can affect a signed-in owner's session. Full editing parity accepts this risk. Embedding allowlists, new markup or CSS restrictions and preview isolation changes are outside this release. [HTML Standard on iframe sandboxing](https://html.spec.whatwg.org/multipage/iframe-embed-object.html)
- The existing public post author field remains manually editable. Derive neither its default value nor public reply attribution from an account's name or email. Preserve public presentation, subscriptions, notification recipients and reply delivery behavior.
- Owner-visible history distinguishes the owner, collaborators and the system. Record the stable actor account ID and display-name snapshot for new account-attributed events. Historical actor identity survives renaming, deletion and slot reuse. Keep legacy events readable and scheduled publications attributed to System. Keep passwords, authenticator secrets, codes, cookies and proof material out of events, logs and diagnostics.

## Repository Map

| Area | Starting points |
| --- | --- |
| Repository rules | [AGENTS.md](../../AGENTS.md), [engineering reference](../agent-reference.md), [setup and authentication](../../README.md#authentication) |
| Persistence and startup | `Moonglade.Data/BlogDbContext.cs`, `BlogAccountEntity`, `BlogAccountConfiguration`, `OwnerOidcEntity`, `Moonglade.Data.PostgreSql`, `Moonglade.Data.SqlServer`, `Moonglade.Setup/StartUpInitializer.cs` and the [database upgrade guide](../../database/README.md) |
| Auth and owner identity | `Moonglade.Auth` owns `OwnerAccountStore`, `LocalAccountPrincipal`, `LocalAccountCookieEvents`, `BlogAuthSchemas`, `OwnerAvatarStore`, `IOwnerProfileReader` and the `Oidc` folder |
| HTTP and UI | `Moonglade.Web` owns `Controllers`, `Pages/SignIn.cshtml.cs`, `Pages/*Authenticator.cshtml.cs`, `Pages/Admin`, shared admin layout/navigation, foreground edit links, `Extensions/ServiceCollectionExtensions.cs`, `Extensions/WebApplicationExtensions.cs`, and local setup/rate-limiting services |
| Business and activity | `Moonglade.Features` owns content mutations. Check Web's `PostManagementCommands`, `BlogControllerBase` and `ActivityLogDispatcher` and the event catalog and persistence in `Moonglade.ActivityLog` |
| Tests | [Test commands and owner account acceptance](../../src/Tests/README.md#owner-account-release-acceptance). Use Auth, Setup, Web and Features suites under `src/Tests`, plus frontend behavior tests and real browser runs |

Project paths in this table are under `src/`. Read the owning code, its callers and tests before editing. Follow the repository's .NET 10, provider, localization and file-format conventions.

## B1 — Unified Account Store and Upgrade

- Deliver the unified account model and owner compatibility path together. Enforce one owner slot, two collaborator slots and unique normalized emails durably on both providers. Represent disabled and forced-password-change state. Use stable IDs and optimistic revision updates. Verify that the database constraints hold under concurrent slot claims, duplicate email writes and slot reuse on both providers.
- Inventory every current owner-account consumer. The owner setup gate, public profile reader, owner avatar and OIDC management must continue to resolve the owner even when collaborator rows exist. Include fresh database creation, seeding and the existing initializer/cleanup paths in the review.
- Add paired, manual, transactional and idempotent upgrade scripts using the next migration number. Migrate the existing owner row with its ID, normalized email, password hash and salt, enrolled authenticator, local-initialization state and revision intact. Preserve the existing owner profile and avatar data. Preserve the OIDC foreign key to the same owner identity, binding and session revisions, protected client secret and Data Protection keys. Preserve unrelated rows and settings timestamps. Add a nullable activity actor-ID snapshot in the same upgrade. Legacy rows retain their existing attribution and the snapshot survives account deletion.
- Verify with Auth, Setup and Web tests and a Web build. In disposable PostgreSQL and SQL Server databases, exercise fresh schema initialization, upgrade a representative initialized owner, and upgrade a profile-only owner where local setup is still pending. Include a bound OIDC installation. Run each upgrade twice and again after later account writes. Verify backup restoration and owner login, setup gating, profile, and OIDC compatibility.
- Owner acceptance walks through the upgrade evidence and sees the existing owner sign in with the preserved account. A fresh installation still reaches and completes initial owner setup. Collaborator provisioning is introduced in B4.

### B1 verification and acceptance

- Developer verification passed on 2026-11-26. `dotnet test --solution src/Moonglade.slnx --no-ansi` passed all 2,176 tests. The Release Web build and publish succeeded.
- PostgreSQL 18.7 and SQL Server 2022 (16.0.4325.4) passed on separate disposable databases using the previous and updated published Web applications over loopback HTTPS. Each provider covered a fresh install, an upgrade from an initialized Local/TOTP owner with a bound synthetic OIDC configuration, and an upgrade from a profile-only owner with local setup pending.
- Both providers passed native backup and restore before and after upgrade. Script 13 preserved the account ID, credential state and revision, OIDC binding and session revisions, Data Protection keys, settings timestamps, content and existing activity. Repeated runs after the upgrade and after an owner email change preserved current values. The restored owner cookie and TOTP sign-in worked, and the stored OIDC client secret decrypted with the restored key ring. The Web suite covers synthetic provider exchanges. Live provider sign-in is reserved for separate acceptance.
- Both providers allowed one of two concurrent claims for the same collaborator slot, rejected a duplicate normalized email and a third collaborator slot, and retained the old actor ID when a disabled account was deleted and its slot reused.
- The configured development PostgreSQL database passed upgrade and a second run of script 13 on 2026-11-26 after native backup and restore rehearsal. Post-upgrade comparisons verified the owner account, OIDC binding, owner presentation, settings, Data Protection keys and activity history.
- The owner signed in successfully after the development upgrade and accepted B1 on 2026-11-26.

## B2 — Authorization, Content Access and Activity

- Map endpoint and Razor Page authorization from the matrix. Keep privileged surfaces owner-protected by default and explicitly grant matching shared permissions to content surfaces. Check inherited attributes and folder conventions, since policies combine. Separate mixed operations by action where necessary, especially recycle-bin restore versus permanent deletion, image upload versus public image delivery, and public comment submission versus moderation. [ASP.NET Core policy combination](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/policies?view=aspnetcore-10.0)
- Audit current authenticated-only paths as well as `BlogControllerBase` descendants. `DataPortingController` and the administrative actions in `MentionController` currently rely on authentication without a named role policy. Check each verb and direct route, including alternate links and page previews. Preserve existing antiforgery and response conventions for controllers and Razor Pages.
- Update content activity attribution at both `BlogControllerBase` dispatch and the post-management path. Preserve existing event keys, detail validation, best-effort postcommit audit and cache/publishing effects. Keep existing author fields, preview and render behavior, comment payloads, reply notifications, and global settings semantics.
- Verify with Web HTTP tests using representative authenticated owner and collaborator principals and an anonymous client, plus affected Features tests and a Web build. Use valid antiforgery proofs when testing denial so the response verifies authorization. Cover direct requests across the complete matrix, public routes, side effects, stable actor IDs and legacy events.
- Owner acceptance reviews the route matrix and test evidence. With synthetic accounts in a disposable host, demonstrate collaborator content access and direct API denial for settings, exports, Webmention management, activity and permanent deletion.

### B2 verification and acceptance

- Developer verification passed on 2026-11-26. `dotnet test --solution src/Moonglade.slnx --no-restore --no-ansi` passed all 2,178 tests, including the Features and Web suites. `node --test src/Tests/Moonglade.Web.Tests/JavaScript/*.mjs` passed 91 tests. The Release Web build and publish succeeded.
- The Web test host exercised 136 method and path combinations with synthetic owner, collaborator, authenticated reader and anonymous identities using the production Razor Pages authorization registration and valid antiforgery proofs. The tests covered both recycle bins, content previews, owner-only pages and APIs, public routes and the intentional public comment, Webmention and image entry points.
- An HTTP integration test verified that a collaborator edited an owner-created draft, created a category and saw comment email and IP fields. Permanent post deletion and activity log clearing were denied with the existing rows preserved. Owner-visible history returned collaborator account IDs and display-name snapshots. The public byline and legacy activity remained readable.
- Verified the published Release site against disposable PostgreSQL in Playwright Chromium at 1440 by 900 CSS pixels. A signed-in owner opened collaborator and legacy activity details in English and checked the collaborator label and account ID in Traditional Chinese. The activity pages reported no browser console errors or warnings. Playwright CLI was used because the Browser plugin and skill were unavailable.
- The owner accepted B2 on 2026-11-26. The disposable HTTP host reproduces the authorization and content-write checks with `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --filter-class '*CollaboratorAccessHttpTests'`.

## B3 — Shared Sign-In and Credential Lifecycle

- Route email sign-in to the correct account and issue application credentials using its stored role, stable ID and applicable display-name claim for activity attribution. Validate ID, role, enabled state, revision and applicable TOTP at the authentication boundaries. Keep initial site setup owner-specific. Reuse existing cookie and proof mechanics where they fit the account model. Apply existing account/IP rate limits to the corresponding sign-in and authenticator steps.
- Handle temporary password authentication using a restricted ticket. Finish the applicable password-change and TOTP flow before issuing a content session. Bind pending and remembered-device credentials to one account and revision. Preserve the owner's existing OIDC sign-in, session verification, reset and password-change behavior. Derive collaborator self-service target identity from the current session.
- Implement the Auth operations for creation, disablement, re-enablement, deletion, email change and resets. Keep web binding and owner proof orchestration in Web when the management surface is introduced in B4. Validate concurrent retries and stale revisions so failed mutations avoid partially changing account or session state.
- Verify with Auth and Web suites and a Web build. Cover valid and invalid credentials, temporary password replacement, TOTP required and optional, remembered devices, cross-account cookie rejection, revision changes, self-service boundaries, session revocation and unchanged owner OIDC/Local behavior.
- Owner acceptance uses test-provisioned accounts to exercise shared sign-in, mandatory first password change, applicable TOTP, sign-out and ordinary self-service password change.

### B3 verification and acceptance

- Developer verification passed on 2026-11-26. `dotnet test --solution src/Moonglade.slnx --no-restore --no-ansi` passed all 2,190 tests. The JavaScript suite passed 91 tests. The Release Web build and publish succeeded.
- Authentication tests exercised the shared sign-in and forced password change with TOTP required and optional, protected ticket expiry and replay, account/role matching, remembered-device isolation, self-service password changes and revision-based session revocation. The full suite also exercised the existing owner Local and synthetic OIDC flows.
- Production Auth stores on disposable PostgreSQL and SQL Server both passed concurrent creation with distinct emails, concurrent duplicate normalized-email rejection, and competing writes against a single revision.
- The desktop sign-in layout passed all 890 Web tests and a Release publish. Playwright Chromium verified sign-in, temporary password change, TOTP enrollment, and returning sign-in with TOTP on disposable PostgreSQL at 1440 by 900 CSS pixels. All four forms rendered at 365 CSS pixels and the collaborator reached Posts. The final page reported zero console errors or warnings. Playwright CLI was used because the Browser plugin and skill were unavailable.
- For owner acceptance, run `dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj --filter-class '*CollaboratorSignInHttpTests'`. The host provisions isolated test collaborators via the Auth store and exercises the real forms, credentials and authorization. The owner management surface and collaborator account navigation are B4.

## B4 — Owner Management and Collaborator UI

- The owner manages collaborators at `/admin/account/collaborators`, reached from Account Settings. Razor form submissions require antiforgery, fresh owner password and applicable TOTP. Updates use the account revision. The list shows slot use, disabled state, required password change and authenticator enrollment. Deletion requires confirmation.
- Collaborators land at `/admin/post`, see permitted content navigation and can change their password at `/admin/account/password`. Public edit links use the shared content policy. The owner alone sees export, recycle-bin emptying and permanent deletion controls. Both roles can open content previews and restore items from the recycle bins.
- Activity Logs shows collaborator creation, email change, password and authenticator resets, disablement, re-enablement and deletion. Records retain the actor account ID and target account ID and display name. Credential material and collaborator email addresses are excluded from management events.

### B4 verification and acceptance

- `dotnet test --solution src/Moonglade.slnx --no-restore --no-ansi` passed 2,193 tests. B4 HTTP coverage includes owner-only forms, antiforgery, owner password and TOTP, stale revisions, session revocation, slot reuse and persisted audit identity. `node --test src/Tests/Moonglade.Web.Tests/JavaScript/*.mjs` passed 91 tests. Release Web publish passed.
- Published Release with disposable PostgreSQL and Chromium at 1440 CSS pixels verified first sign-in, password change, TOTP, English and Simplified Chinese UI, owner and collaborator navigation, denied deep links, slot usage, lifecycle actions and audit display after deletion. A collaborator recycled and restored a post, and the owner alone saw permanent deletion and emptying controls. Both-provider release acceptance is in B5.
- To accept B4, go to Account Settings → Collaborators. Create two test editors and check that both slots are occupied. Sign in as one editor in a separate browser profile and complete the password change and any authenticator steps. Disable the editor from the owner session and verify that the slot stays occupied and the editor session ends. Re-enable the editor and sign in again, delete the test account, and create a replacement. Verify that Activity Logs retains the original account’s identity after replacement. Use an isolated database or designated test accounts.

## B5 — Release Verification and Operator Documentation

- Update the authentication and upgrade sections of the README, the database upgrade guide, engineering reference and account release acceptance checklist to reflect the shipped behavior. Cover role permissions, trusted content publishing, slot and credential management, backup and rollback, and the required upgrade order.
- Run the full solution suite, frontend behavior suite and Release Web build. Use published Release assets with isolated disposable PostgreSQL and SQL Server databases for fresh installs and representative upgrades. Include legacy owner Local/TOTP and owner OIDC cases, two simultaneous collaborator sessions, cross-account content operations, and the complete positive and negative permission matrix. Use a synthetic OIDC provider for repeatable acceptance and report live-provider coverage separately.
- In the browser, publish, schedule, reschedule, unpublish, recycle and restore posts across accounts. Create, edit and publish a page with existing HTML, an embed and page CSS, then verify the published result. Verify image upload, category and tag changes, comment moderation including visitor email/IP and reply notification, author-field behavior, audit attribution and a direct denial for each owner-only area. Confirm owner-only permanent deletion and emptying for both recycle bins.
- Verify existing public URLs, content, cache invalidation, feeds, notifications, health/static routes and setup gating. Test at 1440 CSS pixels and check the existing resource cultures. Record clean reruns on both providers and remove disposable fixtures.
- Owner acceptance checks the final behavior and operational upgrade evidence. The release decision is a separate owner authorization.

## Verification Commands

Run from the repository root. Pick the owning projects for each batch, and use the deployment and release checklists for commands involving a host or database.

```powershell
dotnet test --project src/Tests/Moonglade.Auth.Tests/Moonglade.Auth.Tests.csproj
dotnet test --project src/Tests/Moonglade.Setup.Tests/Moonglade.Setup.Tests.csproj
dotnet test --project src/Tests/Moonglade.Web.Tests/Moonglade.Web.Tests.csproj
dotnet test --project src/Tests/Moonglade.Features.Tests/Moonglade.Features.Tests.csproj
dotnet test --solution src/Moonglade.slnx
node --test src/Tests/Moonglade.Web.Tests/JavaScript/*.mjs
dotnet build src/Moonglade.Web/Moonglade.Web.csproj -c Release
```

Use synthetic data and isolated connections, key and image locations for database and browser verification. Keep private local configuration and secrets out of test outputs and published artifacts. Follow the repository's manual migration and backup procedures, with a separate authorization for production changes.

## Batch Handoff and Resumption

1. Start with the first incomplete batch in the table. Check `git status --short` before editing and before handing off. Preserve unrelated work and apply only the current batch scope. If a question affects requirements, authorization, public behavior or architecture, ask the owner, explain a recommendation and pause the dependent work. Update the relevant requirements with the confirmed current decision.
2. Run the batch's verification, record the actual results and any gaps, and update this file as part of the batch change. Record a short summary of the outcome, exact test commands or browser/provider evidence, remaining risks and steps for owner acceptance. A failed or unverified required check keeps the batch open.
3. Once developer verification passes, make a separate local Git commit for that batch. Record its commit subject with the changes and add the verified SHA to the working copy of this file after committing. Mark the batch as awaiting owner acceptance and hand off the commit, results and concrete acceptance steps. Carry the tracker update into the next batch commit or a documentation-only commit after final acceptance.
4. Wait for explicit owner acceptance before starting the next batch. Record the acceptance decision and date in this file, update the next batch's state, and replace stale verification or blocker details. Reopen the current batch when acceptance finds a gap and record the verified fix before advancing.
5. When resuming in a new conversation, reread this file, the referenced repository rules and the batch's owning code. Inspect the working tree and the latest batch commit, then continue from the documented state. If the tracker and repository evidence disagree, resolve the discrepancy before advancing.
