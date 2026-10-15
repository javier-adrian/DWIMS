# Backlog

Deferred items from the attachments feature review (2026-09-18). None block the current commit.

## Attachments feature

- ~~**Restrict read access to attachment endpoints.**~~ *(done 2026-10-14 by Project item 6
  below: participant checks on `SubmissionService.GetAttachmentsAsync` /
  `DownloadAttachmentAsync`.)*

- **Validate uploaded files.** `UploadAttachmentsAsync` has no file-size limit or content-type
  whitelist. Decide allowed types (e.g. pdf/images/office docs) and a max size, and check them
  before calling `IStorageService.UploadAsync`.

- **Include `ErrorDescription` in endpoint error bodies.** The attachment endpoint handlers
  return `new { result.Error }` while `ToOkResult()` returns `{ Error, ErrorDescription }` —
  make the payloads uniform. See `SubmissionEndpoints.UploadAttachment` / `DownloadAttachment`
  / `GetAttachments`.

- **Orphaned blob cleanup.** Blobs accumulate in storage with no active DB reference:
  re-uploading a template under a different filename leaves the old `templates/{documentId}/…`
  blob behind (old `Document.Link` is overwritten); soft-deleted attachments keep their
  `attachments/{submissionId}/{attachmentId}/…` blobs for audit/restore. Add a periodic job
  that lists storage and removes blobs whose keys are not referenced by any live
  `Document.Link` / `Attachment.Link` row.

## Config / security

- **Remove plaintext Azure storage account key from `appsettings.Production.json`.** It is
  committed to the repo. Move it to Key Vault (like the JWT secret), and rotate the key.
  Also remove/replace the stale `localhost` connection string in the same file — production
  should pull the real one from Key Vault.

## Project

Broader work items (added 2026-09-18; descriptions grounded in the codebase as of that
date — refine when scheduled):

1. ~~**Supporting documents**~~
2. ~~**Routing service**~~ *(done 2026-10-13: routing extracted into
   `IRoutingService`/`RoutingService` with static step advancement — submissions walk
   `Process.Steps` in `Step.Order` as before. The dynamic capabilities once sketched
   here — conditional branching, skips, department overrides — were not part of it and
   remain future work if needed; items 3-6 below still build on the routing seam.)*
3. ~~**Forwarding from specific reviewer to specific reviewer.**~~ *(done 2026-10-13:
   assignment now travels with the submission instead of being implicit — the submitter
   names the first responder (`CreateSubmissionRequest.FirstReviewerEmail`) and each
   responder names the next (`RespondToStepRequest.NextReviewerEmail`), stored as
   `Response.AssigneeId` by `RoutingService.StartRoute`/`Advance`. The pending-review
   queue filters to the assignee, and only the assignee (or a super-admin) may respond;
   assigning the submitter to their own submission is rejected, while reviewers may
   reassign themselves consecutive steps by design. Remaining: `AddAssigneeToResponse`
   migration, reviewer/submitter notification emails (item 13 below), and the frontend
   responder pickers.)*
4. ~~**Submission on-hold status.**~~ *(done 2026-10-14: `Status.OnHold` added; hold is a
   third outcome in `RespondToStepAsync` with the reason required in `Remarks`, stored as a
   completed `Response` row (visible in the step timeline), plus a `POST /submission/{id}/resume`
   endpoint; pending-review queue and analytics pending count include held items. Review
   findings outstanding: resume NRE + stuck-status bugs, hold row missing `ReviewerId`,
   resume endpoint authorization.)*
