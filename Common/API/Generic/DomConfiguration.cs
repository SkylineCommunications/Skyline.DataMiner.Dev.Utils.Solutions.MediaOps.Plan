namespace Skyline.DataMiner.MediaOps.API.Common.API.Generic
{
    using System;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal abstract class DomConfiguration<TDom> : IConfiguration
        where TDom : DomObject
    {
        public Guid? ObjectId { get; set; }

        Guid? IConfiguration.ObjectId { get => ObjectId; set => throw new NotImplementedException(); }

        public abstract ValidationResult Validate();

        /// <summary>
        /// Translate the configuration to a DOM instance.
        /// </summary>
        /// <param name="originalObject">The original object in case the configuration is used for updating.</param>
        /// <returns>The DOM instance that can be saved to database.</returns>
        internal abstract DomInstance TranslateToDomInstance(TDom originalObject = null);
    }
}
