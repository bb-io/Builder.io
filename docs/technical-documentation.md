# Builder.io — Blackbird integration

Version: **1.1.3**. Runtime: .NET 8, portable class-library plugin. Display name: **Builder.io**. Code projects: `Apps.BuilderIo` and `Tests.BuilderIo`, both in the repository root. Live API validation remains pending: the numeric version is required for distinct deployable builds and is not a production-readiness claim.

## Purpose

Move Builder Content text through translation, review and Blacklake workflows, and write translations back while preserving structured content. Supports both inline field-level localization and separate locale-targeted entries. Manage content tags, search content, and explicitly publish or unpublish entries.

This app supports **Builder Content (Visual CMS)** and a separate **Builder Code / Projects** action group. CMS uses Space API keys; Projects uses a separate OAuth connection. Projects operations are an integration candidate pending authenticated acceptance testing.

## Before connecting

1. Create a Builder Content Space.
2. Retrieve its public API key and create a **Space-scoped private API key** in Space Settings → Private Keys. An administrator must create the private key. Use keys from the same Space, not an Organization key or personal access token.
3. Configure source and target locales in Builder. Native localization depends on your Builder plan; its localization integration documentation labels this a Pro feature. A free account is not a guarantee that the full multilingual workflow can be tested.
4. Define localizable model fields and enable localization on component inputs for field-level workflows.
5. For tags, add a **non-localized custom field of type `tags`** to each relevant model. The field can be named `tag`, `tags`, or another name; the app discovers it.
6. The consuming website must implement Builder's locale handling and targeting. API uploads alone do not change website routing.

## Connection

| Field | Required | Purpose |
|---|---|---|
| Public API key | Yes | Identifies the Content Space and namespaces file metadata |
| Private API key | Yes, sensitive | Authenticates Admin API and Write API calls, and reads private/draft content |

No account, workspace, organization or Space selector is required. The credentials already identify the Space. Connection validation queries the Admin API's `id` and verifies it equals the public Space key. Validation checks identity/read access; it does not perform a write or promise all operations are permitted.

Content reads use the public Space key in the Content API `apiKey` parameter and the private key in the `Authorization: Bearer` header. The private key is not placed in content request URLs. Credentials never go into exported translation files. Do not log authorization headers.

## Permissions

| Capability | Required access |
|---|---|
| Search models/locales and contextual pickers | Admin API read access to models and Space settings |
| Search/get/download content | Read access to the selected model and drafts/private content |
| Upload content and modify tags | Write access to the selected model's content |
| Publish/unpublish | Permission to change entry publication state |
| Events | Read Space settings and update **Space/global webhooks** through Admin API; typically Space administrator access |
| Live test suite | Disposable Space with model creation/deletion, content writes, publishing and global webhook administration; two configured locales for translation tests |

A private key does not bypass plan entitlements, model access or provider permissions. A 401/403 produces a clear access error. CMS uses Space API keys. The separate Projects connection uses OAuth; personal access tokens are not a substitute for either connection.

## Actions and inputs

### Shared selectors

- **Model ID:** required wherever content or tags depend on a model. Searchable dynamic picker displays readable names such as Page and Figma imports; stores the actual model ID. Long IDs and redundant kinds are omitted from labels. Search accepts the readable label, original model name or ID. If formatted labels collide, the original model name is appended for distinction. The app resolves its API model name internally.
- **Content ID:** searchable dynamic picker scoped to the required model. Displays name, publication status and ID. Manual and mapped IDs are supported.
- **Tag field:** searchable dynamic picker from the selected model's top-level fields with type `tags` and `localized=false`. Stores the actual field name. Not a global provider label ID.
- **Tags:** collection of text with searchable suggestions from existing values in the selected model and tag field. New text and mapped collections remain valid.
- **Language:** dynamic list from `settings.customTargetingAttributes.locale.enum`. `Default` is an explicit Builder value selector, **not an inferred language code**. For production translation/Blacklake matching choose real configured language tags. The source reader uses the selected locale value, falling back to `Default` if that locale has no value; explicitly empty source values remain empty.

### Complete action catalogue

