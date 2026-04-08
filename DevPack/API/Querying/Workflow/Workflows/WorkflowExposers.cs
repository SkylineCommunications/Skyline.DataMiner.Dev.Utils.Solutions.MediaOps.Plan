namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Workflow"/> objects.
	/// </summary>
	public class WorkflowExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, Guid> Id = new Exposer<Workflow, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Name"/> property.
		/// </summary>
		public static readonly Exposer<Workflow, string> Name = new Exposer<Workflow, string>((obj) => obj.Name, "Name");
	}
}
