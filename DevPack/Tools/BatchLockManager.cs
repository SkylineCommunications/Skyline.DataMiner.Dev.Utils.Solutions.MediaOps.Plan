namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.ConnectorAPI.SkylineLockManager.ConnectorApi;
    using Skyline.DataMiner.ConnectorAPI.SkylineLockManager.ConnectorApi.InterApp.Messages.Locking;
    using Skyline.DataMiner.ConnectorAPI.SkylineLockManager.ConnectorApi.InterApp.Messages.Unlocking;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal class BatchLockManager
    {
        private const string LockManagerElementName = "SkylineLockManager";
        private const int MaxLockAttempts = 5;

        private readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(500);
        private readonly SkylineLockManagerConnectorApi _lockapi;
        private readonly ILogger _logger;

        public BatchLockManager(MediaOpsPlanApi planApi)
        {
            if (planApi == null)
            {
                throw new ArgumentNullException(nameof(planApi));
            }

            _lockapi = new SkylineLockManagerConnectorApi(planApi.Connection, LockManagerElementName);
            _logger = planApi.Logger;
        }

        public void LockAndExecute<T>(ICollection<T> apiObjects, Action<ICollection<T>> action) where T : ApiObject
        {
            int attempts = 0;

            List<T> remainingObjectsToHandle = apiObjects.ToList();

            do
            {
                var lockRequests = remainingObjectsToHandle.Select(x => new LockObjectRequest
                {
                    ObjectId = x.Id.ToString(),
                });

                var result = _lockapi.LockObjects(lockRequests);

                try
                {
                    var lockedItems = apiObjects.Where(x => result.LockInfosPerObjectId.TryGetValue(x.Id.ToString(), out var lockInfo) && lockInfo.IsGranted).ToList();
                    action(lockedItems);
                    remainingObjectsToHandle = remainingObjectsToHandle.Except(lockedItems).ToList();
                }
                finally
                {
                    // Release granted locks
                    _lockapi.UnlockObjects(result.LockInfosPerObjectId.Where(x => x.Value.IsGranted).Select(x => new UnlockObjectRequest
                    {
                        ObjectId = x.Key,
                    }));
                }

                if (remainingObjectsToHandle.Any())
                {
                    // if any remaining objects to lock, wait before retrying
                    Thread.Sleep(_sleepTime);
                }

                attempts++;
            }
            while (attempts < MaxLockAttempts && remainingObjectsToHandle.Any());

            if (remainingObjectsToHandle.Any())
            {
                _logger.LogError("Failed to lock all objects after {0} attempts. Remaining objects: {1}", MaxLockAttempts, string.Join(", ", remainingObjectsToHandle.Select(x => x.Name)));
            }
        }
    }
}
