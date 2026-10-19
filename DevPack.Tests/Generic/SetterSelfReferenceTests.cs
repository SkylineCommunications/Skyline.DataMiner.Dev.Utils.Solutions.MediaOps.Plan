namespace RT_MediaOps.Plan.Generic
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// The Set* methods replace their backing list, while the matching getters expose that same list.
	/// Passing the getter (or a lazy query over it) must not wipe the collection.
	/// </summary>
	[TestClass]
	public sealed class SetterSelfReferenceTests
	{
		[TestMethod]
		public void PropertySettingCollection_SetCustomSettings_WithOwnGetter()
		{
			var collection = new PropertySettingCollection();
			collection.Add(new CustomPropertySetting("A") { Value = "1" });
			collection.Add(new CustomPropertySetting("B") { Value = "2" });

			collection.SetCustomSettings(collection.CustomSettings);
			CollectionAssert.AreEquivalent(new[] { "A", "B" }, collection.CustomSettings.Select(x => x.Name).ToList());

			collection.SetCustomSettings(collection.CustomSettings.Where(x => x.Name != "A"));
			CollectionAssert.AreEquivalent(new[] { "B" }, collection.CustomSettings.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void PropertySettingCollection_SetPropertySettings_WithLazyQueryOverTypedGetter()
		{
			var propertyA = new StringProperty();
			var propertyB = new StringProperty();

			var collection = new PropertySettingCollection();
			collection.Add(new StringPropertySetting(propertyA) { Value = "1" });
			collection.Add(new StringPropertySetting(propertyB) { Value = "2" });

			collection.SetPropertySettings(collection.StringSettings);
			CollectionAssert.AreEquivalent(new[] { propertyA.Id, propertyB.Id }, collection.StringSettings.Select(x => x.Id).ToList());

			collection.SetPropertySettings(collection.StringSettings.Where(x => x.Id != propertyA.Id));
			CollectionAssert.AreEquivalent(new[] { propertyB.Id }, collection.StringSettings.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void Job_SetCustomProperties_WithOwnGetter()
		{
			var job = new Job();
			job.AddCustomProperty(new CustomPropertySetting("A") { Value = "1" });
			job.AddCustomProperty(new CustomPropertySetting("B") { Value = "2" });

			job.SetCustomProperties(job.CustomPropertySettings);
			CollectionAssert.AreEquivalent(new[] { "A", "B" }, job.CustomPropertySettings.Select(x => x.Name).ToList());

			job.SetCustomProperties(job.CustomPropertySettings.Where(x => x.Name != "A"));
			CollectionAssert.AreEquivalent(new[] { "B" }, job.CustomPropertySettings.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void Workflow_SetCustomProperties_WithOwnGetter()
		{
			var workflow = new Workflow();
			workflow.AddCustomProperty(new CustomPropertySetting("A") { Value = "1" });
			workflow.AddCustomProperty(new CustomPropertySetting("B") { Value = "2" });

			workflow.SetCustomProperties(workflow.CustomPropertySettings);
			CollectionAssert.AreEquivalent(new[] { "A", "B" }, workflow.CustomPropertySettings.Select(x => x.Name).ToList());

			workflow.SetCustomProperties(workflow.CustomPropertySettings.Where(x => x.Name != "A"));
			CollectionAssert.AreEquivalent(new[] { "B" }, workflow.CustomPropertySettings.Select(x => x.Name).ToList());
		}

		[TestMethod]
		public void Resource_SetCapabilities_WithOwnGetter()
		{
			var capabilityA = Guid.NewGuid();
			var capabilityB = Guid.NewGuid();

			var resource = new UnmanagedResource();
			resource.AddCapability(new CapabilitySettings(capabilityA));
			resource.AddCapability(new CapabilitySettings(capabilityB));

			resource.SetCapabilities(resource.Capabilities);
			CollectionAssert.AreEquivalent(new[] { capabilityA, capabilityB }, resource.Capabilities.Select(x => x.Id).ToList());

			resource.SetCapabilities(resource.Capabilities.Where(x => x.Id != capabilityA));
			CollectionAssert.AreEquivalent(new[] { capabilityB }, resource.Capabilities.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void Resource_SetProperties_WithOwnGetter()
		{
			var propertyA = Guid.NewGuid();
			var propertyB = Guid.NewGuid();

			var resource = new UnmanagedResource();
			resource.AddProperty(new ResourcePropertySettings(propertyA));
			resource.AddProperty(new ResourcePropertySettings(propertyB));

			resource.SetProperties(resource.Properties);
			CollectionAssert.AreEquivalent(new[] { propertyA, propertyB }, resource.Properties.Select(x => x.Id).ToList());

			resource.SetProperties(resource.Properties.Where(x => x.Id != propertyA));
			CollectionAssert.AreEquivalent(new[] { propertyB }, resource.Properties.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void ResourcePool_SetCapabilities_WithOwnGetter()
		{
			var capabilityA = Guid.NewGuid();
			var capabilityB = Guid.NewGuid();

			var resourcePool = new ResourcePool();
			resourcePool.AddCapability(new CapabilitySettings(capabilityA));
			resourcePool.AddCapability(new CapabilitySettings(capabilityB));

			resourcePool.SetCapabilities(resourcePool.Capabilities);
			CollectionAssert.AreEquivalent(new[] { capabilityA, capabilityB }, resourcePool.Capabilities.Select(x => x.Id).ToList());

			resourcePool.SetCapabilities(resourcePool.Capabilities.Where(x => x.Id != capabilityA));
			CollectionAssert.AreEquivalent(new[] { capabilityB }, resourcePool.Capabilities.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void WorkflowOrchestrationSettings_SetCapabilitiesAndEvents_WithOwnGetter()
		{
			AssertOrchestrationSettings(new WorkflowOrchestrationSettings());
		}

		[TestMethod]
		public void ResourceStudioOrchestrationSettings_SetCapabilitiesAndEvents_WithOwnGetter()
		{
			AssertOrchestrationSettings(new ResourcePool().OrchestrationSettings);
		}

		[TestMethod]
		public void ScriptExecutionDetails_SetCapabilities_WithOwnGetter()
		{
			var capabilityA = Guid.NewGuid();
			var capabilityB = Guid.NewGuid();

			var details = new ScriptExecutionDetails("Script");
			details.AddCapability(new CapabilitySetting(capabilityA));
			details.AddCapability(new CapabilitySetting(capabilityB));

			details.SetCapabilities(details.Capabilities);
			CollectionAssert.AreEquivalent(new[] { capabilityA, capabilityB }, details.Capabilities.Select(x => x.Id).ToList());

			details.SetCapabilities(details.Capabilities.Where(x => x.Id != capabilityA));
			CollectionAssert.AreEquivalent(new[] { capabilityB }, details.Capabilities.Select(x => x.Id).ToList());
		}

		[TestMethod]
		public void DiscreteProperty_SetDiscretes_WithLazyQueryOverGetter()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A");
			property.AddDiscrete("B");

			property.SetDiscretes(property.Discretes);
			CollectionAssert.AreEqual(new[] { "A", "B" }, property.Discretes.ToList());

			property.SetDiscretes(property.Discretes.Where(x => x != "A"));
			CollectionAssert.AreEqual(new[] { "B" }, property.Discretes.ToList());
		}

		[TestMethod]
		public void Capability_SetDiscretes_WithLazyQueryOverGetter()
		{
			var capability = new Capability();
			capability.SetDiscretes(new[] { "A", "B" });

			capability.SetDiscretes(capability.Discretes);
			CollectionAssert.AreEqual(new[] { "A", "B" }, capability.Discretes.ToList());

			capability.SetDiscretes(capability.Discretes.Where(x => x != "A"));
			CollectionAssert.AreEqual(new[] { "B" }, capability.Discretes.ToList());
		}

		private static void AssertOrchestrationSettings(OrchestrationSettings settings)
		{
			var capabilityA = Guid.NewGuid();
			var capabilityB = Guid.NewGuid();

			settings.AddCapability(new CapabilitySetting(capabilityA));
			settings.AddCapability(new CapabilitySetting(capabilityB));

			settings.SetCapabilities(settings.Capabilities);
			CollectionAssert.AreEquivalent(new[] { capabilityA, capabilityB }, settings.Capabilities.Select(x => x.Id).ToList());

			settings.SetCapabilities(settings.Capabilities.Where(x => x.Id != capabilityA));
			CollectionAssert.AreEquivalent(new[] { capabilityB }, settings.Capabilities.Select(x => x.Id).ToList());

			settings.AddOrchestrationEvent(new OrchestrationEvent { EventType = OrchestrationEventType.PrerollStart });
			settings.AddOrchestrationEvent(new OrchestrationEvent { EventType = OrchestrationEventType.PostrollStart });

			settings.SetOrchestrationEvents(settings.OrchestrationEvents);
			CollectionAssert.AreEquivalent(
				new[] { OrchestrationEventType.PrerollStart, OrchestrationEventType.PostrollStart },
				settings.OrchestrationEvents.Select(x => x.EventType).ToList());

			settings.SetOrchestrationEvents(settings.OrchestrationEvents.Where(x => x.EventType != OrchestrationEventType.PrerollStart));
			CollectionAssert.AreEquivalent(
				new[] { OrchestrationEventType.PostrollStart },
				settings.OrchestrationEvents.Select(x => x.EventType).ToList());
		}
	}
}
