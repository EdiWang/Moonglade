# Moonglade Feature Research And Roadmap

Prepared on: 2026-06-08

This document records the current feature research findings for Moonglade and provides a trackable roadmap for future enhancement work.

## Current Feature Overview

Moonglade is already a mature personal blogging platform for developers. Its existing capabilities include:

- Posts: publishing, drafts, scheduled publishing, schedule cancellation, unpublishing, recycle bin, featured posts, outdated markers, and AI-assisted content markers.
- Pages: standalone pages, page-specific CSS, publish state, and recycle bin.
- Content organization: categories, tags, archives, featured lists, tag pages, and category lists.
- Editors: TinyMCE HTML editor, Monaco Markdown editor, drag-and-drop or pasted image upload, syntax highlighting, and LaTeX rendering.
- Comments: built-in comments, third-party comment HTML, review and approval, replies, closing comments on old posts, Gravatar, captcha, and word filtering.
- Social and protocol support: Webmention, RSS, Atom, OPML, OpenSearch, FOAF, Sitemap, and IndexNow.
- SEO: canonical URLs, meta description, keywords, Dublin Core, JSON-LD BlogPosting, and basic OpenGraph metadata.
- Images: local file system and Azure Blob Storage providers, image watermarking, original image retention, and CDN URL replacement.
- Admin portal: posts, pages, categories, tags, comments, menus, widgets, mentions, activity logs, and settings.
- Configuration: runtime blog settings, general settings, content settings, comment settings, notification settings, subscription settings, image settings, advanced settings, appearance settings, and custom menus.
- Authentication: local account authentication and Microsoft Entra ID.
- Operations: Docker, Azure deployment assets, health checks, startup initialization, automatic database migration, update checks, and background services.
- Databases: SQL Server and PostgreSQL.
- Tests: coverage across Features, Web, Auth, Configuration, ImageStorage, Syndication, Theme, Webmention, Moderation, BackgroundServices, and related projects.

## Features Worth Enhancing

### Search

The current search experience mainly covers post titles and tags. It should become a more complete site search experience.

- [x] Search across titles, abstracts, keywords, categories, and tags without scanning full post bodies.
- [x] Support pagination, sorting, and result highlighting.
- [x] Support filters by category, tag, language, and publish date.
- [x] Keep the implementation compatible with SQL Server and PostgreSQL.
- [x] Add matching Features tests for search query behavior.
- [x] Add optional Web tests for search page rendering and query string state.
- [x] Keep full-body search out of scope to avoid the complexity and performance cost of full-text search.

### Editing Experience

Moonglade already has HTML and Markdown editors, but the writing and publishing workflow can be more robust.

- [x] Add draft autosave.
- [ ] Restore drafts after accidental refresh or navigation.
- [ ] Add live Markdown preview.
- [ ] Add SEO preview for title, abstract, canonical URL, and keywords.
- [ ] Add social sharing preview for OpenGraph and Twitter Card metadata.
- [ ] Add a pre-publish checklist for abstract, categories, tags, image alt text, external links, and feed inclusion.

### Media Management

Image upload and watermarking are already strong, but there is no full admin media library yet.

- [ ] Add an admin media library page.
- [ ] Support image search, preview, and URL copying.
- [ ] Support deleting unused images.
- [ ] Detect orphaned images.
- [ ] Detect duplicate images.
- [ ] Support compression, thumbnails, and WebP or AVIF conversion.
- [ ] Make the upload size limit configurable.

### Comment And Webmention Anti-Spam

Moonglade already has captcha, review, word filtering, and Webmention validation. The next step is stronger moderation tooling.

- [ ] Add an IP blacklist.
- [ ] Add an email blacklist.
- [ ] Add a domain blacklist.
- [ ] Add comment rate limiting.
- [x] Add Webmention source rate limiting.
- [ ] Add trusted and blocked domain rules for Webmention.
- [ ] Improve batch approval and batch deletion workflows.
- [ ] Record review reasons and blocking reasons.

### Analytics

The current view count and request count features are useful, but they are closer to counters than analytics.

