namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a script parameter that can be assigned to an automation script.
	/// </summary>
	public class ScriptParameterSetting
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptParameterSetting"/> class.
		/// </summary>
		/// <param name="name">The name of the script parameter. Cannot be null or empty.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="name"/> is null or empty.</exception>
		public ScriptParameterSetting(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			Name = name;
		}

		/// <summary>
		/// Gets the name of the script parameter.
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Gets or sets the script parameter value.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Gets or sets a reference to a data source that provides the value for this parameter.
		/// </summary>
		public DataReference Reference { get; set; }

		/// <summary>
		/// Gets a value indicating whether this parameter has a reference defined.
		/// </summary>
		public bool HasReference => Reference != null;
	}
}
