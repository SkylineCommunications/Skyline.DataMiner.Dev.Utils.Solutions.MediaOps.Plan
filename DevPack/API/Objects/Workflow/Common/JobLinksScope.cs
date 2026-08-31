namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Per-job mutable view over a <see cref="JobLinksContext"/>. Exposes a flat list of <see cref="JobLink"/> objects
	/// to the caller and translates the local state into <see cref="JobLinksPersistenceActions"/> when it is time to persist.
	/// </summary>
	internal sealed class JobLinksScope
	{
		private readonly Func<JobLinksContext> getContext;

		private List<JobLink> links;
		private bool isDirty;

		internal JobLinksScope(Func<JobLinksContext> getContext)
		{
			this.getContext = getContext;
		}

		internal bool IsDirty => isDirty;

		internal IReadOnlyCollection<JobLink> Links => Current;

		private JobLinksContext Context => getContext?.Invoke();

		private List<JobLink> Current => links ??= BuildInitialLinks();

		internal void AddLink(JobLink link)
		{
			if (link == null)
			{
				throw new ArgumentNullException(nameof(link));
			}

			// Adding a link that points at the same object replaces it, so a job never ends up with duplicates.
			var existing = Current.FirstOrDefault(x => x.Equals(link));
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

		internal void SetLinks(IEnumerable<JobLink> newLinks)
		{
			if (newLinks == null)
			{
				throw new ArgumentNullException(nameof(newLinks));
			}

			var replacement = new List<JobLink>();
			foreach (var link in newLinks.Where(x => x != null))
			{
				var existing = replacement.FirstOrDefault(x => x.Equals(link));
				if (existing != null)
				{
					existing.ObjectName = link.ObjectName;
					existing.Url = link.Url;
					continue;
				}

				// Keep the stored identity so replacing the collection updates existing relationships instead of recreating them.
				var match = Current.FirstOrDefault(x => x.Equals(link));
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

		internal void RemoveLink(JobLink link)
		{
			if (link == null)
			{
				throw new ArgumentNullException(nameof(link));
			}

			var existing = Current.FirstOrDefault(x => x.Equals(link));
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
		internal JobLinksPersistenceActions BuildPersistenceActions(Guid jobObjectTypeId)
		{
			if (!isDirty)
			{
				return null;
			}

			var context = Context ?? throw new InvalidOperationException(
				"Cannot persist job links because the owning context has not been wired. " +
				"Ensure the job's context is created (e.g. via EnsureLinksContext) before saving.");

			var actions = new JobLinksPersistenceActions();
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

		private static void ApplyToRelationship(Relationship relationship, JobLink link, JobLinksContext context, Guid jobObjectTypeId)
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

		private List<JobLink> BuildInitialLinks()
		{
			var context = Context;

			return context?.InitialLinks.Select(x => new JobLink(x.ObjectTypeId, x.ObjectId, x.Id, x.JobIsParent)
			{
				ObjectName = x.ObjectName,
				Url = x.Url,
			}).ToList() ?? new List<JobLink>();
		}
	}

	internal sealed class JobLinksPersistenceActions
	{
		internal List<Relationship> ToCreateOrUpdate { get; } = new List<Relationship>();

		internal List<Relationship> ToDelete { get; } = new List<Relationship>();

		internal bool JobObjectTypeMissing { get; set; }
	}
}
