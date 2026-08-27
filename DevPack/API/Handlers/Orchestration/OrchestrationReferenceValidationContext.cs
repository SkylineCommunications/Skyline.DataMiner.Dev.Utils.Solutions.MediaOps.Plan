namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides, per orchestration settings instance, the <see cref="ReferenceResolver"/> that should be used to
	/// resolve the references contained in its orchestration events, together with the node that owns the settings
	/// and a flag indicating whether unresolved references should be reported as errors.
	/// </summary>
	internal sealed class OrchestrationReferenceValidationContext
	{
		private readonly IReadOnlyDictionary<Guid, (ReferenceResolver Resolver, string OwningNodeId, bool ReportErrors)> targetsBySettingsId;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationReferenceValidationContext"/> class.
		/// </summary>
		/// <param name="targetsBySettingsId">The validation targets keyed by orchestration settings identifier.</param>
		public OrchestrationReferenceValidationContext(IReadOnlyDictionary<Guid, (ReferenceResolver Resolver, string OwningNodeId, bool ReportErrors)> targetsBySettingsId)
		{
			this.targetsBySettingsId = targetsBySettingsId ?? throw new ArgumentNullException(nameof(targetsBySettingsId));
		}

		/// <summary>
		/// Tries to get the validation target for the specified orchestration settings instance.
		/// </summary>
		/// <param name="orchestrationSettingsId">The identifier of the orchestration settings instance.</param>
		/// <param name="resolver">When this method returns, contains the resolver to use, or <see langword="null"/> when no target exists.</param>
		/// <param name="owningNodeId">When this method returns, contains the identifier of the node that owns the settings, or <see langword="null"/> when they belong to the job itself.</param>
		/// <param name="reportErrors">When this method returns, indicates whether unresolved references should be reported as errors.</param>
		/// <returns><see langword="true"/> if a target exists for the specified instance; otherwise <see langword="false"/>.</returns>
		public bool TryGetTarget(Guid orchestrationSettingsId, out ReferenceResolver resolver, out string owningNodeId, out bool reportErrors)
		{
			if (targetsBySettingsId.TryGetValue(orchestrationSettingsId, out var target))
			{
				resolver = target.Resolver;
				owningNodeId = target.OwningNodeId;
				reportErrors = target.ReportErrors;
				return true;
			}

			resolver = null;
			owningNodeId = null;
			reportErrors = false;
			return false;
		}
	}
}