- [ ] Add an admin analytics dashboard.
- [ ] Add popular post rankings.
- [ ] Add recent traffic trends.
- [ ] Add referrer statistics.
- [ ] Add search term statistics.
- [ ] Add bot filtering effectiveness statistics.
- [ ] Implement a multi-instance-safe counting strategy that does not rely on single-process locks.

### Data Export And Migration

The current export functionality mainly covers posts and pages. It should become a complete backup and migration capability.

- [ ] Export comments.
- [ ] Export categories.
- [ ] Export tags.
- [ ] Export menus.
- [ ] Export widgets.
- [ ] Export configuration.
- [ ] Export themes and custom CSS.
- [ ] Export an image index.
- [ ] Add import and restore workflows.
- [ ] Add import validation and conflict handling.

### Admin Diagnostics

Moonglade already has an About page and `/health`. A more useful system status page would help production operations.

- [ ] Check database connectivity.
- [ ] Check image storage.
- [ ] Check email delivery configuration.
- [ ] Check IndexNow configuration.
- [ ] Check Webmention send and receive capability.
- [ ] Check content moderation service health.
- [ ] Check background service state.
- [ ] Check cache state.
- [ ] Show update check status.

### SEO And Sharing Quality

Moonglade has a good SEO foundation. The next step is improving search engine and social platform presentation.

- [ ] Support `og:image` for posts.
- [ ] Add Twitter Card metadata.
- [ ] Improve SEO metadata for category and tag pages.
- [ ] Add category-level and tag-level sitemap or feed support.
- [ ] Add redirect management for slug changes.
- [ ] Add structured data validation hints.

### Localization Consistency

Moonglade already has multilingual resources, but some Razor and JavaScript text may still be hard-coded in English.

- [ ] Resource hard-coded text in admin pages.
- [ ] Resource hard-coded text in public pages.
- [ ] Resource JavaScript toast, confirm, and alert text.
- [ ] Update `Program.zh-Hans.resx`.
- [ ] Update `Program.zh-Hant.resx`.
- [ ] Update `Program.de-DE.resx`.
- [ ] Update `Program.ja-JP.resx`.

### Security Hardening

The security foundation is good. Admin protection and configuration risk detection can still be improved.

- [ ] Add login failure rate limiting.
- [ ] Add administrator 2FA.
- [ ] Warn about default accounts and weak configuration.
- [ ] Add a UI for CSP and security response headers.
- [ ] Add stricter validation or a safe mode for custom Head and Foot scripts.
- [ ] Add secondary confirmation for sensitive admin operations.

## New Features Worth Adding

### Post Revision History

- [ ] Save a snapshot for each post change.
- [ ] Support version diff.
- [ ] Support restoring historical versions.
- [ ] Record editor, edit time, and publish state.

### Content Series

- [ ] Add a Series entity.
- [ ] Allow posts to belong to one or more series.
- [ ] Add a series detail page.
- [ ] Add previous and next navigation within a series on post pages.
- [ ] Add a series progress navigation component.

### Related Posts

- [ ] Recommend posts based on tags.
- [ ] Recommend posts based on categories.
- [ ] Recommend posts based on keywords.
- [ ] Exclude the current post and unpublished posts.
- [ ] Make the recommendation count configurable.

### Email Subscription And Newsletter

- [ ] Add a subscriber entity.
- [ ] Send subscription confirmation email.
- [ ] Add unsubscribe links.
- [ ] Send post summaries after publishing.
- [ ] Add subscriber management in the admin portal.
- [ ] Record email delivery failures.

### Social Publishing And Webhooks

- [ ] Add generic webhook support.
- [ ] Trigger webhooks after publishing a post.
- [ ] Support retries and failure records.
- [ ] Optionally support Mastodon, Bluesky, X, LinkedIn, Discord, and Slack.

### AI-Assisted Writing

Moonglade currently has an AI-assisted content marker. Optional AI-assisted authoring features can build on that foundation.

