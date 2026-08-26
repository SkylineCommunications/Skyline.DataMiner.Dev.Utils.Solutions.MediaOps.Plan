namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests for the <see cref="DataReference"/> model itself: construction, equality, hashing and formatting.
	/// Persistence is covered by <see cref="DataReferenceStorageTests"/> and resolution by <see cref="ReferenceResolverTests"/>.
	/// </summary>
	[TestClass]
	public sealed class DataReferenceTests
	{
		private static readonly Guid PayloadId = new Guid("12345678-1234-1234-1234-123456789012");

		public static IEnumerable<object[]> AllTypes => DataReferenceFactory.AllTypes;

		public static IEnumerable<object[]> TypesWithPayload => DataReferenceFactory.TypesWithPayload;

		/// <summary>Guards that every reference type keeps taking part in the data-driven tests below.</summary>
		[TestMethod]
		public void DataReferenceTests_EveryDataReferenceType_HasAReferenceImplementation()
		{
			foreach (DataReferenceType type in Enum.GetValues(typeof(DataReferenceType)))
			{
				var reference = DataReferenceFactory.Create(type, PayloadId);

				Assert.AreEqual(type, reference.Type, $"'{type}' is not mapped onto a matching reference implementation.");
			}
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_Constructor_WithoutNodeId_TargetsTheWorkflowOrJobItself(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId);

			Assert.AreEqual(type, reference.Type);
			Assert.IsNull(reference.NodeId);
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_Constructor_WithNodeId_TargetsThatNode(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId, "node-1");

			Assert.AreEqual("node-1", reference.NodeId);
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_Constructor_EmptyNodeId_NormalizesToNull(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId, String.Empty);

			Assert.IsNull(reference.NodeId);
		}

		[DataTestMethod]
		[DynamicData(nameof(TypesWithPayload))]
		public void DataReferenceTests_Constructor_WithPayload_ExposesTheIdentifier(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId);

			Assert.AreEqual(PayloadId, DataReferenceFactory.GetPayloadId(reference));
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_Equals_SameTypePayloadAndNode_ReturnsTrue(DataReferenceType type)
		{
			var first = DataReferenceFactory.Create(type, PayloadId, "node-1");
			var second = DataReferenceFactory.Create(type, PayloadId, "node-1");

			Assert.IsTrue(first.Equals(second));
			Assert.IsTrue(second.Equals(first));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_Equals_DifferentNodeId_ReturnsFalse(DataReferenceType type)
		{
			var withoutNode = DataReferenceFactory.Create(type, PayloadId);
			var onNode1 = DataReferenceFactory.Create(type, PayloadId, "node-1");
			var onNode2 = DataReferenceFactory.Create(type, PayloadId, "node-2");

			Assert.IsFalse(onNode1.Equals(onNode2));
			Assert.IsFalse(onNode1.Equals(withoutNode));
			Assert.IsFalse(withoutNode.Equals(onNode1));
		}

		[DataTestMethod]
		[DynamicData(nameof(TypesWithPayload))]
		public void DataReferenceTests_Equals_DifferentPayload_ReturnsFalse(DataReferenceType type)
		{
			var first = DataReferenceFactory.Create(type, Guid.NewGuid());
			var second = DataReferenceFactory.Create(type, Guid.NewGuid());

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void DataReferenceTests_Equals_DifferentType_ReturnsFalse()
		{
			var references = Enum.GetValues(typeof(DataReferenceType))
				.Cast<DataReferenceType>()
				.Select(type => DataReferenceFactory.Create(type, PayloadId))
				.ToList();

			foreach (var first in references)
			{
				foreach (var second in references.Where(other => other.Type != first.Type))
				{
					Assert.IsFalse(first.Equals(second), $"'{first.Type}' must not equal '{second.Type}'.");
				}
			}
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_Equals_Null_ReturnsFalse(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId);

			Assert.IsFalse(reference.Equals((DataReference?)null));
			Assert.IsFalse(reference.Equals((object?)null));
		}

		/// <summary>
		/// The untyped overload must stay in sync with the typed one: resolved-reference caches and validators key
		/// their dictionaries on <see cref="DataReference"/>.
		/// </summary>
		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_EqualsAsObject_BehavesLikeTypedEquals(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId, "node-1");
			var same = DataReferenceFactory.Create(type, PayloadId, "node-1");
			var otherNode = DataReferenceFactory.Create(type, PayloadId, "node-2");

			Assert.IsTrue(reference.Equals((object)same));
			Assert.IsFalse(reference.Equals((object)otherNode));
			Assert.IsFalse(reference.Equals("not a reference"));
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_UsedAsDictionaryKey_EqualReferencesShareTheSameEntry(DataReferenceType type)
		{
			var dictionary = new Dictionary<DataReference, string>
			{
				[DataReferenceFactory.Create(type, PayloadId, "node-1")] = "on node 1",
				[DataReferenceFactory.Create(type, PayloadId, "node-2")] = "on node 2",
				[DataReferenceFactory.Create(type, PayloadId)] = "on the job",
			};

			Assert.AreEqual(3, dictionary.Count);
			Assert.AreEqual("on node 1", dictionary[DataReferenceFactory.Create(type, PayloadId, "node-1")]);
			Assert.AreEqual("on node 2", dictionary[DataReferenceFactory.Create(type, PayloadId, "node-2")]);
			Assert.AreEqual("on the job", dictionary[DataReferenceFactory.Create(type, PayloadId)]);
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_ToString_WithoutNodeId_DescribesTypeAndPayload(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId);
			var payloadKey = DataReferenceFactory.GetPayloadKey(type);

			var expected = payloadKey == null
				? $"{type}"
				: $"{type} ({payloadKey}: {PayloadId})";

			Assert.AreEqual(expected, reference.ToString());
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_ToString_WithNodeId_IncludesNodeId(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId, "node-5");
			var payloadKey = DataReferenceFactory.GetPayloadKey(type);

			var expected = payloadKey == null
				? $"{type} (NodeId: node-5)"
				: $"{type} ({payloadKey}: {PayloadId}, NodeId: node-5)";

			Assert.AreEqual(expected, reference.ToString());
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceTests_NodeId_IsMutableSoReferencesCanBeRetargeted(DataReferenceType type)
		{
			var reference = DataReferenceFactory.Create(type, PayloadId);

			reference.NodeId = "node-9";

			Assert.AreEqual("node-9", reference.NodeId);
		}
	}
}
