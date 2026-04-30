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

		public DataReferenceStorage Reference { get; set; }

		protected override void BeforeToSection()
		{
			ReferenceJson = Reference?.Serialize();
		}

		protected override void AfterLoad()
		{
			Reference = DataReferenceStorage.TryDeserialize(ReferenceJson, out var details) ? details : null;
		}
	}
}
