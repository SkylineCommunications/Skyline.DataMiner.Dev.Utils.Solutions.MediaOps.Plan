namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.ServiceManager.Objects;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for all node implementations in workflows, jobs, and recurring jobs.
	/// This class represents common node properties used across different contexts.
	/// </summary>
	public abstract class NodeBase : TrackableObject
	{
		private protected NodeBase() : base()
		{
		}

		private protected NodeBase(StorageWorkflow.NodesSection section)
		{

		}

		/// <summary>
		/// Gets the unique identifier of the node, which is assigned by the system and cannot be modified by users.
		/// </summary>
		public int Id { get; internal set; }

		/// <summary>
		/// Gets or sets the alias or display name of the node.
		/// </summary>
		public string Alias { get; set; }
	}

	public abstract class JobNode : NodeBase
	{
		private protected JobNode() : base()
		{
		}

		private protected JobNode(StorageWorkflow.NodesSection section) : base(section)
		{

		}

	}

	public abstract class WorkflowNode : NodeBase
	{
	}

	public abstract class RecurringJobNode : NodeBase
	{
	}
}
