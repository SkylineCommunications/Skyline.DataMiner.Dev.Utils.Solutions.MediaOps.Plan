namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;

	/// <summary>
	/// An in-memory scheduler task, reconstructed from the <see cref="SetSchedulerInfoMessage"/> a real DataMiner
	/// Agent receives when a task is created or updated through the DataMiner System class library.
	/// </summary>
	public sealed class SimulatedSchedulerTask
	{
		internal SimulatedSchedulerTask(int dmaId, int taskId, SetSchedulerInfoMessage message)
		{
			HandlingAgentId = dmaId;
			Id = taskId;
			Actions = new List<SchedulerAction>();

			var taskData = message.Ppsa.Ppsa;
			var generalInfo = taskData[0].Psa[0].Sa.ToList();

			// The task ID is only present in the general info when an existing task is updated.
			if (Int32.TryParse(generalInfo[0], out _))
			{
				generalInfo = generalInfo.Skip(1).ToList();
			}

			TaskName = generalInfo[0];
			StartTime = DateTime.SpecifyKind(DateTime.ParseExact(generalInfo[1], "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Local)
				+ TimeSpan.ParseExact(generalInfo[3], @"hh\:mm\:ss", CultureInfo.InvariantCulture);
			EndTime = DateTime.SpecifyKind(DateTime.ParseExact(generalInfo[2], "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Local);
			RepeatType = ParseRepeatType(generalInfo[4]);
			RepeatInterval = generalInfo[5];
			Repetitions = String.IsNullOrEmpty(generalInfo[6]) ? 0 : Convert.ToInt32(generalInfo[6], CultureInfo.InvariantCulture);
			Description = generalInfo[7];
			IsEnabled = String.Equals(generalInfo[8], "TRUE", StringComparison.OrdinalIgnoreCase);

			if (taskData.Length > 1)
			{
				foreach (var actionData in taskData[1].Psa)
				{
					var action = ParseAction(actionData.Sa.ToList());

					if (action != null)
					{
						Actions.Add(action);
					}
				}
			}
		}

		/// <summary>
		/// Gets the ID of the task.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Gets the ID of the DataMiner Agent handling the task.
		/// </summary>
		public int HandlingAgentId { get; }

		/// <summary>
		/// Gets the name of the task.
		/// </summary>
		public string TaskName { get; }

		/// <summary>
		/// Gets the description of the task.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets the local time at which the task is scheduled to run.
		/// </summary>
		public DateTime StartTime { get; }

		/// <summary>
		/// Gets the local time at which the task stops repeating.
		/// </summary>
		public DateTime EndTime { get; }

		/// <summary>
		/// Gets a value indicating whether the task is enabled.
		/// </summary>
		public bool IsEnabled { get; }

		/// <summary>
		/// Gets the number of repetitions of the task.
		/// </summary>
		public int Repetitions { get; }

		/// <summary>
		/// Gets the repeat interval of the task.
		/// </summary>
		public string RepeatInterval { get; }

		/// <summary>
		/// Gets the repeat type of the task.
		/// </summary>
		public SchedulerRepeatType RepeatType { get; }

		/// <summary>
		/// Gets the actions the task executes.
		/// </summary>
		public IList<SchedulerAction> Actions { get; }

		/// <summary>
		/// Gets the value of an Automation script parameter of the first Automation action of this task.
		/// </summary>
		/// <param name="parameterId">The ID of the script parameter.</param>
		/// <returns>The configured parameter value, or <see langword="null"/> when the task does not set it.</returns>
		public string GetAutomationParameterValue(int parameterId)
		{
			var scriptInstance = Actions
				.FirstOrDefault(action => action.ActionType == SchedulerActionType.Automation)?.ScriptInstance;

			return scriptInstance?.ParameterIdToValue
				.OfType<AutomationScriptInstanceInfo>()
				.FirstOrDefault(x => x.Key == parameterId)?.Value;
		}

		internal SchedulerTask ToSchedulerTask()
		{
			return new SchedulerTask
			{
				Id = Id,
				HandlingDMA = HandlingAgentId,
				TaskName = TaskName,
				Description = Description,
				StartTime = StartTime,
				EndTime = EndTime,
				Enabled = IsEnabled,
				Repeat = Repetitions,
				RepeatInterval = RepeatInterval,
				RepeatType = RepeatType,
				Actions = Actions.ToArray(),
				FinalActions = Array.Empty<SchedulerAction>(),
			};
		}

		private static SchedulerRepeatType ParseRepeatType(string repeatType)
		{
			switch (repeatType)
			{
				case "once": return SchedulerRepeatType.Once;
				case "daily": return SchedulerRepeatType.Daily;
				case "weekly": return SchedulerRepeatType.Weekly;
				case "monthly": return SchedulerRepeatType.Monthly;
				default: return SchedulerRepeatType.Undefined;
			}
		}

		private static SchedulerAction ParseAction(List<string> actionData)
		{
			if (!String.Equals(actionData[0], "automation", StringComparison.OrdinalIgnoreCase))
			{
				// Only Automation actions are used by the solutions under test.
				return null;
			}

			var scriptInstance = new AutomationScriptInstance
			{
				ScriptName = actionData[1],
			};

			foreach (var option in actionData.Skip(2))
			{
				ParseAutomationScriptOption(scriptInstance, option);
			}

			return new SchedulerAction
			{
				ActionType = SchedulerActionType.Automation,
				ScriptInstance = scriptInstance,
			};
		}

		private static void ParseAutomationScriptOption(AutomationScriptInstance scriptInstance, string option)
		{
			// The value of a parameter option can itself contain the separator, so only the first two parts are split off.
			var parts = option.Split(new[] { ':' }, 3);

			switch (parts[0].ToUpperInvariant())
			{
				case "CHECKSETS":
					scriptInstance.CheckSets = String.Equals(parts[1], "TRUE", StringComparison.OrdinalIgnoreCase);
					return;

				case "DEFER":
					scriptInstance.Synchronous = !String.Equals(parts[1], "TRUE", StringComparison.OrdinalIgnoreCase);
					return;

				case "PARAMETER":
					scriptInstance.ParameterIdToValue.Add(new AutomationScriptInstanceInfo
					{
						IsValue = true,
						Key = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture),
						Value = parts[2],
					});
					return;

				default:
					return;
			}
		}
	}
}