5. **On-hold visibility and discussion** — show *why* a submission is on hold (reason
   recorded when it's put on hold) and let submitter and reviewer discuss it while held.
   Natural second half of item 4; design together so the reason field exists from day one.
6. ~~**Submission copies accessible to reviewers**~~ *(done 2026-10-14: access model decided
   as participants — submitter, any response `ReviewerId`/`AssigneeId` match, or super-admin
   — and applied to submission detail, review detail, attachment list/download, and the
   generated-document endpoint (unauthorized → 403, not-found → 404 there); new
   `GET /submission/reviewed` returns the reviewer's history. Also closes the deferred
   "Restrict read access to attachment endpoints" item above. The reviewer role policies came
   off the review endpoints with this — anyone assigned to a step can respond. Frontend
   history view and nav gating still pending.)*
7. **Admin tooling.** Decide which admin operations get first-class endpoints/UI: user,
   role, and department management; process lifecycle (activate/retire templates); log and
   analytics views; support impersonation.
8. **Categorization of users** (departments, institutions, programs, sections, etc.) —
   departments already exist (`Department` entity + `Step.DepartmentId` reviewer routing);
   this extends the model to the wider academic org structure.
9. **Support more types of entries.** `InputTypes` currently covers Text, Number, Date,
   Boolean, and Signature. Enumerate what real forms need beyond that (dropdowns,
   multi-line text, file/image fields, tables) and how AcroForm extraction maps them.
10. **Tests.** No test project exists today. Start at the service layer — the
    `SubmissionService` Result/ownership logic is the highest-value target — using an
    EF in-memory or SQLite provider; also gives the CI item something to run.
11. **Actual entry of form templates.** The machinery exists (template upload + AcroForm
    extraction, with steps derived from the `{step}:{title}` field-naming convention in
    `ProcessService`). The work is preparing the institution's real PDFs so their fields
    follow that convention and produce the right steps.
12. **MFA.** TOTP authenticator app + recovery codes + remember-device, enforced per role
    (administrators first); integrates with the existing JWT auth.
13. **Documentation.** No README exists. Cover local setup (database, storage provider
    choice, seeding), an architecture map (App / Service / Data + the storage
    abstraction), and expand DEPLOYMENT.md.
14. **EF Core configuration** — candidates seen during review: `IEntityTypeConfiguration`
    classes instead of convention-only mapping, global query filters for `ISoftDeletable`
    entities (currently only `User` has one, which soft-deleted attachments/submissions
    rely on ad-hoc filters to compensate for), and removing
    `EnableSensitiveDataLogging`/command logging from production.
15. **Terms & conditions and privacy policy.** Legal texts plus versioned consent
    (store which version each user accepted and when); given the institutional context,
    alignment with the Philippine Data Privacy Act is likely required.

16. **Signature required before approving** *(added 2026-10-13)*. A reviewer can approve
    a step without ever registering a signature (`UserService.RegisterSignatureAsync`,
    `Signature` entity with `isCurrent`), but `PdfGenerationService.BuildSignaturesAsync`
    stamps every approver's latest signature into the final PDF and dereferences it
    unconditionally (`signature.EncryptedBlob`, PdfGenerationService.cs:74 — already a
    CS8602 warning) — so one signature-less approver makes the generated-document
    endpoint throw NullReferenceException. Enforce in `RespondToStepAsync`: an Approve
    outcome requires a current signature for the reviewer, returning a distinct error
    code (e.g. `SIGNATURE_REQUIRED`) the frontend can route to signature upload. Reject
    outcomes don't get stamped, so they can stay ungated.

17. ~~**Ordering of fields**~~ *(done 2026-10-14: `Field.Order` added via the
    `AddFieldOrder` migration, which backfills existing rows by parsing the numeric
    prefix from `AcroFormKey` (`SUBSTRING_INDEX`/`REGEXP` on MariaDB — hand-written
    SQL must backtick the reserved word `Order`). Template extraction seeds the order
    from the field-name prefix (`1- Name` → 1) with a 1000+ counter as document-order
    fallback for unprefixed names, and `AddFieldAsync` appends at max+1. The
    process-detail projection (ProcessService.cs:84) and both submission-detail
    field-value mappings in `SubmissionService` now sort by `Order` then `Title`, so
    the fill form, admin field list, and reviewer/track views render in server order
    with no frontend change; PDF generation is unaffected (it fills by `AcroFormKey`).
    Review passed with no findings outstanding. Optional follow-ups deliberately
    deferred: exposing `Order` on `FieldDto`, a reorder endpoint/admin UI, and
    stripping the now-redundant numeric prefix from displayed titles.)*

18. ~~**Signer name under the stamped signature**~~ *(done 2026-10-15: rendering-side
    approach chosen, not baked into the stored SVG. `BuildSignaturesAsync` now returns
    `SignatureStamp` records (DWIMS.Service/Common/SignatureStamp.cs) carrying the SVG,
    the reviewer's `FirstName LastName` (via the `Response.Reviewer` include added to
    `GenerateAsync`), and `CompletedOn`; `FillSignatures` draws the name (8pt) and the
    approval date (7pt, "MMMM d, yyyy" invariant culture) centered in a text band at the
    bottom of the widget rectangle via `iText.Layout.Canvas` — with `SetMargin(0)` +
    `SetMultipliedLeading(1)` and an 8pt/15pt band so the text sits flush at the bottom
    edge (iText's default 1.5× leading plus the top-anchored canvas otherwise float the
    glyphs up off the edge). By deliberate choice the signature image still fits the
    full widget rectangle and overlaps the text band — a shrunken image was tried and
    rejected. As part of the same work, filled AcroForm text fields are now
    center-justified (`SetJustification(ALIGN_CENTER)` in `FillFields`, which survives
    `FlattenFields`). Landed across commits `4d7dde8`/`4c5b005`/`028b0e0`/`e6d5224`;
    not yet smoke-tested on a real template. Related gap left open on purpose: item 16's
    signature-less-approver NRE in `BuildSignaturesAsync` remains unguarded for live
    renders, though the item-19 persist path now catches it.)*

