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
		/// Storage key used to persist the optional <see cref="NodeId"/> on a reference.
		/// </summary>
		internal const string NodeIdKey = "NodeId";

		/// <summary>
		/// Initializes a new instance of the <see cref="DataReference"/> class with the specified type.
		/// </summary>
		/// <param name="type">The type of data this reference points to.</param>
		/// <param name="nodeId">
		/// Optional identifier of the workflow node the reference is scoped to.
		/// When <see langword="null"/> or empty the reference is interpreted as targeting the current node.
		/// </param>
		protected DataReference(DataReferenceType type, string nodeId = null)
		{
			Type = type;
			NodeId = String.IsNullOrEmpty(nodeId) ? null : nodeId;
		}

		/// <summary>
		/// Gets the type of data this reference points to.
		/// </summary>
		public DataReferenceType Type { get; }

		/// <summary>
		/// Gets the identifier of the workflow node the reference is scoped to,
		/// or <see langword="null"/> when the reference targets the current node.
		/// </summary>
		public string NodeId { get; }

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
				ReferenceData = BuildReferenceData(),
			};
		}

		/// <summary>
		/// Builds the <c>ReferenceData</c> dictionary used by <see cref="ToStorage"/>.
		/// </summary>
		/// <remarks>
		/// Subclasses with extra storage keys must override this method, call the base implementation
		/// and add their own keys to the returned dictionary.
		/// </remarks>
		/// <returns>The dictionary, or <see langword="null"/> when no data needs to be stored.</returns>
		private protected virtual Dictionary<string, string> BuildReferenceData()
		{
			if (NodeId == null)
			{
				return null;
			}

			return new Dictionary<string, string> { [NodeIdKey] = NodeId };
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

			var nodeId = ReadNodeId(reference);

			return type switch
			{
				DataReferenceType.ResourceName => new ResourceNameReference(nodeId),
				DataReferenceType.ResourceLinkedObjectID => new ResourceLinkedObjectIdReference(nodeId),
				DataReferenceType.ResourceProperty => ResourcePropertyReference.ParseFromStorage(reference, nodeId),
				DataReferenceType.SchedulingConfigurationParameter => SchedulingConfigurationParameterReference.ParseFromStorage(reference, nodeId),
				DataReferenceType.WorkflowName => new WorkflowNameReference(nodeId),
				DataReferenceType.WorkflowProperty => WorkflowPropertyReference.ParseFromStorage(reference, nodeId),
				_ => null,
			};
		}

		internal static string ReadNodeId(Storage.DOM.DataReference reference)
		{
			if (reference?.ReferenceData == null)
			{
				return null;
			}

			return reference.ReferenceData.TryGetValue(NodeIdKey, out var nodeId) && !String.IsNullOrEmpty(nodeId)
				? nodeId
				: null;
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
			return other is not null && Type == other.Type && String.Equals(NodeId, other.NodeId, StringComparison.Ordinal);
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
				hash = (hash * 23) + Type.GetHashCode();
				hash = (hash * 23) + (NodeId != null ? NodeId.GetHashCode() : 0);
				return hash;
			}
		}
	}
}
