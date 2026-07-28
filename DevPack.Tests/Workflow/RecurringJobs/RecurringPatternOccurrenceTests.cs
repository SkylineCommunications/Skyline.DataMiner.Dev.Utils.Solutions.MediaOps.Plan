namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class RecurringPatternOccurrenceTests
	{
		// UTC+1 in winter / UTC+2 in summer (DST). DST switches in Belgium (CET/CEST).
		// This timezone transitions from UTC+1 to UTC+2 on the last Sunday of March at 02:00 local time
		// and from UTC+2 to UTC+1 on the last Sunday of October at 03:00 local time.
		private static readonly TimeZoneInfo CentralEuropeanTime = TimeZoneInfo.FindSystemTimeZoneById(
			Environment.OSVersion.Platform == PlatformID.Win32NT
				? "Central European Standard Time"
				: "Europe/Brussels");

		// UTC-5 in winter / UTC-4 in summer (DST). Transitions on the second Sunday of March and first Sunday of November.
		private static readonly TimeZoneInfo EasternTime = TimeZoneInfo.FindSystemTimeZoneById(
			Environment.OSVersion.Platform == PlatformID.Win32NT
				? "Eastern Standard Time"
				: "America/New_York");

		#region CalculateOccurrencesByAmount – Daily

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_UtcTimezone_ReturnsCorrectDates()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 2, 10, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 3, 10, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_RepeatEvery2_SkipsAlternateDays()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 2,
			};

			var startTime = new DateTimeOffset(2024, 3, 1, 8, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(new DateTimeOffset(2024, 3, 1, 8, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 3, 3, 8, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 3, 5, 8, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_CentralEuropeanTimezone_WinterPeriod_ReturnsCorrectOffset()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			// January is in winter (UTC+1).
			var startTime = new DateTimeOffset(2024, 1, 10, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(2, startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(2, occurrences.Length);
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset);
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_CentralEuropeanTimezone_SummerPeriod_ReturnsCorrectOffset()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			// July is in summer (UTC+2).
			var startTime = new DateTimeOffset(2024, 7, 1, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(2, startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(2, occurrences.Length);
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[0].Offset);
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[1].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_CentralEuropeanTimezone_AcrossDstSpringForward_OffsetChanges()
		{
			// In 2024, DST spring-forward in CET is on March 31.
			// A job starting in winter that spans across the transition should reflect the UTC+2 offset after the switch.
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			// March 30 is still UTC+1.
			var startTime = new DateTimeOffset(2024, 3, 30, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// March 30 – winter time, UTC+1.
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset);
			// March 31 – DST spring-forward, UTC+2.
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[1].Offset);
			// April 1 – summer time, UTC+2.
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[2].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_CentralEuropeanTimezone_AcrossDstFallBack_OffsetChanges()
		{
			// In 2024, DST fall-back in CET is on October 27.
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			// October 26 is in summer time (UTC+2).
			var startTime = new DateTimeOffset(2024, 10, 26, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// October 26 – summer time, UTC+2.
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[0].Offset);
			// October 27 – DST fall-back, UTC+1.
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset);
			// October 28 – winter time, UTC+1.
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[2].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Daily_LessThanOneOccurrence_Throws()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

			Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
				pattern.CalculateOccurrencesByAmount(0, startTime, TimeZoneInfo.Utc).ToArray());
		}

		#endregion

		#region CalculateOccurrencesByAmount – Weekly

		[TestMethod]
		public void CalculateOccurrencesByAmount_Weekly_SingleDay_UtcTimezone_ReturnsCorrectDates()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Monday,
			};

			// 2024-01-01 is a Monday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(DayOfWeek.Monday, occurrences[0].DayOfWeek);
			Assert.AreEqual(DayOfWeek.Monday, occurrences[1].DayOfWeek);
			Assert.AreEqual(DayOfWeek.Monday, occurrences[2].DayOfWeek);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 8, 9, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Weekly_MultipleDays_ReturnsCorrectOrder()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Monday | WeekDays.Wednesday | WeekDays.Friday,
			};

			// 2024-01-01 is a Monday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(5, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(5, occurrences.Length);
			Assert.AreEqual(DayOfWeek.Monday, occurrences[0].DayOfWeek);    // Jan 1
			Assert.AreEqual(DayOfWeek.Wednesday, occurrences[1].DayOfWeek); // Jan 3
			Assert.AreEqual(DayOfWeek.Friday, occurrences[2].DayOfWeek);    // Jan 5
			Assert.AreEqual(DayOfWeek.Monday, occurrences[3].DayOfWeek);    // Jan 8
			Assert.AreEqual(DayOfWeek.Wednesday, occurrences[4].DayOfWeek); // Jan 10
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Weekly_RepeatEvery2_SkipsAlternateWeeks()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 2,
				WeekDays = WeekDays.Monday,
			};

			// 2024-01-01 is a Monday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 29, 9, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Weekly_CentralEuropeanTimezone_AcrossDstSpringForward_OffsetChanges()
		{
			// In 2024, DST spring-forward in CET is on March 31 (Sunday).
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Saturday,
			};

			// March 23 (Saturday) is in winter (UTC+1). Next Saturday is March 30 (winter), then April 6 (summer UTC+2).
			var startTime = new DateTimeOffset(2024, 3, 23, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// March 23 – winter time.
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset);
			// March 30 – still winter time (spring-forward is the following Sunday).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset);
			// April 6 – summer time.
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[2].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Weekly_NoWeekDaysSet_Throws()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.None,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);

			Assert.ThrowsException<InvalidOperationException>(() =>
				pattern.CalculateOccurrencesByAmount(1, startTime, TimeZoneInfo.Utc).ToArray());
		}

		#endregion

		#region CalculateOccurrencesByAmount – Monthly

		[TestMethod]
		public void CalculateOccurrencesByAmount_Monthly_UtcTimezone_ReturnsCorrectDates()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Monthly,
				RepeatEvery = 1,
			};

			var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 2, 15, 10, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 3, 15, 10, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Monthly_RepeatEvery3_SkipsMonths()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Monthly,
				RepeatEvery = 3,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(4, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(4, occurrences.Length);
			Assert.AreEqual(1, occurrences[0].Month);
			Assert.AreEqual(4, occurrences[1].Month);
			Assert.AreEqual(7, occurrences[2].Month);
			Assert.AreEqual(10, occurrences[3].Month);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Monthly_CentralEuropeanTimezone_AcrossDstTransition_OffsetChanges()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Monthly,
				RepeatEvery = 1,
			};

			// February is in winter (UTC+1), April is in summer (UTC+2).
			var startTime = new DateTimeOffset(2024, 2, 15, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset); // February – winter
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset); // March – still winter (DST on March 31)
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[2].Offset); // April – summer
		}

		#endregion

		#region CalculateOccurrencesByAmount – Yearly

		[TestMethod]
		public void CalculateOccurrencesByAmount_Yearly_UtcTimezone_ReturnsCorrectDates()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Yearly,
				RepeatEvery = 1,
			};

			var startTime = new DateTimeOffset(2020, 6, 15, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(3, startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(2020, occurrences[0].Year);
			Assert.AreEqual(2021, occurrences[1].Year);
			Assert.AreEqual(2022, occurrences[2].Year);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Yearly_EasternTimezone_WinterPeriod_ReturnsCorrectOffset()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Yearly,
				RepeatEvery = 1,
			};

			// January is in winter (UTC-5) for Eastern time.
			var startTime = new DateTimeOffset(2022, 1, 10, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(2, startTime, EasternTime).ToArray();

			Assert.AreEqual(2, occurrences.Length);
			Assert.AreEqual(TimeSpan.FromHours(-5), occurrences[0].Offset);
			Assert.AreEqual(TimeSpan.FromHours(-5), occurrences[1].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByAmount_Yearly_EasternTimezone_SummerPeriod_ReturnsCorrectOffset()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Yearly,
				RepeatEvery = 1,
			};

			// July is in summer (UTC-4) for Eastern time.
			var startTime = new DateTimeOffset(2022, 7, 10, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByAmount(2, startTime, EasternTime).ToArray();

			Assert.AreEqual(2, occurrences.Length);
			Assert.AreEqual(TimeSpan.FromHours(-4), occurrences[0].Offset);
			Assert.AreEqual(TimeSpan.FromHours(-4), occurrences[1].Offset);
		}

		#endregion

		#region CalculateOccurrencesByEndDate – Daily

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Daily_UtcTimezone_ReturnsAllDatesUpToEndDate()
		{
			var endDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(5, occurrences.Length);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Daily_EndDateBeforeStartDate_ReturnsEmpty()
		{
			var endDate = new DateTimeOffset(2023, 12, 31, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(0, occurrences.Length);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Daily_CentralEuropeanTimezone_AcrossDstSpringForward_OffsetChanges()
		{
			// In 2024, DST spring-forward in CET is on March 31.
			var endDate = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			// Start on March 30, ending April 1.
			var startTime = new DateTimeOffset(2024, 3, 30, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// March 30 – winter time (UTC+1).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset);
			// March 31 – DST spring-forward, summer time (UTC+2).
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[1].Offset);
			// April 1 – summer time (UTC+2).
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[2].Offset);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Daily_CentralEuropeanTimezone_AcrossDstFallBack_OffsetChanges()
		{
			// In 2024, DST fall-back in CET is on October 27.
			var endDate = new DateTimeOffset(2024, 10, 28, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			// Start on October 26 (summer, UTC+2), ending October 28.
			var startTime = new DateTimeOffset(2024, 10, 26, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// October 26 – summer time (UTC+2).
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[0].Offset);
			// October 27 – DST fall-back, winter time (UTC+1).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset);
			// October 28 – winter time (UTC+1).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[2].Offset);
		}

		#endregion

		#region CalculateOccurrencesByEndDate – Weekly

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Weekly_SingleDay_ReturnsCorrectDates()
		{
			var endDate = new DateTimeOffset(2024, 1, 22, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Monday,
				EndDate = endDate,
			};

			// 2024-01-01 is a Monday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 8, 9, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Weekly_MultipleDays_ReturnsAllDaysWithinRange()
		{
			var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Monday | WeekDays.Friday,
				EndDate = endDate,
			};

			// 2024-01-01 is a Monday. 2024-01-05 is a Friday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(DayOfWeek.Monday, occurrences[0].DayOfWeek); // Jan 1
			Assert.AreEqual(DayOfWeek.Friday, occurrences[1].DayOfWeek); // Jan 5
			Assert.AreEqual(DayOfWeek.Monday, occurrences[2].DayOfWeek); // Jan 8
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Weekly_RepeatEvery2_SkipsAlternateWeeks()
		{
			var endDate = new DateTimeOffset(2024, 1, 30, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 2,
				WeekDays = WeekDays.Monday,
				EndDate = endDate,
			};

			// 2024-01-01 is a Monday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero), occurrences[0]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero), occurrences[1]);
			Assert.AreEqual(new DateTimeOffset(2024, 1, 29, 9, 0, 0, TimeSpan.Zero), occurrences[2]);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Weekly_CentralEuropeanTimezone_AcrossDstSpringForward_OffsetChanges()
		{
			// In 2024, DST spring-forward in CET is on March 31 (Sunday).
			var endDate = new DateTimeOffset(2024, 4, 7, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Saturday,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2024, 3, 23, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// March 23 – winter time (UTC+1).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset);
			// March 30 – still winter time.
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset);
			// April 6 – summer time (UTC+2).
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[2].Offset);
		}

		#endregion

		#region CalculateOccurrencesByEndDate – Monthly

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Monthly_UtcTimezone_ReturnsCorrectDates()
		{
			var endDate = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Monthly,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(1, occurrences[0].Month);
			Assert.AreEqual(2, occurrences[1].Month);
			Assert.AreEqual(3, occurrences[2].Month);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Monthly_CentralEuropeanTimezone_AcrossDstTransition_OffsetChanges()
		{
			var endDate = new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Monthly,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			// Start in February (winter, UTC+1).
			var startTime = new DateTimeOffset(2024, 2, 15, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, CentralEuropeanTime).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			// February – winter (UTC+1).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[0].Offset);
			// March – still winter, DST is March 31 (UTC+1).
			Assert.AreEqual(TimeSpan.FromHours(1), occurrences[1].Offset);
			// April – summer (UTC+2).
			Assert.AreEqual(TimeSpan.FromHours(2), occurrences[2].Offset);
		}

		#endregion

		#region CalculateOccurrencesByEndDate – Yearly

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Yearly_UtcTimezone_ReturnsCorrectYears()
		{
			var endDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Yearly,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2021, 6, 15, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, TimeZoneInfo.Utc).ToArray();

			Assert.AreEqual(3, occurrences.Length);
			Assert.AreEqual(2021, occurrences[0].Year);
			Assert.AreEqual(2022, occurrences[1].Year);
			Assert.AreEqual(2023, occurrences[2].Year);
		}

		[TestMethod]
		public void CalculateOccurrencesByEndDate_Yearly_EasternTimezone_SummerAndWinter_DifferentOffsets()
		{
			var endDate = new DateTimeOffset(2023, 12, 31, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Yearly,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			// January is winter (UTC-5) for Eastern time.
			var startTime = new DateTimeOffset(2022, 1, 10, 10, 0, 0, TimeSpan.Zero);
			var occurrences = pattern.CalculateOccurrencesByEndDate(startTime, EasternTime).ToArray();

			Assert.AreEqual(2, occurrences.Length);
			Assert.AreEqual(TimeSpan.FromHours(-5), occurrences[0].Offset);
			Assert.AreEqual(TimeSpan.FromHours(-5), occurrences[1].Offset);
		}

		#endregion

		#region CalculateLastJobDate

		[TestMethod]
		public void CalculateLastJobDate_Daily_ReturnsLastOccurrence()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var lastDate = pattern.CalculateLastJobDate(5, startTime, TimeZoneInfo.Utc);

			Assert.AreEqual(new DateTimeOffset(2024, 1, 5, 10, 0, 0, TimeSpan.Zero), lastDate);
		}

		[TestMethod]
		public void CalculateLastJobDate_Weekly_ReturnsLastOccurrence()
		{
			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Weekly,
				RepeatEvery = 1,
				WeekDays = WeekDays.Monday,
			};

			// 2024-01-01 is a Monday.
			var startTime = new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.Zero);
			var lastDate = pattern.CalculateLastJobDate(3, startTime, TimeZoneInfo.Utc);

			Assert.AreEqual(new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero), lastDate);
		}

		#endregion

		#region CalculateOccurrences

		[TestMethod]
		public void CalculateOccurrences_Daily_ReturnsCountOfOccurrences()
		{
			var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var count = pattern.CalculateOccurrences(startTime);

			Assert.AreEqual(10, count);
		}

		[TestMethod]
		public void CalculateOccurrences_EndDateBeforeStartDate_ReturnsZero()
		{
			var endDate = new DateTimeOffset(2023, 12, 31, 0, 0, 0, TimeSpan.Zero);

			var pattern = new RecurringPattern
			{
				RepeatType = RepeatType.Daily,
				RepeatEvery = 1,
				EndDate = endDate,
			};

			var startTime = new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);
			var count = pattern.CalculateOccurrences(startTime);

			Assert.AreEqual(0, count);
		}

		#endregion
	}
}