19. ~~**Attach approved documents from other submissions**~~ *(struck 2026-10-15:
    unnecessary — the copy-on-attach design drafted the same day made clear the
    feature is a convenience wrapper over a manual path that already works:
    download the approved submission's PDF (`GET /submission/{id}/pdf`) and upload
    it as an attachment. Not worth a new endpoint plus picker UI. Option (b),
    persist at completion, shipped independently as `Submission.ResultLink`
    (`TryPersistResultPdfAsync`) and survives this strike.)* Supporting
    attachments today can only be freshly uploaded files (`UploadAttachmentsAsync` →
    `attachments/{submissionId}/{attachmentId}/{filename}` blobs); there is no way to
    reuse an already-approved submission's document on a new one without downloading and
    re-uploading it. Complication: an approved submission's PDF is **not persisted
    anywhere** — `GetDocument` renders it on demand from template + inputs + signatures
    (SubmissionEndpoints.cs:133-140, `PdfGenerationService.GenerateAsync`); only blank
    process templates are stored (`Document` → `templates/` blobs). Two shapes: (a)
    *snapshot* — at attach time, render the source submission via `GenerateAsync` and
    store the bytes as a normal attachment (works with the existing entity/queue, but
    duplicates storage, freezes signature state, and inherits item 16: one signature-less
    approver makes `GenerateAsync` throw before anything is stored); or (b) *persist at
    completion* — generate and store the document when a submission reaches Approve
    (new blob space/entity), then attach by reference (`SourceSubmissionId` on
    `Attachment`) so reviewers always see the canonical artifact. Either way needs an
    eligibility rule (own approved submissions at minimum: `SubmitterId == me &&
    Status == Status.Approve`; whether users may attach *others'* approved documents is a
    policy decision that overlaps item 6) and a non-file attach path beside the
    `IFormFile` upload endpoint.

