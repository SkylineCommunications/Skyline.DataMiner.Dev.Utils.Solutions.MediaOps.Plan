namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Resolves and validates job node timings based on the owning job's state and timing window.
	/// </summary>
	/// <remarks>
	/// This component is intentionally pure (no DOM or API side effects beyond mutating the supplied node graph)
	/// so the per-state rules can be unit tested in isolation. Only the <see cref="JobState.Draft"/> branch is
	/// reachable in the first release; the other branches are implemented now so a future release can enable them
	/// by relaxing the state gate in the handler.
	/// </remarks>
	internal static class JobNodeTimingResolver
	{
		/// <summary>
		/// Validates the requested timing window against the rules that apply to the supplied job state.
		/// </summary>
		/// <param name="jobId">The identifier of the job, used to tag the produced errors.</param>
		/// <param name="state">The state of the job.</param>
		/// <param name="requested">The requested (in-memory) timing window.</param>
		/// <param name="original">The originally loaded timing window, or <see langword="null"/> for a new job.</param>
		/// <param name="currentTime">The reference time used to evaluate running-state rules.</param>
		/// <returns>The list of errors; empty when the requested window is allowed.</returns>
		internal static IReadOnlyList<MediaOpsErrorData> Validate(Guid jobId, JobState state, JobTimingWindow requested, JobTimingWindow? original, DateTimeOffset currentTime)
		{
			var errors = new List<MediaOpsErrorData>();

			// The change-aware rules only apply to existing jobs; a new job has no previous window to compare against.
			if (original == null)
			{
				return errors;
			}

			var changes = requested.GetChanges(original.Value);
			if (!changes.Any)
			{
				return errors;
			}

			switch (state)
			{
				case JobState.Draft:
				case JobState.Tentative:
				case JobState.Confirmed:
					// No additional state-based timing restrictions; the general validators cover ordering and required values.
					break;

				case JobState.Running:
					ValidateRunningTimingChanges(jobId, requested, original.Value, currentTime, changes, errors);
					break;

				case JobState.Completed:
				case JobState.Canceled:
					ValidateNoTimingChangesAllowed(jobId, requested, changes, errors);
					break;
			}

			return errors;
		}

		/// <summary>
		/// Validates only the absolute ordering of a timing window (<c>PreRollStart &lt;= Start &lt;= End &lt;= PostRollEnd</c>).
		/// </summary>
		/// <remarks>
		/// This is baseline-independent and therefore safe to run against a window produced by a concurrent field-level
		/// merge, where comparing against a single consumer's original window would be meaningless.
		/// </remarks>
		/// <param name="jobId">The identifier of the job, used to tag the produced errors.</param>
		/// <param name="window">The timing window to validate.</param>
		/// <returns>The list of ordering errors; empty when the chain is valid.</returns>
		internal static IReadOnlyList<MediaOpsErrorData> ValidateTimingChainOrdering(Guid jobId, JobTimingWindow window)
		{
			var errors = new List<MediaOpsErrorData>();

			if (window.PreRollStart > window.Start)
			{
				errors.Add(new JobInvalidPreRollError
				{
					ErrorMessage = "Pre-roll start cannot be after the job start time.",
					Id = jobId,
					PreRollStart = window.PreRollStart,
					Start = window.Start,
				});
			}

			if (window.End < window.Start)
			{
				errors.Add(new JobInvalidTimingError
				{
					ErrorMessage = "Start time must be before end time.",
					Id = jobId,
					Start = window.Start,
					End = window.End,
				});
			}

			if (window.PostRollEnd < window.End)
			{
				errors.Add(new JobInvalidPostRollError
				{
					ErrorMessage = "Post-roll end cannot be before the job end time.",
					Id = jobId,
					PostRollEnd = window.PostRollEnd,
					End = window.End,
				});
			}

			return errors;
		}

		/// <summary>
		/// Applies the timing window to every node in the graph according to the rules of the supplied job state.
		/// </summary>
		/// <remarks>
		/// The method is idempotent: a node time is only written when it actually differs from the current value, so
		/// re-applying an unchanged window produces no field-level diff (and therefore no spurious concurrency conflict).
		/// </remarks>
		/// <param name="state">The state of the job.</param>
		/// <param name="requested">The requested (in-memory) timing window.</param>
		/// <param name="currentTime">The reference time used to evaluate running-state rules.</param>
		/// <param name="nodeGraph">The node graph whose node timings should be aligned with the window.</param>
		internal static void Apply(JobState state, JobTimingWindow requested, DateTimeOffset currentTime, NodeGraph<JobNode> nodeGraph)
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			switch (state)
			{
				case JobState.Draft:
				case JobState.Tentative:
				case JobState.Confirmed:
					ApplyScheduledTimings(requested, nodeGraph);
					break;

				case JobState.Running:
					ApplyRunningTimings(requested, currentTime, nodeGraph);
					break;

				case JobState.Completed:
				case JobState.Canceled:
					// The timings are frozen for these states; node timings are left untouched.
					break;
			}
		}

		private static void ApplyScheduledTimings(JobTimingWindow requested, NodeGraph<JobNode> nodeGraph)
		{
			// For a job that has not started yet every node spans the full pre-roll/post-roll window.
			foreach (var node in nodeGraph.Nodes)
			{
				SetStartIfDifferent(node, requested.PreRollStart);
				SetEndIfDifferent(node, requested.PostRollEnd);
			}
		}

		private static void ApplyRunningTimings(JobTimingWindow requested, DateTimeOffset currentTime, NodeGraph<JobNode> nodeGraph)
		{
			// Rule 2.a: a node that was swapped out while the job is running keeps its start, but its end is truncated
			// to the current time and the original node is restored into the graph next to its replacement.
			foreach (var entry in nodeGraph.SwapMappings)
			{
				var swappedOut = entry.Key;
				if (swappedOut.End > currentTime)
				{
					swappedOut.End = currentTime;
				}

				nodeGraph.RestoreSwappedOutNode(swappedOut);
			}

			foreach (var node in nodeGraph.Nodes)
			{
				if (node.IsNew)
				{
					// A newly added node or a swap target starts at the current time and ends with the post-roll.
					SetStartIfDifferent(node, currentTime);
					SetEndIfDifferent(node, requested.PostRollEnd);
				}
				else if (node.End > currentTime)
				{
					// An existing, still-active node keeps its start; only its end is aligned with the post-roll.
					SetEndIfDifferent(node, requested.PostRollEnd);
				}

				// Rule 3: an existing node that already ended (End <= currentTime) is left untouched.
			}
		}

		private static void ValidateRunningTimingChanges(Guid jobId, JobTimingWindow requested, JobTimingWindow original, DateTimeOffset currentTime, JobTimingFieldChanges changes, List<MediaOpsErrorData> errors)
		{
			ValidateRunningBoundary<JobPreRollStartChangeNotAllowedError>(jobId, changes.PreRollStartChanged, original.PreRollStart, requested.PreRollStart, currentTime, "pre-roll start", errors);
			ValidateRunningBoundary<JobStartChangeNotAllowedError>(jobId, changes.StartChanged, original.Start, requested.Start, currentTime, "start", errors);
			ValidateRunningBoundary<JobEndChangeNotAllowedError>(jobId, changes.EndChanged, original.End, requested.End, currentTime, "end", errors);
			ValidateRunningBoundary<JobPostRollEndChangeNotAllowedError>(jobId, changes.PostRollEndChanged, original.PostRollEnd, requested.PostRollEnd, currentTime, "post-roll end", errors);
		}

		private static void ValidateRunningBoundary<TError>(Guid jobId, bool changed, DateTimeOffset originalValue, DateTimeOffset requestedValue, DateTimeOffset currentTime, string field, List<MediaOpsErrorData> errors)
			where TError : JobTimingChangeNotAllowedError, new()
		{
			if (!changed)
			{
				return;
			}

			if (originalValue <= currentTime)
			{
				// The boundary already lies in the past and can no longer be adapted.
				errors.Add(CreateChangeNotAllowed<TError>(jobId, requestedValue, $"The {field} of a running job cannot be changed because it already occurred."));
			}
			else if (requestedValue <= currentTime)
			{
				// The boundary is still in the future but is being moved to the current time or the past.
				errors.Add(CreateChangeNotAllowed<TError>(jobId, requestedValue, $"The {field} of a running job must be set to a time in the future."));
			}
		}

		private static void ValidateNoTimingChangesAllowed(Guid jobId, JobTimingWindow requested, JobTimingFieldChanges changes, List<MediaOpsErrorData> errors)
		{
			const string message = "Timings of a completed or canceled job cannot be changed.";

			if (changes.PreRollStartChanged)
			{
				errors.Add(CreateChangeNotAllowed<JobPreRollStartChangeNotAllowedError>(jobId, requested.PreRollStart, message));
			}

			if (changes.StartChanged)
			{
				errors.Add(CreateChangeNotAllowed<JobStartChangeNotAllowedError>(jobId, requested.Start, message));
			}

			if (changes.EndChanged)
			{
				errors.Add(CreateChangeNotAllowed<JobEndChangeNotAllowedError>(jobId, requested.End, message));
			}

			if (changes.PostRollEndChanged)
			{
				errors.Add(CreateChangeNotAllowed<JobPostRollEndChangeNotAllowedError>(jobId, requested.PostRollEnd, message));
			}
		}

		private static TError CreateChangeNotAllowed<TError>(Guid jobId, DateTimeOffset value, string message)
			where TError : JobTimingChangeNotAllowedError, new()
		{
			return new TError
			{
				ErrorMessage = message,
				Id = jobId,
				Value = value,
			};
		}

		private static void SetStartIfDifferent(JobNode node, DateTimeOffset start)
		{
			if (node.Start != start)
			{
				node.Start = start;
			}
		}

		private static void SetEndIfDifferent(JobNode node, DateTimeOffset end)
		{
			if (node.End != end)
			{
				node.End = end;
			}
		}
	}
}
