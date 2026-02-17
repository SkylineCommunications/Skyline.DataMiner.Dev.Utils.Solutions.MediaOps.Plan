namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Resource"/> objects.
	/// </summary>
	public class ResourceExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.ID"/> property.
		/// </summary>
		public static readonly Exposer<Resource, Guid> Id = new Exposer<Resource, Guid>((obj) => obj.ID, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="Resource.Name"/> property.
		/// </summary>
		public static readonly Exposer<Resource, string> Name = new Exposer<Resource, string>((obj) => obj.Name, "Name");

		/// <summary>
		/// Gets an exposer for the <see cref="Resource.IsFavorite"/> property.
		/// </summary>
		public static readonly Exposer<Resource, bool> IsFavorite = new Exposer<Resource, bool>((obj) => obj.IsFavorite, "IsFavorite");

		/// <summary>
		/// Gets an exposer for the <see cref="Resource.Concurrency"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> Concurrency = new Exposer<Resource, int>((obj) => obj.Concurrency, "Concurrency");

		/// <summary>
		/// Gets an exposer for the <see cref="Resource.State"/> property.
		/// </summary>
		public static readonly Exposer<Resource, ResourceState> State = new Exposer<Resource, ResourceState>((obj) => obj.State, "State");

		/// <summary>
		/// Gets a dynamic list exposer for the <see cref="Resource.ResourcePoolIds"/> property.
		/// </summary>
		public static readonly DynamicListExposer<Resource, Guid> ResourcePoolIds = DynamicListExposer<Resource, Guid>.CreateFromListExposer(new Exposer<Resource, IEnumerable>((obj) => obj.ResourcePoolIds.Where(x => x != null), "ResourcePoolIds"));

		/// <summary>
		/// Gets an exposer to match the type of <see cref="Resource"/>.
		/// </summary>
		public static readonly Exposer<Resource, Type> Type = new Exposer<Resource, Type>((obj) => obj.GetType(), "Type");

		/// <summary>
		/// Gets an exposer for the <see cref="Resource.VirtualSignalGroupInputId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, Guid> VirtualSignalGroupInputId = new Exposer<Resource, Guid>((obj) => obj.VirtualSignalGroupInputId, "VirtualSignalGroupInputId");

		/// <summary>
		/// Gets an exposer for the <see cref="Resource.VirtualSignalGroupOutputId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, Guid> VirtualSignalGroupOutputId = new Exposer<Resource, Guid>((obj) => obj.VirtualSignalGroupOutputId, "VirtualSignalGroupOutputId");

		/// <summary>
		/// Provides exposers for querying and filtering resource capabilities.
		/// </summary>
		public static class Capabilities
		{
			/// <summary>
			/// Gets a dynamic list exposer for capability IDs.
			/// </summary>
			public static readonly DynamicListExposer<Resource, Guid> CapabilityId = DynamicListExposer<Resource, Guid>.CreateFromListExposer(new Exposer<Resource, IEnumerable>((obj) => obj.Capabilities.Where(x => x != null).Select(x => x.Id).Where(x => x != null), "Capabilities.Id"));

			/// <summary>
			/// Gets a dynamic list exposer for capability discrete values.
			/// </summary>
			public static readonly DynamicListExposer<Resource, string> Discretes = DynamicListExposer<Resource, string>.CreateFromListExposer(new Exposer<Resource, IEnumerable>((obj) => obj.Capabilities.Where(x => x != null).Select(x => x.Discretes).Where(x => x != null), "Capabilities.Discretes"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering resource capacities.
		/// </summary>
		public static class Capacities
		{
			/// <summary>
			/// Gets a dynamic list exposer for capacity IDs.
			/// </summary>
			public static readonly DynamicListExposer<Resource, Guid> CapacityId = DynamicListExposer<Resource, Guid>.CreateFromListExposer(new Exposer<Resource, IEnumerable>((obj) => obj.Capacities.Where(x => x != null).Select(x => x.Id).Where(x => x != null), "Capacities.Id"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering resource properties.
		/// </summary>
		public static class Properties
		{
			/// <summary>
			/// Gets a dynamic list exposer for property IDs.
			/// </summary>
			public static readonly DynamicListExposer<Resource, Guid> PropertyId = DynamicListExposer<Resource, Guid>.CreateFromListExposer(new Exposer<Resource, IEnumerable>((obj) => obj.Properties.Where(x => x != null).Select(x => x.Id).Where(x => x != null), "Properties.Id"));

			/// <summary>
			/// Gets a dynamic list exposer for property values.
			/// </summary>
			public static readonly DynamicListExposer<Resource, string> Value = DynamicListExposer<Resource, string>.CreateFromListExposer(new Exposer<Resource, IEnumerable>((obj) => obj.Properties.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "Properties.Value"));
		}
	}

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="ElementResource"/> objects.
	/// </summary>
	public class ElementResourceExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ElementResource.AgentId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> AgentId = new SettableExposer<Resource, int>((Func<Resource, int>)((Resource x) => (!(x is ElementResource elementResource)) ? -1 : elementResource.AgentId), (Action<Resource, int>)delegate
		{
		}, ["ElementResource.AgentId"]);

		/// <summary>
		/// Gets an exposer for the <see cref="ElementResource.ElementId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> ElementId = new SettableExposer<Resource, int>((Func<Resource, int>)((Resource x) => (!(x is ElementResource elementResource)) ? -1 : elementResource.ElementId), (Action<Resource, int>)delegate
		{
		}, ["ElementResource.ElementId"]);
	}

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="ServiceResource"/> objects.
	/// </summary>
	public class ServiceResourceExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ServiceResource.AgentId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> AgentId = new SettableExposer<Resource, int>((Func<Resource, int>)((Resource x) => (!(x is ServiceResource serviceResource)) ? -1 : serviceResource.AgentId), (Action<Resource, int>)delegate
		{
		}, ["ServiceResource.AgentId"]);

		/// <summary>
		/// Gets an exposer for the <see cref="ServiceResource.ServiceId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> ServiceId = new SettableExposer<Resource, int>((Func<Resource, int>)((Resource x) => (!(x is ServiceResource serviceResource)) ? -1 : serviceResource.ServiceId), (Action<Resource, int>)delegate
		{
		}, ["ServiceResource.ServiceId"]);
	}

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="VirtualFunctionResource"/> objects.
	/// </summary>
	public class VirtualFunctionResourceExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="VirtualFunctionResource.AgentId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> AgentId = new SettableExposer<Resource, int>((Func<Resource, int>)((Resource x) => (!(x is VirtualFunctionResource virtualFunctionResource)) ? -1 : virtualFunctionResource.AgentId), (Action<Resource, int>)delegate
		{
		}, ["VirtualFunctionResource.AgentId"]);

		/// <summary>
		/// Gets an exposer for the <see cref="VirtualFunctionResource.ElementId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, int> ElementId = new SettableExposer<Resource, int>((Func<Resource, int>)((Resource x) => (!(x is VirtualFunctionResource virtualFunctionResource)) ? -1 : virtualFunctionResource.ElementId), (Action<Resource, int>)delegate
		{
		}, ["VirtualFunctionResource.ElementId"]);

		/// <summary>
		/// Gets an exposer for the <see cref="VirtualFunctionResource.FunctionId"/> property.
		/// </summary>
		public static readonly Exposer<Resource, Guid> FunctionId = new SettableExposer<Resource, Guid>((Func<Resource, Guid>)((Resource x) => (!(x is VirtualFunctionResource virtualFunctionResource)) ? Guid.Empty : virtualFunctionResource.FunctionId), (Action<Resource, Guid>)delegate
		{
		}, ["VirtualFunctionResource.FunctionId"]);

		/// <summary>
		/// Gets an exposer for the <see cref="VirtualFunctionResource.FunctionTableIndex"/> property.
		/// </summary>
		public static readonly Exposer<Resource, string> FunctionTableIndex = new SettableExposer<Resource, string>((Func<Resource, string>)((Resource x) => (!(x is VirtualFunctionResource virtualFunctionResource)) ? null : virtualFunctionResource.FunctionTableIndex), (Action<Resource, string>)delegate
		{
		}, ["VirtualFunctionResource.FunctionTableIndex"]);
	}
}
