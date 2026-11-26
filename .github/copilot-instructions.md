# MediaOps Plan DevPack repository instructions

This repository delivers the reusable MediaOps Plan .NET Framework 4.8 API, Automation/GQI/Protocol adapters, simulation support, and API tests. The NuGet package prefix is `Skyline.DataMiner.Dev.Utils.Solutions.MediaOps.Plan`; the C# namespace is `Skyline.DataMiner.Solutions.MediaOps.Plan`. Application scripts, low-code apps, and solution installation belong to separate repositories.

## Pull request reviews

For every code review, use [the code-review skill](skills/code-review/SKILL.md), its [solution map](skills/code-review/references/solution-map.md) and [testing gate](skills/code-review/references/testing.md). Load domain references for the changed behavior.

- **Complete the entire PR in the first review.** Trace every semantic change through callers, contracts, dependencies and tests. Perform an omission sweep before publishing all substantiated findings together. No sampling, early stopping, finding caps or deferred checks; identify blocked scope as incomplete.
- Read the PR intent first. Review changes introduced by this PR; formatting and unchanged pre-existing behavior are context. Prioritize correctness, then consistency, then best practices.
- Check public API, serialized/persisted data, query, and adapter compatibility at affected boundaries. Do not assume application UI restrictions protect direct API callers.
- Require meaningful automated coverage for every new or changed nontrivial/business behavior. Every bug fix must explain why existing tests missed it and close that exact gap.
- **An immediate rerun of the same completed scope must produce zero new remarks.** Follow-ups verify fixes and review new semantic changes and their effects. New findings must be caused by those changes; do not reopen completed scope for previously missed issues.
- Retrieve prior findings, replies, resolutions and reviewed-head SHAs through GitHub MCP. Keep unresolved findings in their original threads. Never repeat findings explicitly declined or resolved without a relevant fix; deduplicate by root cause and behavior. Missing history means incomplete verification.
- Keep each finding to one short, actionable paragraph: location, trigger, consequence and smallest correction. Prefer existing helpers and root-cause fixes. Concision limits wording, never coverage or finding count. No actionable findings means no remarks.

Review this repository's implementation and tests at the PR base/head, not an older published Plan package. For external source and tests, follow [GitHub source retrieval](skills/code-review/references/github-source.md): use hosted GitHub MCP reads at the exact commits producing the relevant releases. Links, branch tips, newer tests and local checkouts are not release evidence. Hosted review must not deploy or run tests against a real DataMiner Agent; use existing CI/QAOps results for that evidence.
