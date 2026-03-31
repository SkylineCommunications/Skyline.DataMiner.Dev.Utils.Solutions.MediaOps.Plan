namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a reference to a data source.
	/// </summary>
	public class DataReference : IEquatable<DataReference>
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
		/// Gets or sets the unique identifier of the referenced data, if applicable. This value may be null if the reference does not point to a specific data item or if the identifier is not available.
		/// </summary>
		public string ReferenceId { get; set; }

		/// <summary>
		/// Converts this <see cref="DataReference"/> to its storage representation.
		/// </summary>
		/// <returns>A <see cref="Storage.DOM.DataReference"/> representing this instance.</returns>
		internal Storage.DOM.DataReference ToStorage()
		{
			return new Storage.DOM.DataReference
			{
				ReferenceType = Type.ToString(),
				ReferenceId = ReferenceId,
			};
		}

		/// <summary>
		/// Creates a <see cref="DataReference"/> from its storage representation.
		/// </summary>
		/// <param name="reference">The storage representation to convert from.</param>
		/// <returns>A new <see cref="DataReference"/> instance, or <see langword="null"/> if the input is null or contains an unrecognized type.</returns>
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

		/// <summary>
		/// Determines whether the specified object is equal to the current <see cref="DataReference"/>.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns><see langword="true"/> if the specified object is equal to the current instance; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as DataReference);
		}

		/// <summary>
		/// Determines whether the specified <see cref="DataReference"/> is equal to the current instance.
		/// </summary>
		/// <param name="other">The <see cref="DataReference"/> to compare with the current instance.</param>
		/// <returns><see langword="true"/> if the specified instance is equal to the current instance; otherwise, <see langword="false"/>.</returns>
		public bool Equals(DataReference other)
		{
			return other is not null &&
				   Type == other.Type &&
				   ReferenceId == other.ReferenceId;
		}

		/// <summary>
		/// Returns a hash code for the current <see cref="DataReference"/>.
		/// </summary>
		/// <returns>A hash code for the current instance.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = hash * 23 + Type.GetHashCode();
				hash = hash * 23 + EqualityComparer<string>.Default.GetHashCode(ReferenceId);
				return hash; 
			}
		}

		/// <summary>
		/// Determines whether two <see cref="DataReference"/> instances are equal.
		/// </summary>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		/// <returns><see langword="true"/> if both instances are equal; otherwise, <see langword="false"/>.</returns>
		public static bool operator ==(DataReference left, DataReference right)
		{
			return EqualityComparer<DataReference>.Default.Equals(left, right);
		}

		/// <summary>
		/// Determines whether two <see cref="DataReference"/> instances are not equal.
		/// </summary>
		/// <param name="left">The left operand.</param>
		/// <param name="right">The right operand.</param>
		/// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.</returns>
		public static bool operator !=(DataReference left, DataReference right)
		{
			return !(left == right);
		}
	}
}
