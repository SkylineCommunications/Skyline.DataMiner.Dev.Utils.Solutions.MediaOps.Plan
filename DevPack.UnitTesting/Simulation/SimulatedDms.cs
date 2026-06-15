namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.AppPackages;
	using Skyline.DataMiner.Net.AppPackages.Messages;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.Advanced;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Connection;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Stores;
	using Skyline.DataMiner.Utils.DOM.UnitTesting;

	using SLDataGateway.API.Querying;

	/// <summary>
	/// Central in-memory dispatcher that replies to SLNet messages in the same way a real
	/// DataMiner Agent would. Used to provide an <see cref="IConnection"/> for unit and
	/// integration testing without a live DataMiner System.
	/// </summary>
	public sealed class SimulatedDms
	{
		private readonly ConcurrentBag<SimulatedConnection> _connections = new ConcurrentBag<SimulatedConnection>();
		private readonly ConcurrentBag<InstalledAppInfo> _appPackages = new ConcurrentBag<InstalledAppInfo>();
		private readonly ConcurrentBag<SimulatedElement> _elements = new ConcurrentBag<SimulatedElement>();
		private readonly ConcurrentDictionary<DomDefinitionId, DomDefinition> _domDefinitions = new ConcurrentDictionary<DomDefinitionId, DomDefinition>();
		private readonly ConcurrentDictionary<DomBehaviorDefinitionId, DomBehaviorDefinition> _domBehaviorDefinitions = new ConcurrentDictionary<DomBehaviorDefinitionId, DomBehaviorDefinition>();

		private readonly DomSLNetMessageHandler _domSlNetMessageHandler = new DomSLNetMessageHandler(validateAgainstDefinition: false);
		private readonly ProfileParameterStore _profileParameterStore = new ProfileParameterStore();
		private readonly ResourceManagerStore _resourceManagerStore = new ResourceManagerStore();
		private readonly ConcurrentDictionary<PagingCookie, InMemoryPagingHandler<DomInstance>> _domPagingHandlers = new ConcurrentDictionary<PagingCookie, InMemoryPagingHandler<DomInstance>>();
		private readonly ConcurrentBag<SimulatedProtocol> _protocols = new ConcurrentBag<SimulatedProtocol>();
		private readonly ConcurrentDictionary<Guid, FunctionDefinition> _functionDefinitions = new ConcurrentDictionary<Guid, FunctionDefinition>();
		private readonly ConcurrentDictionary<string, ProtocolFunction> _protocolFunctions = new ConcurrentDictionary<string, ProtocolFunction>(StringComparer.OrdinalIgnoreCase);

		private int _nextElementId;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimulatedDms"/> class.
		/// </summary>
		public SimulatedDms()
		{
			_domSlNetMessageHandler.OnInstancesChanged += (s, e) => NotifySubscriptions(e);
		}

		/// <summary>
		/// Gets the DOM message handler used to manage in-memory DOM objects.
		/// </summary>
		public DomSLNetMessageHandler DomHandler => _domSlNetMessageHandler;

		/// <summary>
		/// Registers an installed application package so that installation checks succeed.
		/// </summary>
		/// <param name="name">The name of the application package.</param>
		/// <param name="version">The version of the application package.</param>
		public void AddApplicationPackage(string name, string version)
		{
			var appInfo = new InstalledAppInfo
			{
				AppInfo = new AppInfo
				{
					Name = name,
					DisplayName = name,
					Version = version,
					LastModifiedAt = DateTime.UtcNow,
				},
				InstallState = new AppInstallState
				{
					InstallStatus = AppInstallStatus.INSTALLED,
				},
			};

			_appPackages.Add(appInfo);
		}

		/// <summary>
		/// Registers a simulated element so that element existence and information lookups succeed.
		/// </summary>
		/// <param name="name">The element name.</param>
		/// <param name="dmaId">The DataMiner Agent ID hosting the element.</param>
		/// <param name="elementId">The element ID.</param>
		/// <param name="protocolName">The protocol name.</param>
		/// <param name="protocolVersion">The protocol version.</param>
		/// <returns>The created simulated element.</returns>
		public SimulatedElement AddElement(string name, int dmaId = 1, int elementId = 1, string protocolName = "Simulated Protocol", string protocolVersion = "Production")
		{
			var element = new SimulatedElement(dmaId, elementId, name, protocolName, protocolVersion);
			_elements.Add(element);

			return element;
		}

		/// <summary>
		/// Registers a connector (protocol) version so that protocol lookups
		/// (<see cref="GetProtocolsResponseMessage"/>) and element creation against that protocol succeed,
		/// mirroring the installed state of a real DataMiner Agent.
		/// </summary>
		/// <param name="name">The protocol name.</param>
		/// <param name="version">The protocol version.</param>
		/// <param name="type">The protocol type.</param>
		/// <param name="connectionType">The connection type string the connector exposes as its main port.</param>
		public void RegisterProtocol(string name, string version, ProtocolType type = ProtocolType.Virtual, string connectionType = "Virtual")
		{
			if (String.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
			}

			if (String.IsNullOrWhiteSpace(version))
			{
				throw new ArgumentException($"'{nameof(version)}' cannot be null or whitespace.", nameof(version));
			}

			_protocols.Add(new SimulatedProtocol(name, version, type, connectionType));
		}

		/// <summary>
		/// Registers an active function definition for a connector so that protocol function lookups
		/// (<see cref="GetProtocolFunctionsResponseMessage"/>) and function definition lookups
		/// (<see cref="GetFunctionDefinitionsResponseMessage"/>) succeed, mirroring the installed state of
		/// a real DataMiner Agent.
		/// </summary>
		/// <param name="protocolName">The protocol name the function belongs to.</param>
		/// <param name="functionDefinition">The function definition to register.</param>
		public void RegisterProtocolFunction(string protocolName, FunctionDefinition functionDefinition)
		{
			if (String.IsNullOrWhiteSpace(protocolName))
			{
				throw new ArgumentException($"'{nameof(protocolName)}' cannot be null or whitespace.", nameof(protocolName));
			}

			if (functionDefinition is null)
			{
				throw new ArgumentNullException(nameof(functionDefinition));
			}

			_functionDefinitions[functionDefinition.GUID] = functionDefinition;

			var protocolFunction = _protocolFunctions.GetOrAdd(protocolName, name => new ProtocolFunction
			{
				ProtocolName = name,
				ProtocolFunctionVersions = new List<ProtocolFunctionVersion>
				{
					new ProtocolFunctionVersion
					{
						ProtocolName = name,
						Version = "1",
						Active = true,
						FunctionDefinitions = new List<FunctionDefinition>(),
					},
				},
			});

			// The derived FunctionDefinitions getter returns a freshly computed list, so mutating the
			// result of the getter is discarded. Read the current definitions, append, and assign back
			// so the setter persists them to the backing collection.
			var version = protocolFunction.ProtocolFunctionVersions[0];
			var functionDefinitions = version.FunctionDefinitions;
			functionDefinitions.Add(functionDefinition);
			version.FunctionDefinitions = functionDefinitions;
		}

		/// <summary>
		/// Registers the DOM definitions and behavior definitions of a module, mirroring the installed
		/// state a real DataMiner Agent has. This enables initial status assignment on creation and
		/// status transitions, just like a real DataMiner Agent.
		/// </summary>
		/// <param name="moduleId">The DOM module ID.</param>
		/// <param name="definitions">The DOM definitions of the module.</param>
		/// <param name="behaviorDefinitions">The DOM behavior definitions of the module.</param>
		public void RegisterDomModule(string moduleId, IEnumerable<DomDefinition> definitions, IEnumerable<DomBehaviorDefinition> behaviorDefinitions)
		{
			if (String.IsNullOrWhiteSpace(moduleId))
			{
				throw new ArgumentException($"'{nameof(moduleId)}' cannot be null or whitespace.", nameof(moduleId));
			}

			if (definitions is null)
			{
				throw new ArgumentNullException(nameof(definitions));
			}

			if (behaviorDefinitions is null)
			{
				throw new ArgumentNullException(nameof(behaviorDefinitions));
			}

			var definitionList = definitions.ToList();
			var behaviorList = behaviorDefinitions.ToList();

			foreach (var definition in definitionList)
			{
				_domDefinitions[definition.ID] = definition;
			}

			foreach (var behavior in behaviorList)
			{
				_domBehaviorDefinitions[behavior.ID] = behavior;
			}

			_domSlNetMessageHandler.SetDefinitions(moduleId, definitionList);
			_domSlNetMessageHandler.SetBehaviorDefinitions(moduleId, behaviorList);
		}

		/// <summary>
		/// Creates a DOM instance in the in-memory store, applying the initial status and computed
		/// name in the same way a real DataMiner Agent would. Used to seed sample data.
		/// </summary>
		/// <param name="instance">The DOM instance to create.</param>
		/// <returns>The created DOM instance, or <see langword="null"/> when the create failed.</returns>
		public DomInstance CreateDomInstance(DomInstance instance)
		{
			if (instance is null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			var request = new ManagerStoreCreateRequest<DomInstance>(instance)
			{
				ModuleId = instance.ID?.ModuleId,
			};

			if (TryHandleMessage(request, out var responses)
				&& responses.OfType<ManagerStoreCrudResponse<DomInstance>>().FirstOrDefault() is ManagerStoreCrudResponse<DomInstance> crudResponse)
			{
				return crudResponse.Objects.FirstOrDefault();
			}

			return null;
		}

		/// <summary>
		/// Creates a new in-memory <see cref="IConnection"/> backed by this <see cref="SimulatedDms"/>.
		/// </summary>
		/// <returns>The created connection.</returns>
		public SimulatedConnection CreateConnection()
		{
			var connection = new SimulatedConnection(this);
			_connections.Add(connection);

			return connection;
		}

		internal void NotifySubscriptions(EventMessage eventMessage)
		{
			if (eventMessage is null)
			{
				throw new ArgumentNullException(nameof(eventMessage));
			}

			foreach (var connection in _connections)
			{
				connection.NotifySubscriptions(eventMessage);
			}
		}

		internal bool TryHandleMessage(DMSMessage message, out IEnumerable<DMSMessage> responses)
		{
			if (message is null)
			{
				throw new ArgumentNullException(nameof(message));
			}

			ApplyInitialStatuses(message);

			if (TryHandleDomInstanceQuery(message, out var domQueryResponse))
			{
				responses = new[] { domQueryResponse };
				return true;
			}

			if (_domSlNetMessageHandler.TryHandleMessage(message, out var domResponse))
			{
				responses = new[] { domResponse };
				return true;
			}

			if (_profileParameterStore.TryHandleMessage(message, out var profileResponse))
			{
				responses = new[] { profileResponse };
				return true;
			}

			if (_resourceManagerStore.TryHandleMessage(message, out var resourceResponse))
			{
				responses = new[] { resourceResponse };
				return true;
			}

			switch (message)
			{
				case ImpersonateMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetLiteElementInfo msg:
					responses = HandleMessage(msg);
					return true;

				case GetElementByIDMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetElementByNameMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetInstalledAppPackagesRequest msg:
					responses = HandleMessage(msg);
					return true;

				case GetInfoMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetDataMinerByIDMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetAgentBuildInfo msg:
					responses = HandleMessage(msg);
					return true;

				case AddElementMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetProtocolMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetProtocolFunctionsMessage msg:
					responses = HandleMessage(msg);
					return true;

				case GetFunctionDefinitionsRequestMessage msg:
					responses = HandleMessage(msg);
					return true;

				case SetParameterMessage _:
					// Fire-and-forget parameter set (for example enabling a DVE row). No response is
					// expected by the caller, so acknowledge it without producing one.
					responses = Array.Empty<DMSMessage>();
					return true;

				default:
					responses = Array.Empty<DMSMessage>();
					return false;
			}
		}

		private void ApplyInitialStatuses(DMSMessage message)
		{
			switch (message)
			{
				case ManagerStoreCreateRequest<DomInstance> request:
					ApplyInitialStatus(request.Object);
					break;

				case ManagerStoreBulkCreateOrUpdateRequest<DomInstance> request:
					foreach (var instance in request.Objects)
					{
						ApplyInitialStatus(instance);
					}

					break;
			}
		}

		private void ApplyInitialStatus(DomInstance instance)
		{
			if (instance is null || !String.IsNullOrEmpty(instance.StatusId) || instance.DomDefinitionId is null)
			{
				return;
			}

			if (!_domDefinitions.TryGetValue(instance.DomDefinitionId, out var definition) || definition.DomBehaviorDefinitionId is null)
			{
				return;
			}

			if (_domBehaviorDefinitions.TryGetValue(definition.DomBehaviorDefinitionId, out var behavior))
			{
				instance.StatusId = behavior.InitialStatusId;
			}
		}

		/// <summary>
		/// Handles DOM instance read, count and paging requests using a coercion-aware filter
		/// evaluation, mirroring how a real DataMiner Agent coerces value types (for example
		/// <see cref="int"/> versus <see cref="long"/>) before comparing them. All other DOM
		/// messages are delegated to the underlying DOM message handler.
		/// </summary>
		private bool TryHandleDomInstanceQuery(DMSMessage message, out DMSMessage response)
		{
			switch (message)
			{
				case ManagerStoreReadRequest<DomInstance> request:
					{
						var instances = ReadAllDomInstances(request.ModuleId);
						var filtered = CoercingFilterEvaluator.Apply(request.Query.Filter, instances);
						var ordered = request.Query.WithFilter(new TRUEFilterElement<DomInstance>()).ExecuteInMemory(filtered).ToList();

						response = new ManagerStoreCrudResponse<DomInstance>(ordered);
						return true;
					}

				case ManagerStoreCountRequest<DomInstance> request:
					{
						var instances = ReadAllDomInstances(request.ModuleId);
						var count = CoercingFilterEvaluator.Apply(request.Query.Filter, instances).LongCount();

						response = new ManagerStoreCountResponse<DomInstance>(count);
						return true;
					}

				case ManagerStoreStartPagingRequest<DomInstance> request:
					{
						var instances = ReadAllDomInstances(request.ModuleId);
						var filtered = CoercingFilterEvaluator.Apply(request.Filter.Filter, instances);
						var ordered = request.Filter.WithFilter(new TRUEFilterElement<DomInstance>()).ExecuteInMemory(filtered).ToList();

						var pagingHandler = new InMemoryPagingHandler<DomInstance>(ordered);
						_domPagingHandlers.TryAdd(pagingHandler.Cookie, pagingHandler);

						var nextPage = pagingHandler.GetNextPage(request.PreferredPageSize, out var isLast);

						if (isLast)
						{
							_domPagingHandlers.TryRemove(pagingHandler.Cookie, out pagingHandler);
							pagingHandler.Dispose();
						}

						response = new ManagerStorePagingResponse<DomInstance>(nextPage, isLast, pagingHandler.Cookie);
						return true;
					}

				case ManagerStoreNextPagingRequest<DomInstance> request when _domPagingHandlers.ContainsKey(request.PagingCookie):
					{
						_domPagingHandlers.TryGetValue(request.PagingCookie, out var pagingHandler);

						var nextPage = pagingHandler.GetNextPage(request.PreferredPageSize, out var isLast);

						if (isLast)
						{
							_domPagingHandlers.TryRemove(pagingHandler.Cookie, out pagingHandler);
							pagingHandler.Dispose();
						}

						response = new ManagerStorePagingResponse<DomInstance>(nextPage, isLast, request.PagingCookie);
						return true;
					}

				default:
					response = default;
					return false;
			}
		}

		private List<DomInstance> ReadAllDomInstances(string moduleId)
		{
			var request = new ManagerStoreReadRequest<DomInstance>(new TRUEFilterElement<DomInstance>().ToQuery())
			{
				ModuleId = moduleId,
			};

			if (_domSlNetMessageHandler.TryHandleMessage(request, out var domResponse) && domResponse is ManagerStoreCrudResponse<DomInstance> crudResponse)
			{
				return crudResponse.Objects;
			}

			return new List<DomInstance>();
		}

		private IEnumerable<DMSMessage> HandleMessage(ImpersonateMessage msg)
		{
			var responses = new List<DMSMessage>();

			foreach (var clientRequestMessage in msg.Messages)
			{
				if (TryHandleMessage(clientRequestMessage, out var msgResponses))
				{
					responses.AddRange(msgResponses);
				}
			}

			return responses;
		}

		private IEnumerable<DMSMessage> HandleMessage(GetInstalledAppPackagesRequest msg)
		{
			yield return new GetInstalledAppPackagesResponse
			{
				InstalledAppPackages = _appPackages.ToList(),
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetLiteElementInfo msg)
		{
			IEnumerable<SimulatedElement> elements = _elements;

			if (!String.IsNullOrEmpty(msg.ProtocolName))
			{
				elements = elements.Where(x => String.Equals(x.ProtocolName, msg.ProtocolName));
			}

			foreach (var element in elements)
			{
				yield return element.ToLiteElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetElementByIDMessage msg)
		{
			var element = _elements.FirstOrDefault(x => x.DmaId == msg.DataMinerID && x.ElementId == msg.ElementID);

			if (element != null)
			{
				yield return element.ToElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetElementByNameMessage msg)
		{
			var element = _elements.FirstOrDefault(x => String.Equals(x.Name, msg.ElementName));

			if (element != null)
			{
				yield return element.ToElementInfo();
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetInfoMessage msg)
		{
			switch (msg.Type)
			{
				case InfoType.DataMinerInfo:
					yield return new GetDataMinerInfoResponseMessage
					{
						ID = 1,
					};
					break;

				case InfoType.Protocols:
					foreach (var protocolGroup in _protocols.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
					{
						var first = protocolGroup.First();

						yield return new GetProtocolsResponseMessage
						{
							Protocol = first.Name,
							Type = first.Type,
							Versions = protocolGroup.Select(x => x.Version).ToArray(),
							VersionDetails = protocolGroup.Select(x => new ProtocolVersionDetails
							{
								Version = x.Version,
								ReferencedProtocol = x.Version,
							}).ToArray(),
						};
					}

					break;

				default:
					throw new NotSupportedException($"Unsupported InfoType: {msg.Type}");
			}
		}

		private IEnumerable<DMSMessage> HandleMessage(GetDataMinerByIDMessage msg)
		{
			yield return new GetDataMinerInfoResponseMessage
			{
				ID = msg.ID,
				ComputerName = $"SimulatedHost{msg.ID}",
				Name = $"Simulated Agent {msg.ID}",
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetAgentBuildInfo msg)
		{
			yield return new BuildInfoResponse
			{
				Agents = new[]
				{
					new BuildInfoAgent
					{
						RawVersion = "10.5.6",
						DataMinerID = msg.DataMinerID,
					},
				},
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(AddElementMessage msg)
		{
			var dmaId = msg.DataMinerID > 0 ? msg.DataMinerID : 1;
			var elementId = Interlocked.Increment(ref _nextElementId);

			_elements.Add(new SimulatedElement(dmaId, elementId, msg.ElementName, msg.ProtocolName, msg.ProtocolVersion));

			yield return new AddElementResponseMessage(elementId);
		}

		private IEnumerable<DMSMessage> HandleMessage(GetProtocolMessage msg)
		{
			var protocol = _protocols.FirstOrDefault(x =>
				String.Equals(x.Name, msg.Protocol, StringComparison.OrdinalIgnoreCase) &&
				String.Equals(x.Version, msg.Version, StringComparison.OrdinalIgnoreCase));

			if (protocol == null)
			{
				yield break;
			}

			yield return new GetProtocolInfoResponseMessage
			{
				Name = protocol.Name,
				Version = protocol.Version,
				Type = protocol.ConnectionType,
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetProtocolFunctionsMessage msg)
		{
			var functions = _protocolFunctions.Values.AsEnumerable();

			if (!String.IsNullOrEmpty(msg.ProtocolNameFilter))
			{
				functions = functions.Where(x => String.Equals(x.ProtocolName, msg.ProtocolNameFilter, StringComparison.OrdinalIgnoreCase));
			}

			yield return new GetProtocolFunctionsResponseMessage
			{
				Functions = functions.ToList(),
				Success = true,
			};
		}

		private IEnumerable<DMSMessage> HandleMessage(GetFunctionDefinitionsRequestMessage msg)
		{
			var functionDefinitions = new List<SystemFunctionDefinition>();

			foreach (var id in msg.FunctionDefinitionIds ?? new List<FunctionDefinitionID>())
			{
				if (_functionDefinitions.TryGetValue(id.Id, out var functionDefinition))
				{
					functionDefinitions.Add(functionDefinition);
				}
			}

			yield return new GetFunctionDefinitionsResponseMessage(functionDefinitions)
			{
				Success = true,
			};
		}
	}
}
