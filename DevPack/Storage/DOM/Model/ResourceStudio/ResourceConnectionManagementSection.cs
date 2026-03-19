namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;

	internal partial class ResourceConnectionManagementSection
	{
		public Guid VirtualSignalGroupInputId
		{
			get
			{
				if (string.IsNullOrEmpty(InputReference) || !Guid.TryParse(InputReference, out var id))
				{
					return Guid.Empty;
				}

				return id;
			}

			internal set
			{
				InputReference = value == Guid.Empty ? null : value.ToString();
			}
		}

		public Guid VirtualSignalGroupOutputId
		{
			get
			{
				if (string.IsNullOrEmpty(OutputReference) || !Guid.TryParse(OutputReference, out var id))
				{
					return Guid.Empty;
				}

				return id;
			}

			internal set
			{
				OutputReference = value == Guid.Empty ? null : value.ToString();
			}
		}
	}
}
