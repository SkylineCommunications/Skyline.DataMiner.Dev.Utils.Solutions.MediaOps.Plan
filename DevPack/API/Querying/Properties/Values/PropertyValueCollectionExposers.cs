namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="PropertyValueCollection"/> objects.
	/// </summary>
	public class PropertyValueCollectionExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, Guid> Id = new Exposer<PropertyValueCollection, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertyValueCollection.LinkedObjectId"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, string> LinkedObjectId = new Exposer<PropertyValueCollection, string>((obj) => obj.LinkedObjectId, "LinkedObjectId");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertyValueCollection.Scope"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, string> Scope = new Exposer<PropertyValueCollection, string>((obj) => obj.Scope, "Scope");

		/// <summary>
		/// Gets an exposer for the <see cref="PropertyValueCollection.SubId"/> property.
		/// </summary>
		public static readonly Exposer<PropertyValueCollection, string> SubId = new Exposer<PropertyValueCollection, string>((obj) => obj.SubId, "SubId");

		/// <summary>
		/// Provides exposers for querying and filtering custom property values.
		/// </summary>
		public static class CustomPropertyValues
		{
			/// <summary>
			/// Gets a dynamic list exposer for custom property names.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, string> Name = DynamicListExposer<PropertyValueCollection, string>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.CustomValues.Where(x => x != null).Select(x => x.Name).Where(x => x != null), "CustomPropertyValues.Name"));

			/// <summary>
			/// Gets a dynamic list exposer for custom property values.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, string> Value = DynamicListExposer<PropertyValueCollection, string>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.CustomValues.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "CustomPropertyValues.Value"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering property values.
		/// </summary>
		public class PropertyValues
		{
			/// <summary>
			/// Gets a dynamic list exposer for property IDs.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, Guid> PropertyId = DynamicListExposer<PropertyValueCollection, Guid>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.PropertyValues.Where(x => x != null).Select(x => x.PropertyId).Where(x => x != null), "PropertyValues.PropertyId"));

			/// <summary>
			/// Gets a dynamic list exposer for property names.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, string> Name = DynamicListExposer<PropertyValueCollection, string>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.PropertyValues.Where(x => x != null).Select(x => x.Name).Where(x => x != null), "PropertyValues.Name"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering string property values.
		/// </summary>
		public class StringPropertyValues : PropertyValues
		{
			/// <summary>
			/// Gets a dynamic list exposer for string property values.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, string> Value = DynamicListExposer<PropertyValueCollection, string>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.StringValues.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "StringPropertyValues.Value"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering boolean property values.
		/// </summary>
		public class BooleanPropertyValues : PropertyValues
		{
			/// <summary>
			/// Gets a dynamic list exposer for boolean property values.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, bool> Value = DynamicListExposer<PropertyValueCollection, bool>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.BooleanValues.Where(x => x != null).Select(x => x.Value), "BooleanPropertyValues.Value"));
		}

		/// <summary>
		/// Provides exposers for querying and filtering discrete property values.
		/// </summary>
		public class DiscretePropertyValues : PropertyValues
		{
			/// <summary>
			/// Gets a dynamic list exposer for discrete property values.
			/// </summary>
			public static readonly DynamicListExposer<PropertyValueCollection, string> Value = DynamicListExposer<PropertyValueCollection, string>.CreateFromListExposer(new Exposer<PropertyValueCollection, IEnumerable>((obj) => obj.DiscreteValues.Where(x => x != null).Select(x => x.Value).Where(x => x != null), "DiscretePropertyValues.Value"));
		}
	}
}
