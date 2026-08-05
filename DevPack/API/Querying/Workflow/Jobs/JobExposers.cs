namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Job"/> objects.
	/// </summary>
	public static class JobExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> Id = new Exposer<Job, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="ApiNamedObject.Name"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Name = new Exposer<Job, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Key"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Key = new Exposer<Job, string>((obj) => obj.Key, "Key");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Description"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Description = new Exposer<Job, string>((obj) => obj.Description, "Description");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Start"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> Start = new Exposer<Job, DateTimeOffset>((obj) => obj.Start, "Start");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.End"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> End = new Exposer<Job, DateTimeOffset>((obj) => obj.End, "End");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.PreRollStart"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> PreRollStart = new Exposer<Job, DateTimeOffset>((obj) => obj.PreRollStart, "PreRollStart");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.PostRollEnd"/> property.
		/// </summary>
		public static readonly Exposer<Job, DateTimeOffset> PostRollEnd = new Exposer<Job, DateTimeOffset>((obj) => obj.PostRollEnd, "PostRollEnd");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Notes"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> Notes = new Exposer<Job, string>((obj) => obj.Notes, "Notes");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.RecurringJobId"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> RecurringJobId = new Exposer<Job, Guid>((obj) => obj.RecurringJobId, "RecurringJobId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.JobTypeCategoryId"/> property.
		/// </summary>
		public static readonly Exposer<Job, string> JobTypeCategoryId = new Exposer<Job, string>((obj) => obj.JobTypeCategoryId, "JobTypeCategoryId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.Priority"/> property.
		/// </summary>
		public static readonly Exposer<Job, JobPriority> Priority = new Exposer<Job, JobPriority>((obj) => obj.Priority, "Priority");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.State"/> property.
		/// </summary>
		public static readonly Exposer<Job, JobState> State = new Exposer<Job, JobState>((obj) => obj.State, "State");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.OrganizationId"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> OrganizationId = new Exposer<Job, Guid>((obj) => obj.OrganizationId, "OrganizationId");

		/// <summary>
		/// Gets an exposer for the <see cref="Job.OwnerId"/> property.
		/// </summary>
		public static readonly Exposer<Job, Guid> OwnerId = new Exposer<Job, Guid>((obj) => obj.OwnerId, "OwnerId");
	}
}
