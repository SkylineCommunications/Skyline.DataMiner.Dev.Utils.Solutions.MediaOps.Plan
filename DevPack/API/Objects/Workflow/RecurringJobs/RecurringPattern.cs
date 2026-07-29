namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	/// <summary>
	/// Represents the pattern that will be used for all jobs that are going to be created as part of the series.
	/// </summary>
	public class RecurringPattern
	{
		private static readonly IReadOnlyDictionary<DayOfWeek, WeekDays> DayOfWeekToWeekDaysMap = new Dictionary<DayOfWeek, WeekDays>
		{
			{ DayOfWeek.Sunday, WeekDays.Sunday },
			{ DayOfWeek.Monday, WeekDays.Monday },
			{ DayOfWeek.Tuesday, WeekDays.Tuesday },
			{ DayOfWeek.Wednesday, WeekDays.Wednesday },
			{ DayOfWeek.Thursday, WeekDays.Thursday },
			{ DayOfWeek.Friday, WeekDays.Friday },
			{ DayOfWeek.Saturday, WeekDays.Saturday },
		};

		/// <summary>
		/// Represents the base unit of repetition (e.g., daily, weekly, monthly).
		/// </summary>
		public RepeatType RepeatType { get; set; }

		/// <summary>
		/// Represents the interval between each repetition based on the RepeatType.
		/// </summary>
		public int RepeatEvery { get; set; }

		/// <summary>
		/// Represents the specific date on which the recurring pattern should end.
		/// </summary>
		/// <remarks>
		/// Any timing information in the EndDate will be ignored, only the date part will be used.
		/// </remarks>
		public DateTimeOffset EndDate { get; set; }

		/// <summary>
		/// Represents a combination of week days on which jobs will be created.
		/// </summary>
		public WeekDays WeekDays { get; set; }

		/// <summary>
		/// Determines whether the specified object is equal to the current recurring pattern instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current recurring pattern instance.</param>
		/// <returns>true if the specified object is a recurring pattern with the same property values; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not RecurringPattern other)
			{
				return false;
			}

			return RepeatType == other.RepeatType
				&& RepeatEvery == other.RepeatEvery
				&& EndDate == other.EndDate
				&& WeekDays == other.WeekDays;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + RepeatType.GetHashCode();
				hash = (hash * 23) + RepeatEvery.GetHashCode();
				hash = (hash * 23) + EndDate.GetHashCode();
				hash = (hash * 23) + WeekDays.GetHashCode();

				return hash;
			}
		}

		/// <summary>
		/// Converts the RecurringPattern to a string representation.
		/// </summary>
		/// <returns>String representation of the RecurringPattern.</returns>
		public override string ToString()
		{
			var builder = new System.Text.StringBuilder();

			string translatedRepeatType = TranslateRepeatType(RepeatType);

			// Base: "Repeats every n unit(s)"
			if (RepeatEvery > 1)
			{
				builder.Append($"Repeats every {RepeatEvery} {translatedRepeatType}s");
			}
			else
			{
				builder.Append($"Repeats every {translatedRepeatType}");
			}

			// Weekly: append specific weekdays
			if (RepeatType == RepeatType.Weekly && WeekDays != WeekDays.None)
			{
				var selectedDays = Enum.GetValues(typeof(WeekDays))
					.Cast<WeekDays>()
					.Where(d => WeekDays.HasFlag(d) && d != WeekDays.None)
					.Select(d => d.ToString());

				builder.Append(" on ");
				builder.Append(string.Join(", ", selectedDays));
			}

			return builder.ToString();
		}

		/// <summary>
		/// Serializes the RecurringPattern to a JSON string representation.
		/// </summary>
		/// <returns>The serialized pattern.</returns>
		public string Serialize()
		{
			return JsonConvert.SerializeObject(this);
		}

		private static DateTimeOffset ConvertToTimeZoneOffset(DateTimeOffset dateTime, TimeZoneInfo timeZone)
		{
			var localDateTime = dateTime.DateTime;

			var zoneOffset = timeZone.GetUtcOffset(localDateTime);

			var correctLocal = new DateTimeOffset(localDateTime, zoneOffset);

			return correctLocal;
		}

		private static string TranslateRepeatType(RepeatType repeatType)
		{
			switch (repeatType)
			{
				case RepeatType.Daily:
					return "day";
				case RepeatType.Weekly:
					return "week";
				case RepeatType.Monthly:
					return "month";
				case RepeatType.Yearly:
					return "year";
				default:
					throw new NotSupportedException($"{repeatType} is not supported");
			}
		}

		/// <summary>
		/// Returns the time at which the last job in the recurrence starts, based on the start time and the amount of occurrences.
		/// </summary>
		/// <returns>Time at which the last job in the recurrence starts.</returns>
		/// <param name="occurrences">Amount of jobs to be created based on the pattern.</param>
		/// <param name="startTime">Start time of the recurrence.</param>
		/// <param name="timeZone">TimeZone of the start time of the last job in the recurrence.</param>
		/// <exception cref="NotSupportedException">In case the <see cref="RecurringPattern.RepeatType"/> is not supported.</exception>
		/// <exception cref="ArgumentOutOfRangeException">In case the <paramref name="occurrences"/> is less than 1.</exception>
		public DateTimeOffset CalculateLastJobDate(int occurrences, DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			return CalculateOccurrencesByAmount(occurrences, startTime, timeZone).Last();
		}

		/// <summary>
		/// Returns the amount of occurrences between the given start and end time of the recurring pattern.
		/// </summary>
		/// <returns>Amount of jobs between start- and end time of the recurrence.</returns>
		/// <param name="startTime">Start time of the recurrence.</param>
		/// <exception cref="NotImplementedException">If the <see cref="RecurringPattern.EndDate"/> is earlier than the provided <paramref name="startTime"/></exception>
		public int CalculateOccurrences(DateTimeOffset startTime)
		{
			return CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).Count();
		}

		/// <summary>
		/// Calculates the dates of the occurrences in the pattern, based on the start time and the amount of occurrences.
		/// </summary>
		/// <param name="occurrences">Amount of jobs to be created based on the pattern.</param>
		/// <param name="startTime">Start time of the recurrence.</param>
		/// <param name="timeZone">Time zone of the start times returned by this method.</param>
		/// <returns>Sorted list of occurrences.</returns>
		/// <exception cref="NotSupportedException">Thrown if the <see cref="RecurringPattern.RepeatType"/> is not supported.</exception>
		public IEnumerable<DateTimeOffset> CalculateOccurrencesByAmount(int occurrences, DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			if (occurrences < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(occurrences), "The amount of occurrences should be at least 1.");
			}

			switch (RepeatType)
			{
				case RepeatType.Daily:
					return GetOccurrencesByAmount_DailyPattern(occurrences, startTime, timeZone);
				case RepeatType.Weekly:
					return GetOccurrencesByAmount_WeeklyPattern(occurrences, startTime, timeZone);
				case RepeatType.Monthly:
					return GetOccurrencesByAmount_MonthlyPattern(occurrences, startTime, timeZone);
				case RepeatType.Yearly:
					return GetOccurrencesByAmount_YearlyPattern(occurrences, startTime, timeZone);
				default:
					throw new NotSupportedException($"{RepeatType} is not supported");
			}
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByAmount_DailyPattern(int occurrences, DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			var result = new List<DateTimeOffset>();

			for (int i = 0; i < occurrences; i++)
			{
				var localTime = startTime.AddDays(i * RepeatEvery);
				result.Add(ConvertToTimeZoneOffset(localTime, timeZone));
			}

			return result;
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByAmount_WeeklyPattern(int occurrences, DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			if (WeekDays == WeekDays.None)
			{
				throw new InvalidOperationException("At least one day of the week should be included in a weekly pattern.");
			}

			var result = new List<DateTimeOffset>();

			int weeksOffset = 0;
			var sortedDaysOfWeek = GetSortedDaysOfWeek(startTime, WeekDays);

			while (result.Count < occurrences)
			{
				DateTimeOffset weekStartDateTime = startTime.AddDays(weeksOffset * 7);

				foreach (var day in sortedDaysOfWeek)
				{
					int daysToAdd = ((int)day - (int)weekStartDateTime.DayOfWeek + 7) % 7;
					DateTimeOffset localDateTime = weekStartDateTime.AddDays(daysToAdd);

					if (localDateTime.Date < startTime.Date)
					{
						continue;
					}

					result.Add(ConvertToTimeZoneOffset(localDateTime, timeZone));

					if (result.Count >= occurrences)
					{
						break;
					}
				}

				weeksOffset += RepeatEvery;
			}

			return result;
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByAmount_MonthlyPattern(int occurrences, DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			var result = new List<DateTimeOffset>();

			for (int i = 0; i < occurrences; i++)
			{
				var localTime = startTime.AddMonths(i * RepeatEvery);
				result.Add(ConvertToTimeZoneOffset(localTime, timeZone));
			}

			return result;
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByAmount_YearlyPattern(int occurrences, DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			var result = new List<DateTimeOffset>();

			for (int i = 0; i < occurrences; i++)
			{
				var localTime = startTime.AddYears(i * RepeatEvery);
				result.Add(ConvertToTimeZoneOffset(localTime, timeZone));
			}

			return result;
		}

		/// <summary>
		/// Calculates the dates on which occurrences will be created based on the pattern, starting from the given start time until the end date defined in the pattern.
		/// </summary>
		/// <param name="startTime">Start time from which the occurrences will be generated.</param>
		/// <param name="timeZone">Time zone of the start times returned by this method.</param>
		/// <returns>List of dates on which an occurrence should take place.</returns>
		/// <exception cref="NotSupportedException">In case the End date defined in the pattern is earlier than the provided start time.</exception>
		public IEnumerable<DateTimeOffset> CalculateOccurrencesByEndDate(DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			if (EndDate.Date < startTime.Date) return new DateTimeOffset[0];

			switch (RepeatType)
			{
				case RepeatType.Daily:
					return GetOccurrencesByEndDate_DailyPattern(startTime, timeZone);
				case RepeatType.Weekly:
					return GetOccurrencesByEndDate_WeeklyPattern(startTime, timeZone);
				case RepeatType.Monthly:
					return GetOccurrencesByEndDate_MonthlyPattern(startTime, timeZone);
				case RepeatType.Yearly:
					return GetOccurrencesByEndDate_YearlyPattern(startTime, timeZone);
				default:
					throw new NotSupportedException($"{RepeatType} is not supported");
			}
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByEndDate_DailyPattern(DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			var result = new List<DateTimeOffset>();

			while (startTime.Date <= EndDate.Date)
			{
				result.Add(ConvertToTimeZoneOffset(startTime, timeZone));
				startTime = startTime.AddDays(RepeatEvery);
			}

			return result;
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByEndDate_WeeklyPattern(DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			if (WeekDays == WeekDays.None)
			{
				throw new InvalidOperationException("At least one day of the week should be included in a weekly pattern.");
			}

			var result = new List<DateTimeOffset>();

			var sortedDaysOfWeek = GetSortedDaysOfWeek(startTime, WeekDays);

			while (startTime.Date <= EndDate.Date)
			{
				if (sortedDaysOfWeek.Contains(startTime.DayOfWeek))
				{
					result.Add(ConvertToTimeZoneOffset(startTime, timeZone));
				}

				if (startTime.DayOfWeek == DayOfWeek.Sunday)
				{
					startTime = startTime.AddDays((7 * RepeatEvery) - 6); // Move to the next week
				}
				else
				{
					startTime = startTime.AddDays(1);
				}
			}

			return result;
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByEndDate_MonthlyPattern(DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			var result = new List<DateTimeOffset>();

			while (startTime.Date <= EndDate.Date)
			{
				result.Add(ConvertToTimeZoneOffset(startTime, timeZone));
				startTime = startTime.AddMonths(RepeatEvery);
			}

			return result;
		}

		private IEnumerable<DateTimeOffset> GetOccurrencesByEndDate_YearlyPattern(DateTimeOffset startTime, TimeZoneInfo timeZone)
		{
			var result = new List<DateTimeOffset>();

			while (startTime.Date <= EndDate.Date)
			{
				result.Add(ConvertToTimeZoneOffset(startTime, timeZone));
				startTime = startTime.AddYears(RepeatEvery);
			}

			return result;
		}

		private static List<DayOfWeek> GetSortedDaysOfWeek(DateTimeOffset startTime, WeekDays weekDays)
		{
			var daysOfWeek = new List<DayOfWeek>();

			for (int i = 0; i < 7; i++)
			{
				var day = startTime.AddDays(i).DayOfWeek;

				if (DayOfWeekToWeekDaysMap.TryGetValue(day, out var flag) && weekDays.HasFlag(flag))
				{
					daysOfWeek.Add(day);
				}
			}

			return daysOfWeek;
		}

		/// <summary>
		/// Used to translate from <see cref="DayOfWeek"/> to <see cref="WeekDays"/>.
		/// </summary>
		/// <param name="dayOfWeek">Days of the week to translate.</param>
		/// <returns>Translated days of the week.</returns>
		public static WeekDays TranslateDayOfWeek(DayOfWeek dayOfWeek)
		{
			return DayOfWeekToWeekDaysMap[dayOfWeek];
		}

		/// <summary>
		/// Deserializes a JSON string into a <see cref="RecurringPattern"/> object.
		/// </summary>
		/// <param name="json">The JSON text.</param>
		/// <returns>The recurring pattern.</returns>
		/// <exception cref="JsonSerializationException">When the json represents an invalid pattern.</exception>
		public static RecurringPattern Deserialize(string json)
		{
			if (string.IsNullOrEmpty(json))
			{
				throw new JsonSerializationException("The JSON string cannot be null or empty.");
			}

			return SecureNewtonsoftDeserialization.DeserializeObject<RecurringPattern>(json);
		}
	}
}
