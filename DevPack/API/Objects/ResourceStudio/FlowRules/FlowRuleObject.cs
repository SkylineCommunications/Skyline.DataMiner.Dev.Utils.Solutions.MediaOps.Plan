namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	/// <summary>
	/// Identifies an object that can own flow rules or be the target of one.
	/// </summary>
	internal readonly struct FlowRuleObject : IEquatable<FlowRuleObject>
	{
		private static readonly Guid FlowRulesIdNamespace = new Guid("5c0f7b52-3f8e-4c1e-9d2a-6b7e1f4a8c31");

		public FlowRuleObject(FlowRuleObjectType type, Guid id)
		{
			Type = type;
			Id = id;
		}

		public FlowRuleObjectType Type { get; }

		public Guid Id { get; }

		public bool IsEmpty => Id == Guid.Empty;

		public static FlowRuleObject ResourcePool(Guid id) => new FlowRuleObject(FlowRuleObjectType.ResourcePool, id);

		// Name-based (version 5) UUID of the owner, so the rule set of an owner always gets the same ID.
		public Guid GetFlowRulesId()
		{
			var name = new byte[20];
			BitConverter.GetBytes((int)Type).CopyTo(name, 0);
			Id.ToByteArray().CopyTo(name, 4);

			byte[] hash;
			using (var sha1 = System.Security.Cryptography.SHA1.Create())
			{
				var input = FlowRulesIdNamespace.ToByteArray().Concat(name).ToArray();
				hash = sha1.ComputeHash(input);
			}

			var bytes = new byte[16];
			Array.Copy(hash, bytes, 16);
			bytes[7] = (byte)((bytes[7] & 0x0F) | 0x50);
			bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

			return new Guid(bytes);
		}

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
