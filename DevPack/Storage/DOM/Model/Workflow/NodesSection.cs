namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow
{
	using System;

	internal partial class NodesSection
	{
		internal Guid ReferenceId
		{
			get
			{
				if (string.IsNullOrEmpty(NodeReferenceID) || !Guid.TryParse(NodeReferenceID, out var id))
				{
					return Guid.Empty;
				}

				return id;
			}

			set
			{
				NodeReferenceID = value == Guid.Empty ? null : value.ToString();
			}
		}

		internal Guid ParentReferenceId
		{
			get
			{
				if (string.IsNullOrEmpty(NodeParentReferenceID) || !Guid.TryParse(NodeParentReferenceID, out var id))
				{
					return Guid.Empty;
				}

				return id;
			}

			set
			{
				NodeParentReferenceID = value == Guid.Empty ? null : value.ToString();
			}
		}
	}
}