| Action | Required inputs | Optional inputs and defaults | Outputs |
|---|---|---|---|
| Search models | None | None | Models: model ID, name, model type |
| Search locales | None | None | Configured locale values plus explicit `Default` selector |
| Search content | Model ID | Name contains; Status (Draft/Published); Updated after (date); Tag field; Tags | Content collection |
| Get content | Model ID, Content ID | None | Content details |
| Download content | Model ID, Content ID, Source language | Localization mode: Field-level by default, or Whole-entry | HTML content file, source/root content ID, model ID; empty target ID |
| Upload content | Translation file, Target language | Target content ID: optional manual/mapped link to an existing whole-entry translation | Updated HTML/XLIFF file, source/root content ID, target content ID, model ID, Content changed |
| Publish content | Model ID, Content ID | None | Content details with requested Published status |
| Unpublish content | Model ID, Content ID | None | Content details with requested Draft status |
| Search tags | Model ID, Tag field | None | Unique existing tag values |
| Get content tags | Model ID, Content ID, Tag field | None | Tag values on the entry |
| Add content tags | Model ID, Content ID, Tag field, Tags | None | Resulting tags |
| Remove content tags | Model ID, Content ID, Tag field, Tags | None | Resulting tags |

**Content details:** Content ID, Model ID, Name, Status, Updated at (UTC), Editor URL, Tags. Tags combines all supported tag fields for convenience; use Get content tags to distinguish individual fields. Publish/unpublish returns the entry metadata read before the write, with status changed to the requested status; use Get content for a fresh provider timestamp.

Search tags aggregation and Search content follow pagination automatically. Tag filters require **all** specified tags (exact, case-sensitive match). Tag addition removes exact duplicates; removal of an absent tag is a no-op. Tags are preserved during localization and are never translated.

### Required inputs versus suggestions

Required inputs are non-nullable SDK properties. Optional values are nullable. The app does not claim conditional field hiding, automatic clearing of dependent choices, or custom Required/Advanced placement. Missing parents return empty suggestion sets, and field descriptions explain the selection order. Runtime validation reports missing required fields and incompatible choices.

Tag filters in Search content and events are optional, but **Tag field becomes logically necessary when Tags is supplied**; validation enforces this without claiming conditional UI support.

Upload does not require a redundant model selector: the source model is embedded in the exported file. Consequently its optional Target content ID cannot have a reliable normal dependent picker; supply a mapped or manually entered ID. Cross-Space uploads are rejected.

### Search bounds

- Content suggestions search names on the server, escaping the input as a literal regex, then paginate up to 500 matching entries. If there are more, an explicit error asks you to narrow the search or enter/map an ID. IDs may be entered directly; the picker search itself searches names.
- Content actions and tag aggregation allow at most 10,000 entries. Exceeding this fails explicitly, rather than silently returning an incomplete result.
- Tag suggestions enumerate the selected model because there is no documented global tags catalogue endpoint. This can be expensive on large models; use direct tag text/mapping or narrow your content model.
- Models and locales use the documented unpaginated Admin API collections, scoped to the connection. No maintained language catalogue is presented as live provider data.
- Failures propagate as user-facing errors, not misleading empty lists.

## Translation round trip

### Download

Download content returns a self-describing **HTML** file, matching Blackbird CMS conventions. It can pass through AI translation, TMS translation, human review, and Blacklake. Upload accepts `.html`, `.xlf` and `.xliff`; bilingual loading uses `Blackbird.Filters.Transformation`, not a custom XLIFF parser.

Each file preserves:

- Source Space, model, entry, canonical/root entry, source locale and localization mode.
- A source-data snapshot and SHA-256 source fingerprint.
- Per-field reconstruction paths, original values, type and available length limits.
- Stable `data-blackbird-key` values and standard Blackbird system metadata.
- Visual block IDs as path anchors, so matched blocks can survive reordering in a target layout.

The filename is a sanitized content name with a locale suffix. Metadata is encoded in non-translatable attributes. Do not strip these attributes or remove/duplicate translation fields. Files contain source content and should be treated as content exports, not public metadata.

### Field-level localization

