namespace RT_MediaOps.Plan.Properties.Querying
{
	using System;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Creates the properties, scheduling properties and property setting collections that are used to validate querying.
	/// </summary>
	internal sealed class PropertyQueryingSetup
	{
		private const string GlobalScope = "global";

		private readonly TestObjectCreator objectCreator;

		public PropertyQueryingSetup(TestObjectCreator objectCreator)
		{
			this.objectCreator = objectCreator;

			Prefix = Guid.NewGuid().ToString();

			CreateProperties();
			CreatePropertySettingCollections();
		}

		public string Prefix { get; }

		public Property[] Properties => new[]
		{
			GlobalPropertyA!,
			SchedulingPropertyB!,
			GlobalPropertyC!,
			SchedulingPropertyD!,
		};

		public Property[] SchedulingProperties => new[]
		{
			SchedulingPropertyB!,
			SchedulingPropertyD!,
		};

		public PropertySettingCollection[] PropertySettingCollections => new[]
		{
			PropertySettingCollection1!,
			PropertySettingCollection2!,
			PropertySettingCollection3!,
			PropertySettingCollection4!,
		};

		public StringProperty? GlobalPropertyA { get; private set; }

		public StringProperty? SchedulingPropertyB { get; private set; }

		public StringProperty? GlobalPropertyC { get; private set; }

		public StringProperty? SchedulingPropertyD { get; private set; }

		public BooleanProperty? GlobalBooleanProperty { get; private set; }

		public DiscreteProperty? GlobalDiscreteProperty { get; private set; }

		public FileProperty? GlobalFileProperty { get; private set; }

		public PropertySettingCollection? PropertySettingCollection1 { get; private set; }

		public PropertySettingCollection? PropertySettingCollection2 { get; private set; }

		public PropertySettingCollection? PropertySettingCollection3 { get; private set; }

		public PropertySettingCollection? PropertySettingCollection4 { get; private set; }

		private void CreateProperties()
		{
			GlobalPropertyA = objectCreator.CreateProperty(new StringProperty(new PropertyData { Scope = GlobalScope })
			{
				Name = $"Property_A_{Prefix}",
				SectionName = "General",
				Order = 1,
			});

			SchedulingPropertyB = objectCreator.CreateSchedulingProperty(new StringProperty
			{
				Name = $"Property_B_{Prefix}",
				SectionName = "General",
				Order = 2,
			});

			GlobalPropertyC = objectCreator.CreateProperty(new StringProperty(new PropertyData { Scope = GlobalScope })
			{
				Name = $"Property_C_{Prefix}",
				SectionName = "Other",
				Order = 3,
			});

			SchedulingPropertyD = objectCreator.CreateSchedulingProperty(new StringProperty
			{
				Name = $"Property_D_{Prefix}",
				SectionName = "Other",
				Order = 4,
			});

			GlobalBooleanProperty = objectCreator.CreateProperty(new BooleanProperty(new PropertyData { Scope = GlobalScope })
			{
				Name = $"Property_Boolean_{Prefix}",
				SectionName = "Other",
				Order = 5,
			});

			var discreteProperty = new DiscreteProperty(new PropertyData { Scope = GlobalScope })
			{
				Name = $"Property_Discrete_{Prefix}",
				SectionName = "Other",
				Order = 6,
			};
			discreteProperty.AddDiscrete("Option_1").AddDiscrete("Option_2");
			GlobalDiscreteProperty = objectCreator.CreateProperty(discreteProperty);

			GlobalFileProperty = objectCreator.CreateProperty(new FileProperty(new PropertyData { Scope = GlobalScope })
			{
				Name = $"Property_File_{Prefix}",
				SectionName = "Other",
				Order = 7,
			});
		}

		private void CreatePropertySettingCollections()
		{
			var collection1 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_1_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});

			// Settings of every type, mixed with custom settings and added in another order than the one of PropertySettingCollection.PropertySettings.
			collection1.Add(new DiscretePropertySetting(GlobalDiscreteProperty!) { Value = "Option_1" });
			collection1.Add(new FilePropertySetting(GlobalFileProperty!));
			collection1.Add(new CustomPropertySetting($"Custom_1_{Prefix}") { Value = "Custom value 1" });
			collection1.Add(new BooleanPropertySetting(GlobalBooleanProperty!) { Value = true });
			collection1.Add(new StringPropertySetting(GlobalPropertyA!) { Value = "First" });
			collection1.Add(new CustomPropertySetting($"Custom_2_{Prefix}"));
			collection1.Add(new StringPropertySetting(GlobalPropertyC!) { Value = "Extra" });

			var collection2 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_2_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});

			// Only settings with a value that are linked to a property, so their values are combined per section by position.
			collection2.Add(new BooleanPropertySetting(GlobalBooleanProperty!) { Value = false });
			collection2.Add(new StringPropertySetting(GlobalPropertyA!) { Value = "Second" });
			collection2.Add(new DiscretePropertySetting(GlobalDiscreteProperty!) { Value = "Option_2" });

			var collection3 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_3_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});
			collection3.Add(new StringPropertySetting(GlobalPropertyC!) { Value = "Third" });

			// Only custom settings.
			var collection4 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_4_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});
			collection4.Add(new CustomPropertySetting($"Custom_3_{Prefix}") { Value = "Custom value 3" });
			collection4.Add(new CustomPropertySetting($"Custom_4_{Prefix}") { Value = "Custom value 4" });

			PropertySettingCollection1 = objectCreator.CreatePropertySettingCollection(collection1);
			PropertySettingCollection2 = objectCreator.CreatePropertySettingCollection(collection2);
			PropertySettingCollection3 = objectCreator.CreatePropertySettingCollection(collection3);
			PropertySettingCollection4 = objectCreator.CreatePropertySettingCollection(collection4);
		}
	}
}
