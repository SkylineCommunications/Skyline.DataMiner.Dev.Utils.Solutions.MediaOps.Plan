namespace Skyline.DataMiner.MediaOps.Plan.API
{
    /// <summary>
    /// Defines a repository for managing <see cref="Configuration"/> entities, providing  basic CRUD operations and
    /// additional functionality as defined by the  <see cref="ICrudRepository{T}"/> interface.
    /// </summary>
    public interface IConfigurationsRepository : ICrudRepository<Configuration>
    {
    }
}
