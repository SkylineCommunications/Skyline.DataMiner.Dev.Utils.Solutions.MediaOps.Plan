namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="PropertySettingCollection"/> objects.
	/// </summary>
	public static class PropertySettingCollectionExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<PropertySettingCollection, Guid> Id = new Exposer<PropertySettingCollection, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertySettingCollection.LinkedObjectId"/> property.
		/// </summary>
		public static readonly Exposer<PropertySettingCollection, string> LinkedObjectId = new Exposer<PropertySettingCollection, string>((obj) => obj.LinkedObjectId, "LinkedObjectId");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertySettingCollection.Scope"/> property.
		/// </summary>
		public static readonly Exposer<PropertySettingCollection, string> Scope = new Exposer<PropertySettingCollection, string>((obj) => obj.Scope, "Scope");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertySettingCollection.SubId"/> property.
		/// </summary>
		public static readonly Exposer<PropertySettingCollection, string> SubId = new Exposer<PropertySettingCollection, string>((obj) => obj.SubId, "SubId");

		/// <summary>
		/// Provides exposers for querying and filtering custom property settings.
		/// </summary>
		public static class CustomPropertySettings
		{
			/// <summary>
			/// Gets a dynamic list exposer for custom property names.
			/// </summary>
			public static readonly DynamicListExposer<PropertySettingCollection, string> Name = DynamicListExposer<PropertySettingCollection, string>.CreateFromListExposer(new Exposer<PropertySettingCollection, IEnumerable>((obj) => obj.CustomSettings.Where(x => x != null).Select(x => x.Name).Where(x => x != null), "CustomPropertySettings.Name"));

			/// <summary>
			/// Gets a dynamic list exposer for custom property settings.
			/// </summary>
			public static readonly DynamicListExposer<PropertySettingCollection, string> Value = DynamicListExposer<PropertySettingCollection, string>.CreateFromListExposer(new Exposer<PropertySettingCollection, IEnumerable>((obj) => obj.CustomSettings.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "CustomPropertySettings.Value"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering property settings.
		/// </summary>
		public class PropertySettings
		{
			/// <summary>
			/// Gets a dynamic list exposer for property IDs.
			/// </summary>
			public static readonly DynamicListExposer<PropertySettingCollection, Guid> PropertyId = DynamicListExposer<PropertySettingCollection, Guid>.CreateFromListExposer(new Exposer<PropertySettingCollection, IEnumerable>((obj) => obj.PropertySettings.Where(x => x != null).Select(x => x.Id).Where(x => x != null), "PropertySettings.PropertyId"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering string property settings.
		/// </summary>
		public class StringPropertySettings : PropertySettings
		{
			/// <summary>
			/// Gets a dynamic list exposer for string property settings.
			/// </summary>
			public static readonly DynamicListExposer<PropertySettingCollection, string> Value = DynamicListExposer<PropertySettingCollection, string>.CreateFromListExposer(new Exposer<PropertySettingCollection, IEnumerable>((obj) => obj.StringSettings.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "StringPropertySettings.Value"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering boolean property settings.
		/// </summary>
		public class BooleanPropertySettings : PropertySettings
		{
			/// <summary>
			/// Gets a dynamic list exposer for boolean property settings.
			/// </summary>
			public static readonly DynamicListExposer<PropertySettingCollection, bool> Value = DynamicListExposer<PropertySettingCollection, bool>.CreateFromListExposer(new Exposer<PropertySettingCollection, IEnumerable>((obj) => obj.BooleanSettings.Where(x => x != null).Select(x => x.Value), "BooleanPropertySettings.Value"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering discrete property settings.
		/// </summary>
		public class DiscretePropertySettings : PropertySettings
		{
			/// <summary>
			/// Gets a dynamic list exposer for discrete property settings.
			/// </summary>
			public static readonly DynamicListExposer<PropertySettingCollection, string> Value = DynamicListExposer<PropertySettingCollection, string>.CreateFromListExposer(new Exposer<PropertySettingCollection, IEnumerable>((obj) => obj.DiscreteSettings.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "DiscretePropertySettings.Value"));
		}
	}
}