Translations are written into target-locale values in the original entry. Source/default values, other locales, tags, layout and unrelated data are preserved. Only supported localizable fields are exported. Explicitly localized string fields whose stored source is a plain string are converted to Builder's `LocalizedValue` wrapper when needed, retaining the original under `Default`.

Supported structured fields: text/string, long text, rich text/HTML and Markdown; nested object/list fields with declared subfields. Native localized text component inputs are supported. Referenced content is not traversed.

### Whole-entry localization

The app clones the current source's content data, metadata and targeting into a separate **draft** for the target language, replacing the locale targeting rule. It retains other source targeting conditions; review locale-specific URLs and routing in Builder before publishing.

A deterministic ID derived from Space + model + canonical source + target locale prevents sequential retries from creating duplicates. The target stores `meta.blackbirdSourceId` and `meta.blackbirdTargetLocale`. Later uploads reuse that entry. If a matching translation was created outside this app, supply its Target content ID explicitly; the app does not guess relationships from names.

Repeat uploads merge translated fields into the **current target**, preserving target-only fields, tags and layout edits. A removed target field or block causes an actionable failure; the app does not reconstruct a deleted section behind an editor's back. Whole-entry model lists require a stable `id` on every item; otherwise export fails clearly. This prevents index-based writes from targeting the wrong item after a target editor reorders the list. Field-level lists use source-validated index paths where no IDs exist.

Whole-entry mode exports supported structured text fields and localized component text, plus common unlocalized text inputs of built-in Text, Button, Image, Input and TextArea components. It does not guess that every arbitrary string inside a custom component is translatable. Enable localization on custom component text inputs to make them discoverable.

### Publishing and conflict protection

**No upload or tag action publishes implicitly.** A changed upload/tag update requires the target entry to be a draft. If it is published, explicitly Unpublish content, perform the update, review, then Publish content. This can interrupt delivery, especially for field-level localization because the original entry contains all inline locales. Plan this workflow with the site's content owners; this release does not implement a separate draft-revision API for published entries.

Publishing/unpublishing operates on the entire entry, never one inline locale. The app does not infer publication decisions from a file or language selection.

The source fingerprint checks source data after resolving localized values for the selected source locale; translations added to other locales do not invalidate it. Source structural or data changes require a fresh download. The target is read again immediately before a write to detect an intervening update. Builder's documented Write API does not supply an atomic conditional-write guarantee here: simultaneous writes can still race between the check and PATCH/PUT. Serialize uploads per source/target and webhook configuration changes per Space.

Unchanged uploads skip API writes. A new translation with unchanged wording can still be a real change because it creates a new locale value or entry. User-selected source and target languages must differ. XLIFF target-language metadata must match the selected target language.

### Blacklake interoperability

UCID is `SpaceID:ModelID:CanonicalSourceEntryID`. Whole-entry translations use the same canonical source ID across languages; the actual target ID is returned separately. Stable field keys hash the Space/model/root identity and reconstruction path. Output files contain name, editor URL, system name and system URL. Public URLs and human-editor provenance are omitted when not reliably known.

After upload the returned HTML/XLIFF is stamped with target language and target editor/name references. Use **Upload content → returned Content** for downstream Add to lake, not the pre-upload translator file. For bilingual files the output remains bilingual.

## Events

| Event | Provider operations | Output |
|---|---|---|
| On content published | `publish`, `scheduledStart` | Content details; CMS ContentCreatedOrUpdated blueprint |
| On content unpublished | `unpublish`, `archive`, `scheduledEnd` | Content details |
| On content deleted | `delete` | Previous content details |

All events require Model ID. Optional filters: Content ID, Tag field, Tags (all-match). Content suggestions and tag-field suggestions depend on the required model. Tags depend on model plus tag field. Deletion filtering uses `previousValue`.

The app registers a **global Space webhook** and filters received events to the selected model. It reads existing webhook definitions from Space settings, retains them, appends only its callback, and uses the documented `updateWebhooks` mutation. Unsubscribe removes only the exact callback URL. Existing unknown settings are passed through; if the API rejects an unsupported setting, setup fails rather than silently removing it. Registration needs broader Space webhook permissions than content-only actions.

