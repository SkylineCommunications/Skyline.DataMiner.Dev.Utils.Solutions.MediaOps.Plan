namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Identifies an object that can own flow rules or be the target of one.
	/// </summary>
	internal readonly struct FlowRuleObject : IEquatable<FlowRuleObject>
	{
		public FlowRuleObject(FlowRuleObjectType type, Guid id)
		{
			Type = type;
			Id = id;
		}

		public FlowRuleObjectType Type { get; }

		public Guid Id { get; }

		public bool IsEmpty => Id == Guid.Empty;

		public static FlowRuleObject ResourcePool(Guid id) => new FlowRuleObject(FlowRuleObjectType.ResourcePool, id);

		public static FlowRuleObjectType FromStorage(StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleobjecttype? type)
		{
			return type.HasValue
				? type.Value.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleobjecttype, FlowRuleObjectType>()
				: FlowRuleObjectType.ResourcePool;
		}

		public static StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleobjecttype ToStorage(FlowRuleObjectType type)
		{
			return type.MapEnum<FlowRuleObjectType, StorageResourceStudio.SlcResource_StudioIds.Enums.Flowruleobjecttype>();
		}

		public static void ValidateType(FlowRuleObjectType type, string paramName)
		{
			if (!Enum.IsDefined(typeof(FlowRuleObjectType), type))
			{
				throw new ArgumentException($"Value '{type}' is not a valid flow rule object type.", paramName);
			}
		}

		public static bool operator ==(FlowRuleObject left, FlowRuleObject right) => left.Equals(right);

		public static bool operator !=(FlowRuleObject left, FlowRuleObject right) => !left.Equals(right);

		public bool Equals(FlowRuleObject other) => Type == other.Type && Id == other.Id;

		public override bool Equals(object obj) => obj is FlowRuleObject other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				return (Type.GetHashCode() * 397) ^ Id.GetHashCode();
			}
		}

		public override string ToString() => $"{Type} {Id}";
	}
}
