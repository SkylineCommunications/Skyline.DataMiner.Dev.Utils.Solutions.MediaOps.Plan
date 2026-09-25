namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	/// <summary>
	/// Represents the value of a dynamic input of an orchestration script, either provided directly or through a reference.
	/// </summary>
	public class DynamicInputSetting
	{
		private OrchestrationInputValue value;
		private DataReference reference;

		/// <summary>
		/// Initializes a new instance of the <see cref="DynamicInputSetting"/> class.
		/// </summary>
		/// <param name="path">The path of the input field, for example <c>Destinations/Destination 1/Endpoint</c>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null or empty.</exception>
		public DynamicInputSetting(string path)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException(nameof(path));
			}

			Path = path;
		}

		/// <summary>
		/// Gets the path of the input field.
		/// </summary>
		public string Path { get; }

		/// <summary>
		/// Gets or sets the value of the input. Setting a value clears the reference.
		/// </summary>
		public OrchestrationInputValue Value
		{
			get => value;
			set
			{
				this.value = value;
				if (value != null)
				{
					reference = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets a reference to a data source that provides the value of the input. Setting a reference clears the value.
		/// </summary>
		public DataReference Reference
		{
			get => reference;
			set
			{
				reference = value;
				if (value != null)
				{
					this.value = null;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this input has a reference defined.
		/// </summary>
		public bool HasReference => Reference != null;
	}
}
