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
            this.original = original ?? throw new ArgumentNullException(nameof(original));
            this.updated = updated ?? throw new ArgumentNullException(nameof(updated));
            this.stored = stored ?? throw new ArgumentNullException(nameof(stored));

            results = new DomChangeResults(this.stored);
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
                HandleChangedFieldValue(changedFieldValue);
            }
        }

        private void HandleChangedFieldValue(FieldValueDifference difference)
        {
            var storedSection = stored.Sections.FirstOrDefault(x => x.ID.Id == difference.SectionId.Id);
            var originalSection = original.Sections.FirstOrDefault(x => x.ID.Id == difference.SectionId.Id);

            if ((storedSection == null && originalSection != null)
                || (storedSection != null && originalSection == null))
            {
                results.Errors.Add($"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.");
                return;
            }

            if (storedSection == null)
            {
                ApplyChangedValue(difference);

                return;
            }

            var storedFieldValue = storedSection.FieldValues.FirstOrDefault(x => x.FieldDescriptorID.Id == difference.FieldDescriptorId.Id);
            var originalFieldValue = originalSection.FieldValues.FirstOrDefault(x => x.FieldDescriptorID.Id == difference.FieldDescriptorId.Id);

            if ((storedFieldValue == null && originalFieldValue != null)
                || (storedFieldValue != null && originalFieldValue == null))
            {
                results.Errors.Add($"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.");
                return;
            }

            if (storedFieldValue == null)
            {
                ApplyChangedValue(storedSection, difference);

                return;
            }

            if (!storedFieldValue.Value.Equals(difference.ValueBefore))
            {
                results.Errors.Add($"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.");
                return;
            }

            ApplyChangedValue(storedSection, storedFieldValue, difference);
        }

        private void ApplyChangedValue(FieldValueDifference difference)
        {
            if (difference.Type == CrudType.Delete)
            {
                return;
            }

            var storedSection = new Section(difference.SectionDefinitionId);
            storedSection.AddOrReplaceFieldValue(new FieldValue(difference.FieldDescriptorId, difference.ValueAfter));
            stored.Sections.Add(storedSection);

            results.ChangedFieldDescriptorIds.Add(difference.FieldDescriptorId.Id);
        }

        private void ApplyChangedValue(Section storedSection, FieldValueDifference difference)
        {
            if (difference.Type == CrudType.Delete)
            {
                return;
            }

            var storedFieldValue = new FieldValue(difference.FieldDescriptorId, difference.ValueAfter);
            storedSection.AddOrReplaceFieldValue(storedFieldValue);

            results.ChangedFieldDescriptorIds.Add(difference.FieldDescriptorId.Id);
        }

        private void ApplyChangedValue(Section storedSection, FieldValue storedFieldValue, FieldValueDifference difference)
        {
            if (difference.Type == CrudType.Delete)
            {
                storedSection.RemoveFieldValueById(difference.FieldDescriptorId);
            }
            else
            {
                storedFieldValue.Value = difference.ValueAfter;
            }

            results.ChangedFieldDescriptorIds.Add(difference.FieldDescriptorId.Id);
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
