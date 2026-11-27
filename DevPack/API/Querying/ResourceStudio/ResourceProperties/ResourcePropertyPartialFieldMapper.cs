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
	using PropertyInfo = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo;

	/// <summary>
	/// Maps the <see cref="ResourcePropertyExposers"/> that can be selected in a partial read to the DOM fields they are read
	/// from, and converts those values the same way <see cref="ResourceProperty"/> does when it parses a full DOM instance.
	/// All resource property data is stored in the single property info section, so every field holds one value.
	/// </summary>
	internal sealed class ResourcePropertyPartialFieldMapper : IDomPartialFieldMapper<ResourceProperty>
	{
		private static readonly Dictionary<string, PartialField> Fields = new Dictionary<string, PartialField>
		{
			[ResourcePropertyExposers.Id.fieldName] = PartialField.Create(x => x.Id.Id),
			[ResourcePropertyExposers.Name.fieldName] = PartialField.ForString(PropertyInfo.PropertyName),
		};

		private ResourcePropertyPartialFieldMapper()
		{
		}

		public static ResourcePropertyPartialFieldMapper Instance { get; } = new ResourcePropertyPartialFieldMapper();

		/// <summary>
		/// Gets the names of the fields that can be selected.
		/// </summary>
		public static IReadOnlyCollection<string> SupportedFieldNames => Fields.Keys;

		/// <summary>
		/// Creates the DOM field selection needed to read the specified resource property fields from the resource property DOM instances.
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
		/// Converts partial resource property DOM instances into partial resource properties containing the specified fields.
		/// </summary>
		/// <param name="domPartialObjects">The partial resource property DOM instances, read with <see cref="CreateSelectedFields"/>.</param>
		/// <param name="fields">The fields to return.</param>
		/// <exception cref="NotSupportedException">Thrown when one of the fields can't be read partially.</exception>
		public static IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<ResourceProperty>> CreatePartialResourceProperties(IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
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
				.Select(domPartialObject =>
				{
					var builder = Skyline.DataMiner.SDM.PartialObject<ResourceProperty>.CreateBuilder();
					foreach (var (exposer, field) in selected)
					{
						field.Include(builder, exposer, domPartialObject);
					}

					return builder.Build();
				})
				.ToList();
		}

		SelectedFields<DomInstance> IDomPartialFieldMapper<ResourceProperty>.CreateSelectedFields(IEnumerable<FieldExposer> fields)
		{
			return CreateSelectedFields(fields);
		}

		IReadOnlyList<Skyline.DataMiner.SDM.PartialObject<ResourceProperty>> IDomPartialFieldMapper<ResourceProperty>.CreatePartialObjects(MediaOpsPlanApi planApi, IEnumerable<DomPartialObject> domPartialObjects, IEnumerable<FieldExposer> fields)
		{
			return CreatePartialResourceProperties(domPartialObjects, fields);
		}

		private static IEnumerable<(FieldExposer Exposer, PartialField Field)> GetFields(IEnumerable<FieldExposer> fields)
		{
			// Partial objects look up values by exposer instance, so the instances themselves are de-duplicated.
			var seen = new HashSet<FieldExposer>();

			foreach (var exposer in fields)
			{
				if (exposer == null || !seen.Add(exposer))
				{
					continue;
				}

				if (exposer.FilterType != typeof(ResourceProperty) || !Fields.TryGetValue(exposer.fieldName, out var field) || !field.ExposerType.IsInstanceOfType(exposer))
				{
					throw new NotSupportedException($"Field '{exposer.fieldName}' can't be read partially. Supported fields: {String.Join(", ", Fields.Keys)}.");
				}

				yield return (exposer, field);
			}
		}

		private sealed class PartialField
		{
			private readonly Action<Skyline.DataMiner.SDM.PartialObject<ResourceProperty>.Builder, FieldExposer, DomPartialObject> include;

			private PartialField(Type exposerType, Action<Skyline.DataMiner.SDM.PartialObject<ResourceProperty>.Builder, FieldExposer, DomPartialObject> include, FieldDescriptorID descriptor)
			{
				ExposerType = exposerType;
				this.include = include;
				Descriptor = descriptor;
			}

			/// <summary>
			/// Gets the type the selected exposer must have, so the value can be included with the exposer's field type.
			/// </summary>
			public Type ExposerType { get; }

			public FieldDescriptorID Descriptor { get; }

			public static PartialField Create<TField>(Func<DomPartialObject, TField> read, FieldDescriptorID descriptor = null)
			{
				return new PartialField(
					typeof(Exposer<ResourceProperty, TField>),
					(builder, exposer, domPartialObject) => builder.Include((Exposer<ResourceProperty, TField>)exposer, read(domPartialObject)),
					descriptor);
			}

			public static PartialField ForString(FieldDescriptorID descriptor)
			{
				return Create(x => x.TryGetValue<string>(descriptor, out var value) ? value : null, descriptor);
			}

			public void Include(Skyline.DataMiner.SDM.PartialObject<ResourceProperty>.Builder builder, FieldExposer exposer, DomPartialObject domPartialObject)
			{
				include(builder, exposer, domPartialObject);
			}
		}
	}
}
