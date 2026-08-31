namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Owner-scoped context that lazily loads every <see cref="Relationship"/> a job takes part in and hands out a
	/// <see cref="JobLinksScope"/> that hides the storage details (the reserved "Job" object type, the parent/child
	/// sides and the job's own object id) from the user.
	/// </summary>
	internal sealed class JobLinksContext
	{
		private static readonly IReadOnlyCollection<JobLink> EmptyLinks = [];

		private readonly Guid ownerId;
		private readonly Func<string> getOwnerName;
		private readonly Lazy<LoadedLinks> lazy;

		internal JobLinksContext(MediaOpsPlanApi planApi, Guid ownerId, Func<string> getOwnerName)
		{
			this.ownerId = ownerId;
			this.getOwnerName = getOwnerName;

			lazy = new Lazy<LoadedLinks>(() => Load(planApi, ownerId));
		}

		/// <summary>
		/// Gets the object id the job is stored with. The solution stores it as a plain string, so the formatting has to match exactly.
		/// </summary>
		internal string ObjectId => ownerId.ToString();

		/// <summary>
		/// Gets the name of the job, used as the denormalized snapshot on the job side of every link.
		/// </summary>
		internal string OwnerName => getOwnerName?.Invoke();

		internal IReadOnlyCollection<JobLink> InitialLinks => lazy.Value.Links;

		internal IReadOnlyCollection<Relationship> OriginalRelationships => lazy.Value.Relationships.Values.ToList();

		internal JobLinksScope CreateOwnerScope() => new JobLinksScope(() => this);

		internal bool TryGetOriginalRelationship(Guid id, out Relationship relationship)
			=> lazy.Value.Relationships.TryGetValue(id, out relationship);

		/// <summary>
		/// Returns every relationship already loaded by the context, or <see langword="null"/> when nothing has triggered
		/// the lazy load yet. Callers can use this to avoid forcing a load when none is required (e.g. before a delete).
		/// </summary>
		internal IReadOnlyCollection<Relationship> TryGetCachedOriginalRelationships()
		{
			if (!lazy.IsValueCreated)
			{
				return null;
			}

			return lazy.Value.Relationships.Values.ToList();
		}

		/// <summary>
		/// Builds the symmetric filter that finds every relationship an object takes part in, on either side.
		/// This matches how the solution looks up the links of a job.
		/// </summary>
		internal static FilterElement<Relationship> BuildLinkedObjectFilter(Guid objectTypeId, IEnumerable<string> objectIds)
		{
			var filters = objectIds
				.Select(objectId => new ANDFilterElement<Relationship>(RelationshipExposers.Parent.ObjectTypeId.Equal(objectTypeId), RelationshipExposers.Parent.ObjectId.Equal(objectId))
					.OR(new ANDFilterElement<Relationship>(RelationshipExposers.Child.ObjectTypeId.Equal(objectTypeId), RelationshipExposers.Child.ObjectId.Equal(objectId))))
				.ToArray();

			return new ORFilterElement<Relationship>(filters);
		}

		/// <summary>
		/// Resolves the reserved "Job" object type. It is seeded by the solution at setup time, so it has to be looked up by name.
		/// </summary>
		internal static Guid ResolveJobObjectTypeId(MediaOpsPlanApi planApi)
		{
			var objectType = planApi.RelationshipObjectTypes
				.Read(RelationshipObjectTypeExposers.Name.Equal(RelationshipObjectType.JobObjectTypeName))
				.FirstOrDefault();

			return objectType?.Id ?? Guid.Empty;
		}

		private static LoadedLinks Load(MediaOpsPlanApi planApi, Guid ownerId)
		{
			if (planApi == null)
			{
				// New/unsaved job: nothing has ever been persisted yet.
				return new LoadedLinks(Guid.Empty, EmptyLinks, new Dictionary<Guid, Relationship>());
			}

			var jobObjectTypeId = ResolveJobObjectTypeId(planApi);
			if (jobObjectTypeId == Guid.Empty)
			{
				return new LoadedLinks(Guid.Empty, EmptyLinks, new Dictionary<Guid, Relationship>());
			}

			var objectId = ownerId.ToString();
			var relationships = planApi.Relationships
				.Read(BuildLinkedObjectFilter(jobObjectTypeId, [objectId]))
				.ToList();

			var links = new List<JobLink>();
			var relationshipsById = new Dictionary<Guid, Relationship>();

			foreach (var relationship in relationships)
			{
				relationshipsById[relationship.Id] = relationship;
				links.Add(ToJobLink(relationship, jobObjectTypeId, objectId));
			}

			return new LoadedLinks(jobObjectTypeId, links, relationshipsById);
		}

		/// <summary>
		/// Projects a relationship onto the endpoint that is not the job.
		/// </summary>
		internal static JobLink ToJobLink(Relationship relationship, Guid jobObjectTypeId, string jobObjectId)
		{
			var jobIsParent = relationship.Parent != null
				&& relationship.Parent.ObjectTypeId == jobObjectTypeId
				&& String.Equals(relationship.Parent.ObjectId, jobObjectId, StringComparison.Ordinal);

			var other = jobIsParent ? relationship.Child : relationship.Parent;

			return new JobLink(other?.ObjectTypeId ?? Guid.Empty, other?.ObjectId, relationship.Id, jobIsParent)
			{
				ObjectName = other?.ObjectName,
				Url = other?.Url,
			};
		}

		private sealed class LoadedLinks
		{
			internal LoadedLinks(Guid jobObjectTypeId, IReadOnlyCollection<JobLink> links, Dictionary<Guid, Relationship> relationships)
			{
				JobObjectTypeId = jobObjectTypeId;
				Links = links;
				Relationships = relationships;
			}

			internal Guid JobObjectTypeId { get; }

			internal IReadOnlyCollection<JobLink> Links { get; }

			internal Dictionary<Guid, Relationship> Relationships { get; }
		}
	}
}
