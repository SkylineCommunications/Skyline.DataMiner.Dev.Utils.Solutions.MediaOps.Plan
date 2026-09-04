namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	using Newtonsoft.Json;

	/// <summary>
	/// Serializes detached <see cref="OrchestrationSettings"/> values for transport between MediaOps Plan clients.
	/// </summary>
	public static class OrchestrationSettingsJsonSerializer
	{
		private const int CurrentVersion = 1;

		/// <summary>
		/// Serializes the supplied orchestration settings.
		/// </summary>
		/// <param name="settings">The settings to serialize.</param>
		/// <returns>A versioned JSON representation of the settings.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="settings"/> is <see langword="null"/>.</exception>
		public static string Serialize(OrchestrationSettings settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			return JsonConvert.SerializeObject(new OrchestrationSettingsDto
			{
				Version = CurrentVersion,
				Capabilities = settings.Capabilities.Select(ToDto).ToList(),
				Capacities = settings.Capacities.Select(ToDto).ToList(),
				Configurations = settings.Configurations.Select(ToDto).ToList(),
				Events = settings.OrchestrationEvents.Select(ToDto).ToList(),
			});
		}

		/// <summary>
		/// Deserializes settings created by <see cref="Serialize(OrchestrationSettings)"/>.
		/// </summary>
		/// <param name="json">The serialized settings, or <see langword="null"/> for no settings.</param>
		/// <returns>A detached, editable settings instance, or <see langword="null"/> when <paramref name="json"/> is empty.</returns>
		/// <exception cref="ArgumentException">When the JSON is invalid or uses an unsupported version.</exception>
		public static OrchestrationSettings Deserialize(string json)
		{
			if (String.IsNullOrWhiteSpace(json))
			{
				return null;
			}

			OrchestrationSettingsDto dto;
			try
			{
				dto = SecureNewtonsoftDeserialization.DeserializeObject<OrchestrationSettingsDto>(json);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("The orchestration settings JSON is invalid.", nameof(json), ex);
			}

			if (dto == null || dto.Version != CurrentVersion)
			{
				throw new ArgumentException($"The orchestration settings JSON must use version {CurrentVersion}.", nameof(json));
			}

			var settings = new WorkflowOrchestrationSettings();
			foreach (var capability in dto.Capabilities ?? Enumerable.Empty<SettingDto>())
			{
				settings.AddCapability(ToCapability(capability));
			}

			foreach (var capacity in dto.Capacities ?? Enumerable.Empty<SettingDto>())
			{
				settings.AddCapacity(ToCapacity(capacity));
			}

			foreach (var configuration in dto.Configurations ?? Enumerable.Empty<SettingDto>())
			{
				settings.AddConfiguration(ToConfiguration(configuration));
			}

			foreach (var orchestrationEvent in dto.Events ?? Enumerable.Empty<OrchestrationEventDto>())
			{
				settings.AddOrchestrationEvent(ToEvent(orchestrationEvent));
			}

			return settings;
		}

		private static SettingDto ToDto(CapabilitySetting setting) => new SettingDto { Id = setting.Id, Value = setting.Value, Reference = ToDto(setting.Reference) };

		private static SettingDto ToDto(CapacitySetting setting)
		{
			return setting switch
			{
				NumberCapacitySetting number => new SettingDto { Kind = "number", Id = number.Id, DecimalValue = number.Value, Reference = ToDto(number.Reference) },
				RangeCapacitySetting range => new SettingDto { Kind = "range", Id = range.Id, MinValue = range.MinValue, MaxValue = range.MaxValue, Reference = ToDto(range.Reference) },
				_ => throw new ArgumentException($"Unsupported capacity setting type '{setting.GetType().Name}'.", nameof(setting)),
			};
		}

		private static SettingDto ToDto(ConfigurationSetting setting)
		{
			return setting switch
			{
				TextConfigurationSetting text => new SettingDto { Kind = "text", Id = text.Id, Value = text.Value, Reference = ToDto(text.Reference) },
				NumberConfigurationSetting number => new SettingDto { Kind = "number", Id = number.Id, DecimalValue = number.Value, Reference = ToDto(number.Reference) },
				DiscreteTextConfigurationSetting discreteText => new SettingDto { Kind = "discrete-text", Id = discreteText.Id, Value = discreteText.Value?.Value, DisplayName = discreteText.Value?.DisplayName, Reference = ToDto(discreteText.Reference) },
				DiscreteNumberConfigurationSetting discreteNumber => new SettingDto { Kind = "discrete-number", Id = discreteNumber.Id, DecimalValue = discreteNumber.Value?.Value, DisplayName = discreteNumber.Value?.DisplayName, Reference = ToDto(discreteNumber.Reference) },
				_ => throw new ArgumentException($"Unsupported configuration setting type '{setting.GetType().Name}'.", nameof(setting)),
			};
		}

		private static CapabilitySetting ToCapability(SettingDto dto) => new CapabilitySetting(RequireId(dto)) { Value = dto.Value, Reference = ToReference(dto.Reference) };

		private static CapacitySetting ToCapacity(SettingDto dto)
		{
			return dto?.Kind switch
			{
				"number" => new NumberCapacitySetting(RequireId(dto)) { Value = dto.DecimalValue, Reference = ToReference(dto.Reference) },
				"range" => new RangeCapacitySetting(RequireId(dto)) { MinValue = dto.MinValue, MaxValue = dto.MaxValue, Reference = ToReference(dto.Reference) },
				_ => throw new ArgumentException("The capacity setting type is invalid."),
			};
		}

		private static ConfigurationSetting ToConfiguration(SettingDto dto)
		{
			return dto?.Kind switch
			{
				"text" => new TextConfigurationSetting(RequireId(dto)) { Value = dto.Value, Reference = ToReference(dto.Reference) },
				"number" => new NumberConfigurationSetting(RequireId(dto)) { Value = dto.DecimalValue, Reference = ToReference(dto.Reference) },
				"discrete-text" => new DiscreteTextConfigurationSetting(RequireId(dto)) { Value = dto.Value == null ? null : new TextDiscreet(dto.Value, dto.DisplayName), Reference = ToReference(dto.Reference) },
				"discrete-number" => new DiscreteNumberConfigurationSetting(RequireId(dto)) { Value = dto.DecimalValue.HasValue ? new NumberDiscreet(dto.DecimalValue.Value, dto.DisplayName) : null, Reference = ToReference(dto.Reference) },
				_ => throw new ArgumentException("The configuration setting type is invalid."),
			};
		}

		private static OrchestrationEventDto ToDto(OrchestrationEvent orchestrationEvent) => new OrchestrationEventDto { EventType = orchestrationEvent.EventType, Metadata = orchestrationEvent.Metadata, ExecutionDetails = ToDto(orchestrationEvent.ExecutionDetails) };

		private static OrchestrationEvent ToEvent(OrchestrationEventDto dto)
		{
			if (dto == null)
			{
				throw new ArgumentException("An orchestration event cannot be null.");
			}

			return new OrchestrationEvent { EventType = dto.EventType, Metadata = dto.Metadata, ExecutionDetails = ToExecutionDetails(dto.ExecutionDetails) };
		}

		private static ScriptExecutionDetailsDto ToDto(ScriptExecutionDetails details)
		{
			if (details == null)
			{
				return null;
			}

			return new ScriptExecutionDetailsDto
			{
				ScriptName = details.ScriptName,
				Elements = details.ScriptElements.Select(x => new ScriptElementDto { Name = x.Name, ElementId = x.DmsElementId == default ? null : x.DmsElementId.Value, ElementName = x.ElementName, Reference = ToDto(x.Reference) }).ToList(),
				Parameters = details.ScriptParameters.Select(x => new ScriptParameterDto { Name = x.Name, Value = x.Value, Reference = ToDto(x.Reference) }).ToList(),
				Capabilities = details.Capabilities.Select(ToDto).ToList(),
				Capacities = details.Capacities.Select(ToDto).ToList(),
				Configurations = details.Configurations.Select(ToDto).ToList(),
			};
		}

		private static ScriptExecutionDetails ToExecutionDetails(ScriptExecutionDetailsDto dto)
		{
			if (dto == null)
			{
				return null;
			}

			if (String.IsNullOrWhiteSpace(dto.ScriptName))
			{
				throw new ArgumentException("Script execution details require a script name.");
			}

			var details = new ScriptExecutionDetails(dto.ScriptName);
			foreach (var element in dto.Elements ?? Enumerable.Empty<ScriptElementDto>())
			{
				var setting = new ScriptElementSetting(element.Name) { ElementName = element.ElementName, Reference = ToReference(element.Reference) };
				if (!String.IsNullOrWhiteSpace(element.ElementId) && DmsElementId.TryParse(element.ElementId, out var elementId))
				{
					setting.DmsElementId = elementId;
				}

				details.AddScriptElement(setting);
			}

			foreach (var parameter in dto.Parameters ?? Enumerable.Empty<ScriptParameterDto>())
			{
				details.AddScriptParameter(new ScriptParameterSetting(parameter.Name) { Value = parameter.Value, Reference = ToReference(parameter.Reference) });
			}

			details.SetCapabilities((dto.Capabilities ?? Enumerable.Empty<SettingDto>()).Select(ToCapability));
			details.SetCapacities((dto.Capacities ?? Enumerable.Empty<SettingDto>()).Select(ToCapacity));
			details.SetConfigurations((dto.Configurations ?? Enumerable.Empty<SettingDto>()).Select(ToConfiguration));
			return details;
		}

		private static DataReferenceDto ToDto(DataReference reference)
		{
			if (reference == null)
			{
				return null;
			}

			return new DataReferenceDto
			{
				Type = reference.Type,
				NodeId = reference.NodeId,
				Id = reference switch
				{
					ParameterReference parameter => parameter.ParameterId,
					ResourcePropertyReference property => property.ResourcePropertyId,
					JobPropertyReference property => property.JobPropertyId,
					_ => null,
				},
			};
		}

		private static DataReference ToReference(DataReferenceDto dto)
		{
			if (dto == null)
			{
				return null;
			}

			return dto.Type switch
			{
				DataReferenceType.ResourceName => new ResourceNameReference(dto.NodeId),
				DataReferenceType.ResourceLinkedObjectID => new ResourceLinkedObjectIdReference(dto.NodeId),
				DataReferenceType.JobName => new JobNameReference(dto.NodeId),
				DataReferenceType.ResourceProperty => new ResourcePropertyReference(RequireId(dto), dto.NodeId),
				DataReferenceType.JobProperty => new JobPropertyReference(RequireId(dto), dto.NodeId),
				DataReferenceType.CapabilityParameter => new CapabilityParameterReference(RequireId(dto), dto.NodeId),
				DataReferenceType.CapacityParameter => new CapacityParameterReference(RequireId(dto), dto.NodeId),
				DataReferenceType.ConfigurationParameter => new ConfigurationParameterReference(RequireId(dto), dto.NodeId),
				_ => throw new ArgumentException("The data reference type is invalid."),
			};
		}

		private static Guid RequireId(SettingDto dto) => dto == null || dto.Id == Guid.Empty ? throw new ArgumentException("A setting identifier is required.") : dto.Id;

		private static Guid RequireId(DataReferenceDto dto) => dto == null || !dto.Id.HasValue || dto.Id.Value == Guid.Empty ? throw new ArgumentException("A data reference identifier is required.") : dto.Id.Value;

		private sealed class OrchestrationSettingsDto
		{
			public int Version { get; set; }

			public List<SettingDto> Capabilities { get; set; }

			public List<SettingDto> Capacities { get; set; }

			public List<SettingDto> Configurations { get; set; }

			public List<OrchestrationEventDto> Events { get; set; }
		}

		private sealed class SettingDto
		{
			public string Kind { get; set; }

			public Guid Id { get; set; }

			public string Value { get; set; }

			public decimal? DecimalValue { get; set; }

			public decimal? MinValue { get; set; }

			public decimal? MaxValue { get; set; }

			public string DisplayName { get; set; }

			public DataReferenceDto Reference { get; set; }
		}

		private sealed class OrchestrationEventDto
		{
			public OrchestrationEventType EventType { get; set; }

			public string Metadata { get; set; }

			public ScriptExecutionDetailsDto ExecutionDetails { get; set; }
		}

		private sealed class ScriptExecutionDetailsDto
		{
			public string ScriptName { get; set; }

			public List<ScriptElementDto> Elements { get; set; }

			public List<ScriptParameterDto> Parameters { get; set; }

			public List<SettingDto> Capabilities { get; set; }

			public List<SettingDto> Capacities { get; set; }

			public List<SettingDto> Configurations { get; set; }
		}

		private sealed class ScriptElementDto
		{
			public string Name { get; set; }

			public string ElementId { get; set; }

			public string ElementName { get; set; }

			public DataReferenceDto Reference { get; set; }
		}

		private sealed class ScriptParameterDto
		{
			public string Name { get; set; }

			public string Value { get; set; }

			public DataReferenceDto Reference { get; set; }
		}

		private sealed class DataReferenceDto
		{
			public DataReferenceType Type { get; set; }

			public Guid? Id { get; set; }

			public string NodeId { get; set; }
		}
	}
}