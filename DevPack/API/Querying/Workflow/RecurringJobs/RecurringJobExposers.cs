namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="RecurringJob"/> objects.
	/// </summary>
	public static class RecurringJobExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, Guid> Id = new Exposer<RecurringJob, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="ApiNamedObject.Name"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, string> Name = new Exposer<RecurringJob, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.Description"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, string> Description = new Exposer<RecurringJob, string>((obj) => obj.Description, "Description");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.Notes"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, string> Notes = new Exposer<RecurringJob, string>((obj) => obj.Notes, "Notes");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.Priority"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, RecurringJobPriority> Priority = new Exposer<RecurringJob, RecurringJobPriority>((obj) => obj.Priority, "Priority");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.Start"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, DateTimeOffset> Start = new Exposer<RecurringJob, DateTimeOffset>((obj) => obj.Start, "Start");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.Duration"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, TimeSpan> Duration = new Exposer<RecurringJob, TimeSpan>((obj) => obj.Duration, "Duration");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.DesiredJobState"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, DesiredJobState> DesiredJobState = new Exposer<RecurringJob, DesiredJobState>((obj) => obj.DesiredJobState, "DesiredJobState");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.OrganizationId"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, Guid> OrganizationId = new Exposer<RecurringJob, Guid>((obj) => obj.OrganizationId, "OrganizationId");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.OwnerId"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, Guid> OwnerId = new Exposer<RecurringJob, Guid>((obj) => obj.OwnerId, "OwnerId");

		/// <summary>
		/// Gets an exposer for the <see cref="RecurringJob.JobTypeCategoryId"/> property.
		/// </summary>
		public static readonly Exposer<RecurringJob, string> JobTypeCategoryId = new Exposer<RecurringJob, string>((obj) => obj.JobTypeCategoryId, "JobTypeCategoryId");

		/// <summary>
		/// Provides exposers for querying and filtering the <see cref="RecurringJob.Pattern"/> property.
		/// </summary>
		public static class Pattern
		{
			/// <summary>
			/// Gets an exposer for the <see cref="RecurringPattern.EndDate"/> property.
			/// </summary>
			/// <remarks>
			/// The recurring pattern is stored as a serialized value. Therefore only the <see cref="Comparer.Equals"/> and
			/// <see cref="Comparer.NotEquals"/> comparisons are supported.
			/// </remarks>
			public static readonly Exposer<RecurringJob, DateTimeOffset> EndDate = new Exposer<RecurringJob, DateTimeOffset>((obj) => obj.Pattern != null ? obj.Pattern.EndDate : default, "Pattern.EndDate");
		}
	}
}
