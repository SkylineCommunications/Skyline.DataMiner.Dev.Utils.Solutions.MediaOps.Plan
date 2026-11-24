namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.ManagerStore.Select;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Querying;

	using DomPartialObject = Skyline.DataMiner.Net.Apps.ManagerStore.Select.PartialObject<Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstance, Skyline.DataMiner.Net.Apps.DataMinerObjectModel.DomInstanceId>;
	using LinkInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships.SlcRelationshipsIds.Sections.LinkInfo;

	/// <summary>
	/// Maps the <see cref="RelationshipExposers"/> that can be selected in a partial read to the DOM fields they are read
	/// from, and converts those values the same way <see cref="Relationship"/> does when it parses a full DOM instance.
	/// All relationship data is stored in the single link info section of the link DOM instance, so every field holds one value.
	/// </summary>
	internal sealed class RelationshipPartialFieldMapper : IDomPartialFieldMapper<Relationship>
	{
		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[RelationshipExposers.Id.fieldName] = new PartialField(x => x.Id.Id),

			[RelationshipExposers.Parent.ObjectTypeId.fieldName] = PartialField.ForGuid(LinkInfo.ParentObjectType),
			[RelationshipExposers.Parent.ObjectId.fieldName] = PartialField.ForString(LinkInfo.ParentObjectID),
			[RelationshipExposers.Parent.ObjectName.fieldName] = PartialField.ForString(LinkInfo.ParentObjectName),
			[RelationshipExposers.Parent.Url.fieldName] = PartialField.ForString(LinkInfo.ParentURL),
			[RelationshipExposers.Parent.Order.fieldName] = PartialField.ForLong(LinkInfo.ParentOrder),

			[RelationshipExposers.Child.ObjectTypeId.fieldName] = PartialField.ForGuid(LinkInfo.ChildObjectType),
			[RelationshipExposers.Child.ObjectId.fieldName] = PartialField.ForString(LinkInfo.ChildObjectID),
			[RelationshipExposers.Child.ObjectName.fieldName] = PartialField.ForString(LinkInfo.ChildObjectName),
			[RelationshipExposers.Child.Url.fieldName] = PartialField.ForString(LinkInfo.ChildURL),
			[RelationshipExposers.Child.Order.fieldName] = PartialField.ForLong(LinkInfo.ChildOrder),
		};

		private RelationshipPartialFieldMapper()
		{
		}

		public static RelationshipPartialFieldMapper Instance { get; } = new RelationshipPartialFieldMapper();

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified relationship fields from the link DOM instances.
		/// </summary>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static SelectedFields<DomInstance> CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selectedFields = new SelectedFields<DomInstance>();
			var descriptorIds = new HashSet<Guid>();

			foreach (var descriptor in GetFields(fields).Select(x => x.Field.Descriptor).Where(x => x != null && descriptorIds.Add(x.Id)))
			{
				selectedFields.Add(descriptor);
			}

			if (descriptorIds.Count == 0)
			{
				// Only the ID is read, which is always returned, but the read must select at least one field.
				selectedFields.Add(DomInstanceExposers.StatusId);
			}

			return selectedFields;
		}

		/// <summary>
		/// Converts partial link DOM instances into partial relationships containing the specified fields.
		/// </summary>
		/// <param name="domPartialObjects">The partial link DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Relationship>> CreatePartialRelationships(IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			if (domPartialObjects == null)
			{
				throw new ArgumentNullException(nameof(domPartialObjects));
			}

			if (fields == null)
			{
				throw new ArgumentNullException(nameof(fields));
			}

			var selected = GetFields(fields).ToList();

			return domPartialObjects
				.Select(domPartialObject => new Skyline.DataMiner.SDM.PartialObject<Relationship>(
					domPartialObject.Id.Id,
					selected.Select(x => new KeyValuePair<FieldExposer, IEnumerable<object>>(x.Exposer, ToValues(x.Field.Read(domPartialObject))))))
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<Relationship>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<Relationship>> IDomPartialFieldMapper<Relationship>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialRelationships(domPartialObjects, fields);
		}

		private static IEnumerable<(FieldExposer Exposer, PartialField Field)> GetFields(IEnumerable<FieldExposer> fields)
		{
			var seen = new HashSet<FieldExposer>(FieldExposerComparer.Instance);

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(Relationship) || !Fields.TryGetValue(exposer.fieldName, out var field))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private static IReadOnlyList<object> ToValues(object value)
		{
			return value == null ? Array.Empty<object>() : new[] { value };
		}

		private sealed class PartialField
		{
			public PartialField(Func<DomPartialObject, object> read, FieldDescriptorID descriptor = null)
			{
				Read = read;
				Descriptor = descriptor;
			}

			public Func<DomPartialObject, object> Read { get; }

			public FieldDescriptorID Descriptor { get; }

			public static PartialField ForGuid(FieldDescriptorID descriptor)
			{
				return new PartialField(x => x.TryGetValue<Guid>(descriptor, out var value) ? value : Guid.Empty, descriptor);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return new PartialField(x => x.TryGetValue<string>(descriptor, out var value) ? value : null, descriptor);
			}

			public static PartialField ForLong(FieldDescriptorID descriptor)
			{
				return new PartialField(x => x.TryGetValue<long>(descriptor, out var value) ? value : 0L, descriptor);
			}
		}
	}
}
