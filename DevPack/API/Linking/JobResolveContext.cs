namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

	/// <summary>
	/// Provides contextual information for resolving jobs, including access to the associated job and its properties.
	/// </summary>
	/// <remarks>Inherits from WorkflowResolveContext to include workflow-level context in addition to job-level details.</remarks>
	public class JobResolveContext : WorkflowResolveContext
	{
		/// <summary>
		/// Initializes a new instance of the JobResolveContext class with the specified job.
		/// </summary>
		/// <param name="job">The job to be associated with this context. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if job is null.</exception>
		public JobResolveContext(Job job)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
		}

		/// <summary>
		/// Initializes a new instance of the JobResolveContext class with the specified job and workflow.
		/// </summary>
		/// <param name="job">The job to be resolved within the workflow context. Cannot be null.</param>
		/// <param name="workflow">The workflow in which the job is being resolved. Cannot be null.</param>
		/// <exception cref="ArgumentNullException">Thrown if job or workflow is null.</exception>
		public JobResolveContext(Job job, Workflow workflow) : base(workflow)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
		}

		/// <summary>
		/// Gets the job associated with this instance.
		/// </summary>
		public Job Job { get; }

		/// <summary>
		/// Gets or sets the collection of job properties.
		/// </summary>
		public IDictionary<Guid, Property> JobProperties { get; set; }

		/// <summary>
		/// Gets or sets the collection of job property values, indexed by property identifier.
		/// </summary>
		public IDictionary<Guid, PropertyValueBase> JobPropertyValues { get; set; }
	}
}
