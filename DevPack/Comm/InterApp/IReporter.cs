namespace Skyline.DataMiner.Solutions.MediaOps.Plan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IReporter
    {
        void ReportCreatedResources(IEnumerable<string> resourceIds);

        void ReportUpdatedResources(IEnumerable<string> resourceIds);

        void ReportAddOrUpdatedResources(IEnumerable<string> resourceIds);

        void ReportDeletedResources(IEnumerable<string> resourceIds);
    }
}
