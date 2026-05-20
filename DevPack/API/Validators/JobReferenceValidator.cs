namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Validates whether all <see cref="DataReference"/> instances within a <see cref="Job"/> can be resolved to an actual value.
    /// </summary>
    public class JobReferenceValidator
    {
        private readonly ReferenceResolver _referenceResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobReferenceValidator"/> class.
        /// </summary>
        /// <param name="referenceResolver">The resolver used to resolve references.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="referenceResolver"/> is <see langword="null"/>.</exception>
        public JobReferenceValidator(ReferenceResolver referenceResolver)
        {
            _referenceResolver = referenceResolver ?? throw new ArgumentNullException(nameof(referenceResolver));
        }

        /// <summary>
        /// Validates that all references in the specified job can be resolved to an actual value.
        /// </summary>
        /// <param name="job">The job to validate.</param>
        /// <returns>A <see cref="JobReferenceValidationResult"/> containing the validation outcome.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is <see langword="null"/>.</exception>
        public JobReferenceValidationResult Validate(Job job)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));

			var unresolvedReferences = new HashSet<DataReference>();

			if (job.OrchestrationSettings != null)
			{
				ValidateSettings(job.OrchestrationSettings, unresolvedReferences);
				ValidateOrchestrationEvents(job.OrchestrationSettings, unresolvedReferences);
			}

			return new JobReferenceValidationResult(unresolvedReferences);
		}

		private void ValidateSettings(OrchestrationSettings orchestrationSettings, ICollection<DataReference> unresolvedReferences)
        {
            var settings = orchestrationSettings.Capabilities.Cast<Setting>()
                .Concat(orchestrationSettings.Capacities.Cast<Setting>())
                .Concat(orchestrationSettings.Configurations.Cast<Setting>());

            ValidateReferences(settings.Where(s => s.HasReference).Select(s => s.Reference), unresolvedReferences);
        }

        private void ValidateOrchestrationEvents(OrchestrationSettings orchestrationSettings, ICollection<DataReference> unresolvedReferences)
        {
            if (orchestrationSettings.OrchestrationEvents == null)
            {
                return;
            }

            foreach (var orchestrationEvent in orchestrationSettings.OrchestrationEvents)
            {
                if (orchestrationEvent.ExecutionDetails == null)
                {
                    continue;
                }

                var executionDetails = orchestrationEvent.ExecutionDetails;

                var settingReferences = executionDetails.Capabilities.Cast<Setting>()
                    .Concat(executionDetails.Capacities.Cast<Setting>())
                    .Concat(executionDetails.Configurations.Cast<Setting>())
                    .Where(s => s.HasReference)
                    .Select(s => s.Reference);

                var scriptElementReferences = executionDetails.ScriptElements
                    .Where(e => e.HasReference)
                    .Select(e => e.Reference);

                var scriptParameterReferences = executionDetails.ScriptParameters
                    .Where(p => p.HasReference)
                    .Select(p => p.Reference);

                ValidateReferences(settingReferences.Concat(scriptElementReferences).Concat(scriptParameterReferences), unresolvedReferences);
            }
        }

        private void ValidateReferences(IEnumerable<DataReference> references, ICollection<DataReference> unresolvedReferences)
        {
            foreach (var reference in references)
            {
                try
                {
                    var resolved = _referenceResolver.ResolveValue(reference);

                    if (!resolved.IsResolved)
                    {
                        unresolvedReferences.Add(reference);
                    }
                }
                catch (CircularReferenceException)
                {
                    unresolvedReferences.Add(reference);
                }
            }
        }
    }
}
