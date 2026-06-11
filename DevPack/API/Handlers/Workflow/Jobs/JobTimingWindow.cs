namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Immutable snapshot of the four job timing boundaries used to apply and validate node timings.
	/// The boundaries always satisfy <c>PreRollStart &lt;= Start &lt;= End &lt;= PostRollEnd</c> for a valid job.
	/// </summary>
	internal readonly struct JobTimingWindow
	{
		internal JobTimingWindow(DateTimeOffset preRollStart, DateTimeOffset start, DateTimeOffset end, DateTimeOffset postRollEnd)
		{
			PreRollStart = preRollStart;
			Start = start;
			End = end;
			PostRollEnd = postRollEnd;
		}

		internal DateTimeOffset PreRollStart { get; }

		internal DateTimeOffset Start { get; }

		internal DateTimeOffset End { get; }

		internal DateTimeOffset PostRollEnd { get; }

		/// <summary>
		/// Builds a window from the requested (in-memory) values of a job.
		/// </summary>
		internal static JobTimingWindow FromJob(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			return new JobTimingWindow(job.PreRollStart, job.Start, job.End, job.PostRollEnd);
		}

		/// <summary>
		/// Builds a window from a stored DOM instance, mirroring the pre-roll/post-roll fallbacks used when parsing a job.
		/// </summary>
		internal static JobTimingWindow FromInstance(StorageWorkflow.JobsInstance instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			var start = instance.JobInfo.JobStart.Value;
			var end = instance.JobInfo.JobEnd.Value;
			var preRollStart = instance.JobInfo.Preroll.HasValue ? instance.JobInfo.Preroll.Value : start;
			var postRollEnd = instance.JobInfo.Postroll.HasValue ? instance.JobInfo.Postroll.Value : end;

			return new JobTimingWindow(preRollStart, start, end, postRollEnd);
		}

		/// <summary>
		/// Determines which of the four timing boundaries differ from the supplied original window.
		/// </summary>
		internal JobTimingFieldChanges GetChanges(JobTimingWindow original)
		{
			return new JobTimingFieldChanges(
				PreRollStart != original.PreRollStart,
				Start != original.Start,
				End != original.End,
				PostRollEnd != original.PostRollEnd);
		}
	}

	/// <summary>
	/// Captures which of the four job timing boundaries changed between two windows.
	/// </summary>
	internal readonly struct JobTimingFieldChanges
	{
		internal JobTimingFieldChanges(bool preRollStartChanged, bool startChanged, bool endChanged, bool postRollEndChanged)
		{
			PreRollStartChanged = preRollStartChanged;
			StartChanged = startChanged;
			EndChanged = endChanged;
			PostRollEndChanged = postRollEndChanged;
		}

		internal bool PreRollStartChanged { get; }

		internal bool StartChanged { get; }

		internal bool EndChanged { get; }

		internal bool PostRollEndChanged { get; }

		/// <summary>
		/// Gets a value indicating whether any of the four timing boundaries changed.
		/// </summary>
		internal bool Any => PreRollStartChanged || StartChanged || EndChanged || PostRollEndChanged;

		/// <summary>
		/// Gets a value indicating whether a start-side boundary (pre-roll start or start) changed.
		/// </summary>
		internal bool AnyStartTimingChanged => PreRollStartChanged || StartChanged;

		/// <summary>
		/// Gets a value indicating whether an end-side boundary (end or post-roll end) changed.
		/// </summary>
		internal bool AnyEndTimingChanged => EndChanged || PostRollEndChanged;
	}
}
