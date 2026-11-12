namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Validates that the node graph of a running job that already passed its end time is left untouched.
	/// </summary>
	/// <remarks>
	/// Once a job runs in its post-roll, its resources are being released and the topology can no longer be changed in a
	/// meaningful way, so every structural change is rejected. Node configuration is not covered by this validator and
	/// remains editable. This component is intentionally pure (no DOM or API side effects) so the rules can be unit
	/// tested in isolation.
	/// </remarks>
	internal static class JobNodeGraphPostRollFreezeValidator
	{
		/// <summary>
		/// Validates the in-memory node graph against the persisted state of the job.
		/// </summary>
		/// <param name="jobId">The identifier of the job, used to tag the produced errors.</param>
		/// <param name="nodeGraph">The requested (in-memory) node graph.</param>
		/// <param name="original">The persisted instance the node graph was loaded from.</param>
		/// <returns>The list of errors; empty when the node graph is unchanged.</returns>
		internal static IReadOnlyList<MediaOpsErrorData> Validate(Guid jobId, NodeGraph<JobNode> nodeGraph, StorageWorkflow.JobsInstance original)
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			if (original == null)
			{
				throw new ArgumentNullException(nameof(original));
			}

			var errors = new List<MediaOpsErrorData>();

			// A swap retargets every connection, link and group that referenced the swapped-out node. All further
			// comparisons map the current node IDs back to the original ones so that retargeting is reported as the
			// single swap it is instead of as a cascade of connection, link and group changes.
			var swappedNodeIds = new Dictionary<string, string>();
			foreach (var swap in nodeGraph.SwapMappings)
			{
				swappedNodeIds[swap.Value.Id] = swap.Key.Id;
			}

			ValidateNodes(jobId, nodeGraph, original, errors);
			ValidateConnections(jobId, nodeGraph, original, swappedNodeIds, errors);
			ValidateLinks(jobId, nodeGraph, original, swappedNodeIds, errors);
			ValidateGroups(jobId, nodeGraph, original, swappedNodeIds, errors);

			return errors;
		}

		private static void ValidateNodes(Guid jobId, NodeGraph<JobNode> nodeGraph, StorageWorkflow.JobsInstance original, List<MediaOpsErrorData> errors)
		{
			var swappedOutNodeIds = new HashSet<string>();
			var swapTargetNodeIds = new HashSet<string>();

			foreach (var swap in nodeGraph.SwapMappings)
			{
				swappedOutNodeIds.Add(swap.Key.Id);
				swapTargetNodeIds.Add(swap.Value.Id);

				errors.Add(new JobNodeSwappedInPostRollNotAllowedError
				{
					ErrorMessage = "A node of a running job cannot be swapped once the job passed its end time.",
					Id = jobId,
					NodeId = swap.Key.Id,
					TargetNodeId = swap.Value.Id,
				});
			}

			foreach (var node in nodeGraph.Nodes.Where(x => x.IsNew && !swapTargetNodeIds.Contains(x.Id)))
			{
				errors.Add(new JobNodeAddedInPostRollNotAllowedError
				{
					ErrorMessage = "A node cannot be added to a running job once the job passed its end time.",
					Id = jobId,
					NodeId = node.Id,
				});
			}

			var currentNodeIds = new HashSet<string>(nodeGraph.Nodes.Select(x => x.Id).Where(x => x != null));
			var removedNodeIds = original.Nodes
				.Select(x => x.NodeID)
				.Where(x => x != null && !currentNodeIds.Contains(x) && !swappedOutNodeIds.Contains(x));

			foreach (var nodeId in removedNodeIds)
			{
				errors.Add(new JobNodeRemovedInPostRollNotAllowedError
				{
					ErrorMessage = "A node cannot be removed from a running job once the job passed its end time.",
					Id = jobId,
					NodeId = nodeId,
				});
			}
		}

		private static void ValidateConnections(Guid jobId, NodeGraph<JobNode> nodeGraph, StorageWorkflow.JobsInstance original, IReadOnlyDictionary<string, string> swappedNodeIds, List<MediaOpsErrorData> errors)
		{
			var originalSectionsById = new Dictionary<string, StorageWorkflow.ConnectionsSection>();
			foreach (var section in original.Connections.Where(x => x.ConnectionID != null))
			{
				originalSectionsById[section.ConnectionID] = section;
			}

			foreach (var connection in nodeGraph.Connections)
			{
				if (connection.Id == null || !originalSectionsById.TryGetValue(connection.Id, out var originalSection))
				{
					errors.Add(CreateConnectionError(jobId, connection.Id, "added to"));
					continue;
				}

				if (HasChanged(connection, originalSection, swappedNodeIds))
				{
					errors.Add(CreateConnectionError(jobId, connection.Id, "changed on"));
				}
			}

			var currentConnectionIds = new HashSet<string>(nodeGraph.Connections.Select(x => x.Id).Where(x => x != null));
			foreach (var connectionId in originalSectionsById.Keys.Where(x => !currentConnectionIds.Contains(x)))
			{
				errors.Add(CreateConnectionError(jobId, connectionId, "removed from"));
			}
		}

		private static bool HasChanged(NodeConnection<JobNode> connection, StorageWorkflow.ConnectionsSection original, IReadOnlyDictionary<string, string> swappedNodeIds)
		{
			if (Normalize(connection.From?.Id, swappedNodeIds) != original.SourceNodeID
				|| Normalize(connection.To?.Id, swappedNodeIds) != original.DestinationNodeID)
			{
				return true;
			}

			// The connection configurations have no value equality, so they are compared through the fields they persist.
			var requested = new StorageWorkflow.ConnectionsSection();
			connection.Configuration.WriteTo(requested);

			return requested.ConnectionType != original.ConnectionType
				|| requested.ConnectionSubtype != original.ConnectionSubtype
				|| requested.ConnectionDetails != original.ConnectionDetails;
		}

		private static void ValidateLinks(Guid jobId, NodeGraph<JobNode> nodeGraph, StorageWorkflow.JobsInstance original, IReadOnlyDictionary<string, string> swappedNodeIds, List<MediaOpsErrorData> errors)
		{
			var originalLinkKeys = new HashSet<string>(original.NodeRelationships.Select(x => LinkKey(x.ParentNodeID, x.ChildNodeID)));
			var currentLinkKeys = new HashSet<string>();

			foreach (var link in nodeGraph.Links)
			{
				var parentId = Normalize(link.Value?.Id, swappedNodeIds);
				var childId = Normalize(link.Key?.Id, swappedNodeIds);

				currentLinkKeys.Add(LinkKey(parentId, childId));

				if (!originalLinkKeys.Contains(LinkKey(parentId, childId)))
				{
					errors.Add(CreateLinkError(jobId, link.Value?.Id, link.Key?.Id, "added to"));
				}
			}

			foreach (var relationship in original.NodeRelationships.Where(x => !currentLinkKeys.Contains(LinkKey(x.ParentNodeID, x.ChildNodeID))))
			{
				errors.Add(CreateLinkError(jobId, relationship.ParentNodeID, relationship.ChildNodeID, "removed from"));
			}
		}

		private static void ValidateGroups(Guid jobId, NodeGraph<JobNode> nodeGraph, StorageWorkflow.JobsInstance original, IReadOnlyDictionary<string, string> swappedNodeIds, List<MediaOpsErrorData> errors)
		{
			// Stored groups are identified exactly as when the job is parsed, so duplicate IDs resolve to the same groups.
			var originalSectionsById = new Dictionary<Guid, StorageWorkflow.NodeGroupsSection>();
			foreach (var section in original.NodeGroups)
			{
				originalSectionsById.Add(section.GetUniqueGroupId(originalSectionsById.ContainsKey), section);
			}

			var currentGroupIds = new HashSet<Guid>();
			foreach (var group in nodeGraph.Groups)
			{
				currentGroupIds.Add(group.Id);

				if (!originalSectionsById.TryGetValue(group.Id, out var originalSection))
				{
					errors.Add(CreateGroupError(jobId, group.Id, group.Name, "added to"));
					continue;
				}

				var membersChanged = MembershipKey(group.Nodes.Select(x => Normalize(x.Id, swappedNodeIds))) != MembershipKey(originalSection.GroupNodeIds);
				if (group.Name != originalSection.GroupName || membersChanged)
				{
					errors.Add(CreateGroupError(jobId, group.Id, group.Name, "changed on"));
				}
			}

			foreach (var removed in originalSectionsById.Where(x => !currentGroupIds.Contains(x.Key)))
			{
				errors.Add(CreateGroupError(jobId, removed.Key, removed.Value.GroupName, "removed from"));
			}
		}

		private static JobNodeConnectionChangedInPostRollNotAllowedError CreateConnectionError(Guid jobId, string connectionId, string action)
		{
			return new JobNodeConnectionChangedInPostRollNotAllowedError
			{
				ErrorMessage = $"A connection cannot be {action} a running job once the job passed its end time.",
				Id = jobId,
				ConnectionId = connectionId,
			};
		}

		private static JobNodeLinkChangedInPostRollNotAllowedError CreateLinkError(Guid jobId, string parentNodeId, string childNodeId, string action)
		{
			return new JobNodeLinkChangedInPostRollNotAllowedError
			{
				ErrorMessage = $"A parent-child link cannot be {action} a running job once the job passed its end time.",
				Id = jobId,
				ParentNodeId = parentNodeId,
				ChildNodeId = childNodeId,
			};
		}

		private static JobNodeGroupChangedInPostRollNotAllowedError CreateGroupError(Guid jobId, Guid groupId, string groupName, string action)
		{
			return new JobNodeGroupChangedInPostRollNotAllowedError
			{
				ErrorMessage = $"A node group cannot be {action} a running job once the job passed its end time.",
				Id = jobId,
				GroupId = groupId,
				GroupName = groupName,
			};
		}

		private static string Normalize(string nodeId, IReadOnlyDictionary<string, string> swappedNodeIds)
		{
			return nodeId != null && swappedNodeIds.TryGetValue(nodeId, out var originalNodeId) ? originalNodeId : nodeId;
		}

		private static string LinkKey(string parentNodeId, string childNodeId)
		{
			return $"{parentNodeId}|{childNodeId}";
		}

		private static string MembershipKey(IEnumerable<string> nodeIds)
		{
			return string.Join(",", (nodeIds ?? []).OrderBy(x => x, StringComparer.Ordinal));
		}
	}
}
