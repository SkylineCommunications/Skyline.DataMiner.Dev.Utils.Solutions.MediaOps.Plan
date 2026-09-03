namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Per-job mutable view over a <see cref="JobRelationshipsContext"/>. Exposes a flat list of <see cref="JobRelationshipEndpoint"/> objects
	/// to the caller and translates the local state into <see cref="JobRelationshipsPersistenceActions"/> when it is time to persist.
	/// </summary>
	internal sealed class JobRelationshipsScope
	{
		private readonly Func<JobRelationshipsContext> getContext;

		private List<JobRelationshipEndpoint> links;
		private bool isDirty;

		internal JobRelationshipsScope(Func<JobRelationshipsContext> getContext)
		{
			this.getContext = getContext;
		}

		internal bool IsDirty => isDirty;

		internal IReadOnlyCollection<JobRelationshipEndpoint> RelationshipEndpoints => Current;

		private JobRelationshipsContext Context => getContext?.Invoke();

		private List<JobRelationshipEndpoint> Current => links ??= BuildInitialEndpoints();

		internal void AddRelationshipEndpoint(JobRelationshipEndpoint link)
		{
			if (link == null)
			{
				throw new ArgumentNullException(nameof(link));
			}

			// Adding a link that points at the same object replaces it, so a job never ends up with duplicates.
			var existing = Current.FirstOrDefault(x => PointsAtSameObject(x, link));
			if (existing != null)
			{
				existing.ObjectName = link.ObjectName;
				existing.Url = link.Url;
			}
			else
			{
				Current.Add(link);
			}

			isDirty = true;
		}

		internal void SetRelationshipEndpoints(IEnumerable<JobRelationshipEndpoint> newLinks)
		{
			if (newLinks == null)
			{
				throw new ArgumentNullException(nameof(newLinks));
			}

			var replacement = new List<JobRelationshipEndpoint>();
			foreach (var link in newLinks.Where(x => x != null))
			{
				var existing = replacement.FirstOrDefault(x => PointsAtSameObject(x, link));
				if (existing != null)
				{
					existing.ObjectName = link.ObjectName;
					existing.Url = link.Url;
					continue;
				}

				// Keep the stored identity so replacing the collection updates existing relationships instead of recreating them.
				var match = Current.FirstOrDefault(x => PointsAtSameObject(x, link));
				if (match != null && link.Id == Guid.Empty)
				{
					link.Id = match.Id;
					link.JobIsParent = match.JobIsParent;
				}

				replacement.Add(link);
			}

			links = replacement;
			isDirty = true;
		}

		internal void RemoveRelationshipEndpoint(JobRelationshipEndpoint link)
		{
			if (link == null)
			{
				throw new ArgumentNullException(nameof(link));
			}

			var existing = Current.FirstOrDefault(x => PointsAtSameObject(x, link));
			if (existing != null)
			{
				Current.Remove(existing);
			}

			isDirty = true;
		}

		/// <summary>
		/// Produces the relationships that have to be created, updated or deleted, or <see langword="null"/> when the scope was never mutated.
		/// </summary>
		/// <param name="jobObjectTypeId">The unique identifier of the reserved "Job" object type, resolved once per save.</param>
		internal JobRelationshipsPersistenceActions BuildPersistenceActions(Guid jobObjectTypeId)
		{
			if (!isDirty)
			{
				return null;
			}

			var context = Context ?? throw new InvalidOperationException(
				"Cannot persist job links because the owning context has not been wired. " +
				"Ensure the job's context is created (e.g. via EnsureRelationshipsContext) before saving.");

			var actions = new JobRelationshipsPersistenceActions();
			var current = Current;

			if (current.Count > 0 && jobObjectTypeId == Guid.Empty)
			{
				actions.JobObjectTypeMissing = true;
				return actions;
			}

			var retainedRelationshipIds = new HashSet<Guid>();

			foreach (var link in current)
			{
				if (link.Id != Guid.Empty && context.TryGetOriginalRelationship(link.Id, out var original))
				{
					retainedRelationshipIds.Add(link.Id);
					ApplyToRelationship(original, link, context, jobObjectTypeId);
					actions.ToCreateOrUpdate.Add(original);
					continue;
				}

				var relationship = new Relationship();
				ApplyToRelationship(relationship, link, context, jobObjectTypeId);
				link.Id = relationship.Id;
				actions.ToCreateOrUpdate.Add(relationship);
			}

			foreach (var original in context.OriginalRelationships.Where(x => !retainedRelationshipIds.Contains(x.Id)))
			{
				actions.ToDelete.Add(original);
			}

			return actions;
		}

		// Matching is on the object a link points at, not on its whole value: AddRelationshipEndpoint refreshes the name and URL of a
		// link that already points at the same object, and RemoveRelationshipEndpoint does not need them to be passed in. Without an
		// object id there is nothing to match on, so only the stored relationship tells two such links apart.
		private static bool PointsAtSameObject(JobRelationshipEndpoint left, JobRelationshipEndpoint right)
		{
			if (left.ObjectTypeId != right.ObjectTypeId)
			{
				return false;
			}

			if (String.IsNullOrWhiteSpace(left.ObjectId) || String.IsNullOrWhiteSpace(right.ObjectId))
			{
				return left.Id != Guid.Empty && left.Id == right.Id;
			}

			return String.Equals(left.ObjectId, right.ObjectId, StringComparison.Ordinal);
		}

		private static void ApplyToRelationship(Relationship relationship, JobRelationshipEndpoint link, JobRelationshipsContext context, Guid jobObjectTypeId)
		{
			var jobEndpoint = link.JobIsParent ? relationship.Parent : relationship.Child;
			var linkedEndpoint = link.JobIsParent ? relationship.Child : relationship.Parent;

			jobEndpoint.ObjectTypeId = jobObjectTypeId;
			jobEndpoint.ObjectId = context.ObjectId;
			jobEndpoint.ObjectName = context.OwnerName;

			linkedEndpoint.ObjectTypeId = link.ObjectTypeId;
			linkedEndpoint.ObjectId = link.ObjectId;
			linkedEndpoint.ObjectName = link.ObjectName;
			linkedEndpoint.Url = link.Url;
		}

		private List<JobRelationshipEndpoint> BuildInitialEndpoints()
		{
			var context = Context;

			return context?.InitialEndpoints.Select(x => new JobRelationshipEndpoint(x.ObjectTypeId, x.ObjectId, x.Id, x.JobIsParent)
			{
				ObjectName = x.ObjectName,
				Url = x.Url,
			}).ToList() ?? new List<JobRelationshipEndpoint>();
		}
	}

	internal sealed class JobRelationshipsPersistenceActions
	{
		internal List<Relationship> ToCreateOrUpdate { get; } = new List<Relationship>();

		internal List<Relationship> ToDelete { get; } = new List<Relationship>();

		internal bool JobObjectTypeMissing { get; set; }
	}
}
