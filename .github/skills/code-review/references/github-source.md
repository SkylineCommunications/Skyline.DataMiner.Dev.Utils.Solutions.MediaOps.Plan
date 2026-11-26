# GitHub source retrieval

Load before using another repository's implementation, tests or documentation. This procedure targets **GitHub-hosted Copilot code review**. Repository aliases and source paths are in [solution-map](solution-map.md).

## Access through the hosted GitHub MCP server

Use the GitHub MCP tools available in the review session to retrieve PR metadata/history and external source. Do not assume that reading a Markdown hyperlink retrieves its target, or that review inherits developer checkouts, local credentials or local MCP servers.

The built-in GitHub MCP server's default token is scoped to the current repository. Wider access requires repository-side configuration described in [the maintainer setup guide](../../../COPILOT-REVIEW-SETUP.md). A listed server or successful current-repository read does not establish cross-repository access.

Discover available tools and follow their schemas. Use relevant repository, pull-request and Actions read tools explicitly. Do not ask a reviewer to paste a token into a PR or commit credentials. A denied read is a verification limitation, not a defect in the changed code.

This repository is public. Read-only credentials still expose source to the reviewer: use only authorized repositories and do not quote private implementation, internal documentation, customer information or secrets in public comments. If evidence cannot be safely disclosed, identify the restricted verification scope without reproducing its content.

## Local changes are not external dependencies

Review this repository's implementation/tests at the PR base/head. It is the producer of the Plan API being changed; an older released Plan package is not a substitute for that diff.

For an external consumer, identify the supported/relevant application or package release and the Plan version it actually consumes. Its released code can establish existing compatibility expectations, but it does not automatically implement an explicitly coordinated future contract. Distinguish those cases and record any missing consumer-version evidence.

## Resolve the version before reading behavior

For every external dependency reached by the changed execution path:

1. **Find the effective version at the PR head.** Read [Directory.Packages.props](../../../../Directory.Packages.props), the affected project and its imports/conditions. Account for `VersionOverride`, `PackageReference`, `PackageDownload` and central transitive pinning. A declared minimum is not necessarily the resolved version. Use lock/resolved-package metadata only if it belongs to the same revision and configuration.
2. **Identify the artifact being assessed.** Distinguish a compile-time NuGet package, deployed DLL and installed application package. Follow [deployment ownership](packaging-and-ci.md#dependency-and-deployment-contracts) when runtime installation matters. The Plan application installer and installed Live solution can own deployed binaries independently of this library's dependency declarations.
3. **Find the producing commit.** Prefer the published package's `.nuspec` repository commit or matching SourceLink/build provenance. Alternatively resolve the exact package-version tag and verify its correspondence to the release; consult publishing metadata if ambiguous. Include the full prerelease suffix. A similarly named branch, latest release, assembly version, or newer commit on the correct release branch is insufficient.
4. **Resolve to a full commit SHA.** Follow an annotated tag to its commit rather than using the tag object's SHA. Verify repository redirects/renames when package metadata uses an older repository name. Investigate conflicting provenance rather than choosing the newer revision.
5. **Pin every related read.** Read implementation, relevant callers, contracts, documentation and tests at that SHA. Resolve adapters such as `.Automation`, `.GQI` and Live `.Plan` according to their actual versions too. Do not infer a shared branch/release number across repositories.
6. **Compare the right sides.** For an upgrade, resolve both previous and proposed effective versions. For follow-up reviews, use the versions at the previous reviewed head and current head. Later dependency commits are not new changes in this PR unless its consumed version changes.

Keep a small working provenance record: `package or app ID -> effective version -> owner/repository -> full commit SHA -> evidence used to resolve it`. This is review context, not a new committed lockfile.

For the Plan, Live and Categories **application solution** repositories, use the relevant deployed/supported app release and its producing commit when assessing executable scripts, callbacks, schema or app behavior. DevPack NuGet versions do not identify the application release. A prerequisite minimum is a compatibility bound, not proof of the installed version. If only a range is known, establish the relevant supported contracts; do not silently select the latest or minimum as the actual deployment.

Package-feed access is separate from repository access. A GitHub source-reading token does not automatically permit downloading private NuGet packages. When package metadata is unavailable, use verified release/tag provenance through GitHub; if the producing revision remains unknown, mark the boundary unverified.

## Retrieve source at that commit

Use the repository name and path from the domain maps, replacing any navigation snapshot revision with the commit resolved above.

- For `get_file_contents`, supply `owner`, `repo`, `path`, and the resolved full commit in **`sha`**. That selector takes precedence over `ref`; do not pass a stale SHA alongside a new tag. If the available tool schema differs, use its documented exact-commit selector and verify the returned revision.
- Enumerate directories or the repository tree at the same commit to find callers/tests. Follow pagination and truncation indicators. A partial listing does not prove a file/test is absent.
- Default-branch code search is only a path/symbol discovery aid. Re-read candidates at the pinned SHA before using them as evidence. Locate renamed paths in that commit's tree rather than reading a newer replacement from `main`.
- Retrieve file contents, not just filenames, metadata, download links or search excerpts. Read enough surrounding source to establish the full execution path required by the main skill.
- Cite public evidence with immutable `https://github.com/OWNER/REPO/blob/FULL_COMMIT_SHA/PATH` links and useful line anchors. Directory navigation uses `/tree/FULL_COMMIT_SHA/PATH`; URL-encode spaces. Do not expose restricted content or inaccessible private links as if they were public evidence.

Tests added after the consumed commit do not count as that release's existing coverage. Establish its escape gap from the suite at that commit. For a dependency-upgrade fix, inspect old-version tests for the gap and proposed-version regression tests for closure, together with applicable Plan/consumer tests. A later green dependency workflow does not validate the consumed version. Clearly label later fixes/tests as history rather than silently replacing release evidence.

## Documentation and incomplete access

The maps contain local summaries so review does not depend on arbitrary external-link following. Retrieve authorized GitHub documentation through MCP when needed. DevPack documentation should come from the same package-producing commit. For separately versioned public/internal documentation, record its revision and distinguish current guidance, historical notes and roadmap material from the actual dependency contract.

When access, a tool, package provenance or required source is unavailable, name the repository/version/path and resulting unverified scope, subject to confidentiality. Continue independent checks that can be substantiated. Do not fall back silently to `main`, a local checkout, a navigation snapshot or a later suite; do not turn access limitations into speculative production or missing-test findings. Claim complete cross-repository verification only when necessary reads actually succeeded.
