namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Defines methods for managing <see cref="Resource"/> objects.
    /// </summary>
    public interface IResourcesRepository : ICrudRepository<Resource>, ICounterRepository<Resource>
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
        /// <param name="configuration">The configuration for the element link.</param>
        /// <returns>The converted <see cref="ElementResource"/>.</returns>
        ElementResource ConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration);

        /// <summary>
        /// Converts the resource with the specified identifier to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <returns>The converted <see cref="ElementResource"/>.</returns>
        ElementResource ConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <param name="elementResource">When this method returns, contains the converted <see cref="ElementResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration, out ElementResource elementResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to an <see cref="ElementResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <param name="elementResource">When this method returns, contains the converted <see cref="ElementResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration, out ElementResource elementResource);

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <returns>The converted <see cref="ServiceResource"/>.</returns>
        ServiceResource ConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration);

        /// <summary>
        /// Converts the resource with the specified identifier to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <returns>The converted <see cref="ServiceResource"/>.</returns>
        ServiceResource ConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <param name="serviceResource">When this method returns, contains the converted <see cref="ServiceResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to a <see cref="ServiceResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <param name="serviceResource">When this method returns, contains the converted <see cref="ServiceResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource);

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <returns>The converted <see cref="VirtualFunctionResource"/>.</returns>
        VirtualFunctionResource ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration);

        /// <summary>
        /// Converts the resource with the specified identifier to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <returns>The converted <see cref="VirtualFunctionResource"/>.</returns>
        VirtualFunctionResource ConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration);

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <param name="virtualFunctionResource">When this method returns, contains the converted <see cref="VirtualFunctionResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource);

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to a <see cref="VirtualFunctionResource"/> using the provided configuration.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <param name="virtualFunctionResource">When this method returns, contains the converted <see cref="VirtualFunctionResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        bool TryConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource);
    }
}
