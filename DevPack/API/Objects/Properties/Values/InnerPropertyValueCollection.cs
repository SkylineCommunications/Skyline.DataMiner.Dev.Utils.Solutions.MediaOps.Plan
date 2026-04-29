namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StorageProperties = Storage.DOM.SlcProperties;

	internal class InnerPropertyValueCollection : PropertyValueCollection
	{
		private StorageProperties.PropertyValuesInstance originalInstance;
		private StorageProperties.PropertyValuesInstance updatedInstance;

		internal InnerPropertyValueCollection() : base()
		{
		}

		internal InnerPropertyValueCollection(MediaOpsPlanApi planApi, StorageProperties.PropertyValuesInstance instance) : base(instance.ID.Id)
		{
			ParseInstance(planApi, instance);
			InitTracking();
		}

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
			var propertiesById = planApi.Properties.Read(propertyIds).ToDictionary(p => p.Id);

			foreach (var section in propertyValues)
			{
				Property property = null;
				if (!section.PropertyID.HasValue)
				{
					customValues.Add(new InnerCustomPropertyValue(section));
				}
				else if (!propertiesById.TryGetValue(section.PropertyID.Value, out property))
				{
					planApi.Logger.Information(this, $"Property with ID '{section.PropertyID.Value}' not found.");
				}

				if (property == null)
				{
					continue;
				}

				if (property is StringProperty)
				{
					stringValues.Add(new InnerStringPropertyValue(section));
				}
				else if (property is BooleanProperty)
				{
					booleanValues.Add(new InnerBooleanPropertyValue(section));
				}
				else if (property is DiscreteProperty)
				{
					discreteValues.Add(new InnerDiscretePropertyValue(section));
				}
			}
		}
	}

	internal class InnerCustomPropertyValue : CustomPropertyValue
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		internal InnerCustomPropertyValue(StorageProperties.PropertyValueSection section) : base()
		{
			ParseSection(section);
			InitTracking();
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
			Value = section.Value;
		}
	}

	internal class InnerStringPropertyValue : StringPropertyValue
	{
		private StorageProperties.PropertyValueSection originalSection;

		internal InnerStringPropertyValue(StorageProperties.PropertyValueSection section) : base()
		{
			IsNew = false;
			ParseSection(section);
			InitTracking();
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
			PropertyId = section.PropertyID.Value;
			Value = section.Value;
		}
	}

	internal class InnerBooleanPropertyValue : BooleanPropertyValue
	{
		private StorageProperties.PropertyValueSection originalSection;

		internal InnerBooleanPropertyValue(StorageProperties.PropertyValueSection section) : base()
		{
			IsNew = false;
			ParseSection(section);
			InitTracking();
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
			PropertyId = section.PropertyID.Value;
			Value = Convert.ToBoolean(section.Value);
		}
	}

	internal class InnerDiscretePropertyValue : DiscretePropertyValue
	{
		private StorageProperties.PropertyValueSection originalSection;

		internal InnerDiscretePropertyValue(StorageProperties.PropertyValueSection section) : base()
		{
			IsNew = false;
			ParseSection(section);
			InitTracking();
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));
			PropertyId = section.PropertyID.Value;
			Value = section.Value;
		}
	}
}
