namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Generic helper that copies an <see cref="OrchestrationSettings"/> instance into another one
	/// and retargets every embedded <see cref="DataReference"/> using a caller-supplied node-id map.
	/// </summary>
	/// <remarks>
	/// This helper is reusable for any "build X from Y" scenario where both X and Y expose orchestration settings
	/// (workflow ? job, workflow ? recurring job, job ? recurring job, ...).
	///
	/// The cloner is intentionally agnostic about the concrete object types involved; it only relies on the
	/// public <see cref="OrchestrationSettings"/> API. The only context it needs is a map describing how the
	/// node ids change between source and destination, which is typically produced by <see cref="NodeGraphCloner"/>.
	///
	/// Reference handling notes:
	/// - References pointing at nodes that were cloned have their <see cref="DataReference.NodeId"/> rewritten so
	///   they target the new destination node.
	/// - References pointing at nodes that are not part of the cloned graph are passed through untouched.
	/// - Workflow-/job-scoped references (name and property references) are intentionally left untouched for now;
	///   the property model on jobs and workflows is still being designed and the workflow-name use case has not been
	///   confirmed. Once those are clarified the type swaps can be added here in a single place.
	/// </remarks>
	internal static class OrchestrationSettingsCloner
	{
		/// <summary>
		/// Copies all settings from <paramref name="source"/> into <paramref name="destination"/> and retargets
		/// every embedded <see cref="DataReference"/> using <paramref name="nodeIdMap"/>.
		/// </summary>
		/// <param name="source">The orchestration settings to copy from. May be <see langword="null"/>, in which case nothing happens.</param>
		/// <param name="destination">The orchestration settings to copy into. May be <see langword="null"/>, in which case nothing happens.</param>
		/// <param name="nodeIdMap">A map from source node ids to destination node ids, as produced by <see cref="NodeGraphCloner"/>.</param>
		public static void Clone(OrchestrationSettings source, OrchestrationSettings destination, IReadOnlyDictionary<string, string> nodeIdMap)
		{
			if (source == null || destination == null)
			{
				return;
			}

			destination.SetCapabilities(source.Capabilities);
			destination.SetCapacities(source.Capacities);
			destination.SetConfigurations(source.Configurations);
			destination.SetOrchestrationEvents(source.OrchestrationEvents);

			// SetX wrappers re-use the source instances; replace shared references with retargeted copies so the
			// source object stays untouched.
			foreach (var setting in destination.Capabilities)
			{
				setting.Reference = RemapReference(setting.Reference, nodeIdMap);
			}

			foreach (var setting in destination.Capacities)
			{
				setting.Reference = RemapReference(setting.Reference, nodeIdMap);
			}

			foreach (var setting in destination.Configurations)
			{
				setting.Reference = RemapReference(setting.Reference, nodeIdMap);
			}

			// OrchestrationEvent.ExecutionDetails is shared with the source after SetOrchestrationEvents;
			// replace it with a deep clone that has its own retargeted references.
			foreach (var orchestrationEvent in destination.OrchestrationEvents)
			{
				if (orchestrationEvent.ExecutionDetails != null)
				{
					orchestrationEvent.ExecutionDetails = CloneExecutionDetails(orchestrationEvent.ExecutionDetails, nodeIdMap);
				}
			}
		}

		/// <summary>
		/// Creates an independent copy of <paramref name="source"/> in which every <see cref="DataReference"/>
		/// has been retargeted using <paramref name="nodeIdMap"/>.
		/// </summary>
		private static ScriptExecutionDetails CloneExecutionDetails(ScriptExecutionDetails source, IReadOnlyDictionary<string, string> nodeIdMap)
		{
			var clone = new ScriptExecutionDetails(source.ScriptName);

			foreach (var element in source.ScriptElements)
			{
				clone.AddScriptElement(new ScriptElementSetting(element.Name)
				{
					DmsElementId = element.DmsElementId,
					ElementName = element.ElementName,
					Reference = RemapReference(element.Reference, nodeIdMap),
				});
			}

			foreach (var parameter in source.ScriptParameters)
			{
				clone.AddScriptParameter(new ScriptParameterSetting(parameter.Name)
				{
					Value = parameter.Value,
					Reference = RemapReference(parameter.Reference, nodeIdMap),
				});
			}

			clone.SetCapabilities(source.Capabilities);
			clone.SetCapacities(source.Capacities);
			clone.SetConfigurations(source.Configurations);

			foreach (var setting in clone.Capabilities)
			{
				setting.Reference = RemapReference(setting.Reference, nodeIdMap);
			}

			foreach (var setting in clone.Capacities)
			{
				setting.Reference = RemapReference(setting.Reference, nodeIdMap);
			}

			foreach (var setting in clone.Configurations)
			{
				setting.Reference = RemapReference(setting.Reference, nodeIdMap);
			}

			return clone;
		}

		/// <summary>
		/// Returns a new <see cref="DataReference"/> with its <see cref="DataReference.NodeId"/> rewritten when it
		/// points at a node that was cloned. References that are unknown to the map are returned as-is.
		/// </summary>
		/// <remarks>
		/// Workflow-/job-scoped references (<c>WorkflowNameReference</c>, <c>WorkflowPropertyReference</c>,
		/// <c>JobNameReference</c>, <c>JobPropertyReference</c>) are intentionally not transformed here.
		/// Support for those will be added in a later phase once the property model on jobs and workflows is in place.
		/// </remarks>
		private static DataReference RemapReference(DataReference reference, IReadOnlyDictionary<string, string> nodeIdMap)
		{
			if (reference == null)
			{
				return null;
			}

			var nodeId = reference.NodeId;
			if (nodeId == null || !nodeIdMap.TryGetValue(nodeId, out var mappedNodeId))
			{
				// Reference does not point at a cloned node; keep it untouched.
				return reference;
			}

			return reference switch
			{
				ResourceNameReference _ => new ResourceNameReference(mappedNodeId),
				ResourceLinkedObjectIdReference _ => new ResourceLinkedObjectIdReference(mappedNodeId),
				ResourcePropertyReference rpr => new ResourcePropertyReference(rpr.ResourcePropertyId, mappedNodeId),
				CapabilityParameterReference cpr => new CapabilityParameterReference(cpr.ParameterId, mappedNodeId),
				CapacityParameterReference cpr => new CapacityParameterReference(cpr.ParameterId, mappedNodeId),
				ConfigurationParameterReference cpr => new ConfigurationParameterReference(cpr.ParameterId, mappedNodeId),
				_ => reference,
			};
		}
	}
}