A custom verification header contains a model-scoped HMAC derived from the private key, not the key itself. Requests without the matching header are acknowledged without triggering a Flight. Rotate keys by re-establishing event subscriptions. Callback URLs use HTTPS. Payloads must be enabled. The implementation acknowledges non-matching events with HTTP 200/preflight.

Routine draft edits and all draft deletions are **not guaranteed** by Builder's documented webhook behavior. There is no misleading “On every content update” event. Webhooks can be delivered more than once; downstream operations should tolerate duplicate events. Upload and tag writes set `triggerWebhooks=false` to avoid loops. Explicit Publish/Unpublish actions allow normal provider notifications.

## Defaults and limits

- Default localization mode: field-level; no target language chosen automatically.
- Required source/target languages: explicit user selection.
- New translations: draft; existing published targets require an explicit unpublish step.
- No recursive reference/Symbol export. Process those entries separately.
- No CMS asset upload, model mutation actions, content deletion action or bulk ZIP export in this release. Projects agent operations are documented separately.
- Complex localized component inputs whose value is an entire object/list fail clearly; this release supports individually localized text inputs and model-schema object/list fields.
- Excluded visual blocks (`meta.excludeFromTranslation`) and explicitly excluded top-level component inputs (`meta.nonTranslatableInputs`) are skipped. Nested custom exclusion rules need live verification; do not assume every third-party component convention is understood.
- Arbitrary custom component text without localization markers is omitted; make those inputs explicitly localizable.
- Size hints are emitted for supported model `options.maxLength` constraints. Other custom validation rules remain enforced by Builder's API.
- Upload input size: 20 MB; reference entries are not bundled.
- Raw HTML fallback still requires the app's round-trip metadata. Generic website HTML cannot be imported.
- HTML/XLIFF conversions can normalize markup; actual CMS compatibility must be verified with your models.
- CMS rate-limit retries apply only to HTTP 429, five retries, honoring Retry-After seconds/date up to 120 seconds per delay, otherwise exponential backoff with jitter. Network errors, validation errors and ambiguous mutation failures are not blindly retried.
- Search/read/write operations use documented Content v3, Write v1 and Admin v2 APIs. API schema/entitlement changes can affect compatibility.

## Example Bird

A whole-entry translation flow avoids editing the live source entry:

1. **Builder.io — On content published:** select your Page model, Tag field and `localize` tag filter.
2. **Builder.io — Download content:** map Model ID and Content ID from the event; explicitly choose `en-US`; choose Whole-entry localization.
3. Translate/review the returned HTML using your preferred Blackbird translation actions, or convert to interoperable XLIFF for a TMS. Explicitly target `de-DE`.
4. **Builder.io — Upload content:** map the translated file and select `de-DE`. Leave Target content ID empty for an app-managed translation.
5. **Blacklake — Add to lake:** map the Content returned by Upload content.
6. Use a human review checkpoint.
7. **Builder.io — Publish content:** map Model ID and Target content ID from Upload content after approval.

Prevent the published target from re-entering the source translation Bird: before publishing, remove the `localize` tag from the target with Remove content tags. New whole-entry targets inherit source tags. Alternatively use an explicit source Content ID filter in a single-entry Bird. No locale event filter is claimed in this release.

## Builder Code / Projects

Use the Projects action group for Builder Code projects. These are separate from CMS models and entries; they do not appear in Model ID or Content ID. A Code Space showing a legacy Content screen does not mean published Projects become CMS entries. Projects support in this release is an integration candidate: its implementation is built and packaged, but authenticated Builder/Blackbird acceptance testing is still required.

Blackbird invokes specific advertised Builder MCP tools over HTTPS. No LLM chooses a tool or routes a Bird. Bird logic controls dependencies, approvals, branching and subsequent GitHub/GitLab steps. The three agent-submission actions explicitly invoke Builder's AI agent; discovery and status actions do not.

### Projects connection setup

Create a separate **Projects (OAuth)** connection. Existing **Space API keys** connections continue to work for CMS actions and events. Using either connection for the wrong family returns an actionable error.

