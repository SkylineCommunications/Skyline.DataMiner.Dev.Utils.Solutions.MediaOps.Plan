namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.General;
    using Skyline.DataMiner.Net.Sections;

    internal class DomChangeHandler
    {
        private readonly DomInstance original;
        private readonly DomInstance updated;
        private readonly DomInstance stored;

        private readonly DomChangeResults results;

        private DomChangeHandler(DomInstanceBase original, DomInstanceBase updated, DomInstanceBase stored)
        {
            this.original = original?.ToInstance() ?? throw new ArgumentNullException(nameof(original));
            this.updated = updated?.ToInstance() ?? throw new ArgumentNullException(nameof(updated));
            this.stored = stored?.ToInstance() ?? throw new ArgumentNullException(nameof(stored));

            results = new DomChangeResults(stored);
        }

        public static DomChangeResults HandleChanges(DomInstanceBase original, DomInstanceBase updated, DomInstanceBase stored)
        {
            var handler = new DomChangeHandler(original, updated, stored);
            handler.Handle();

            return handler.results;
        }

        private void Handle()
        {
            var changes = DomTools.CompareDomInstances(original, updated);
            if (!changes.FieldValues.Any())
            {
                return;
            }

            foreach (var changedFieldValue in changes.FieldValues)
            {
                if (!TryHandleChangedFieldValue(changedFieldValue))
                {
                    continue;
                }
            }
        }

        private bool TryHandleChangedFieldValue(FieldValueDifference difference)
        {
            var storedSection = stored.Sections.FirstOrDefault(x => x.ID.Id == difference.SectionId.Id);
            var originalSection = original.Sections.FirstOrDefault(x => x.ID.Id == difference.SectionId.Id);

            if ((storedSection == null && originalSection != null)
                || (storedSection != null && originalSection == null))
            {
                results.Errors.Add($"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.");
                return false;
            }

            if (storedSection == null)
            {
                return true;
            }

            var storedFieldDescriptor = storedSection.FieldValues.FirstOrDefault(x => x.FieldDescriptorID.Id == difference.FieldDescriptorId.Id);
            var originalFieldDescriptor = originalSection.FieldValues.FirstOrDefault(x => x.FieldDescriptorID.Id == difference.FieldDescriptorId.Id);

            if ((storedFieldDescriptor == null && originalFieldDescriptor != null)
                || (storedFieldDescriptor != null && originalFieldDescriptor == null))
            {
                results.Errors.Add($"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.");
                return false;
            }

            if (storedFieldDescriptor == null)
            {
                return true;
            }

            if (storedFieldDescriptor.Value != difference.ValueBefore)
            {
                results.Errors.Add($"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.");
                return false;
            }

            if (difference.Type == CrudType.Delete)
            {
                storedSection.RemoveFieldValueById(difference.FieldDescriptorId);
            }
            else
            {
                storedSection.AddOrUpdateValue(difference.FieldDescriptorId, difference.ValueAfter);
            }

            results.ChangedFieldDescriptorIds.Add(difference.FieldDescriptorId.Id);

            return true;
        }
    }

    internal class DomChangeResults
    {
        private readonly DomInstance instance;

        internal DomChangeResults(DomInstance instance)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        internal DomInstance Instance => instance;

        internal bool HasErrors => Errors.Count != 0;

        internal List<string> Errors { get; } = new List<string>();

        internal List<Guid> ChangedFieldDescriptorIds { get; } = new List<Guid>();
    }
}
