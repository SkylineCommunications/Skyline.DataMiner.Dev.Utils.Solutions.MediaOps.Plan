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
		};

		public StringProperty? GlobalPropertyA { get; private set; }

		public StringProperty? SchedulingPropertyB { get; private set; }

		public StringProperty? GlobalPropertyC { get; private set; }

		public StringProperty? SchedulingPropertyD { get; private set; }

		public PropertySettingCollection? PropertySettingCollection1 { get; private set; }

		public PropertySettingCollection? PropertySettingCollection2 { get; private set; }

		public PropertySettingCollection? PropertySettingCollection3 { get; private set; }

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
		}

		private void CreatePropertySettingCollections()
		{
			var collection1 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_1_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});
			collection1.Add(new StringPropertySetting(GlobalPropertyA!) { Value = "First" });

			var collection2 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_2_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});
			collection2.Add(new StringPropertySetting(GlobalPropertyA!) { Value = "Second" });

			var collection3 = new PropertySettingCollection(new PropertySettingCollectionData
			{
				LinkedObjectId = $"Object_3_{Prefix}",
				Scope = GlobalScope,
				SubId = String.Empty,
			});
			collection3.Add(new StringPropertySetting(GlobalPropertyC!) { Value = "Third" });

			PropertySettingCollection1 = objectCreator.CreatePropertySettingCollection(collection1);
			PropertySettingCollection2 = objectCreator.CreatePropertySettingCollection(collection2);
			PropertySettingCollection3 = objectCreator.CreatePropertySettingCollection(collection3);
		}
	}
}
