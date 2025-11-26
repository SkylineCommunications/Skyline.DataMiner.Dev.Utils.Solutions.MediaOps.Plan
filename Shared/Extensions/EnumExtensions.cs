namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions
{
    using System;
    using System.Collections.Generic;
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
    }
}
