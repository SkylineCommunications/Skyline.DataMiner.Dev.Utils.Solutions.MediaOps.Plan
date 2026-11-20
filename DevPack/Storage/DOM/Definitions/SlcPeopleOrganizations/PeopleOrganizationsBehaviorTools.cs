namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.Definitions.SlcPeopleOrganizations
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Actions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Buttons;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Conditions;

	internal static class PeopleOrganizationsBehaviorTools
	{
		internal static void AddActionAndButton(DomBehaviorDefinition definition, string script, string actionId, string actionValue, string buttonText, params string[] statuses)
		{
			definition.ActionDefinitions.Add(new ExecuteScriptDomActionDefinition(actionId)
			{
				Condition = null,
				Script = script,
				Async = false,
				ScriptOptions = new List<string> { $"PARAMETERBYNAME:Action:{actionValue}" },
				IsInteractive = false,
			});
			definition.ButtonDefinitions.Add(new DomInstanceButtonDefinition(actionId)
			{
				ActionDefinitionIds = new List<string> { actionId },
				Layout = new() { Text = buttonText, Icon = String.Empty, ToolTip = String.Empty, Order = 0 },
				VisibilityCondition = new StatusCondition(new List<string>(statuses)),
			});
		}
	}
}
