# Image Streaming Cache

## Original Goal

Optimize image output without adding YARP by switching the existing provider-backed image response to streaming output with HTTP cache headers and conditional request handling. Remove the legacy block that prevented access to `-origin.` image filenames.

## Background

Moonglade serves uploaded images through `ImageController` and the `IBlogImageStorage` abstraction. The previous GET flow cached full image byte arrays in memory, then returned `FileContentResult`; Azure Blob Storage and file system providers both loaded the whole image into memory. CDN redirect behavior remains separate and should stay unchanged.

## Scope

- Replace full byte-array image reads with provider metadata plus provider-opened read streams.
- Add HTTP `Cache-Control`, `ETag`, `Last-Modified`, 304 handling, and range-enabled file responses for `/image/{filename}`.
- Remove the old `-origin.` access block from image GET handling.
- Update storage provider and controller tests.

## Out of Scope

- Adding YARP or any reverse proxy package.
- Changing image upload, watermarking, CDN redirect, or storage configuration behavior.
- Changing public image URL shape.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Add metadata and stream methods to `IBlogImageStorage` | Existing provider implementations | ImageStorage tests compile and pass | Done |
| 2 | Stream image responses from `ImageController` and add cache validators | Task 1 | Web tests compile and pass | Done |
| 3 | Remove `-origin.` GET block | Task 2 | Controller test updated | Done |
| 4 | Run focused tests and build | Tasks 1-3 | `dotnet test` / `dotnet build` | Done |

## Execution Order

First update the storage contract and provider implementations, then update the web controller to consume metadata and streams. Tests are updated alongside each changed behavior, followed by focused test runs and a web build.

## Current Progress

Implemented and verified. The image storage contract now returns metadata separately from read streams, both providers support stream opening, and `ImageController` returns range-enabled `FileStreamResult` with cache validators. The legacy `-origin.` block was removed.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-07-18 | `dotnet test src\Tests\Moonglade.ImageStorage.Tests\Moonglade.ImageStorage.Tests.csproj` | Passed | 93 tests passed |
| 2026-07-18 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | 105 tests passed |
| 2026-07-18 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | 0 warnings, 0 errors |

## Issues and Resolutions

Initial Web test run had two cache header string expectation failures because ASP.NET Core serializes `Cache-Control` as `public, max-age=300`. Updated tests to match the framework output.

## Follow-ups

Consider whether image cache duration should become a browser cache setting separate from provider metadata cache duration.

## Notes

Keep repository documentation in English. Do not add YARP for this task.
