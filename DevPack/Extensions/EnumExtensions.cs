namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	internal static class EnumExtensions
	{
		public static TTarget MapEnum<TSource, TTarget>(this TSource sourceEnum)
		{
			if (EqualityComparer<TSource>.Default.Equals(sourceEnum, default))
			{
				return default;
			}

			return Enum.GetValues(typeof(TTarget))
				.Cast<TTarget>()
				.Single(target => target.ToString().Equals(sourceEnum.ToString(), StringComparison.OrdinalIgnoreCase));
		}

		public static string GetDescription<T>(this T value) where T : struct, Enum
		{
			var name = Convert.ToString(value);
			var field = typeof(T).GetField(name);

			if (field != null &&
				Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attr)
			{
				return attr.Description;
			}

			return name;
		}
	}
}
