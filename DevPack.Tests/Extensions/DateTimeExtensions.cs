namespace RT_MediaOps.Plan.Extensions
{
	using System;

	internal static class DateTimeExtensions
	{
		public static DateTime RoundToNextSecond(this DateTime dateTime)
		{
			long remainder = dateTime.Ticks % TimeSpan.TicksPerSecond;
			return remainder == 0 ? dateTime : new DateTime(dateTime.Ticks - remainder + TimeSpan.TicksPerSecond);
		}
	}
}
