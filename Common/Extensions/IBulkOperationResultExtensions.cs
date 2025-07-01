namespace Skyline.DataMiner.MediaOps.Plan.Extensions
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    internal static class IBulkOperationResultExtensions
    {
        internal static bool HasFailures<K>(this IBulkOperationResult<K> result) where K : IEquatable<K>
        {
            if (result == null)
            {
                throw new ArgumentException(nameof(result));
            }

            List<K> unsuccessfulIds = result.UnsuccessfulIds;
            if (unsuccessfulIds == null)
            {
                return false;
            }

            return unsuccessfulIds.Count > 0;
        }

        internal static void ThrowOnFailure<K>(this IBulkOperationResult<K> result) where K : IEquatable<K>
        {
            if (result == null)
            {
                throw new ArgumentException(nameof(result));
            }

            if (result.HasFailures())
            {
                throw new MediaOpsBulkException<K>(result);
            }
        }
    }
}
