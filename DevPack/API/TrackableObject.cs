namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents an object that can be tracked for changes and state management.
	/// </summary>
	/// <remarks>This class is intended to be used as a base class for objects that require tracking of their
	/// state,  such as whether they are newly created or have been modified. It is not intended for direct
	/// instantiation  outside of derived classes.</remarks>
	/// <seealso cref="ApiObject"/>
	/// <seealso cref="CapabilitySettings"/>
	/// <seealso cref="FlowRule"/>
	/// <seealso cref="LinkedResourcePool"/>
	/// <seealso cref="NodeBase"/>
	/// <seealso cref="NodeConnection{TNode}"/>
	/// <seealso cref="OrchestrationEvent"/>
	/// <seealso cref="PropertySettingBase"/>
	/// <seealso cref="ResourcePropertySettings"/>
	/// <seealso cref="Setting"/>
	public abstract class TrackableObject
	{
		[ThreadStatic]
		private static int trackingHashDepth;

		private bool wasInitialized = false;
		private int initialHashCode = -1;

		/// <summary>
		/// Initializes a new instance of the <see cref="TrackableObject"/> class.
		/// </summary>
		/// <remarks>This constructor is protected internal, allowing access from derived classes or
		/// classes within the same assembly.</remarks>
		private protected TrackableObject()
		{
		}

		/// <summary>
		/// Gets a value indicating whether the current thread is computing a hash for change tracking. Objects holding
		/// lazily loaded data use this to avoid loading it just to detect changes.
		/// </summary>
		internal static bool IsComputingTrackingHash => trackingHashDepth > 0;

		internal bool IsNew { get; set; }

		internal virtual bool HasChanges
		{
			get
			{
				if (IsNew)
				{
					return false;
				}

				if (!wasInitialized)
				{
					throw new InvalidOperationException("Tracking for this object was never initialized");
				}

				return initialHashCode != ComputeTrackingHashCode();
			}
		}

		internal void InitTracking()
		{
			if (IsNew)
			{
				return;
			}

			if (wasInitialized)
			{
				throw new InvalidOperationException("Tracking was already initialized for this object");
			}

			initialHashCode = ComputeTrackingHashCode();
			wasInitialized = true;
		}

		private int ComputeTrackingHashCode()
		{
			trackingHashDepth++;
			try
			{
				return GetHashCode();
			}
			finally
			{
				trackingHashDepth--;
			}
		}
	}
}