namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using PropertyIdSubId = (System.Guid, System.String);

	/// <summary>
	/// A <see cref="ReferenceResolver"/> that resolves <see cref="DataReference"/> instances in the context
	/// of a specific <see cref="Job"/>. Job-level and workflow-level references are resolved using the
	/// job's own data and its associated workflow.
	/// </summary>
	public class JobReferenceResolver : ReferenceResolver
	{
		private readonly Lazy<IDictionary<PropertyIdSubId, PropertySettingBase>> _lazyJobPropertySettings;
		private readonly IDictionary<Guid, Resource> _resourceCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobReferenceResolver"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API used to retrieve definitions and property settings.</param>
		/// <param name="job">The job whose context is used to resolve references.</param>
		public JobReferenceResolver(IMediaOpsPlanApi planApi, Job job) : base(planApi)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));

			_lazyJobPropertySettings = new Lazy<IDictionary<PropertyIdSubId, PropertySettingBase>>(() => ReadPropertySettings(Job.Id));
			_resourceCache = new Dictionary<Guid, Resource>();
		}

		/// <summary>
		/// Gets the job used as the resolution context.
		/// </summary>
		protected Job Job { get; }

		/// <summary>
		/// Gets the lazily-loaded dictionary of property settings defined at the job level.
		/// </summary>
		protected IDictionary<PropertyIdSubId, PropertySettingBase> JobPropertySettings => _lazyJobPropertySettings.Value;

		/// <inheritdoc />
		protected override ResolvedValue ResolveJobName(JobNameReference reference)
		{
			return new StringResolvedValue(Job.Name);
		}

		/// <inheritdoc />
		protected override ResolvedValue ResolveJobPropertyValue(JobPropertyReference reference)
		{
			if (JobPropertySettings.TryGetValue((reference.JobPropertyId, reference.NodeId ?? string.Empty), out var value))
			{
				return ConvertPropertySetting(value);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <inheritdoc />
		protected override Resource GetResource(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return null;
			}

			var node = Job.NodeGraph.Nodes.FirstOrDefault(n => String.Equals(n.Id, reference.NodeId, StringComparison.OrdinalIgnoreCase));

			if (node == null ||
				!node.IsResourceNode(out var resourceNode))
			{
				return null;
			}

			var resourceId = resourceNode.ResourceId;

			if (resourceId == Guid.Empty)
			{
				return null;
			}

			if (_resourceCache.TryGetValue(resourceId, out var cachedResource))
			{
				return cachedResource;
			}

			var resource = PlanApi.Resources.Read(resourceId);
			_resourceCache[resourceId] = resource;

			return resource;
		}

		/// <inheritdoc />
		protected override OrchestrationSettings GetOrchestrationSettings(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return Job.OrchestrationSettings;
			}

			var node = Job.NodeGraph.Nodes.FirstOrDefault(n => String.Equals(n.Id, reference.NodeId, StringComparison.OrdinalIgnoreCase));

			if (node != null)
			{
				return node.OrchestrationSettings;
			}

			return null;
		}
	}
}
