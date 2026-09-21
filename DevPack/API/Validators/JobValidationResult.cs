namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	/// <summary>Contains the errors produced while validating a job and synchronizes them to that job.</summary>
	public sealed class JobValidationResult
	{
		private readonly Dictionary<string, JobValidationError> errors = new Dictionary<string, JobValidationError>(StringComparer.Ordinal);
		private readonly HashSet<string> quarantinedNodeIds = new HashSet<string>(StringComparer.Ordinal);

		internal JobValidationResult(Job job)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
		}

		/// <summary>Gets the validated job.</summary>
		public Job Job { get; }

		/// <summary>Gets the errors produced by the validator.</summary>
		public IReadOnlyCollection<JobValidationError> Errors => errors.Values;

		/// <summary>Gets a value indicating whether validation produced errors.</summary>
		public bool HasErrors => errors.Count != 0;

		/// <summary>Gets the job node IDs whose resource usages are quarantined.</summary>
		public IReadOnlyCollection<string> QuarantinedNodeIds => quarantinedNodeIds;

		/// <summary>Determines whether validation produced the specified error code.</summary>
		public bool HasError(string errorCode)
		{
			if (String.IsNullOrWhiteSpace(errorCode))
			{
				throw new ArgumentException("Error code cannot be null or whitespace.", nameof(errorCode));
			}

			return errors.ContainsKey(errorCode);
		}

		/// <summary>Synchronizes validation errors and quarantined resource-node states to the job.</summary>
		/// <returns><see langword="true"/> when the job changed; otherwise, <see langword="false"/>.</returns>
		public bool SyncToJob()
		{
			var changed = false;
			var existingErrors = Job.Errors.ToDictionary(error => error.Code, StringComparer.Ordinal);

			foreach (var error in errors.Values)
			{
				if (!existingErrors.TryGetValue(error.Code, out var existing) || !existing.Equals(error))
				{
					Job.AddError(error);
					changed = true;
				}
			}

			foreach (var error in existingErrors.Values.Where(error => JobValidationError.MediaOpsOwnedErrorCodes.Contains(error.Code) && !errors.ContainsKey(error.Code)))
			{
				Job.RemoveError(error.Code);
				changed = true;
			}

			foreach (var node in Job.NodeGraph.Nodes.OfType<JobResourceNode>())
			{
				var hasError = quarantinedNodeIds.Contains(node.Id)
					|| (node.CoreReservationNodeId.HasValue && quarantinedNodeIds.Contains(node.CoreReservationNodeId.Value.ToString(CultureInfo.InvariantCulture)));
				if (node.HasError != hasError)
				{
					node.HasError = hasError;
					changed = true;
				}
			}

			return changed;
		}

		internal void SetError(JobValidationError error)
		{
			if (error == null)
			{
				throw new ArgumentNullException(nameof(error));
			}

			errors[error.Code] = error;
		}

		internal void AddQuarantinedNodeId(string nodeId)
		{
			if (!String.IsNullOrEmpty(nodeId))
			{
				quarantinedNodeIds.Add(nodeId);
			}
		}
	}
}