- [ ] Generate abstracts.
- [ ] Suggest titles.
- [ ] Suggest SEO keywords.
- [ ] Translate content.
- [ ] Check spelling and grammar.
- [ ] Keep every AI feature configurable, optional, and testable.

### Full Backup And Restore

- [ ] Add manual backup.
- [ ] Add scheduled backup.
- [ ] Back up to the local file system.
- [ ] Back up to Azure Blob Storage.
- [ ] Validate before restore.
- [ ] Add restore dry-run mode.

### Multiple Administrators And Roles

This is a larger architectural change and should be deferred until higher-priority work is complete.

- [ ] Add user entities and admin management pages.
- [ ] Add Author, Editor, and Administrator roles.
- [ ] Bind posts to authors.
- [ ] Authorize admin pages by role.
- [ ] Track activity logs by user.

## Suggested Execution Batches

### Batch 1: Low Risk, High Value

Goal: improve daily use and create a stronger foundation for future features.

- [x] Enhance site search.
- [ ] Expand complete export coverage.
- [ ] Add an admin system diagnostics page.
- [ ] Clean up hard-coded localization text.
- [ ] Add Features and Web tests.

Acceptance criteria:

- [x] Search results are more complete and support pagination, filtering, and sorting.
- [ ] Exported data covers the main business entities.
- [ ] Admin users can inspect the health of critical dependencies.
- [ ] New or changed UI text is synchronized to non-English resource files.
- [ ] Affected test projects pass.

### Batch 2: Content Production Efficiency

Goal: make writing, editing, and publishing more reliable.

- [x] Add draft autosave.
- [ ] Add draft recovery.
- [ ] Add post revision history.
- [ ] Add Markdown preview.
- [ ] Add SEO and social sharing preview.
- [ ] Add a pre-publish checklist.
- [ ] Add the first version of the media library.

Acceptance criteria:

- [ ] Editing work is not lost after accidental refresh.
- [ ] Users can view and restore historical versions.
- [ ] The pre-publish flow can catch obvious content quality issues.
- [ ] Images can be managed from the admin portal.

### Batch 3: Interaction And Growth

Goal: improve reader interaction, subscription, and internal content discovery.

- [ ] Enhance comment anti-spam.
- [ ] Enhance Webmention management.
- [ ] Add related posts.
- [ ] Add content series.
- [ ] Add newsletter subscription.
- [ ] Add publishing webhooks.

Acceptance criteria:

- [ ] Admin users can moderate comments and Webmentions more efficiently.
- [ ] Post pages guide readers to additional relevant content.
- [ ] Readers can subscribe to email updates.
- [ ] Publishing events can integrate with external services.

### Batch 4: Production Resilience And Platform Capability

Goal: improve production stability, security, and extensibility.

- [ ] Make view counting safe for multi-instance deployments.
- [ ] Add scheduled backup and restore.
- [ ] Harden login security.
- [ ] Add configuration risk warnings.
- [ ] Add security response header configuration.
- [ ] Add optional AI-assisted writing.
- [ ] Evaluate multiple administrators and role-based authorization.

Acceptance criteria:

- [ ] Statistics do not obviously lose updates or conflict in multi-instance deployments.
- [ ] Main data can be backed up and restored reliably.
- [ ] Admin login security is stronger.
- [ ] AI features are disabled by default and have clear configuration, logging, and test boundaries.

## Implementation Notes

- Put business logic in the owning project such as `src/Moonglade.Features`, `src/Moonglade.Configuration`, or `src/Moonglade.Data`, and keep the Web layer thin.
- Consider both SQL Server and PostgreSQL for every database-related change.
- Treat post URLs, `RouteLink`, publish dates, slugs, feeds, sitemap, IndexNow, Webmention, and cache invalidation as high-risk boundaries.
- Follow the `IBlogSettings<T>` pattern for new settings, and update defaults, initialization, settings pages, and resource files.
- Make new external service calls configurable, testable, logged, and preferably event-driven or backgrounded.
- When adding or renaming UI text, update the non-English resource files.
- Add or update tests in the matching test project for behavioral changes. At minimum, run the affected test project or the Web project build.