1. Ask your Blackbird administrator for the exact OAuth authorization-code callback URI for the environment hosting this custom app. Do not guess the URL, add whitespace, or use the published Builder website URL.
2. Register a public client with Builder. The source ZIP includes `scripts/register-projects-oauth.py` (Python 3, standard library only). Run it once with the exact callback URI:

   ```bash
   python3 scripts/register-projects-oauth.py --redirect-uri 'EXACT_HTTPS_BLACKBIRD_CALLBACK_URI' --output /private/path/builder-projects-oauth.json
   ```

   Replace the placeholder before running. The script calls Builder's advertised registration endpoint and writes the response to a new private file (0600); it does not print credentials or overwrite an existing file. This step creates an OAuth registration and a private local PKCE signing key. If the request times out, check for an existing registration before retrying. A client has already been registered for `https://bridge.blackbird.io/api/AuthorizationCode`; use its supplied credentials file instead of registering again for this setup.
3. In Blackbird, add a Builder.io connection using **Projects (OAuth)**. Enter `client_id` as **Projects OAuth client ID** and `pkce_signing_key` as **PKCE signing key** from the credentials file generated by the script. The signing key is generated locally, not issued by Builder; keep it private and never commit the credentials file. One administrator-managed client can be used for connections in its registered environment, subject to Builder policy.
4. Connect, sign in to Builder and select the intended organization. The app requests project read, agent run, AI invocation and offline access scopes. Authorization does not grant access to other organizations or repositories automatically.
5. Run **Get Projects connection details** and check the organization before submitting any agent work. Then run Search projects and Get project branch with a disposable project.

Authentication uses authorization code, PKCE S256, public-client authentication (`none`), refresh tokens and token revocation. The PKCE verifier is derived using HMAC-SHA256 from the private, locally generated signing key and Blackbird's per-authorization state; it is never put in the authorization URL. Authorization/token requests include the MCP resource. Tokens refresh within two minutes of expiry. Tokens and the signing key are retained only in connection credentials, not action outputs or exported content files.

Live registration with the callback `https://bridge.blackbird.io/api/AuthorizationCode` returned a public client (`token_endpoint_auth_method=none`) and no client secret. Version 1.1.1 supports that response. The PKCE signing key remains in Blackbird's connection: it is not sent in token or revocation requests. Changing the key during an in-progress authorization invalidates that flow. Callback handling, token persistence, refresh and revoke still require end-to-end Blackbird verification. Builder discovery advertises `client_secret_post`, but the actual registration response is authoritative. Version 1.1.0's confidential-client setup must not be used for this registration.

### Projects permissions and access

| Scope or permission | Purpose |
|---|---|
| `builder:projects:read` | Discover/read authorized projects and branches |
| `builder:agents:run` | Submit Builder agent work |
| `builder:ai:invoke` | Requested for Builder AI invocation; account policy still applies |
| `offline_access` | Request refresh-token access for unattended Birds |
| Builder organization membership and project access | Restrict resources visible to the connection |
| Builder agent entitlement/credits | Needed for agent tasks; free access is not guaranteed |
| Separate GitHub/GitLab connection | Repository access, commits and PR/MR operations in other Blackbird apps |

The app requests all listed scopes for a Projects connection. It does not implement separate read-only OAuth scope profiles. Successful identity validation does not prove permission to run an agent or access every project. Builder's own repository integration and the Blackbird repository connection must each have the access required for their respective steps.

### Projects actions and all inputs

| Action | Required inputs | Optional inputs/defaults | Outputs |
|---|---|---|---|
| Get Projects connection details | None | None | User ID/name/email; organization ID/name |
| Search projects | None | Search text; omitted searches accessible projects | Projects collection |
| Get project | Project ID | None | Project details |
| Search project branches | Project ID | Search text | Branches collection |
| Get project branch | Project ID, Branch name | None | Branch details |
| Get branch prompt history | Project ID, Branch name | Maximum prompts: 50; allowed 1–200 | Project ID, branch name, prompts with optional title |
| Create project with agent | Prompt | Idempotency key | Run details; starts a new project |
| Start project task | Project ID, Prompt | New branch name; Idempotency key | Run details; starts work on a new branch |
| Continue branch task | Project ID, Branch name, Prompt | Idempotency key | Run details; continues an existing branch |
| Get project task | Run ID | None | Current run details |

**Project details:** Project ID, Name, Project URL, optional Main branch name, Creator ID and Creator name.

