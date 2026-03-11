namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;

	internal partial class ProfileParameterValuesSection
	{
		public Guid ProfileParameterId
		{
			get
			{
				if (string.IsNullOrEmpty(ProfileParameterID) || !Guid.TryParse(ProfileParameterID, out var id))
				{
					return Guid.Empty;
				}

				return id;
			}

			internal set
			{
				ProfileParameterID = value == Guid.Empty ? null : value.ToString();
			}
		}
	}
}
