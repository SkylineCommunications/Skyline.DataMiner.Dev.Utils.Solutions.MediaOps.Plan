namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Validates whether a node swap is allowed within the node graph of a job.
	/// </summary>
	/// <remarks>
	/// Inside jobs a node can only be swapped to a resource node or a resource pool node. In addition, a resource node
	/// can only be swapped to another resource node, while a resource pool node can be swapped to either a resource pool
	/// node or a resource node. The rule is evaluated against the net original-to-final transition; intermediate swap
	/// steps are ignored.
	/// </remarks>
	internal static class JobNodeSwapValidator
	{
		/// <summary>
		/// Determines whether swapping <paramref name="original"/> to <paramref name="target"/> is allowed inside a job.
		/// </summary>
		/// <param name="original">The node that was originally part of the graph.</param>
		/// <param name="target">The node that currently represents the original node after one or more swaps.</param>
		/// <param name="errorMessage">When the swap is not allowed, contains the reason; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> when the swap is allowed; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="target"/> is null.</exception>
		public static bool IsSwapAllowed(JobNode original, JobNode target, out string errorMessage)
		{
			if (original == null)
			{
				throw new ArgumentNullException(nameof(original));
			}

			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			var targetIsResourceNode = target.IsResourceNode(out _);

			// A node can only be swapped to a resource node or a resource pool node.
			if (!targetIsResourceNode && !target.IsResourcePoolNode(out _))
			{
				errorMessage = "A node in a job can only be swapped to a resource node or a resource pool node.";
				return false;
			}

			// A resource node can only be swapped to another resource node.
			if (original.IsResourceNode(out _) && !targetIsResourceNode)
			{
				errorMessage = "A resource node in a job can only be swapped to another resource node.";
				return false;
			}

			// A resource pool node can be swapped to a resource pool node or a resource node; no further restriction.
			errorMessage = null;
			return true;
		}
	}

	/// <summary>
	/// Validates whether a node swap is allowed within the node graph of a workflow.
	/// </summary>
	/// <remarks>
	/// Inside workflows both resource and resource pool nodes can be swapped to either a resource node or a resource
	/// pool node. The rule is evaluated against the net original-to-final transition; intermediate swap steps are ignored.
	/// </remarks>
	internal static class WorkflowNodeSwapValidator
	{
		/// <summary>
		/// Determines whether swapping <paramref name="original"/> to <paramref name="target"/> is allowed inside a workflow.
		/// </summary>
		/// <param name="original">The node that was originally part of the graph.</param>
		/// <param name="target">The node that currently represents the original node after one or more swaps.</param>
		/// <param name="errorMessage">When the swap is not allowed, contains the reason; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> when the swap is allowed; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="original"/> or <paramref name="target"/> is null.</exception>
		public static bool IsSwapAllowed(WorkflowNode original, WorkflowNode target, out string errorMessage)
		{
			if (original == null)
			{
				throw new ArgumentNullException(nameof(original));
			}

			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			// In workflows both resource and pool nodes can be swapped to a resource or pool node.
			if (!target.IsResourceNode(out _) && !target.IsResourcePoolNode(out _))
			{
				errorMessage = "A node in a workflow can only be swapped to a resource node or a resource pool node.";
				return false;
			}

			errorMessage = null;
			return true;
		}
	}
}
