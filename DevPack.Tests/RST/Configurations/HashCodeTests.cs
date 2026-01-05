namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;

    [TestClass]
    public sealed class HashCodeTests
    {
        [TestMethod]
        public void DiscreteNumberConfiguration_TrackableObject_Name()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteNumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            configuration.AddDiscrete("Low", 10);

            var initialHash = configuration.GetHashCode();

            configuration.Name += "_Updated";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteNumberConfiguration_TrackableObject_IsMandatory()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteNumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                IsMandatory = false,
            };

            configuration.AddDiscrete("Low", 10);

            var initialHash = configuration.GetHashCode();

            configuration.IsMandatory = true;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMandatory should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteNumberConfiguration_TrackableObject_DefaultValue()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteNumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            configuration.AddDiscrete("Low", 10);
            configuration.AddDiscrete("Medium", 20);
            configuration.DefaultValue = "Low";

            var initialHash = configuration.GetHashCode();

            configuration.DefaultValue = "Medium";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing DefaultValue should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteNumberConfiguration_TrackableObject_Discretes()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteNumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            configuration.AddDiscrete("Low", 10);

            var initialHash = configuration.GetHashCode();

            configuration.AddDiscrete("Medium", 20);

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Discretes should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteTextConfiguration_TrackableObject_Name()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            configuration.AddDiscrete("Low", "low_value");

            var initialHash = configuration.GetHashCode();

            configuration.Name += "_Updated";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteTextConfiguration_TrackableObject_IsMandatory()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                IsMandatory = false,
            };

            configuration.AddDiscrete("Low", "low_value");

            var initialHash = configuration.GetHashCode();

            configuration.IsMandatory = true;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMandatory should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteTextConfiguration_TrackableObject_DefaultValue()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            configuration.AddDiscrete("Low", "low_value");
            configuration.AddDiscrete("Medium", "medium_value");
            configuration.DefaultValue = "Low";

            var initialHash = configuration.GetHashCode();

            configuration.DefaultValue = "Medium";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing DefaultValue should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void DiscreteTextConfiguration_TrackableObject_Discretes()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            configuration.AddDiscrete("Low", "low_value");

            var initialHash = configuration.GetHashCode();

            configuration.AddDiscrete("Medium", "medium_value");

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Discretes should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_Name()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
            };

            var initialHash = configuration.GetHashCode();

            configuration.Name += "_Updated";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_IsMandatory()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                IsMandatory = false,
            };

            var initialHash = configuration.GetHashCode();

            configuration.IsMandatory = true;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMandatory should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_Units()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                Units = "MHz",
            };

            var initialHash = configuration.GetHashCode();

            configuration.Units = "kHz";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Units should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_RangeMin()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                RangeMin = 1,
            };

            var initialHash = configuration.GetHashCode();

            configuration.RangeMin = 2;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing RangeMin should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_RangeMax()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                RangeMax = 10,
            };

            var initialHash = configuration.GetHashCode();

            configuration.RangeMax = 20;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing RangeMax should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_StepSize()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                StepSize = 1,
            };

            var initialHash = configuration.GetHashCode();

            configuration.StepSize = 2;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing StepSize should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void NumberConfiguration_TrackableObject_Decimals()
        {
            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{Guid.NewGuid()}_Configuration",
                Decimals = 1,
            };

            var initialHash = configuration.GetHashCode();

            configuration.Decimals = 2;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Decimals should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void TextConfiguration_TrackableObject_Name()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
            };

            var initialHash = configuration.GetHashCode();

            configuration.Name += "_Updated";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void TextConfiguration_TrackableObject_IsMandatory()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                IsMandatory = false,
            };

            var initialHash = configuration.GetHashCode();

            configuration.IsMandatory = true;

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMandatory should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void TextConfiguration_TrackableObject_DefaultValue()
        {
            var configurationId = Guid.NewGuid();

            var configuration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration(configurationId)
            {
                Name = $"{configurationId}_Configuration",
                DefaultValue = "Initial",
            };

            var initialHash = configuration.GetHashCode();

            configuration.DefaultValue = "Updated";

            var updatedHash = configuration.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing DefaultValue should affect the hash code for change tracking.");
        }
    }
}
