namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the base configuration for a resource link.
	/// </summary>
	/// <seealso cref="ResourceElementLinkSetting"/>
	/// <seealso cref="ResourceServiceLinkSetting"/>
	/// <seealso cref="ResourceVirtualFunctionLinkSetting"/>
	public abstract class ResourceLinkSetting
	{
		/// <summary>
		/// Gets or sets the agent ID associated with the resource link.
		/// </summary>
		public int AgentId { get; set; }

		/// <summary>
		/// Determines whether this link setting is an element link and, if so, returns it as a <see cref="ResourceElementLinkSetting"/>.
		/// </summary>
		/// <param name="linkSetting">When this method returns, contains the current link setting as a <see cref="ResourceElementLinkSetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this link setting is a <see cref="ResourceElementLinkSetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsResourceElementLinkSetting(out ResourceElementLinkSetting linkSetting)
		{
			linkSetting = this as ResourceElementLinkSetting;
			return linkSetting != null;
		}

		/// <summary>
		/// Determines whether this link setting is a service link and, if so, returns it as a <see cref="ResourceServiceLinkSetting"/>.
		/// </summary>
		/// <param name="linkSetting">When this method returns, contains the current link setting as a <see cref="ResourceServiceLinkSetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this link setting is a <see cref="ResourceServiceLinkSetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsResourceServiceLinkSetting(out ResourceServiceLinkSetting linkSetting)
		{
			linkSetting = this as ResourceServiceLinkSetting;
			return linkSetting != null;
		}

		/// <summary>
		/// Determines whether this link setting is a virtual function link and, if so, returns it as a <see cref="ResourceVirtualFunctionLinkSetting"/>.
		/// </summary>
		/// <param name="linkSetting">When this method returns, contains the current link setting as a <see cref="ResourceVirtualFunctionLinkSetting"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this link setting is a <see cref="ResourceVirtualFunctionLinkSetting"/>; otherwise, <c>false</c>.</returns>
		public bool IsResourceVirtualFunctionLinkSetting(out ResourceVirtualFunctionLinkSetting linkSetting)
		{
			linkSetting = this as ResourceVirtualFunctionLinkSetting;
			return linkSetting != null;
		}
	}

	/// <summary>
	/// Represents the configuration for a resource element link.
	/// </summary>
	public class ResourceElementLinkSetting : ResourceLinkSetting
	{
		/// <summary>
		/// Gets or sets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; set; }
	}

	/// <summary>
	/// Represents the configuration for a resource service link.
	/// </summary>
	public class ResourceServiceLinkSetting : ResourceLinkSetting
	{
		/// <summary>
		/// Gets or sets the service ID associated with the resource link.
		/// </summary>
		public int ServiceId { get; set; }
	}

	/// <summary>
	/// Represents the configuration for a resource virtual function link.
	/// </summary>
	public class ResourceVirtualFunctionLinkSetting : ResourceLinkSetting
	{
		/// <summary>
		/// Gets or sets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; set; }

		/// <summary>
		/// Gets or sets the function ID associated with the resource link.
		/// </summary>
		public Guid FunctionId { get; set; }

		/// <summary>
		/// Gets or sets the function table index associated with the resource link.
		/// </summary>
		public string FunctionTableIndex { get; set; }
	}
}
