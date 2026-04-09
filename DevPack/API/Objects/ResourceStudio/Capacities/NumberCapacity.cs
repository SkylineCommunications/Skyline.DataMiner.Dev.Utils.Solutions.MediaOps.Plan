namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using CoreParameter = Net.Profiles.Parameter;

	/// <summary>
	/// Represents a Number Capacity in the MediaOps Plan API.
	/// </summary>
	public class NumberCapacity : Capacity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NumberCapacity"/> class.
		/// </summary>
		public NumberCapacity() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NumberCapacity"/> class with the specified unique identifier.
		/// </summary>
		/// <param name="id">The unique identifier for the capacity.</param>
		public NumberCapacity(Guid id) : base(id)
		{
		}

		internal NumberCapacity(CoreParameter parameter) : base(parameter)
		{
			InitTracking();
		}

		/// <inheritdoc/>
		protected internal override CoreParameter.ParameterType ParameterType => CoreParameter.ParameterType.Number;
	}
}
