namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.General;
    using Skyline.DataMiner.Net.Helper;
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

            HandleAddedSections(updated.Sections.ExceptBy(original.Sections, x => x.ID.Id));
            HandleRemovedSections(original.Sections.ExceptBy(updated.Sections, x => x.ID.Id));
            HandleChangedFieldValues(changes.FieldValues);
        }

        private void HandleAddedSections(IEnumerable<Section> sections)
        {
            if (sections == null)
            {
                throw new ArgumentNullException(nameof(sections));
            }

            if (!sections.Any())
            {
                return;
            }

            foreach (var section in sections)
            {
                HandleAddedSection(section);
            }
        }

        private void HandleAddedSection(Section section)
        {
            stored.Sections.Add(section);

            results.AddedSections.Add(new DomDetails
            {
                SectionDefinitionId = section.SectionDefinitionID.Id,
                SectionId = section.ID.Id,
            });
        }

        private void HandleRemovedSections(IEnumerable<Section> sections)
        {
            if (sections == null)
            {
                throw new ArgumentNullException(nameof(sections));
            }

            if (!sections.Any())
            {
                return;
            }

            foreach (var section in sections)
            {
                HandleRemovedSection(section);
            }
        }

        private void HandleRemovedSection(Section section)
        {
            var storedSection = stored.Sections.FirstOrDefault(x => x.ID.Id == section.ID.Id);
            var orginalSection = original.Sections.FirstOrDefault(x => x.ID.Id == section.ID.Id);

            if (storedSection == null)
            {
                results.Errors.Add(new ErrorDetails
                {
                    Reason = ErrorDetails.ErrorReason.SectionRemoved,
                    Message = $"Section with ID '{section.ID.Id}' has already been removed.",
                    Details = new DomDetails
                    {
                        SectionDefinitionId = orginalSection.SectionDefinitionID.Id,
                        SectionId = orginalSection.ID.Id,
                    },
                });

                return;
            }

            if (!storedSection.FieldValues.ScrambledEquals(orginalSection.FieldValues, new FieldValueComparer()))
            {
                results.Errors.Add(new ErrorDetails
                {
                    Reason = ErrorDetails.ErrorReason.ValueChanged,
                    Message = $"Section with ID '{section.ID.Id}' has field value changes.",
                    Details = new DomDetails
                    {
                        SectionDefinitionId = orginalSection.SectionDefinitionID.Id,
                        SectionId = orginalSection.ID.Id,
                    },
                });

                return;
            }

            stored.Sections.Remove(storedSection);

            results.RemovedSections.Add(new DomDetails
            {
                SectionDefinitionId = section.SectionDefinitionID.Id,
                SectionId = section.ID.Id,
            });
        }

        private void HandleChangedFieldValues(FieldValueDifferences differences)
        {
            if (differences == null)
            {
                throw new ArgumentNullException(nameof(differences));
            }

            var sectionsToIgnore = results.AddedSections.Select(s => s.SectionId)
                .Concat(results.RemovedSections.Select(s => s.SectionId))
                .Concat(results.Errors.Select(e => e.Details.SectionId))
                .ToList();
            var filteredDifferences = differences
                .Where(d => !sectionsToIgnore.Contains(d.SectionId.Id))
                .ToList();

            foreach (var difference in filteredDifferences)
            {
                HandleChangedFieldValue(difference);
            }
        }

        private void HandleChangedFieldValue(FieldValueDifference difference)
        {
            var storedSection = stored.Sections.FirstOrDefault(x => x.ID.Id == difference.SectionId.Id);
            var originalSection = original.Sections.FirstOrDefault(x => x.ID.Id == difference.SectionId.Id);

            if ((storedSection == null && originalSection != null)
                || (storedSection != null && originalSection == null))
            {
                results.Errors.Add(new ErrorDetails
                {
                    Reason = ErrorDetails.ErrorReason.ValueChanged,
                    Message = $"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.",
                    Details = new DomDetails
                    {
                        FieldDescriptorId = difference.FieldDescriptorId.Id,
                        SectionDefinitionId = difference.SectionDefinitionId.Id,
                        SectionId = difference.SectionId.Id,
                    },
                });

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
                results.Errors.Add(new ErrorDetails
                {
                    Reason = ErrorDetails.ErrorReason.ValueChanged,
                    Message = $"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.",
                    Details = new DomDetails
                    {
                        FieldDescriptorId = difference.FieldDescriptorId.Id,
                        SectionDefinitionId = difference.SectionDefinitionId.Id,
                        SectionId = difference.SectionId.Id,
                    },
                });

                return;
            }

            if (storedFieldValue == null)
            {
                ApplyChangedValue(storedSection, difference);

                return;
            }

            if (!storedFieldValue.Value.Equals(difference.ValueBefore))
            {
                results.Errors.Add(new ErrorDetails
                {
                    Reason = ErrorDetails.ErrorReason.ValueChanged,
                    Message = $"Value for field '{difference.FieldDescriptorId.Id}' has already been changed.",
                    Details = new DomDetails
                    {
                        FieldDescriptorId = difference.FieldDescriptorId.Id,
                        SectionDefinitionId = difference.SectionDefinitionId.Id,
                        SectionId = difference.SectionId.Id,
                    },
                });

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

            results.ChangedFields.Add(new DomDetails
            {
                FieldDescriptorId = difference.FieldDescriptorId.Id,
                SectionDefinitionId = difference.SectionDefinitionId.Id,
                SectionId = difference.SectionId.Id,
            });
        }

        private void ApplyChangedValue(Section storedSection, FieldValueDifference difference)
        {
            if (difference.Type == CrudType.Delete)
            {
                return;
            }

            var storedFieldValue = new FieldValue(difference.FieldDescriptorId, difference.ValueAfter);
            storedSection.AddOrReplaceFieldValue(storedFieldValue);

            results.ChangedFields.Add(new DomDetails
            {
                FieldDescriptorId = difference.FieldDescriptorId.Id,
                SectionDefinitionId = difference.SectionDefinitionId.Id,
                SectionId = difference.SectionId.Id,
            });
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

            results.ChangedFields.Add(new DomDetails
            {
                FieldDescriptorId = difference.FieldDescriptorId.Id,
                SectionDefinitionId = difference.SectionDefinitionId.Id,
                SectionId = difference.SectionId.Id,
            });
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

        internal ICollection<ErrorDetails> Errors { get; } = [];

        internal ICollection<DomDetails> AddedSections { get; } = [];

        internal ICollection<DomDetails> RemovedSections { get; } = [];

        internal ICollection<DomDetails> ChangedFields { get; } = [];
    }

    internal class ErrorDetails
    {
        internal enum ErrorReason
        {
            ValueChanged,

            SectionRemoved,
        }

        internal ErrorReason Reason { get; set; }

        internal string Message { get; set; }

        internal DomDetails Details { get; set; }
    }

    internal class DomDetails
    {
        internal Guid FieldDescriptorId { get; set; }

        internal Guid SectionId { get; set; }

        internal Guid SectionDefinitionId { get; set; }
    }

    internal class FieldValueComparer : IEqualityComparer<FieldValue>
    {
        public bool Equals(FieldValue x, FieldValue y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            return x.FieldDescriptorID.Id == y.FieldDescriptorID.Id &&
                   Equals(x.Value, y.Value);
        }
        public int GetHashCode(FieldValue obj)
        {
            if (obj is null) return 0;

            int hashId = obj.FieldDescriptorID.Id.GetHashCode();
            int hashValue = obj.Value?.GetHashCode() ?? 0;

            return hashId ^ hashValue;
        }
    }
}
