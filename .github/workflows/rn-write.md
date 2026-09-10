---
description: |
  Automated release note writer that runs when a pull request is merged.
  Reads the PR diff and any linked issues, then posts a plain-language release
  note entry as a comment — written for a changelog audience rather than for
  PR reviewers. The comment includes a machine-readable anchor so a downstream
  publish workflow can push it to an external release note platform when a
  publish-rn label is applied.

on:
  pull_request_target:
    types: [closed, labeled]
  skip-bots: ["dependabot[bot]", "github-actions[bot]"]
  reaction: eyes

checkout: false

if: >
    github.event.pull_request.head.repo.owner.login == github.repository_owner &&
    (
      (github.event.action == 'closed' && github.event.pull_request.merged == true) ||
      (github.event.action == 'labeled' && github.event.label.name == 'rn-request' && github.event.pull_request.merged == true)
    )

permissions:
  copilot-requests: write
  contents: read
  issues: read
  pull-requests: read

network: defaults

safe-outputs:
  add-comment:
    max: 5
  add-labels:
    allowed: [rn-proposal]
    max: 1
  remove-labels:
    allowed: [rn-request]
    max: 1
  noop:
    max: 1

tools:
  web-fetch:
  github:
    toolsets: [pull_requests, issues]

timeout-minutes: 10
---

# Agentic Release Notes Writer
{{#runtime-import https://raw.githubusercontent.com/SkylineCommunications/_ReusableAgenticWorkflows/main/agents/rn-write.agent.md}}