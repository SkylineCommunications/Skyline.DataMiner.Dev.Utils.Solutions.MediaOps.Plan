namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents an abstract reference to a data source. Use a concrete subclass that matches the desired <see cref="DataReferenceType"/>.
	/// </summary>
	public abstract class DataReference : IEquatable<DataReference>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DataReference"/> class with the specified type.
		/// </summary>
		/// <param name="type">The type of data this reference points to.</param>
		protected DataReference(DataReferenceType type)
		{
			Type = type;
		}

		/// <summary>
		/// Gets the type of data this reference points to.
		/// </summary>
		public DataReferenceType Type { get; }

		/// <summary>
		/// Serializes this <see cref="DataReference"/> to a string representation suitable for storage or transmission.
		/// </summary>
		/// <returns>The serialized string.</returns>
		internal string Serialize()
		{
			return ToStorage().Serialize();
		}

		/// <summary>
		/// Attempts to deserialize the specified string into a DataReference instance.
		/// </summary>
		/// <param name="serialized">The string containing the serialized representation of a DataReference.</param>
		/// <param name="result">When this method returns, contains the deserialized DataReference if the operation succeeds; otherwise, null. This
		/// parameter is passed uninitialized.</param>
		/// <returns>true if the string was successfully deserialized into a DataReference; otherwise, false.</returns>
		internal static bool TryDeserialize(string serialized, out DataReference result)
		{
			result = null;
			if (!Storage.DOM.DataReference.TryDeserialize(serialized, out var storageReference))
				return false;

			result = FromStorage(storageReference);
			return result != null;
		}

		/// <summary>
		/// Converts this <see cref="DataReference"/> to its storage representation.
		/// </summary>
		/// <returns>A <see cref="Storage.DOM.DataReference"/> representing this instance.</returns>
		internal virtual Storage.DOM.DataReference ToStorage()
		{
			return new Storage.DOM.DataReference
			{
				ReferenceType = Type.ToString(),
			};
		}

		/// <summary>
		/// Creates a <see cref="DataReference"/> from its storage representation.
		/// </summary>
		/// <param name="reference">The storage representation to convert from.</param>
		/// <returns>A new <see cref="DataReference"/> instance, or <see langword="null"/> if the input is null or contains an unrecognized type or an invalid identifier.</returns>
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

			return type switch
			{
				DataReferenceType.ResourceName => new ResourceNameReference(),
				DataReferenceType.ResourceLinkedObjectID => new ResourceLinkedObjectIdReference(),
				DataReferenceType.ResourceProperty => ResourcePropertyReference.ParseFromStorage(reference),
				DataReferenceType.SchedulingConfigurationParameter => SchedulingConfigurationParameterReference.ParseFromStorage(reference),
				_ => null,
			};
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
		public virtual bool Equals(DataReference other)
		{
			return other is not null && Type == other.Type;
		}

		/// <summary>
		/// Returns a hash code for the current <see cref="DataReference"/>.
		/// </summary>
		/// <returns>A hash code for the current instance.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				return 17 * 23 + Type.GetHashCode();
			}
		}
	}
}
