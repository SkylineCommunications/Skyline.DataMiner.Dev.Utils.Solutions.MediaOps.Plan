namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Provides read access to flow rules and evaluates connections against them.
	/// </summary>
	/// <remarks>Flow rules are edited and persisted together with the object that owns them.</remarks>
	public interface IFlowRulesRepository
	{
		/// <summary>
		/// Reads the flow rules of an object.
		/// </summary>
		/// <param name="ownerType">The type of the object that owns the flow rules.</param>
		/// <param name="ownerId">The unique identifier of the object that owns the flow rules.</param>
		/// <returns>The flow rules of the object, or <see langword="null"/> when the object does not exist or has no stored flow rules.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="ownerId"/> is <see cref="Guid.Empty"/> or <paramref name="ownerType"/> is not a defined value.</exception>
		FlowRules Read(FlowRuleObjectType ownerType, Guid ownerId);

		/// <summary>
		/// Reads the flow rules of multiple objects of the same type.
		/// </summary>
		/// <param name="ownerType">The type of the objects that own the flow rules.</param>
		/// <param name="ownerIds">The unique identifiers of the objects that own the flow rules.</param>
		/// <returns>The flow rules keyed by owner ID. Objects that do not exist or have no stored flow rules are not included.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="ownerIds"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when <paramref name="ownerType"/> is not a defined value.</exception>
		IReadOnlyDictionary<Guid, FlowRules> Read(FlowRuleObjectType ownerType, IEnumerable<Guid> ownerIds);

		/// <summary>
		/// Reads all stored flow rules.
		/// </summary>
		/// <returns>All stored flow rules, of all owners.</returns>
		IEnumerable<FlowRules> Read();

		/// <summary>
		/// Evaluates a connection from one object to another.
		/// </summary>
		/// <param name="fromType">The type of the object the connection starts from.</param>
		/// <param name="fromId">The unique identifier of the object the connection starts from.</param>
		/// <param name="toType">The type of the object the connection goes to.</param>
		/// <param name="toId">The unique identifier of the object the connection goes to.</param>
		/// <returns>The evaluation of the connection.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="fromId"/> or <paramref name="toId"/> is <see cref="Guid.Empty"/>, or when a type is not a defined value.</exception>
		FlowRuleEvaluation Evaluate(FlowRuleObjectType fromType, Guid fromId, FlowRuleObjectType toType, Guid toId);

		/// <summary>
		/// Evaluates multiple connections between objects, reading the required flow rules once.
		/// </summary>
		/// <param name="connections">The connections to evaluate, each from one object to another.</param>
		/// <returns>The evaluations, in the same order as <paramref name="connections"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="connections"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when a connection references <see cref="Guid.Empty"/> or a type that is not a defined value.</exception>
		IReadOnlyCollection<FlowRuleEvaluation> Evaluate(IEnumerable<(FlowRuleObjectType FromType, Guid FromId, FlowRuleObjectType ToType, Guid ToId)> connections);

		/// <summary>
		/// Evaluates which objects may be connected to an object in a direction, for example to filter the options when adding a node before or after an existing node.
		/// </summary>
		/// <param name="type">The type of the object of the existing node.</param>
		/// <param name="id">The unique identifier of the object of the existing node.</param>
		/// <param name="direction">
		/// <see cref="FlowRuleDirection.After"/> to evaluate the candidates as nodes after the existing node (existing → candidate);
		/// <see cref="FlowRuleDirection.Before"/> to evaluate them as nodes before it (candidate → existing).
		/// </param>
		/// <param name="candidateType">The type of the candidate objects.</param>
		/// <param name="candidateIds">The unique identifiers of the candidate objects.</param>
		/// <returns>The evaluation per candidate ID.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>, or when <paramref name="type"/>, <paramref name="direction"/> or <paramref name="candidateType"/> is not a defined value.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="candidateIds"/> is <see langword="null"/>.</exception>
		IReadOnlyDictionary<Guid, FlowRuleEvaluation> EvaluateCandidates(FlowRuleObjectType type, Guid id, FlowRuleDirection direction, FlowRuleObjectType candidateType, IEnumerable<Guid> candidateIds);

		/// <summary>
		/// Evaluates all connections of a node graph, for example of a job, workflow or recurring job that is being edited.
		/// </summary>
		/// <typeparam name="TNode">The type of the nodes in the graph.</typeparam>
		/// <param name="nodeGraph">The node graph to evaluate.</param>
		/// <returns>The evaluation per connection. Connections of which an endpoint does not reference an object that can have flow rules are not included.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="nodeGraph"/> is <see langword="null"/>.</exception>
		IReadOnlyCollection<FlowRuleConnectionEvaluation> Evaluate<TNode>(NodeGraph<TNode> nodeGraph) where TNode : NodeBase;
	}
}
