namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Workflow"/> objects.
	/// </summary>
	public static class WorkflowExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, Guid> Id = new Exposer<Workflow, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="ApiNamedObject.Name"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, string> Name = new Exposer<Workflow, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.Description"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, string> Description = new Exposer<Workflow, string>((obj) => obj.Description, "Description");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.Priority"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, WorkflowPriority> Priority = new Exposer<Workflow, WorkflowPriority>((obj) => obj.Priority, "Priority");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.IsFavorite"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, bool> IsFavorite = new Exposer<Workflow, bool>((obj) => obj.IsFavorite, "IsFavorite");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.PreRoll"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, TimeSpan> PreRoll = new Exposer<Workflow, TimeSpan>((obj) => obj.PreRoll, "PreRoll");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.PostRoll"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, TimeSpan> PostRoll = new Exposer<Workflow, TimeSpan>((obj) => obj.PostRoll, "PostRoll");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.Notes"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, string> Notes = new Exposer<Workflow, string>((obj) => obj.Notes, "Notes");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.JobTypeCategoryId"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, string> JobTypeCategoryId = new Exposer<Workflow, string>((obj) => obj.JobTypeCategoryId, "JobTypeCategoryId");

		/// <summary>
		/// Gets an exposer for the <see cref="Workflow.State"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, WorkflowState> State = new Exposer<Workflow, WorkflowState>((obj) => obj.State, "State");
	}
}
