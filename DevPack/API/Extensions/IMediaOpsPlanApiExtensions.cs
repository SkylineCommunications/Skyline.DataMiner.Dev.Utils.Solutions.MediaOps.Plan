namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcProperties;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcRelationships;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcResourceStudio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcWorkflow;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Tools;

	/// <summary>
	/// Defines extension methods on the <see cref="IMediaOpsPlanApi"/> interface.
	/// </summary>
	public static class IMediaOpsPlanApiExtensions
	{
		/// <summary>
		/// Installs or reconciles the DOM schemas owned by MediaOps Plan.
		/// </summary>
		/// <param name="api">A MediaOps Plan API client created by the DevPack.</param>
		/// <remarks>
		/// Requires DataMiner 10.6.11 or later and permission to configure module settings.
		/// Module settings and declared schema metadata are authoritative. Removed fields and section links
		/// are retained as soft-deleted metadata; unrelated definitions and sections are not removed.
		/// Install the People and Organizations schema separately through its owning DevPack.
		/// This method does not install scripts, migrate instance data, or register the solution.
		/// Each module's DOM Manager is reinitialized after reconciliation. Exceptions propagate and
		/// previously completed writes are not rolled back.
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="api"/> is <see langword="null"/>.</exception>
		/// <exception cref="NotSupportedException"><paramref name="api"/> is not a DevPack-created client.</exception>
		public static void InstallDomModules(this IMediaOpsPlanApi api)
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (!(api is MediaOpsPlanApi client))
			{
				throw new NotSupportedException("DOM schema installation requires a MediaOps Plan API client created by the DevPack.");
			}

			Action<string> logAction = message => client.Logger.Information(message);
			DomModuleInstaller.Install(client.Connection.HandleMessages, new SlcPropertiesDomModule(), logAction);
			DomModuleInstaller.Install(client.Connection.HandleMessages, new SlcRelationshipsDomModule(), logAction);
			DomModuleInstaller.Install(client.Connection.HandleMessages, new SlcResourceStudioDomModule(), logAction);
			DomModuleInstaller.Install(client.Connection.HandleMessages, new SlcWorkflowDomModule(), logAction);
		}
	}
}
