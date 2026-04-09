namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a workflow in MediaOps Plan.
	/// </summary>
	public class Workflow : ApiObject
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class.
		/// </summary>
		public Workflow() : base()
		{
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Workflow"/> class with a specific workflow ID.
		/// </summary>
		public Workflow(Guid jobId) : base(jobId)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		internal Workflow(WorkflowsInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name of the workflow.
		/// </summary>
		public override string Name { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);

				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current workflow instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current workflow instance.</param>
		/// <returns>true if the specified object is a workflow and has the same values for all properties as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not Workflow other)
			{
				return false;
			}

			return Id == other.Id &&
				   Name == other.Name;
		}

		private void ParseInstance(WorkflowsInstance instance)
		{
			Name = instance.WorkflowInfo.WorkflowName;
		}
	}
}
