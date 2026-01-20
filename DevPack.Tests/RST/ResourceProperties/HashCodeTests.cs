namespace RT_MediaOps.Plan.RST.ResourceProperties
{
    using System;

    [TestClass]
    public sealed class HashCodeTests
    {
        [TestMethod]
        public void ResourceProperty_TrackableObject_Name()
        {
            var propertyId = Guid.NewGuid();

            var property = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty(propertyId)
            {
                Name = $"{propertyId}_Property",
            };

            var initialHash = property.GetHashCode();

            property.Name += "_Updated";

            var updatedHash = property.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }
    }
}
