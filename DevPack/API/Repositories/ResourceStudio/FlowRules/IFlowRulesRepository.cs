namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides read access to the flow rules of resource pools and evaluates connections against them.
	/// </summary>
	/// <remarks>Flow rules are edited through <see cref="ResourcePool.FlowRules"/> and persisted together with the resource pool, for example through <see cref="IResourcePoolsRepository"/>.</remarks>
	public interface IFlowRulesRepository
	{
		/// <summary>
		/// Reads the flow rules of a resource pool.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
		/// <returns>The flow rules of the resource pool, or <see langword="null"/> when the resource pool does not exist or has no stored flow rules.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
		FlowRules Read(Guid resourcePoolId);

		/// <summary>
		/// Reads the flow rules of multiple resource pools.
		/// </summary>
		/// <param name="resourcePoolIds">The unique identifiers of the resource pools.</param>
		/// <returns>The flow rules keyed by resource pool ID. Resource pools that do not exist or have no stored flow rules are not included.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePoolIds"/> is <see langword="null"/>.</exception>
		IReadOnlyDictionary<Guid, FlowRules> Read(IEnumerable<Guid> resourcePoolIds);

		/// <summary>
		/// Reads all stored flow rules.
		/// </summary>
		/// <returns>The stored flow rules of all resource pools.</returns>
		IEnumerable<FlowRules> Read();

		/// <summary>
		/// Evaluates a connection from one resource pool to another.
		/// </summary>
		/// <param name="fromResourcePoolId">The unique identifier of the resource pool the connection starts from.</param>
		/// <param name="toResourcePoolId">The unique identifier of the resource pool the connection goes to.</param>
		/// <returns>The evaluation of the connection.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="fromResourcePoolId"/> or <paramref name="toResourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
		FlowRuleEvaluation Evaluate(Guid fromResourcePoolId, Guid toResourcePoolId);

		/// <summary>
		/// Evaluates multiple connections between resource pools, reading the required flow rules once.
		/// </summary>
		/// <param name="connections">The connections to evaluate, each from one resource pool to another.</param>
		/// <returns>The evaluations, in the same order as <paramref name="connections"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connections"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when a connection references <see cref="Guid.Empty"/>.</exception>
		IReadOnlyCollection<FlowRuleEvaluation> Evaluate(IEnumerable<(Guid FromResourcePoolId, Guid ToResourcePoolId)> connections);

		/// <summary>
		/// Evaluates which resource pools may be connected to a resource pool in a direction, for example to filter the options when adding a node before or after an existing node.
		/// </summary>
		/// <param name="resourcePoolId">The unique identifier of the resource pool of the existing node.</param>
		/// <param name="direction">
		/// <see cref="FlowRuleDirection.After"/> to evaluate the candidates as nodes after the existing node (existing → candidate);
		/// <see cref="FlowRuleDirection.Before"/> to evaluate them as nodes before it (candidate → existing).
		/// </param>
		/// <param name="candidateResourcePoolIds">The unique identifiers of the candidate resource pools.</param>
		/// <returns>The evaluation per candidate resource pool ID.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="candidateResourcePoolIds"/> is <see langword="null"/>.</exception>
		IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(Guid resourcePoolId, FlowRuleDirection direction, IEnumerable<Guid> candidateResourcePoolIds);

		/// <summary>
		/// Evaluates all connections of a node graph, for example of a job, workflow or recurring job that is being edited.
		/// </summary>
		/// <typeparam name="TNode">The type of the nodes in the graph.</typeparam>
		/// <param name="nodeGraph">The node graph to evaluate.</param>
		/// <returns>The evaluation per connection. Connections of which an endpoint does not reference a resource pool are not included.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodeGraph"/> is <see langword="null"/>.</exception>
		IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase;
	}
}
