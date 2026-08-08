# Image Upload Validation

## Original Goal

Add content validation for blog image uploads while preserving SVG upload support through sanitization.

## Background

The upload endpoint in `src/Moonglade.Web/Controllers/ImageController.cs` accepted image files based mainly on extension. Stored files are later served with image content types inferred from extension or storage metadata. SVG upload is a product requirement, so the mitigation must sanitize SVG content rather than disabling SVG.

## Scope

- Add repository-local image upload validation for PNG, JPEG, GIF, WebP, and SVG.
- Sanitize SVG before saving primary or original copies.
- Reject extension/content mismatches and invalid image bytes.
- Add focused Web tests for the upload boundary.

## Out of Scope

- Changing image storage provider APIs to persist validated content type metadata.
- Reworking CDN, image cache, or thumbnail behavior.
- Changing upload size limits.

## Task Breakdown

| No. | Task | Dependencies | Verification | Status |
| --- | --- | --- | --- | --- |
| 1 | Inspect upload path and tests | None | Static inspection | Done |
| 2 | Add image validation and SVG sanitization service | Task 1 | Unit/controller tests | Done |
| 3 | Wire validator into upload path and DI | Task 2 | Web tests/build | Done |
| 4 | Add regression tests | Task 2 | `Moonglade.Web.Tests` | Done |
| 5 | Run focused verification | Tasks 2-4 | `dotnet test`, `dotnet build` | Done |

## Execution Order

Implemented the validator first, then enforced it in the controller before storage writes. Updated existing upload tests to use real image/SVG payloads and added malicious payload tests.

## Current Progress

Upload source-to-sink path was confirmed: `IFormFile` filename and bytes are accepted by `ImageController.Image`, then written through `IBlogImageStorage.InsertAsync`. The new control validates extension/content agreement for raster images, decodes raster images with SkiaSharp, and sanitizes SVG before primary or original image storage.

## Verification Log

| Date | Command or check | Result | Notes |
| --- | --- | --- | --- |
| 2026-08-08 | Static inspection | Passed | Confirmed upload extension-only validation path |
| 2026-08-08 | `dotnet test src\Tests\Moonglade.Web.Tests\Moonglade.Web.Tests.csproj` | Passed | 158 tests passed |
| 2026-08-08 | `dotnet build src\Moonglade.Web\Moonglade.Web.csproj` | Passed | Build succeeded with 0 warnings and 0 errors |

## Issues and Resolutions

- Initial PNG signature collection expression did not compile because the target type was inferred as `byte`; fixed by using an explicit static byte array.
- New tests needed `System.Text` for SVG byte construction; added the missing using.

## Follow-ups

- Consider passing validated MIME metadata through `IBlogImageStorage` in a future storage API change.
- SVG sanitization intentionally strips active elements, event attributes, unsafe URI schemes, and CSS with URL/import/expression constructs. This may remove some advanced SVG styling but preserves basic vector images.

## Notes

SVG remains supported. Sanitization should remove scriptable/active content and unsafe URL references while preserving basic vector markup.
