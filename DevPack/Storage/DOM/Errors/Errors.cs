namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	internal static class ResourcePoolErrors
	{
		public static ErrorDefinition GenericException { get; } = new ErrorDefinition("RP000", "Generic exception occurred");

		public static ErrorDefinition CrudException { get; } = new ErrorDefinition("RP001", "CRUD exception occurred");

		public static ErrorDefinition ExecuteAction_MarkCompleteException { get; } = new ErrorDefinition("RP002", "Exception occurred while trying to complete a resource pool (action).");
	}

	internal static class ResourceErrors
	{
		public static ErrorDefinition GenericException { get; } = new ErrorDefinition("R000", "Generic exception occurred");

		public static ErrorDefinition CrudException { get; } = new ErrorDefinition("R001", "Crud exception occurred");

		public static ErrorDefinition ExecuteAction_MarkCompleteException { get; } = new ErrorDefinition("R002", "Exception occurred while trying to complete a resource (action).");
	}
}
