namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

	/// <summary>
	/// Provides contextual information for resolving workflow-related data, including access to the associated workflow and its properties.
	/// </summary>
	public class WorkflowResolveContext : ResolveContext
	{
		/// <summary>
		/// Initializes a new instance of the WorkflowResolveContext class with the specified workflow.
		/// </summary>
		/// <param name="workflow">The workflow to associate with this context. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if workflow is null.</exception>
		public WorkflowResolveContext(Workflow workflow)
		{
			Workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
		}

		// Allows to create a JobResolveContext without an associated workflow.
		internal WorkflowResolveContext()
		{
		}

		/// <summary>
		/// Gets the workflow associated with this instance.
		/// </summary>
		public Workflow Workflow { get; }

		/// <summary>
		/// Gets or sets the collection of workflow properties.
		/// </summary>
		public IDictionary<Guid, Property> WorkflowProperties { get; set; }

		/// <summary>
		/// Gets or sets the collection of workflow property values, indexed by property identifier.
		/// </summary>
		public IDictionary<Guid, PropertyValueBase> WorkflowPropertyValues { get; set; }
	}
}
