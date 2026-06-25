namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Dictionary-backed implementation of <see cref="IOrchestrationReferenceValidationContext"/>.
	/// </summary>
	internal sealed class OrchestrationReferenceValidationContext : IOrchestrationReferenceValidationContext
	{
		private readonly IReadOnlyDictionary<Guid, (ReferenceResolver Resolver, bool ReportErrors)> targetsBySettingsId;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationReferenceValidationContext"/> class.
		/// </summary>
		/// <param name="targetsBySettingsId">The validation targets keyed by orchestration settings identifier.</param>
		public OrchestrationReferenceValidationContext(IReadOnlyDictionary<Guid, (ReferenceResolver Resolver, bool ReportErrors)> targetsBySettingsId)
		{
			this.targetsBySettingsId = targetsBySettingsId ?? throw new ArgumentNullException(nameof(targetsBySettingsId));
		}

		/// <inheritdoc/>
		public bool TryGetTarget(Guid orchestrationSettingsId, out ReferenceResolver resolver, out bool reportErrors)
		{
			if (targetsBySettingsId.TryGetValue(orchestrationSettingsId, out var target))
			{
				resolver = target.Resolver;
				reportErrors = target.ReportErrors;
				return true;
			}

			resolver = null;
			reportErrors = false;
			return false;
		}
	}
}
