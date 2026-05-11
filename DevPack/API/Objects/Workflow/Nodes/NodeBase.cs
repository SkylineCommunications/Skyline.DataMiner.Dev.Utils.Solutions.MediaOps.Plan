namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.ServiceManager.Objects;

	/// <summary>
	/// Base class for all node implementations in workflows, jobs, and recurring jobs.
	/// This class represents common node properties used across different contexts.
	/// </summary>
	public abstract class NodeBase : TrackableObject
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NodeBase"/> class.
		/// </summary>
		private protected NodeBase() : base()
		{
		}

		/// <summary>
		/// Gets or sets the alias or display name of the node.
		/// </summary>
		public string Alias { get; set; }
	}

	public abstract class JobNode : NodeBase
	{
	}

	public abstract class WorkflowNode : NodeBase
	{
	}

	public abstract class RecurringJobNode : NodeBase
	{
	}
}
