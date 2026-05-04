namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// Represents an error that occurs when a discrete property configuration is invalid due to the absence of required discrete values.
	/// </summary>
	public sealed class PropertyNoDiscretesError : PropertyInvalidDiscretesError
	{
	}
}
