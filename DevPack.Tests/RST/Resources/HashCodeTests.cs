namespace RT_MediaOps.Plan.RST.Resources
{
	using System;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class HashCodeTests
	{
		[TestMethod]
		public void UnmanagedResource_TrackableObject_Name()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
			};

			var initialHash = resource.GetHashCode();

			resource.Name += "_Updated";

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_IsFavorite()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				IsFavorite = false,
			};

			var initialHash = resource.GetHashCode();

			resource.IsFavorite = true;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_IsExternallyManaged()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				IsExternallyManaged = false,
			};

			var initialHash = resource.GetHashCode();

			resource.IsExternallyManaged = true;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_Concurrency()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				Concurrency = 1,
			};

			var initialHash = resource.GetHashCode();

			resource.Concurrency = 2;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_IconImage()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				IconImage = "icon1.png",
			};

			var initialHash = resource.GetHashCode();

			resource.IconImage = "icon2.png";

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_Url()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				Url = "http://example.com/1",
			};

			var initialHash = resource.GetHashCode();

			resource.Url = "http://example.com/2";

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_VirtualSignalGroupInputId()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				VirtualSignalGroupInputId = Guid.Empty,
			};

			var initialHash = resource.GetHashCode();

			resource.VirtualSignalGroupInputId = Guid.NewGuid();

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_VirtualSignalGroupOutputId()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				VirtualSignalGroupOutputId = Guid.Empty,
			};

			var initialHash = resource.GetHashCode();

			resource.VirtualSignalGroupOutputId = Guid.NewGuid();

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_Pools()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
			};

			var poolId = Guid.NewGuid();

			var initialHash = resource.GetHashCode();

			resource.AssignToPool(poolId);

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_Capabilities()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
			};

			var capability = new Capability
			{
				Name = $"Capability_{Guid.NewGuid()}",
			};
			capability.SetDiscretes(new[] { "A", "B" });
			var capabilitySetting = new CapabilitySettings(capability.Id);
			capabilitySetting.SetDiscretes(new[] { "A" });

			var initialHash = resource.GetHashCode();

			resource.AddCapability(capabilitySetting);

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_Capacities()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
			};

			var numberCapacity = new NumberCapacity(Guid.NewGuid())
			{
				Name = $"Capacity_{Guid.NewGuid()}",
			};
			var numberCapacitySetting = new NumberCapacitySetting(numberCapacity.Id)
			{
				Value = 1,
			};

			var initialHash = resource.GetHashCode();

			resource.AddCapacity(numberCapacitySetting);

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void UnmanagedResource_TrackableObject_Properties()
		{
			var resource = new UnmanagedResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
			};

			var property = new ResourceProperty(Guid.NewGuid())
			{
				Name = $"Property_{Guid.NewGuid()}",
			};
			var propertySettings = new ResourcePropertySettings(property.Id)
			{
				Value = "X",
			};

			var initialHash = resource.GetHashCode();

			resource.AddProperty(propertySettings);

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void ElementResource_TrackableObject_AgentId()
		{
			var resource = new ElementResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ElementId = 100,
			};

			var initialHash = resource.GetHashCode();

			resource.AgentId = 2;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void ElementResource_TrackableObject_ElementId()
		{
			var resource = new ElementResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ElementId = 100,
			};

			var initialHash = resource.GetHashCode();

			resource.ElementId = 200;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void ServiceResource_TrackableObject_AgentId()
		{
			var resource = new ServiceResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ServiceId = 10,
			};

			var initialHash = resource.GetHashCode();

			resource.AgentId = 2;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void ServiceResource_TrackableObject_ServiceId()
		{
			var resource = new ServiceResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ServiceId = 10,
			};

			var initialHash = resource.GetHashCode();

			resource.ServiceId = 20;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void VirtualFunctionResource_TrackableObject_AgentId()
		{
			var resource = new VirtualFunctionResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ElementId = 10,
				FunctionId = Guid.NewGuid(),
				FunctionTableIndex = "1",
			};

			var initialHash = resource.GetHashCode();

			resource.AgentId = 2;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void VirtualFunctionResource_TrackableObject_ElementId()
		{
			var resource = new VirtualFunctionResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ElementId = 10,
				FunctionId = Guid.NewGuid(),
				FunctionTableIndex = "1",
			};

			var initialHash = resource.GetHashCode();

			resource.ElementId = 20;

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void VirtualFunctionResource_TrackableObject_FunctionId()
		{
			var resource = new VirtualFunctionResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ElementId = 10,
				FunctionId = Guid.NewGuid(),
				FunctionTableIndex = "1",
			};

			var initialHash = resource.GetHashCode();

			resource.FunctionId = Guid.NewGuid();

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}

		[TestMethod]
		public void VirtualFunctionResource_TrackableObject_FunctionTableIndex()
		{
			var resource = new VirtualFunctionResource
			{
				Name = $"Resource_{Guid.NewGuid()}",
				AgentId = 1,
				ElementId = 10,
				FunctionId = Guid.NewGuid(),
				FunctionTableIndex = "1",
			};

			var initialHash = resource.GetHashCode();

			resource.FunctionTableIndex = "2";

			var updatedHash = resource.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash);
		}
	}
}
