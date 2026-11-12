namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio
{
	using System;

	internal partial class FlowruleSection
	{
		internal Guid? TargetId
		{
			get
			{
				if (string.IsNullOrEmpty(Target) || !Guid.TryParse(Target, out var id) || id == Guid.Empty)
				{
					return null;
				}

				return id;
			}

			set
			{
				Target = value.HasValue && value.Value != Guid.Empty ? value.Value.ToString() : null;
			}
		}
	}
}
