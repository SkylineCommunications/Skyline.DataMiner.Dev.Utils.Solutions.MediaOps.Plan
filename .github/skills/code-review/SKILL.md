---
name: code-review
description: >-
  Review pull requests in Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan
  completely in the first invocation, with concise findings, strict test coverage
  and delta-only follow-ups. Use for every PR review: public C# API, repositories,
  validation, persistence, querying, Automation/GQI/Protocol adapters, dependencies,
  NuGet packaging, simulation, and QAOps tests.
---

# MediaOps Plan DevPack code review

**Complete the entire PR review in the first invocation and publish all substantiated findings together. An immediate rerun of the same completed review scope must produce zero new remarks.** Concision limits wording, never investigation or finding count.

Priority: **correctness, then consistency, then best practices**. Test coverage is part of correctness. Understand the operation end to end before judging individual lines.

## Required context

Always read:

1. The PR title, description, accessible acceptance criteria, and complete changed-file list. Retrieve missing metadata through GitHub tools; record the base/merge base and head SHA.
2. [Solution map](references/solution-map.md): product goal, ownership, dependencies, and search routes.
3. [Testing](references/testing.md): mandatory behavior coverage and bug-fix gate.

Load additional references according to the changed execution path:

| Changed behavior or boundary | Reference |
| --- | --- |
| Resources, pools, workflows, jobs, recurrence, configuration, references, properties, relationships, queries, context adapters | [Plan](references/plan.md) |
| Orchestration events, dynamic inputs crossing into Live, connectivity, endpoints, virtual signal groups | [Live](references/live.md) |
| PNO-managed resources/pools, ownership, bookability, or compatibility with PNO consumers | [People & Organizations](references/people-and-organizations.md) |
| Category scopes, hierarchies, assignments, navigation, cache queries | [Categories](references/categories.md) |
| Public API compatibility, NuGet contents, DOM mapping, dependency versions, CI or test discovery | [Packaging and CI](references/packaging-and-ci.md) |
| Any external repository source, dependency tests, or linked documentation | [GitHub source retrieval](references/github-source.md): MCP access and exact-version resolution |

These maps guide source reading. Follow changed behavior across boundaries; a package reference alone does not require reviewing every domain it exposes. This repository owns the Plan API, not the application solution. Review local implementation/tests at the PR base/head; resolve versions separately for external dependencies and consumers.

## First-review completion gate

Review the full base-to-head semantic diff, including removals, callers, public interfaces/defaults, configuration, serialized contracts, persisted mappings, package contents and dependency versions. Formatting, behavior-preserving moves and unchanged pre-existing behavior are context; do not report historical defects or coverage debt. A new caller exposing an old limitation is reviewable through that new use and its consequence.

Keep a working checklist in review context: every semantic change, intended behavior, execution path, contracts, test evidence, candidate findings and reviewed/blocked status.

For every changed behavior, complete all of these before publishing:

1. **Trace the full flow.** Read the containing method/class, relevant callers and downstream consumers. Follow state readers, writers, ownership and lifetime; search symbols beyond changed files. Trace adapters, repositories, handlers, validation, transitions, persistence, queries and outputs. Inspect affected external callers when a changed contract reaches them; do not assume local callers are the only API consumers.
2. **Verify dependencies.** Use hosted GitHub MCP reads at the exact commits producing relevant external releases, following [GitHub source retrieval](references/github-source.md). Inspect relevant implementation, contracts and tests; compare both versions for upgrades. Retain package/version/repository/SHA provenance. Links, branch tips, later tests and local checkouts are not release evidence.
3. **Check reachable failures and safeguards.** Inspect affected boundaries and supported failure paths, including upstream validation, lifecycle/data invariants, shared helpers and downstream enforcement. Invalid input is reachable unless a verified contract prevents it; avoid impossible states or contrived failure chains.
4. **Prove meaningful coverage.** Apply [the testing gate](references/testing.md): map changed rules and cases to tests, assertions and discovery/execution wiring. For every bug fix, identify why existing tests missed it and how that exact gap is closed. Require regression evidence that detects the original defect; distinguish source inspection from demonstrated fail-before/pass-after results. Missing execution access does not waive coverage requirements; honor the real-environment execution boundary.
5. **Resolve every candidate.** Check it against the complete flow, contracts, guards and tests. Substantiate or reject it; deduplicate by root cause. Search existing coverage before claiming a test is missing.

**Before publishing, perform a separate omission sweep within this same invocation.** Reconcile the checklist against the complete changed-file list and diff, then recheck cross-file interactions, API/data compatibility, boundaries and test coverage. Finish any missed investigation now. Every semantic change must be accounted for and every candidate resolved.

Publish all substantiated findings together only after this gate. No sampling, early stopping, finding caps or deferred domain/testing passes. If required intent or source is unavailable, identify the blocked scope and mark the review incomplete; never invent evidence or claim completion.

## Concise, actionable findings

Report only concrete defects introduced by the reviewed changes or specific testing-gate violations:

- **Defect:** identify the responsible semantic change, realistic trigger, incorrect observable outcome and consequence, and why existing guards do not prevent it.
- **Coverage gap:** identify the changed rule or fixed bug, missing case/assertion or execution wiring, and smallest suitable test location. Untested nontrivial/business behavior violates the testing gate without needing an invented production failure.
- Anchor each finding to the relevant location. Use one short paragraph stating the trigger, consequence and smallest correction; include only evidence needed to act. No repeated explanations, speculative questions, inflated severity or duplicate call-site comments.
- Omit naming/formatting preferences, hypothetical scale, impossible null states and broad modernization. Consistency or best-practice advice must protect a concrete contract or benefit this change. Do not duplicate compiler/analyzer style remarks.
- Prefer existing helpers, platform features, standard libraries and installed dependencies. Fix root causes where callers converge; check sibling callers. Avoid speculative abstractions and dependencies; test observable behavior with the smallest meaningful test.

No actionable findings means no remarks.

## Follow-up reviews

Verify existing findings and review **previously reviewed head -> current head**. New findings must be caused by new semantic changes, including fix-induced regressions or coverage gaps and effects on otherwise unchanged callers/contracts. **No semantic delta after a completed review means zero new remarks.** Do not reopen completed scope to report previously missed issues.

1. Retrieve earlier findings, replies, resolution status and reviewed-head SHAs through GitHub MCP, including older dismissed findings. Do not assume history is automatically supplied.
2. Verify actual fixes and regression coverage. Keep unresolved findings in their original threads; zero new remarks does not mean they are fixed. Apply the completion gate to the new semantic scope.
3. Never repeat or repackage a dismissed finding, including summary reminders or test demands for the same declined change. Dismissed means explicitly declined or resolved without a relevant fix; resolution alone is not proof of a fix, and an outdated line alone is not dismissal. Match root cause and affected behavior, regardless of wording or location.
4. Account for rebases, base-branch changes and formatting churn without resetting completed coverage, dismissals or deduplication. Rewritten SHAs do not make unchanged behavior new or bring unrelated pre-existing defects into scope.

If the baseline or history is unavailable, request it and mark follow-up verification incomplete; preserve known dismissals and do not assume prior findings were fixed or accepted. Previously declared blocked scope remains incomplete until verified.