**Branch details:** Branch name, Project ID, Branch URL; optional Friendly name, Description, Category, State, Pull request status, Pull request URL, Agent state and Has unpushed changes. Null means Builder did not supply that value; it must not be interpreted as false or a successful push.

**Run details:** Run ID, Status, optional Project ID, Branch name and Preview URL; Prompt, optional Idempotency key, Created at, Updated at and optional Error (code/message). Status is a provider output: `submitted`, `working`, `completed` or `failed`. A successful submission is not completion. A failed run is returned with its status/error so a Bird can route it. Unknown future statuses should route to manual inspection.

### Projects dropdowns, defaults and limits

- Project ID uses live project discovery scoped to the selected OAuth organization. Names are readable; an ID suffix appears only for duplicate names. The stored value is the real project ID. Server-side search handles name matching. The picker follows pages up to 500 matches; the action follows up to 10,000 across at most 100 requests. More matches or invalid pagination produces a clear error asking for a narrower search.
- Branch name depends on Project ID and the same OAuth connection. Empty Project ID returns no suggestions; its description explains the prerequisite. Suggestions show friendly name plus the actual Git branch name when they differ. The stored value is the Git branch name. Search is sent to Builder. There is no documented branch pagination parameter, so none is invented. Results indicating `hasMore`, `truncated`, a higher total, or exceeding 500 picker / 10,000 action entries fail clearly. Undeclared provider truncation cannot be detected. The app requires structured JSON and never parses the provider's 50-branch prose summary as complete data.
- Manual and mapped IDs/branch names remain valid. No automatic clearing or conditional hiding is claimed. Required fields are SDK non-nullable inputs; custom Required/Advanced placement is not claimed.
- New branch name is free text because it names a branch that does not exist yet. When omitted, Builder generates a name. To operate on an existing branch use Continue branch task. The app does not select a source branch or assume a locale.
- Prompt is required text. Agent submissions can change code and consume credits. Explicitly state your intended changes and review them before merging or deploying.
- Run ID is manual/mapped because no run-listing capability is documented. Map it from the submission action. Idempotency key is manual/mapped text; use a unique persisted value per logical task and reuse it on retries. The app does not silently generate a new key on each retry.
- Maximum prompts is a typed integer, default 50, range 1–200. Prompt history contains user prompts only, in provider order; it is not a full agent conversation or complete history export.
- No automatic retries are performed for Projects requests or OAuth token exchanges. A network failure may occur after Builder accepts a task. Inspect Builder and reuse the same idempotency key before retrying. CMS rate-limit handling is separate.
- Each MCP invocation has a 90-second HTTP timeout. Initialization negotiates a supported protocol, retrieves the advertised tool catalogue (at most 20 pages), and matches a unique documented name/title. Input schemas are checked for incompatible required arguments. Missing/ambiguous tools, changed schemas or prose-only responses fail rather than call guessed endpoints.
- Actual authenticated tool names and structured payloads have not been captured in this environment. If Builder's advertised names/titles differ from its documentation, the app deliberately refuses to guess; an adapter update will be required after inspecting an authorized catalogue.
- No new Projects events are included. CMS publication events do not signal agent completion, Git pushes, PR approvals or Project deployment. Use Get project task in an explicitly bounded delay/recheck workflow.
- This version has no project-file download/upload, arbitrary source-file replacement, attachment upload, project deletion, design-system actions or repository linking action. It does not silently commit, merge or deploy through GitHub/GitLab. An agent prompt is not a deterministic replacement for a localization file upload.

### Example Projects and repository Bird

This is a workflow recipe, not an importable Bird or a claim that a customer's repository is already connected.

