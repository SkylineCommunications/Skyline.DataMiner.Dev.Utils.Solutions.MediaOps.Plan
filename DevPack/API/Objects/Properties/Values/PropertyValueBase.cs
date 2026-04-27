namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageProperties = Storage.DOM.SlcProperties;

	public class PropertyValueCollection : ApiObject
	{
		private readonly List<CustomPropertyValue> customValues = [];
		private readonly List<StringPropertyValue> stringValues = [];
		private readonly List<BooleanPropertyValue> booleanValues = [];
		private readonly List<DiscretePropertyValue> discreteValues = [];

		private StorageProperties.PropertyValuesInstance originalInstance;
		private StorageProperties.PropertyValuesInstance updatedInstance;

		private string linkedObjectId;
		private string scope;
		private string subId;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyValueCollection"/> class.
		/// </summary>
		public PropertyValueCollection() : base()
		{
			IsNew = true;
		}

		internal PropertyValueCollection(MediaOpsPlanApi planApi, StorageProperties.PropertyValuesInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public override string Name { get; set; }

		public string LinkedObjectId { get => linkedObjectId; init => linkedObjectId = value; }

		public string Scope { get => scope; init => scope = value; }

		public string SubId { get => subId; init => subId = value; }

		public IReadOnlyCollection<CustomPropertyValue> CustomValues => customValues;

		public IReadOnlyCollection<StringPropertyValue> StringValues => stringValues;

		public IReadOnlyCollection<BooleanPropertyValue> BooleanValues => booleanValues;

		public IReadOnlyCollection<DiscretePropertyValue> DiscreteValues => discreteValues;

		private void ParseInstance(MediaOpsPlanApi planApi, StorageProperties.PropertyValuesInstance instance)
		{
			originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

			linkedObjectId = instance.PropertyValueInfo.LinkedObjectID;
			scope = instance.PropertyValueInfo.Scope;
			subId = instance.PropertyValueInfo.SubID;

			ParsePropertyValues(planApi, instance.PropertyValue);
		}

		private void ParsePropertyValues(MediaOpsPlanApi planApi, IList<StorageProperties.PropertyValueSection> propertyValues)
		{
			if (propertyValues == null || propertyValues.Count == 0)
			{
				return;
			}

			var propertyIds = propertyValues.Where(pv => pv.PropertyID.HasValue).Select(pv => pv.PropertyID.Value).Distinct();
			var propertiesById = planApi.PropertyDefinitions.Read(propertyIds).ToDictionary(p => p.Id);

			foreach (var section in propertyValues)
			{
				Property property = null;
				if (!section.PropertyID.HasValue)
				{
					customValues.Add(new CustomPropertyValue(section));
				}
				else if (!propertiesById.TryGetValue(section.PropertyID.Value, out property))
				{
					planApi.Logger.Information(this, $"Property with ID '{section.PropertyID.Value}' not found.");
				}

				if (property == null)
				{
					continue;
				}

				if (property is StringProperty stringProperty)
				{
					stringValues.Add(new StringPropertyValue(section));
				}
				else if (property is BooleanProperty booleanProperty)
				{
					booleanValues.Add(new BooleanPropertyValue(section));
				}
				else if (property is DiscreteProperty discreteProperty)
				{
					discreteValues.Add(new DiscretePropertyValue(section));
				}
			}
		}
	}

	public abstract class PropertyValueBase : TrackableObject
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		private protected PropertyValueBase()
		{
			IsNew = true;
		}

		internal PropertyValueBase(StorageProperties.PropertyValueSection section)
		{
			ParseSection(section);
		}

		public string Name { get; protected set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
		}
	}

	public class CustomPropertyValue : PropertyValueBase
	{
		public CustomPropertyValue(string name)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		internal CustomPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		public string Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = section.Value;
		}
	}

	public abstract class LinkedPropertyValue : PropertyValueBase
	{
		public LinkedPropertyValue(Property property)
		{
			PropertyId = property?.Id ?? throw new ArgumentNullException(nameof(property));
		}

		internal LinkedPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
		}

		public Guid PropertyId { get; private set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			PropertyId = section.PropertyID.Value;
		}
	}

	public class StringPropertyValue : LinkedPropertyValue
	{
		public StringPropertyValue(StringProperty property) : base(property)
		{
		}

		internal StringPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		public string Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = section.Value;
		}
	}

	public class BooleanPropertyValue : LinkedPropertyValue
	{
		public BooleanPropertyValue(BooleanProperty property) : base(property)
		{
		}

		internal BooleanPropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		public bool Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = Convert.ToBoolean(section.Value);
		}
	}

	public class DiscretePropertyValue : LinkedPropertyValue
	{
		public DiscretePropertyValue(DiscreteProperty property) : base(property)
		{
		}

		internal DiscretePropertyValue(StorageProperties.PropertyValueSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		public string Value { get; set; }

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			Value = section.Value;
		}
	}
}