20. ~~**View the template while submitting**~~ *(done 2026-10-15:
    `GET /process/{processId}/document` (`ProcessService.DownloadDocumentTemplateAsync`)
    streams the blob at `Document.Link` to any authenticated user, with a missing or
    empty template — and storage failures — mapped to `DOCUMENT_NOT_FOUND` → 404
    (exception-filtered so request cancellation still propagates; logging inside the
    catch is a TODO). Frontend: `getProcessTemplate` helper in client.js plus View
    Template buttons in both NewSubmission states (disabled until a process is chosen),
    opening the PDF in the shared FileViewerModal. Not yet smoke-tested — dev DB needs
    a reseed first.)*

21. ~~**View files in-app instead of force-downloading**~~ *(done 2026-10-15,
    frontend-only: new shared `src/components/FileViewerModal.js` exports
    `openFileViewer({title, fileName, blob, mimeType})` — it re-types the blob via
    `getMimeType` when the server sent `application/octet-stream`, then renders PDFs in
    an `<iframe>` inside a modal (`#toolbar=1` keeps the viewer's own controls),
    images/video/audio inline, text via `escapeHtml`-wrapped `blob.text()` (so an HTML
    attachment renders inert instead of live), and unknown types get a no-preview card
    with an Open-in-Tab fallback; every variant offers Open-in-Tab (`window.open` on
    the object URL, revoked after 60s) and Escape/backdrop close. ReviewerDetail.js
    and TrackDetail.js route both attachments and the generated submission PDF
    (`getSubmissionDocument`) through it, replacing the synthetic `<a download>`
    anchor click — no API change, since the token-auth fetch-blob path already
    bypasses `Content-Disposition`. Residual nits, non-blocking: Open-in-Tab on an
    HTML/SVG blob URL executes scripts with the SPA origin (the JWT lives in
    `localStorage`) — consider gating that button to viewable types; and with no
    explicit download button anymore, saving from the PDF viewer toolbar suggests
    the blob UUID rather than the real filename.)*

22. **Reviewer redirect of a misrouted step** *(added 2026-10-15)*. Item 3 lets the
    submitter name the first responder and each reviewer name the next, but a
    reviewer who receives a submission **by mistake** has no way to pass it to the
    right person without acting on it. Every path through `RespondToStepAsync`
    (SubmissionService.cs:290-347) records an outcome: Approve stamps them in as the
    step's `ReviewerId` (whose signature later lands in the generated PDF), Reject
    kills the whole submission, OnHold freezes it — and the pending `Response` row's
    `AssigneeId` (SubmissionService.cs:301-307) is otherwise immutable, so their only
    real option today is to approve-and-forward or bother a super-admin. Add a
    redirect action, e.g. `POST /submission/{id:guid}/redirect` mirroring the
    `/resume` shape: find the open response for the current step and rewrite only its
    `AssigneeId` to the named reviewer, leaving `Result`/`ReviewerId`/`CompletedOn`
    untouched, reusing the `StartRoute` target guards (user must exist, must not be
    the submitter — RoutingService.cs:18-26) so the misroute can't be "fixed" into a
    worse one. Policy decisions to make alongside: who may redirect (at minimum the
    current assignee, matching the respond guard at SubmissionService.cs:306;
    super-admins naturally, and arguably the *submitter* for a botched
    first-reviewer pick, since `StartRoute` already trusts their choice); whether the
    target must belong to the step's `Step.DepartmentId` (the dept/role barrier is
    the same undecided question item 3 left open); and access for the mistaken
    assignee afterward — under the item-6 participant model their visibility comes
    from the `AssigneeId` match on the response row (SubmissionService.cs:110), which
    a redirect rewrites away, so decide whether they should keep it and make the
    bounce traceable either way (audit-log entry via `LogService`, and/or record the
    original assignee in `Remarks`). Notify the new assignee by email — blocked on
    review item 13 (`SendStepActivatedAsync` still never called); the frontend
    redirect picker is blocked on the same DepartmentEndpoints `GetMembers`
    route-param fix the item-3 pickers are.

