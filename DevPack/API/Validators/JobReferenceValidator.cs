namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Resolves and validates the <see cref="DataReference"/> instances contained in the orchestration
	/// settings of a <see cref="Job"/> (both job level and node level). Only the capability, capacity and
	/// configuration settings are inspected here; orchestration event references are validated separately.
	/// </summary>
	internal sealed class JobReferenceValidator
	{
		private readonly ReferenceResolver resolver;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobReferenceValidator"/> class.
		/// </summary>
		/// <param name="resolver">The resolver used to resolve the references against the job's context.</param>
		public JobReferenceValidator(ReferenceResolver resolver)
		{
			this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
		}

		/// <summary>
		/// Resolves all settings references in the specified job and reports which references resolved to an
		/// actual value and which could not be resolved.
		/// </summary>
		/// <param name="job">The job whose settings references should be resolved.</param>
		/// <returns>A <see cref="JobReferenceResolution"/> describing the outcome.</returns>
		public JobReferenceResolution Resolve(Job job)
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			var unresolved = new List<DataReference>();
			var resolved = new Dictionary<DataReference, ResolvedValue>();

			foreach (var setting in EnumerateReferenceSettings(job))
			{
				var reference = setting.Reference;
				if (reference == null || resolved.ContainsKey(reference) || unresolved.Contains(reference))
				{
					continue;
				}

				ResolvedValue value;
				try
				{
					value = resolver.ResolveValue(reference);
				}
				catch (CircularReferenceException)
				{
					value = null;
				}

				if (value != null && value.IsResolved)
				{
					resolved[reference] = value;
				}
				else
				{
					unresolved.Add(reference);
				}
			}

			return new JobReferenceResolution(unresolved, resolved);
		}

		private static IEnumerable<Setting> EnumerateReferenceSettings(Job job)
		{
			foreach (var setting in EnumerateReferenceSettings(job.OrchestrationSettings))
			{
				yield return setting;
			}

			foreach (var node in job.NodeGraph.Nodes)
			{
				foreach (var setting in EnumerateReferenceSettings(node.OrchestrationSettings))
				{
					yield return setting;
				}
			}
		}

		private static IEnumerable<Setting> EnumerateReferenceSettings(OrchestrationSettings orchestrationSettings)
		{
			if (orchestrationSettings == null)
			{
				yield break;
			}

			foreach (var capability in orchestrationSettings.Capabilities)
			{
				yield return capability;
			}

			foreach (var capacity in orchestrationSettings.Capacities)
			{
				yield return capacity;
			}

			foreach (var configuration in orchestrationSettings.Configurations)
			{
				yield return configuration;
			}
		}
	}
}
