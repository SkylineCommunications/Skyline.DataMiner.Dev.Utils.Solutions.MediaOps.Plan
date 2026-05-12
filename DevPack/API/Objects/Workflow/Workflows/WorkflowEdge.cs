namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents an edge between two workflow nodes.
	/// </summary>
	public class WorkflowEdge
	{
		/// <summary>
		/// Gets or sets the parent node ID.
		/// </summary>
		public string ParentNodeId { get; set; }

		/// <summary>
		/// Gets or sets the child node ID.
		/// </summary>
		public string ChildNodeId { get; set; }

		/// <summary>
		/// Gets or sets the relationship action ID.
		/// </summary>
		public Guid? RelationshipActionId { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (ParentNodeId?.GetHashCode() ?? 0);
				hash = (hash * 23) + (ChildNodeId?.GetHashCode() ?? 0);
				hash = (hash * 23) + RelationshipActionId.GetHashCode();
				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not WorkflowEdge other)
			{
				return false;
			}

			return ParentNodeId == other.ParentNodeId
				&& ChildNodeId == other.ChildNodeId
				&& RelationshipActionId == other.RelationshipActionId;
		}

		internal static WorkflowEdge FromStorageSection(StorageWorkflow.NodeRelationshipsSection section)
		{
			if (section == null)
			{
				return null;
			}

			return new WorkflowEdge
			{
				ParentNodeId = section.ParentNodeID,
				ChildNodeId = section.ChildNodeID,
				RelationshipActionId = section.RelationshipAction,
			};
		}

		internal StorageWorkflow.NodeRelationshipsSection ToStorageSection()
		{
			return new StorageWorkflow.NodeRelationshipsSection
			{
				ParentNodeID = ParentNodeId,
				ChildNodeID = ChildNodeId,
				RelationshipAction = RelationshipActionId,
			};
		}
	}
}