23. **Template versioning** *(added 2026-10-15)*. A template re-upload is a destructive
    overwrite today: `UploadDocumentAsync` reuses the process's single `Document` row and
    overwrites its `Link` (ProcessService.cs:340, 360), then deletes and re-extracts the
    process's `Steps` and `Fields` (:326-327) — and because the `AppDbContext` soft-delete
    interceptor turns those deletes into `IsDeleted = 1` instead of removing rows, every
    existing submission's `Inputs`/`Responses` survive but point at tombstones the
    rendering filters out (`!input.Field.IsDeleted` / `!response.Step.IsDeleted`,
    PdfGenerationService.cs:52/72), blanking **every** past and in-flight submission of
    the process at once (hit 2026-10-15; recovered by re-linking `FieldId`/`StepId` to
    the re-extracted rows matched by `AcroFormKey` / `(Order, Title)`). The
    completion snapshots (`Submission.ResultLink`, landed 2026-10-15) will protect
    finished submissions, but in-flight ones still
    live-render from whatever template is current. Fix by making each upload a *new
    version* instead of an overwrite: insert a fresh `Document` row per upload (blob keys
    are already document-scoped — `templates/{documentId}/{filename}`,
    ProcessService.cs:353-358 — so the old version's blob is simply retained); add a
    nullable `Submission.DocumentId` binding each submission to the version it was
    created under, backfilled to the existing row for legacy submissions and falling
    back to the latest version when null; scope steps per version too (`Step` gets a
    `DocumentId` — `Field` already carries one, set during extraction at :399); bind the
    active version in `CreateSubmissionAsync`; and render in
    `PdfGenerationService.GenerateAsync` from `submission.Document`'s template plus that
    version's steps/fields rather than `Process.Documents.FirstOrDefault()`
    (PdfGenerationService.cs:34-35). In-flight submissions intentionally stay on their
    old version across an upload — that's the feature. Decisions to make alongside:
    what marks the active version (latest non-deleted `Document`, retiring =
    soft-delete); whether to also keep the cheap interim guard (refuse re-upload while
    in-flight submissions exist — versioning makes it unnecessary for correctness, but
    it still catches accidental uploads); and updating the orphaned-blob cleanup rule
    above so superseded *version* blobs count as live history (any blob referenced by a
    `Document.Link` row, deleted or not).

24. **Signature registration from images** *(added 2026-10-15)*. A signature can only be
    registered as drawn SVG markup today: `RegisterSignatureRequest` carries a single
    required `SvgContent` string, and `UserService.RegisterSignatureAsync` rejects
    anything that doesn't start with `<svg` (UserService.cs:47-48) while hardcoding
    `MimeType = "image/svg+xml"` into the row (UserService.cs:56, 66) — on the frontend,
    the Settings page is likewise a draw-only canvas that serializes strokes to an SVG
    string (`pathsToSvg`, Settings.js:229-241). Users who can't draw a usable signature
    with a mouse — or who already have a scanned/photographed one — have no path in. Add
    an image option: a multipart upload endpoint beside the existing JSON one (watch the
    known .NET 8 gotcha — antiforgery metadata is auto-added to `IFormFile` endpoints and
    needs `.DisableAntiforgery()`, as in the attachments fix), accepting PNG/JPEG with a
    size cap and content sniffing (the same validation gap as the deferred
    "Validate uploaded files" item above). The `Signature` entity already stores
    `MimeType` beside `EncryptedBlob` (which despite the name holds plain bytes), so the
    row generalizes without a migration — but the consumer doesn't:
    `PdfGenerationService.FillSignatures` unconditionally rasterizes the blob through
    `ConvertSvgToPng`/SkiaSharp (PdfGenerationService.cs:194, 237-257), and
    `SignatureStamp.Svg` (SignatureStamp.cs:3) names the payload as SVG — branch on the
    stored `MimeType` there and pass raster bytes straight to `ImageDataFactory.Create`.
    Practical wrinkles: photos/scans of signatures have opaque backgrounds and would
    stamp a solid rectangle over the widget — require transparent PNGs at minimum, or
    knock out near-white pixels with SkiaSharp (already a dependency) as polish; and
    downscale phone photos at registration time, or every generated PDF embeds
    multi-megabyte images. Item 16's approve-time gate is format-agnostic (any current
    signature satisfies it) and item 18's name/date band is drawn independently of the
    image, so neither needs rework.

