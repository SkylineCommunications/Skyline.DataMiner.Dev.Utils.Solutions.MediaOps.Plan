# Live and Plan integration map

Load when a change reaches orchestration, connectivity, Live-backed dynamic inputs, VSG references or Plan/Live synchronization. Pure Plan changes do not require the full Live implementation. Use [GitHub source retrieval](github-source.md) and [repository ownership](solution-map.md#ownership-across-repositories) to match external packages/apps to producing commits.

Within the Live DevPack, shorthand paths starting with `API/`, `Orchestration/`, `Mediation/`, `Take/`, or `DOM/` are under `Skyline.DataMiner.MediaOps.Live/`. Those paths are external; the Plan handlers below are local.

## Ownership and search routes

| Behavior | Owning source |
| --- | --- |
| Plan job -> Live configuration/events | Local [LiveJobConfigHandler](../../../../DevPack/API/Handlers/Workflow/Jobs/LiveJobConfigHandler.cs), callers in `DomJobHandler`, orchestration/reference validators |
| Resource/VSG references and installed-Live checks | Local resource handlers, API objects and `MediaOpsPlanApi.LiveApi`; follow the affected resource/pool operation |
| Event result -> Plan orchestration status | External Live DevPack `Skyline.DataMiner.MediaOps.Live.Plan/MediaOpsPlanHelper.cs` and `Extensions.cs`; local `JobsRepository.SetOrchestrationState` |
| Live event/job configuration and persistence | Live DevPack `API/` and `Orchestration/` |
| Scheduling/execution | Live DevPack `Orchestration/Scheduling/`, execution/script helpers; Live solution `ORC-AS-EventOrchestration/`, `ORC-AS-SlidingWindowScheduler/`, `ORC-GQIDS-Events/` |
| Script discovery and input contracts | Live DevPack `Skyline.DataMiner.MediaOps.Live.Automation/Orchestration/`, core script/input types; local orchestration/reference handling |
| VSG/endpoint/level/transport models | Live DevPack `API/Objects/`, `API/Repositories/`, `API/Validation/`, `DOM/` |
| Requested/observed connectivity | Live DevPack `Take/`, `API/Connectivity/`, `API/Caching/`, `API/Subscriptions/`, `Mediation/`; external mediation/device connectors when reached |
| Control Surface and VSG apps | Live solution `VSG-AS-*`, `VSG-GQIDS-ControlSurface/`, shared VSG code and application tests |

The compiled Live dependency, installed Live application and mediation/device connectors have separate ownership/version evidence. A DLL or package reference alone establishes none of the other installed components.

## Plan boundary

- Live is optional for Plan, and Plan is optional for Live. Verify the reached `IsInstalled`/registration checks. A deployed fallback DLL is not an installed application.
- Plan job IDs are used as Live job references by the integration, but Live jobs are independently usable. Not every Live event belongs to a Plan job.
- `LiveJobConfigHandler` maps timing, nodes, configuration and references into events. Inspect affected lifecycle states, existing/already-triggered events and rescheduling; do not infer behavior from one creation branch.
- Lightweight event/job objects and full configuration objects have different read/update contracts. Use the actual path before claiming configuration was lost.
- Live's `.Plan` helper reports orchestration outcomes. Plan solution core-scheduling callbacks handle running/completed transitions. Trace both before alleging a missing lifecycle update.
- Cancellation, return to tentative, reconfirmation, running graph edits and completion can require different event handling. Not every save must recreate/resave all events.
- Dynamic references may resolve from job/node/resource data before Live execution. Inspect both the stored reference and the concrete value delivered to the event/script.

## Orchestration and connectivity contracts

Trace affected events through configuration, scheduling, execution scripts, node/global scripts, connectivity and reported outcomes.

A global orchestration script can own node orchestration and connections. Without it, default orchestration handles those operations. Read that ownership contract before reporting missing automatic work or recommending a duplicate call. Profile parameters, ordinary script inputs/dummies, dynamic inputs and optional metadata are separate contracts; use the actual discovery/validation APIs.

- A VSG groups source/destination endpoints by levels. Level numbers, transport compatibility and multiple VSG memberships matter.
- A take request is not proof of actual connectivity. Mediation observes device state and invokes connector-specific handlers; observed updates establish the route.
- An unknown/unprovisioned source can be valid observed state. Trace destination identity and subscription results before treating it as corruption.
- Source-to-many-destinations, partial-level and shuffled-level routing use different request shapes. Check the caller's supported mode.
- Ad hoc locks and scheduled-job routing have intentional semantics. Validate the actual override contract before labeling it a bypass.
- Follow cache/subscription ownership, disposal, filtering and update propagation when changed. Do not require another polling/cache layer without a concrete need.

Connection-handler discovery and parameter contracts belong to the relevant Live/connector release. Tutorial parameter/table IDs are device examples, not universal constants.

## Tests and escape-gap investigation

- Local [LiveOrchestrationSimulationTests](../../../../DevPack.Tests/Workflow/Jobs/LiveOrchestrationSimulationTests.cs) exercises the actual Plan/Live APIs with simulated DOM, scheduler tasks and scripts. It does not have a real SRM engine or physical routing.
- Local `DevPack.Tests/Orchestration/`, `Generic/DataReferences/`, and `Workflow/Jobs/` contain reference, status, lifecycle and running-job simulation coverage. Follow the changed contract rather than requiring a test from every folder.
- Live DevPack `Skyline.DataMiner.MediaOps.Live.Tests/` owns its model/query/orchestration tests. The Live solution owns its executable-script, GQI and UI suites.
- Plan solution `SLC-S-MediaOps.Tests/EndToEnd/EndToEndTests.cs`, `SCH/OrchestrationErrorTests.cs` and RST/WFD/SCH dynamic-input Playwright tests can establish the actual application/runtime boundary. They are external, separately versioned and not run by this repository's local suite.

For a bug fix, establish the exact layer that escaped old tests. Mocked event objects or simulated script execution do not prove real scheduling/observed routing. Use release-matched external evidence where needed, keep verification limitations explicit, and honor [the real-environment execution boundary](testing.md#real-environment-execution-boundary).