1. Use a manual trigger or an appropriate repository event. Set the project, source branch and explicit target locales in your Bird.
2. Optionally use **Get project branch** and **Get branch prompt history** for context. If internationalization setup is missing, use **Start project task** with a precise prompt to extract text into your chosen locale-file format, and persist a unique Idempotency key before submission.
3. Map the returned Run ID to **Get project task** after a delay. Repeat only while `submitted` or `working`, with a finite attempt/time limit. Route `failed` and unknown statuses to inspection. Do not resubmit the agent task on each status check.
4. At `completed`, review the resulting branch and its `Has unpushed changes` state. Ensure changes are pushed and visible in the repository before proceeding. Agent completion alone is not proof of a Git push, a PR, or a deployment. Perform the push/repository synchronization in Builder or your established repository workflow if needed.
5. Use the separate Blackbird GitHub/GitLab app to retrieve agreed language files from a specific branch/commit. Translate through your chosen translation/Blacklake workflow with explicit source and target languages; review outputs.
6. Write translated files to a translation branch through that repository app and open a pull/merge request. Use its repository/branch selectors and permissions. Review and merge through the customer's normal governance and CI/CD process.

For customers whose locale files already exist, start at the repository-file stage; Builder agent work is optional. Use the CMS Download/Upload pair only for actual Builder Content entries.

## Build and deploy

```bash
dotnet restore BuilderIo.sln
dotnet build BuilderIo.sln --configuration Release
dotnet publish Apps.BuilderIo/Apps.BuilderIo.csproj --configuration Release --output publish
```

Do not specify a runtime identifier or self-contained deployment. ZIP the **contents** of `publish`, including dependencies and `.deps.json`, with no enclosing folder. Blackbird's documented upload limit is below 16 MB (up to 15 MB accepted). Upload the app-package ZIP in Apps → My custom apps → Create app. The source ZIP is not the deployable package.

## Release versioning

Every distributed build must increment the numeric `Version` in `Apps.BuilderIo/Apps.BuilderIo.csproj`: 1.1.0 → 1.1.1 → 1.1.2. AssemblyVersion and FileVersion are derived as `$(Version).0`; InformationalVersion uses the same Version. Do not reuse a numeric version with a new rc suffix or merely rename the ZIP. The earlier rc.1 and rc.2 packages both contained assembly version 1.0.0.0.

The build rejects prerelease-only version labels. Supply the actual highest previously published numeric version to activate the monotonic-version guard:

```bash
dotnet publish Apps.BuilderIo/Apps.BuilderIo.csproj -c Release -p:PreviousPublishedVersion=1.1.0 -o publish
```

For the release after 1.1.1, set Version to 1.1.2 and PreviousPublishedVersion to 1.1.1. CI must pass this property to enforce the comparison; the project cannot discover the server's installed version. Release-status notes belong in documentation. Use a fresh publish directory and check the packaged DLL version before distribution. Existing CI builds without the comparison property remain supported.

## Validation and live tests

The shipped source includes 29 discoverable MSTest integration tests (16 CMS and 13 Projects tests) for every action, all dynamic dropdowns, connection validation, inline/whole-entry HTML round trips, XLIFF round trips, source-conflict rejection, pagination beyond 100 entries, webhook payload filtering and webhook subscribe/unsubscribe preservation. All tests use the real Builder API; no fake HTTP backend is presented as live validation.

Copy `Tests.BuilderIo/appsettings.json.example` to `Tests.BuilderIo/appsettings.json`, fill in test-Space keys, and configure at least two locales. Then run:

```bash
dotnet test Tests.BuilderIo/Tests.BuilderIo.csproj --configuration Release
```

Use an isolated disposable Space. Tests create/delete temporary models and their content; publishing tests briefly publish synthetic content. The webhook lifecycle test registers an intentionally non-delivering `example.invalid` HTTPS callback while checking configuration persistence, then removes it. It tests registration/removal, not actual webhook delivery. Verify delivery and filters in a real Blackbird Bird using a real callback before production. Failed test cleanup can leave disposable objects; inspect the test Space afterward.

Projects tests use `ProjectsConnectionDefinition.access_token` and the `ProjectsTest` fixture values from the example settings. Use a short-lived authorized token only in private local test settings; the token fixture does not test OAuth refresh. Set `AllowAgentMutations` to true only in a disposable organization to run the three submission tests. Those tests may consume credits and leave projects/branches to clean up manually because deletion is not implemented. Missing credentials or mutation consent marks tests skipped, not passed.

No API credentials were supplied for this build. Live tests, real webhook delivery, Blackbird app upload/activation and plan entitlements remain unverified. See release notes for build/package results.

