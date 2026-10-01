namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	/// <summary>
	/// Finds the references in the orchestration events of orchestration settings that don't produce a value the setting,
	/// script input or dynamic input they feed can take.
	/// </summary>
	internal sealed class OrchestrationEventReferenceValidator
	{
		private readonly MediaOpsPlanApi planApi;
		private readonly ReferenceDefinitionCache definitions;

		public OrchestrationEventReferenceValidator(MediaOpsPlanApi planApi, ReferenceDefinitionCache definitions)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
		}

		/// <summary>
		/// Returns the references in the orchestration events of the settings that can't be used, with the reason why.
		/// </summary>
		/// <param name="orchestrationSettings">The settings whose orchestration events are checked.</param>
		/// <param name="resolver">The resolver for the job or workflow that holds the settings.</param>
		/// <param name="owningNodeId">The node that holds the settings, or <see langword="null"/> when they belong to the job itself.</param>
		/// <returns>The references that can't be used, with a reason that completes the sentence "Reference 'X' ...".</returns>
		public IEnumerable<(DataReference Reference, string Reason)> GetFailures(OrchestrationSettings orchestrationSettings, ReferenceResolver resolver, string owningNodeId)
		{
			if (orchestrationSettings == null)
			{
				throw new ArgumentNullException(nameof(orchestrationSettings));
			}

			if (resolver == null)
			{
				throw new ArgumentNullException(nameof(resolver));
			}

			var executionDetails = orchestrationSettings.OrchestrationEvents.Select(x => x.ExecutionDetails).Where(x => x != null).ToList();

			foreach (var entry in executionDetails.SelectMany(EnumerateExecutionDetailReferences))
			{
				var reason = GetUnresolvedReason(resolver, entry, owningNodeId);
				if (reason != null)
				{
					yield return (entry.Reference, reason);
				}
			}

			foreach (var failure in executionDetails.SelectMany(x => GetDynamicInputFailures(resolver, x, owningNodeId)))
			{
				yield return failure;
			}
		}

		private static ResolvedValue ResolveReference(ReferenceResolver resolver, DataReference reference, string owningNodeId)
		{
			try
			{
				return resolver.ResolveValue(reference, owningNodeId);
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static IEnumerable<(DataReference Reference, Guid? TargetParameterId)> EnumerateExecutionDetailReferences(ScriptExecutionDetails executionDetails)
		{
			return executionDetails.ScriptElements.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)null))
				.Concat(executionDetails.ScriptParameters.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)null)))
				.Concat(executionDetails.Capabilities.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)x.Id)))
				.Concat(executionDetails.Capacities.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)x.Id)))
				.Concat(executionDetails.Configurations.Where(x => x.HasReference).Select(x => (x.Reference, (Guid?)x.Id)));
		}

		// Returns null when the reference produced a value the parameter it feeds can take. A parameter can be a
		// dropdown, which only holds one of its own options, so the resolved value has to fit it as well.
		private string GetUnresolvedReason(ReferenceResolver resolver, (DataReference Reference, Guid? TargetParameterId) entry, string owningNodeId)
		{
			var resolved = ResolveReference(resolver, entry.Reference, owningNodeId);

			// A script element or parameter has no target parameter, so it takes any value.
			var target = entry.TargetParameterId == null
				? null
				: definitions.GetParameterDefinition(entry.TargetParameterId.Value);

			return ResolvedValueConverter.GetFailureReason(resolved, target);
		}

		// A linked dynamic input is checked against the field it feeds, like a linked profile parameter is.
		private IEnumerable<(DataReference Reference, string Reason)> GetDynamicInputFailures(ReferenceResolver resolver, ScriptExecutionDetails executionDetails, string owningNodeId)
		{
			var linkedInputs = executionDetails.DynamicInputs.Where(x => x.HasReference).ToList();
			if (linkedInputs.Count == 0)
			{
				yield break;
			}

			var inputs = GetDynamicInputDefinition(executionDetails);

			foreach (var input in linkedInputs)
			{
				var resolved = ResolveReference(resolver, input.Reference, owningNodeId);

				// A link to an input the script no longer has is kept for when it returns, so it is only checked for a value.
				var reason = inputs != null && inputs.TryGetField(input.Path, out var field)
					? ResolvedValueConverter.GetFailureReason(resolved, field)
					: ResolvedValueConverter.GetFailureReason(resolved, (Parameter)null);

				if (reason != null)
				{
					yield return (input.Reference, reason);
				}
			}
		}

		private OrchestrationInputDefinition GetDynamicInputDefinition(ScriptExecutionDetails executionDetails)
		{
			try
			{
				// The values of the event decide which inputs exist and what they accept.
				return planApi.LiveApi.Orchestration.Scripts.GetOrchestrationScriptInputInfo(executionDetails.ScriptName, executionDetails.GetDynamicInputValues())?.InputDefinition;
			}
			catch (Exception ex)
			{
				planApi.Logger.Warning(this, $"Failed to evaluate the inputs of orchestration script '{executionDetails.ScriptName}', so its linked inputs are only checked for a value: {ex.Message}");
				return null;
			}
		}
	}
}
