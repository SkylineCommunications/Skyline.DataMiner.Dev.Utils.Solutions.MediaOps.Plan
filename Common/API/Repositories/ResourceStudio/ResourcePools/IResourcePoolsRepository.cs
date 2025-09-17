namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Defines methods for managing <see cref="ResourcePool"/> objects, including state transitions.
    /// </summary>
    public interface IResourcePoolsRepository : ICrudRepository<ResourcePool>
    {
        /// <summary>
        /// Moves the specified <see cref="ResourcePool"/> to the desired state.
        /// </summary>
        /// <param name="resourcePool">The resource pool to move.</param>
        /// <param name="desiredState">The state to move the resource pool to.</param>
        void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState);

        /// <summary>
        /// Moves the resource pool with the specified identifier to the desired state.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to move.</param>
        /// <param name="desiredState">The state to move the resource pool to.</param>
        void MoveTo(Guid resourcePoolId, ResourcePoolState desiredState);

        /// <summary>
        /// Deprecates the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeprecateOptions"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to deprecate.</param>
        /// <param name="options">Options specifying hot the resource pool and its resources should be deprecated.</param>
        void Deprecate(ResourcePool resourcePool, ResourcePoolDeprecateOptions options);

        /// <summary>
        /// Deletes the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to delete.</param>
        /// <param name="options">Options specifying how the resource pool and its resources should be deleted.</param>
        void Delete(ResourcePool resourcePool, ResourcePoolDeleteOptions options);
    }
}
