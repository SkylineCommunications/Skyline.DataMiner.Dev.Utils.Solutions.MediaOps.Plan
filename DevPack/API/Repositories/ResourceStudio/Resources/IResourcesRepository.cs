namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines methods for managing <see cref="Resource"/> objects.
    /// </summary>
    public interface IResourcesRepository : IRepository<Resource>
    {
        /// <summary>
        /// Moves the specified <see cref="Resource"/> to the desired state.
        /// </summary>
        /// <param name="resource">The resource to move.</param>
        /// <param name="desiredState">The state to move the resource to.</param>
        void MoveTo(Resource resource, ResourceState desiredState);

        /// <summary>
        /// Moves the resource with the specified identifier to the desired state.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to move.</param>
        /// <param name="desiredState">The state to move the resource to.</param>
        void MoveTo(Guid resourceId, ResourceState desiredState);

        /// <summary>
        /// Moves the specified <see cref="Resource"/> to the desired state.
        /// </summary>
        /// <param name="resources">The resources to move.</param>
        /// <param name="desiredState">The state to move the resources to.</param>
        void MoveTo(IEnumerable<Resource> resources, ResourceState desiredState);

        /// <summary>
        /// Moves the resources with the specified identifiers to the desired state.
        /// </summary>
        /// <param name="resourceIds">The unique identifier of the resources to move.</param>
        /// <param name="desiredState">The state to move the resources to.</param>
        void MoveTo(IEnumerable<Guid> resourceIds, ResourceState desiredState);

        /// <summary>
        /// Marks the specified resource as deprecated, indicating that it is no longer recommended for use.
        /// </summary>
        /// <param name="resource">The resource to be marked as deprecated. Cannot be null.</param>
        void Deprecate(Resource resource);

        /// <summary>
        /// Marks the specified resource as deprecated, indicating that it is no longer recommended for use.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to deprecate.</param>
        void Deprecate(Guid resourceId);

        /// <summary>
        /// Marks the specified resources as deprecated, indicating that they are no longer recommended for use.
        /// </summary>
        /// <param name="resources">A collection of resources to be marked as deprecated. Cannot be null or empty.</param>
        void Deprecate(IEnumerable<Resource> resources);

        /// <summary>
        /// Marks the specified resources as deprecated, indicating that they are no longer recommended for use.
        /// </summary>
        /// <param name="resourceIds">The unique identifiers of the resources to deprecate.</param>
        void Deprecate(IEnumerable<Guid> resourceIds);

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <returns>The converted <see cref="UnmanagedResource"/>.</returns>
        UnmanagedResource ConvertToUnmanagedResource(Resource resource);

        /// <summary>
        /// Converts the resource with the specified identifier to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <returns>The converted <see cref="UnmanagedResource"/>.</returns>
        UnmanagedResource ConvertToUnmanagedResource(Guid resourceId);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="unmanagedResource">When this method returns, contains the converted <see cref="UnmanagedResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToUnmanagedResource(Resource resource, out UnmanagedResource unmanagedResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="unmanagedResource">When this method returns, contains the converted <see cref="UnmanagedResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        bool TryConvertToUnmanagedResource(Guid resourceId, out UnmanagedResource unmanagedResource);

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="setting">The configuration for the element link.</param>
        /// <returns>The converted <see cref="ElementResource"/>.</returns>
        ElementResource ConvertToElementResource(Resource resource, ResourceElementLinkSetting setting);

        /// <summary>
        /// Converts the resource with the specified identifier to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="setting">The configuration for the element link.</param>
        /// <returns>The converted <see cref="ElementResource"/>.</returns>
        ElementResource ConvertToElementResource(Guid resourceId, ResourceElementLinkSetting setting);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="setting">The configuration for the element link.</param>
        /// <param name="elementResource">When this method returns, contains the converted <see cref="ElementResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToElementResource(Resource resource, ResourceElementLinkSetting setting, out ElementResource elementResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="setting">The configuration for the element link.</param>
        /// <param name="elementResource">When this method returns, contains the converted <see cref="ElementResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToElementResource(Guid resourceId, ResourceElementLinkSetting setting, out ElementResource elementResource);

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="setting">The configuration for the service link.</param>
        /// <returns>The converted <see cref="ServiceResource"/>.</returns>
        ServiceResource ConvertToServiceResource(Resource resource, ResourceServiceLinkSetting setting);

        /// <summary>
        /// Converts the resource with the specified identifier to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="setting">The configuration for the service link.</param>
        /// <returns>The converted <see cref="ServiceResource"/>.</returns>
        ServiceResource ConvertToServiceResource(Guid resourceId, ResourceServiceLinkSetting setting);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="setting">The configuration for the service link.</param>
        /// <param name="serviceResource">When this method returns, contains the converted <see cref="ServiceResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToServiceResource(Resource resource, ResourceServiceLinkSetting setting, out ServiceResource serviceResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="setting">The configuration for the service link.</param>
        /// <param name="serviceResource">When this method returns, contains the converted <see cref="ServiceResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToServiceResource(Guid resourceId, ResourceServiceLinkSetting setting, out ServiceResource serviceResource);

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="setting">The configuration for the virtual function link.</param>
        /// <returns>The converted <see cref="VirtualFunctionResource"/>.</returns>
        VirtualFunctionResource ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkSetting setting);

        /// <summary>
        /// Converts the resource with the specified identifier to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="setting">The configuration for the virtual function link.</param>
        /// <returns>The converted <see cref="VirtualFunctionResource"/>.</returns>
        VirtualFunctionResource ConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkSetting setting);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="setting">The configuration for the virtual function link.</param>
        /// <param name="virtualFunctionResource">When this method returns, contains the converted <see cref="VirtualFunctionResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkSetting setting, out VirtualFunctionResource virtualFunctionResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="setting">The configuration for the virtual function link.</param>
        /// <param name="virtualFunctionResource">When this method returns, contains the converted <see cref="VirtualFunctionResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkSetting setting, out VirtualFunctionResource virtualFunctionResource);

        /// <summary>
        /// Gets all resources in the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool to search for resources.</param>
        /// <returns>An enumerable collection of resources in the specified pool.</returns>
        IEnumerable<Resource> GetResourcesInPool(ResourcePool resourcePool);

        /// <summary>
        /// Retrieves a collection of resources from the specified resource pool that match the given state.
        /// </summary>
        /// <param name="resourcePool">The resource pool to search for resources.</param>
        /// <param name="state">The state that the resources must match to be included in the result.</param>
        /// <returns>An enumerable collection of resources that belong to the specified resource pool and match the given state.
        /// If no matching resources are found, the collection will be empty.</returns>
        IEnumerable<Resource> GetResourcesInPool(ResourcePool resourcePool, ResourceState state);

        /// <summary>
        /// Gets a dictionary mapping each specified resource pool to its resource.
        /// </summary>
        /// <param name="resourcePools">The resource pools to search for resources.</param>
        /// <returns>A read-only dictionary mapping each resource pool to its resources.</returns>
        IReadOnlyDictionary<ResourcePool, IEnumerable<Resource>> GetResourcesPerPool(IEnumerable<ResourcePool> resourcePools);

        /// <summary>
        /// Retrieves a mapping of resource pools to their associated resources that match the specified state.
        /// </summary>
        /// <remarks>This method filters resources based on their state and groups them by their
        /// respective resource pools. The returned dictionary is read-only and cannot be modified.</remarks>
        /// <param name="resourcePools">The collection of resource pools to evaluate. Cannot be null.</param>
        /// <param name="state">The desired state of the resources to include in the result.</param>
        /// <returns>A read-only dictionary where each key is a resource pool and the value is a collection of resources  within
        /// that pool that match the specified state. The dictionary will be empty if no matching resources are found.</returns>
        IReadOnlyDictionary<ResourcePool, IEnumerable<Resource>> GetResourcesPerPool(IEnumerable<ResourcePool> resourcePools, ResourceState state);

        /// <summary>
        /// Determines whether the specified <see cref="ResourcePool"/> contains any resources, independent of its state.
        /// </summary>
        /// <param name="resourcePool">The resource pool to check for resources.</param>
        /// <returns><c>true</c> if the resource pool contains resources; otherwise, <c>false</c>.</returns>
        bool HasResources(ResourcePool resourcePool);

        /// <summary>
        /// Gets the total number of resources, independent of its state, in the specified <see cref="ResourcePool"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to count resources in.</param>
        /// <returns>The count of resources in the resource pool.</returns>
        long ResourceCount(ResourcePool resourcePool);
    }
}