## Troubleshooting content dropdown access

Version 1.0.0-rc.1 incorrectly supplied the private key as the Content API `apiKey` query parameter. This could produce an Access denied error even when Admin API connection validation succeeded. Upgrade to 1.0.2 or later and reopen the Content ID picker with a Model ID selected. Existing connection fields and action mappings remain compatible. If access is still denied, verify the selected model and private key permissions; this fix does not bypass provider access controls.

## Reference material

- https://www.builder.io/c/docs/builder-code-mcp-server
- https://www.builder.io/c/docs/agents-run-api
- https://mcp.builder.io/.well-known/oauth-authorization-server
- https://mcp.builder.io/.well-known/oauth-protected-resource/mcp/fusion
- https://docs.blackbird.io/sdk/connections/

- https://github.com/bb-io/Contentful — reference commit `f813479e7d0dfe5db83a1004991aab7db50571db`
- https://docs.blackbird.io/sdk/deploying/
- https://www.builder.io/c/docs/content-api
- https://www.builder.io/c/docs/write-api
- https://www.builder.io/c/docs/admin-api
- https://www.builder.io/c/docs/admin-api-content
- https://www.builder.io/c/docs/admin-api-space-config
- https://www.builder.io/c/docs/admin-api-manage-spaces
- https://www.builder.io/c/docs/localization-intro
- https://www.builder.io/c/docs/localization-whole-entry
- https://www.builder.io/c/docs/tags-and-views
- https://www.builder.io/c/docs/webhooks
- Supplied `skills-main(5).zip`; root app-builder, CMS round-trip and webhook conventions applied. The template repository was unavailable; scaffold follows its supplied specification and the accessible Braze/Contentful references.

## Localization default in the action UI

The Download content field is labeled **Localization mode (default: Field-level)**, and its static dropdown labels the field option **Field-level (default)**. An omitted mode still resolves to `field`; stored `field` and `entry` values are unchanged. No automatic UI preselection is claimed.

Builder documents inline and whole-entry localization as choices, not a universal account-wide default mode. Its guidance favors inline localization for a shared structure and whole-entry localization for locale-specific structures. Blackbird keeps field-level as its own default; it does not read or infer a provider default. A plain Text block without localized values requires explicit Whole-entry mode for this app's export. The Source language option Default is a value fallback, not a localization strategy.

## Projects tool mapping diagnostic (1.1.2)

If validation cannot identify a documented operation, the error now includes the actual tool count, match count, and the first 50 advertised names/titles. Labels are stripped of control characters and bounded to 100 characters each. The diagnostic includes no tokens, credentials, arguments, tool results, descriptions or content. Copy the full error text to app support so the adapter can be aligned with the actual catalogue. This diagnostic build does not claim to fix unknown tool mappings. It retains fail-closed matching and does not mark an unresolved connection valid.

The user's connection reached authenticated MCP discovery in 1.1.1 but failed to match Get authenticated user. No new OAuth registration or key change is indicated by that error. Keep the EU-1 client registered for https://eu-1.blackbird.io/api-rest/connections/AuthorizationCode and its PKCE signing key. Refresh/revoke and agent execution remain unverified. Deploy 1.1.2 and retry connection validation; if Blackbird requires a new consent flow, reuse the same EU-1 credentials.

## Verified tool identifiers (1.1.3)

The authorized connection's 1.1.2 diagnostic returned nine tools: run_fusion_agent, get_fusion_run, list_projects, get_project, who_am_i, list_branches, get_message_history, get_branch and list_design_systems. Version 1.1.3 explicitly maps Get authenticated user to who_am_i. The remaining implemented operations match their advertised identifiers under the existing normalization rules. List design systems remains outside the action scope. Identity mapping is used by connection validation and Get Projects connection details. Argument-schema checks, ambiguity rejection and diagnostic messages remain enabled.

Deploy 1.1.3 and retry connection validation using the existing EU-1 client ID and PKCE signing key. No registration change is required. The catalogue identifiers are now evidenced by a real authorized connection; identity response parsing, subsequent action calls, OAuth refresh/revoke and agent execution still require live verification. This fixes the observed name mismatch, not a claim that the complete workflow has passed.
