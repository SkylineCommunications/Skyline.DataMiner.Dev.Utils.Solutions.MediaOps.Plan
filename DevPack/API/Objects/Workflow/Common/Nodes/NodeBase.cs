namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for all node implementations in workflows, jobs, and recurring jobs.
	/// This class represents common node properties used across different contexts.
	/// </summary>
	public abstract class NodeBase : TrackableObject, INode
	{
		private StorageWorkflow.NodesSection originalSection;
		private StorageWorkflow.NodesSection updatedSection;

		private protected NodeBase() : base()
		{
			Id = Guid.NewGuid().ToString();

			IsNew = true;

			OrchestrationSettings = new WorkflowOrchestrationSettings();
		}

		private protected NodeBase(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section)
		{
			ParseSection(planApi, section);
		}

		/// <inheritdoc/>
		public string Id { get; private set; }

		/// <summary>
		/// Gets or sets the alias or display name of the node.
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets or sets the icon of the node.
		/// </summary>
		public string IconImage { get; set; }

		/// <summary>
		/// Gets the orchestration settings assigned to this node.
		/// </summary>
		public OrchestrationSettings OrchestrationSettings { get; private set; }

		/// <summary>
		/// Determines whether this node represents a resource and, if so, returns it as an <see cref="IResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as an <see cref="IResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out IResourceNode resourceNode)
		{
			resourceNode = this as IResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as an <see cref="IResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as an <see cref="IResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out IResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as IResourcePoolNode;
			return resourcePoolNode != null;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not NodeBase other)
			{
				return false;
			}

			return Id == other.Id
				&& Alias == other.Alias
				&& IconImage == other.IconImage
				&& OrchestrationSettings == other.OrchestrationSettings;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Alias != null ? Alias.GetHashCode() : 0);
				hash = (hash * 23) + (IconImage != null ? IconImage.GetHashCode() : 0);
				hash = (hash * 23) + (OrchestrationSettings != null ? OrchestrationSettings.GetHashCode() : 0);

				return hash;
			}
		}

		/// <summary>
		/// Applies changes from this node to the specified storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to apply changes to.</param>
		internal abstract void ApplyChanges(StorageWorkflow.NodesSection section);

		/// <summary>
		/// Gets or creates a section with the current changes applied.
		/// </summary>
		/// <returns>A <see cref="StorageWorkflow.NodesSection"/> containing the current state of the node.</returns>
		internal StorageWorkflow.NodesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew
					? new StorageWorkflow.NodesSection()
					{
						NodeID = Id,
					}
					: originalSection.Clone();
			}

			updatedSection.NodeAlias = Alias;
			updatedSection.NodeIcon = IconImage;

			updatedSection.NodeConfiguration = OrchestrationSettings.Id;

			ApplyChanges(updatedSection);

			return updatedSection;
		}

		private void ParseSection(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.NodeID;
			Alias = section.NodeAlias;
			IconImage = section.NodeIcon;

			if (section.NodeConfiguration == null || section.NodeConfiguration == Guid.Empty)
			{
				OrchestrationSettings = new WorkflowOrchestrationSettings();
			}
			else
			{
				var domConfiguration = planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations([section.NodeConfiguration.Value]).FirstOrDefault();
				if (domConfiguration != null)
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings(planApi, domConfiguration);
				}
				else
				{
					OrchestrationSettings = new WorkflowOrchestrationSettings();
				}
			}
		}
	}
}
