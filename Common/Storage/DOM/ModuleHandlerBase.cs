namespace Skyline.DataMiner.MediaOps.API.Common.Storage.DOM
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ModuleHandlerBase
    {
        private readonly ConcurrentDictionary<Guid, DomDefinition> domDefinitionsById = new ConcurrentDictionary<Guid, DomDefinition>();

        private readonly ConcurrentDictionary<Guid, DomBehaviorDefinition> domBehaviorDefinitionsById = new ConcurrentDictionary<Guid, DomBehaviorDefinition>();

        protected ModuleHandlerBase(DomHelper domHelper, string moduleId)
        {
            if (domHelper == null)
            {
                throw new ArgumentNullException(nameof(domHelper));
            }

            if (domHelper.ModuleId != moduleId)
            {
                throw new ArgumentException($"DomHelper with module ID '{domHelper.ModuleId}' is provided while module ID '{moduleId}' is expected");
            }

            DomHelper = domHelper;
        }

        public DomHelper DomHelper { get; }

        internal IReadOnlyCollection<DomStatus> GetStatusesForDomDefinition(DomDefinitionId domDefinitionId)
        {
            if (domDefinitionId == null)
            {
                throw new ArgumentNullException(nameof(domDefinitionId));
            }

            var domDefinition = GetDomDefinition(domDefinitionId.Id);
            var domBehaviorDefinition = GetDomBehaviorDefinition(domDefinition.DomBehaviorDefinitionId.Id);

            return domBehaviorDefinition.Statuses;
        }

        private DomDefinition GetDomDefinition(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return domDefinitionsById.GetOrAdd(
                id, x =>
                {
                    var domDefinition = DomHelper.DomDefinitions.Read(DomDefinitionExposers.Id.Equal(id)).SingleOrDefault();
                    if (domDefinition == null)
                    {
                        throw new InvalidOperationException($"Module {DomHelper.ModuleId} does not contain a DOM Definition with ID {id}.");
                    }

                    return domDefinition;
                });
        }

        private DomBehaviorDefinition GetDomBehaviorDefinition(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return domBehaviorDefinitionsById.GetOrAdd(
                id, x =>
                {
                    var domBehaviorDefinition = DomHelper.DomBehaviorDefinitions.Read(DomBehaviorDefinitionExposers.Id.Equal(id)).SingleOrDefault();
                    if (domBehaviorDefinition == null)
                    {
                        throw new InvalidOperationException($"Module {DomHelper.ModuleId} does not contain a DOM Behavior Definition with ID {id}.");
                    }

                    return domBehaviorDefinition;
                });
        }
    }
}
