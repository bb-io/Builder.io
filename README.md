# Blackbird.io Builder.io

Blackbird connects content systems and language technology through automated workflows.

## Introduction

<!-- begin docs -->

Connect Builder Content to translation, review and publishing workflows, and orchestrate Builder Code Projects with separate project, branch and agent actions. Localize individual fields or create separate entries for different markets, while retaining content structure and tags.

## Before setting up

You need a Builder Content Space, its public API key, and a private key for the same Space. An administrator can create private keys in Space Settings → Private Keys. The private key needs access to models, Space settings and the content you use. Events additionally need Space webhook administration. Native localization may require a paid Builder plan.

Configure your locales and enable localization on relevant text fields and component inputs. To use tags, add a non-localized custom field of type **tags** to the model.

## Connecting

1. Navigate to Apps and search for **Builder.io**.
2. Click **Add connection**.
3. Choose **Space API keys**, name your connection and enter the **Public API key** and **Private API key** from the same Content Space.
4. Click **Connect**.

## Connecting Projects

Choose **Projects (OAuth)** for the Projects action group. Register a public Builder OAuth client with the exact callback URI from your Blackbird administrator using `scripts/register-projects-oauth.py`, then enter its client ID and locally generated PKCE signing key in the connection. Sign in to Builder and select the intended organization. CMS API keys do not authorize Projects.

The app requests project read, agent run, AI invocation and offline-access scopes. Agent actions require appropriate Builder permissions and credits. See [technical documentation](docs/technical-documentation.md) for setup, scope details and limits. Projects support is an integration candidate pending authenticated Builder/Blackbird acceptance testing.

## Actions

### Projects

- **Get Projects connection details** identifies the authorized organization and user.
- **Search projects** and **Get project** find projects and their editor URLs.
- **Search project branches** and **Get project branch** inspect branches and pull request/agent state.
- **Get branch prompt history** returns prior user prompts; default 50, maximum 200.
- **Create project with agent** starts a new project from a required prompt.
- **Start project task** starts agent work on a new branch in the selected project.
- **Continue branch task** continues work on an existing selected branch.
- **Get project task** returns the run status. Submitted/working need later checks; completed/failed are terminal.

Project and branch inputs have contextual searchable dropdowns and support manual/mapped values. Run ID is mapped from the submission output. Reuse an explicit idempotency key when retrying a submission; the app does not automatically retry agent requests. No target locale, commit, merge or deployment decision is inferred.

For repository localization, use Blackbird's GitHub/GitLab apps to read locale files, translate/review them and open a PR/MR. These require their own authorized connections. Builder Projects actions complement that workflow; CMS Download/Upload actions do not transfer Project source files. No Projects completion webhook is included.

### Content

- **Search models** outputs accessible content models with their names and IDs.
- **Search locales** outputs configured languages and the explicit Default-value option.
- **Search content** finds entries in a selected model.
  - **Name contains**: filter by part of an entry name.
  - **Status**: choose Draft or Published.
  - **Updated after**: filter by the last update date.
  - **Tag field**: choose the model field used for tag filtering.
  - **Tags**: match entries containing all supplied tags; requires Tag field.
- **Get content** outputs an entry's name, status, tags, last update date and editor URL.
- **Publish content** publishes the entire entry, including every inline language.
- **Unpublish content** moves the entire entry to draft, affecting every inline language.

### Localization

- **Download content** exports localizable text as a self-describing file for translation. Select the model, entry and source language.
  - **Localization mode**: Field-level by default, or Whole-entry for a separate market-specific entry.
- **Upload content** writes a translated file into a draft and outputs the file with updated target references. Select the target language explicitly.
  - **Target content ID**: optionally link an existing whole-entry translation using a manual or mapped ID; otherwise the app reuses its stable target ID.

Uploads do not publish. A changed upload requires a draft target; explicitly unpublish a published target first and publish after review. For field-level localization this affects the complete source entry and its languages. Whole-entry mode creates a separate draft, so the live source can remain published.

Source changes after download cause upload to stop and request a fresh file. Repeat whole-entry uploads retain target-only layout and fields while updating matching translated fields. Whole-entry model lists require stable item IDs. References and Symbols are handled separately. Complex localized component object/list inputs are not supported in this release; individually localized text inputs are supported.

### Tags

- **Search tags** outputs existing tag values for a selected model and tag field.
- **Get content tags** outputs one entry's tags from a selected field.
- **Add content tags** adds tags to a draft without removing existing values or creating exact duplicates.
- **Remove content tags** removes selected tags from a draft while preserving the others.

Tags are never translated. Choose the model before the content and tag fields. Searchable suggestions support direct and mapped values. Tag suggestions aggregate existing content rather than a separate global tag catalogue.

## Events

- **On content published** starts when an entry is published or its scheduled start occurs.
- **On content unpublished** starts when published content is unpublished, archived or reaches its scheduled end.
- **On content deleted** starts when a supported content deletion notification is received.

Choose the model. Optionally filter by Content ID or by Tag field and Tags. Routine draft edits and all draft deletions are not guaranteed to trigger these events. Use source-only tag filters to avoid translating already translated entries again.

## Example workflow

On content published → Download content (Whole-entry, explicit source language) → Translate and review → Upload content (explicit target language) → Add returned file to Blacklake → Remove the source trigger tag from the target → Human approval → Publish the target entry.

## Limitations

This is a release candidate with live API and Blackbird activation testing pending. Content suggestions are bounded to 500 matching entries, and content/tag scans to 10,000; an exceeded limit is reported explicitly. Upload files are limited to 20 MB. Serialize writes to the same target; the provider API does not offer an atomic conflict guarantee in this implementation.

## Feedback

For feedback, reach out through [Blackbird](https://www.blackbird.io/) or create an issue in the app repository.

<!-- end docs -->
