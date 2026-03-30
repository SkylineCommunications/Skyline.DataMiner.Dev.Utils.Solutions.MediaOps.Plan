namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a reference to a data source.
	/// </summary>
	public class DataReference
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataReference"/> class with the specified type.
		/// </summary>
		/// <param name="type">The type of data this reference points to.</param>
		public DataReference(DataReferenceType type)
		{
			Type = type;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataReference"/> class with the specified type and identifier.
		/// </summary>
		/// <param name="type">The type of data this reference points to.</param>
		/// <param name="referenceId">The unique identifier of the referenced data item.</param>
		public DataReference(DataReferenceType type, string referenceId)
		{
			Type = type;
			ReferenceId = referenceId;
		}

		/// <summary>
		/// Gets the type of data this reference points to.
		/// </summary>
		public DataReferenceType Type { get; }

		/// <summary>
		/// Gets the unique identifier of the referenced data, if applicable. This value may be null if the reference does not point to a specific data item or if the identifier is not available.
		/// </summary>
		public string ReferenceId { get; }

		internal Storage.DOM.DataReference ToStorage()
		{
			return new Storage.DOM.DataReference
			{
				ReferenceType = Type.ToString(),
				ReferenceId = ReferenceId,
			};
		}

		internal static DataReference FromStorage(Storage.DOM.DataReference reference)
		{
			if (reference == null)
			{
				return null;
			}

			if (!Enum.TryParse<DataReferenceType>(reference.ReferenceType, out var type))
			{
				return null;
			}

			return new DataReference(type, reference.ReferenceId);
		}
	}
}
