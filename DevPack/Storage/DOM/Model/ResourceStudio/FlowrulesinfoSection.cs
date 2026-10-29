namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;

	internal partial class FlowrulesinfoSection
	{
		internal Guid OwnerId
		{
			get
			{
				if (string.IsNullOrEmpty(Owner) || !Guid.TryParse(Owner, out var id))
				{
					return Guid.Empty;
				}

				return id;
			}

			set
			{
				Owner = value == Guid.Empty ? null : value.ToString();
			}
		}
	}
}
