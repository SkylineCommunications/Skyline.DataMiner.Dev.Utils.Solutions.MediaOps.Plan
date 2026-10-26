namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.SRM.Capabilities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using CoreParameter = Net.Profiles.Parameter;

	/// <summary>
	/// Builds the core <see cref="ResourceCapabilityUsage"/> objects required for a capability value, including the usage on
	/// the linked time-dependent parameter of a time-dependent capability. The linked usage is what makes the core software
	/// reserve the value for the duration of the booking, so overlapping bookings cannot use the same resource with a
	/// different value. Resolving the links takes at most two profile parameter reads, regardless of the number of capabilities.
	/// </summary>
	internal sealed class TimeDependentCapabilityUsageBuilder
	{
		private readonly MediaOpsPlanApi planApi;
		private readonly Dictionary<Guid, CoreParameter> linkedParameterByParentId;
		private readonly Dictionary<Guid, Guid> unresolvedLinkedIdByParentId;

		private TimeDependentCapabilityUsageBuilder(MediaOpsPlanApi planApi, Dictionary<Guid, CoreParameter> linkedParameterByParentId, Dictionary<Guid, Guid> unresolvedLinkedIdByParentId)
		{
			this.planApi = planApi;
			this.linkedParameterByParentId = linkedParameterByParentId;
			this.unresolvedLinkedIdByParentId = unresolvedLinkedIdByParentId;
		}

		/// <summary>
		/// Gets the parent capability ID per linked time-dependent parameter ID, for all links that were resolved.
		/// </summary>
		public IReadOnlyDictionary<Guid, Guid> ParentCapabilityIdByLinkedId =>
			linkedParameterByParentId.ToDictionary(x => x.Value.ID, x => x.Key);

		public static TimeDependentCapabilityUsageBuilder Create(MediaOpsPlanApi planApi, IEnumerable<Guid> capabilityIds)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (capabilityIds == null)
			{
				throw new ArgumentNullException(nameof(capabilityIds));
			}

			var linkedParameterByParentId = new Dictionary<Guid, CoreParameter>();
			var unresolvedLinkedIdByParentId = new Dictionary<Guid, Guid>();

			var distinctIds = capabilityIds.Where(x => x != Guid.Empty).Distinct().ToList();
			if (distinctIds.Count == 0)
			{
				return new TimeDependentCapabilityUsageBuilder(planApi, linkedParameterByParentId, unresolvedLinkedIdByParentId);
			}

			var linkedIdByParentId = planApi.CoreHelpers.ProfileProvider.GetParametersById(distinctIds)
				.Where(x => x.HasLinkedTimeDependentParameter())
				.ToDictionary(x => x.ID, x => x.LinkedTimeDependentParameterId());

			if (linkedIdByParentId.Count == 0)
			{
				return new TimeDependentCapabilityUsageBuilder(planApi, linkedParameterByParentId, unresolvedLinkedIdByParentId);
			}

			var linkedParametersById = planApi.CoreHelpers.ProfileProvider.GetParametersById(linkedIdByParentId.Values.Distinct())
				.ToDictionary(x => x.ID);

			foreach (var link in linkedIdByParentId)
			{
				// Only text parameters can be assigned as time-dependent capabilities to resources.
				if (linkedParametersById.TryGetValue(link.Value, out var linkedParameter) && linkedParameter.Type == CoreParameter.ParameterType.Text)
				{
					linkedParameterByParentId[link.Key] = linkedParameter;
				}
				else
				{
					unresolvedLinkedIdByParentId[link.Key] = link.Value;
				}
			}

			return new TimeDependentCapabilityUsageBuilder(planApi, linkedParameterByParentId, unresolvedLinkedIdByParentId);
		}

		/// <summary>
		/// Creates the usages required for the given capability value. A time-dependent capability yields the usage on its
		/// linked parameter followed by the usage on the capability itself. A time-dependent capability of which the linked
		/// parameter could not be resolved yields nothing, as booking only the capability would not reserve the value.
		/// </summary>
		public IEnumerable<ResourceCapabilityUsage> CreateUsages(Guid capabilityId, string value)
		{
			if (unresolvedLinkedIdByParentId.TryGetValue(capabilityId, out var unresolvedLinkedId))
			{
				planApi.Logger.Error(this, $"Unable to find the linked time-dependent parameter with ID {unresolvedLinkedId} of capability {capabilityId}. The capability will not be assigned to the reservation.");
				yield break;
			}

			if (linkedParameterByParentId.TryGetValue(capabilityId, out var linkedParameter))
			{
				yield return new ResourceCapabilityUsage
				{
					CapabilityProfileID = linkedParameter.ID,
					RequiredString = value,
				};
			}

			yield return new ResourceCapabilityUsage
			{
				CapabilityProfileID = capabilityId,
				RequiredDiscreet = value,
			};
		}
	}
}
