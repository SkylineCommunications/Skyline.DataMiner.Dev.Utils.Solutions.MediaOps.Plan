namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the days of the week, allowing for combinations of days using bit flags.
	/// </summary>
	/// 

	[Flags]
	public enum WeekDays
	{
		/// <summary>
		/// Represents no day of the week being selected.
		/// </summary>
		None = 0,

		/// <summary>
		/// Represents Monday.
		/// </summary>
		Monday = 1,

		/// <summary>
		/// Represents Tuesday.
		/// </summary>
		Tuesday = 2,

		/// <summary>
		/// Represents Wednesday.
		/// </summary>
		Wednesday = 4,

		/// <summary>
		/// Represents Thursday.
		/// </summary>
		Thursday = 8,

		/// <summary>
		/// Represents Friday.
		/// </summary>
		Friday = 16,

		/// <summary>
		/// Represents Saturday.
		/// </summary>
		Saturday = 32,

		/// <summary>
		/// Represents Sunday.
		/// </summary>
		Sunday = 64,
	}
}