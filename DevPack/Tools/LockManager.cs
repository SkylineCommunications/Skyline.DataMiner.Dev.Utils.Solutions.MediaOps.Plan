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

    internal class LockManager
    {
        private const string LockManagerElementName = "MediaOps Lock Manager";
        private const int MaxLockAttempts = 5;

        private readonly TimeSpan _sleepTime = TimeSpan.FromMilliseconds(500);
        private readonly SkylineLockManagerConnectorApi _lockapi;
        private readonly ILogger _logger;

        private readonly HashSet<string> lockedObjectIds = new HashSet<string>(); // Only used for Integration Testing outside of a DataMiner Agent

        public LockManager(MediaOpsPlanApi planApi)
        {
            if (planApi == null)
            {
                throw new ArgumentNullException(nameof(planApi));
            }

            _lockapi = new SkylineLockManagerConnectorApi(planApi.Connection, LockManagerElementName, planApi.LoggerFactory);
            _logger = planApi.Logger;
        }

        public LockResult<T> LockAndExecute<T>(ICollection<T> apiObjects, Action<ICollection<T>> action) where T : ApiObject
        {
            var result = LockAndExecuteInternal(apiObjects, lockedObjects =>
            {
                action(lockedObjects);
                return (ICollection<object>)null;
            });

            return new LockResult<T>(result.FailedToLockObjects);
        }

        public LockResult<T, K> LockAndExecute<T, K>(ICollection<T> apiObjects, Func<ICollection<T>, ICollection<K>> action) where T : ApiObject
        {
            var result = LockAndExecuteInternal(apiObjects, action);
            return new LockResult<T, K>(result.FailedToLockObjects, result.ActionResults);
        }

        private LockAndExecuteResult<T, K> LockAndExecuteInternal<T, K>(ICollection<T> apiObjects, Func<ICollection<T>, ICollection<K>> action) where T : ApiObject
        {
            int attempts = 0;
            List<T> remainingObjectsToHandle = new List<T>(apiObjects);
            List<K> allResults = new List<K>();

            do
            {
                var lockResult = LockObjects(remainingObjectsToHandle);
                if (lockResult.LockedObjects.Any())
                {
                    try
                    {
                        var actionResult = action(lockResult.LockedObjects);
                        if (actionResult != null)
                        {
                            allResults.AddRange(actionResult);
                        }

                        remainingObjectsToHandle = remainingObjectsToHandle.Except(lockResult.LockedObjects).ToList();
                    }
                    finally
                    {
                        // Release granted locks
                        UnlockObjects(lockResult.LockedObjects);
                    }
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
                _logger.LogError("Failed to lock all {0} objects after {1} attempts. Remaining objects: {2}", typeof(T).Name, MaxLockAttempts, string.Join(", ", remainingObjectsToHandle.Select(x => x.Id)));
            }

            return new LockAndExecuteResult<T, K>(remainingObjectsToHandle, allResults);
        }

        private LockManagerApiResult<T> LockObjects<T>(ICollection<T> objectsToLock) where T : ApiObject
        {
            if (DataMinerAgentHelper.IsRunningOnDataMinerAgent())
            {
                var lockRequests = objectsToLock.Select(x => new LockObjectRequest
                {
                    ObjectId = x.LockId,
                });

                var result = _lockapi.LockObjects(lockRequests);
                return new LockManagerApiResult<T>(objectsToLock, result);
            }
            else
            {
                _logger.LogWarning("This code isn't running on a DataMiner agent, unable to communicate with Lock Manager as NATS communication will fail, keeping locks in memory");

                List<string> grantedObjectLocks = new List<string>();
                foreach (var objectToLock in objectsToLock)
                {
                    if (lockedObjectIds.Add(objectToLock.LockId))
                    {
                        grantedObjectLocks.Add(objectToLock.LockId);
                    }
                }

                return new LockManagerApiResult<T>(objectsToLock, grantedObjectLocks);
            }
        }

        private void UnlockObjects<T>(ICollection<T> lockedObjects) where T : ApiObject
        {
            if (DataMinerAgentHelper.IsRunningOnDataMinerAgent())
            {
                var unlockRequests = lockedObjects.Select(x => new UnlockObjectRequest
                {
                    ObjectId = x.LockId,
                });

                _lockapi.UnlockObjects(unlockRequests);
            }
            else
            {
                _logger.LogWarning("This code isn't running on a DataMiner agent, unable to communicate with Lock Manager as NATS communication will fail, unlocking locks from memory");

                Thread.Sleep(1000); // Add some delay to simulate lock communication

                foreach (var lockedObject in lockedObjects)
                {
                    lockedObjectIds.Remove(lockedObject.LockId);
                }
            }
        }

        public class LockResult<T> where T : ApiObject
        {
            public LockResult(ICollection<T> failedToLockObjects)
            {
                FailedToLockObjects = failedToLockObjects;
            }

            public bool AllLocksGranted => !FailedToLockObjects.Any();

            public ICollection<T> FailedToLockObjects { get; }
        }

        public class LockResult<T, K> : LockResult<T> where T : ApiObject
        {
            public LockResult(ICollection<T> failedToLockObjects, ICollection<K> actionResults) : base(failedToLockObjects)
            {
                ActionResults = actionResults;
            }
            public ICollection<K> ActionResults { get; }
        }

        private sealed class LockAndExecuteResult<T, K> where T : ApiObject
        {
            public LockAndExecuteResult(ICollection<T> failedToLockObjects, ICollection<K> actionResults)
            {
                FailedToLockObjects = failedToLockObjects;
                ActionResults = actionResults;
            }

            public ICollection<T> FailedToLockObjects { get; }
            public ICollection<K> ActionResults { get; }
        }

        private sealed class LockManagerApiResult<T> where T : ApiObject
        {
            public LockManagerApiResult(ICollection<T> objectsToLock, ICollection<string> lockedObjectIds)
            {
                if (objectsToLock == null) throw new ArgumentNullException(nameof(objectsToLock));
                if (lockedObjectIds == null) throw new ArgumentNullException(nameof(lockedObjectIds));

                LockedObjects = objectsToLock.Where(x => lockedObjectIds.Contains(x.LockId)).ToList();
                FailedToLockObjects = objectsToLock.Except(LockedObjects).ToList();
            }

            public LockManagerApiResult(ICollection<T> objectsToLock, ILockObjectsResult result)
            {
                if (objectsToLock == null) throw new ArgumentNullException(nameof(objectsToLock));
                if (result == null) throw new ArgumentNullException(nameof(result));

                LockedObjects = objectsToLock.Where(x => result.LockInfosPerObjectId.TryGetValue(x.Id.ToString(), out var lockInfo) && lockInfo.IsGranted).ToList();
                FailedToLockObjects = objectsToLock.Except(LockedObjects).ToList();
            }

            public ICollection<T> LockedObjects { get; private set; }

            public ICollection<T> FailedToLockObjects { get; private set; }
        }
    }
}
