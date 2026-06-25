namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Provides, per orchestration settings instance, the <see cref="ReferenceResolver"/> that should be used to
	/// resolve the references contained in its orchestration events, together with a flag indicating whether
	/// unresolved references should be reported as errors.
	/// </summary>
	internal interface IOrchestrationReferenceValidationContext
	{
		/// <summary>
		/// Tries to get the validation target for the specified orchestration settings instance.
		/// </summary>
		/// <param name="orchestrationSettingsId">The identifier of the orchestration settings instance.</param>
		/// <param name="resolver">When this method returns, contains the resolver to use, or <see langword="null"/> when no target exists.</param>
		/// <param name="reportErrors">When this method returns, indicates whether unresolved references should be reported as errors.</param>
		/// <returns><see langword="true"/> if a target exists for the specified instance; otherwise <see langword="false"/>.</returns>
		bool TryGetTarget(Guid orchestrationSettingsId, out ReferenceResolver resolver, out bool reportErrors);
	}
}
