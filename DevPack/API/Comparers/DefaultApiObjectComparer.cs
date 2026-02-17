namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System.Collections.Generic;

    internal class DefaultApiObjectComparer : IEqualityComparer<ApiObject>
    {
        public bool Equals(ApiObject x, ApiObject y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.ID == y.ID; // Compare by unique ID
        }

        public int GetHashCode(ApiObject obj)
        {
            return obj?.ID.GetHashCode() ?? 0;
        }
    }
}