## Suggested during review (by ZCode, 2026-09-18)

Items grounded in code observed during the attachments review; triage hints inline.

1. **Gate API docs to development** *(cheap, urgent)*. `MapSwagger` / `MapScalarApiReference`
   (`Program.cs:188-189`) run unconditionally, so the full API documentation ships in
   production. Wrap in `if (app.Environment.IsDevelopment())` or equivalent.

2. **Move super-admin seeding out of code** *(cheap, urgent)*. The production branch of
   `Program.cs` hardcodes the admin email and a bcrypt hash of a literal password — anyone
   with repo access knows the production admin password. Seed from configuration/Key Vault,
   rotate the credential, force a change on first login.

3. **Rate limiting** *(cheap, urgent)*. No rate-limiter middleware exists; login and
   password-reset endpoints are brute-forceable. ASP.NET Core's built-in
   `AddRateLimiter`/`UseRateLimiter` covers this.

4. **Malware scanning on uploads**. Attachments are user-uploaded and re-served to
   reviewers; a hostile Office/PDF file will eventually land. Scan (e.g. ClamAV) before
   accepting the blob.

5. **Rework flow for rejected submissions** *(structurally important)*. Rejection sets
   `Status.Reject` and nothing ever moves a submission back — the only recovery is a
   brand-new submission, losing history. Same lifecycle work as items 3 and 4 in Project
   above (reviewer forwarding, on-hold status).

6. **Reviewer notifications on attachment changes**. Reviewers are notified on submission
   creation only; attachments uploaded afterward are silent, so a reviewer can act without
   knowing new documents arrived.

7. **Audit-log coverage**. `LogService` records Created/Responded/Cancelled; attachment
   upload/delete and template upload write no log entries.

8. **Normalize stored link format across providers** *(structurally important)*. Azure
   `UploadAsync` returns a full blob URI, S3 returns a bare key, and Download/Delete parse
   accordingly — so `attachments.link` / `documents.link` hold provider-specific values and
   switching `Storage:Provider` breaks every stored link. Store a provider-neutral key and
   resolve per provider.

9. **Presigned/SAS direct downloads**. Every download streams the whole file through the
   API. `StorageOptions.ExpiryMinutes` exists but is unused — direct expiring URLs were
   apparently intended and never built.

10. **Log retention**. The `logs` table grows unboundedly; nothing prunes or archives it.

11. **Dev database seeding**. The only seed path is the production admin branch — rebuilds
    leave dev empty (as after the 2026-09-18 reset). A dev seed (users, process, steps,
    sample submission) makes resets painless and supports Project item 10 (Tests).

12. **Fix CI/CD — `.github/workflows/dotnet.yml` never worked**. It is deploy-only with no
    validation: triggers on every push to `main`, runs `dotnet publish` but never builds
    tests or gates PRs, stops the service before copying (downtime window + broken state if
    scp fails), and has no post-start health check. Split into CI (build + test on PR and
    push) and CD (deploy on tag or manual), deploy the publish artifact atomically, and
    verify health after restart.

13. **Wire up the never-called notification hooks** *(added 2026-10-13)*.
    `INotificationService.SendStepActivatedAsync` and `SendSubmissionCompletedAsync` are
    implemented but no code invokes them — reviewers are never told a step landed on their
    desk, and submitters never told their submission finished. Call them from
    `RespondToStepAsync`; easiest after the routing-service extraction, where
    `RouteResult.Outcome` already says which one to send.

