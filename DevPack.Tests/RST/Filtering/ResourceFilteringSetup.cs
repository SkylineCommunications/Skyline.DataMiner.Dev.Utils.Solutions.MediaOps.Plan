namespace RT_MediaOps.Plan.RST.Filtering
{
    using System;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal sealed class ResourceFilteringSetup
    {
        private readonly TestObjectCreator objectCreator;
        private readonly IntegrationTestContext TestContext;

        public ResourceFilteringSetup(TestObjectCreator objectCreator, IntegrationTestContext TestContext)
        {
            this.objectCreator = objectCreator;
            this.TestContext = TestContext;

            CreateCapabilities();
            CreateCapacities();
            CreateConfigurations();
            CreateProperties();
            CreateResourcePools();

            CreateDraftResources();
            CreateCompleteResources();
            CreateElementResources();
            CreateServiceResources();
            CreateVirtualFunctionResources();
        }

        public Resource[] Resources => new Resource[]
        {
            DraftResource1!,
            DraftResource2!,
            DraftResource3!,
            CompleteResource4!,
            CompleteResource5!,
            ElementResource1!,
            ServiceResource1!,
            VirtualFunctionResource1!,
        };

        public ResourcePool[] ResourcePools => new ResourcePool[]
        {
            ResourcePool1!,
            ResourcePool2!,
            ResourcePool3!,
            ResourcePool4!,
            ResourcePool5!,
        };

        public Capability[] Capabilities => new Capability[]
        {
            Location!,
            Priority!,
            Resolution!,
        };

        public Capacity[] Capacities => new Capacity[]
        {
            Frequency!,
            Bandwidth!,
            Reach!,
        };

        public ResourceProperty[] Properties => new ResourceProperty[]
        {
            Channel!,
            Color!,
            Format!,
        };

        public Configuration[] Configurations => new Configuration[]
        {
            Region!,
            Distance!,
            ResolutionConfig!,
            PriorityConfig!,
        };

        public UnmanagedResource? DraftResource1 { get; private set; }

        public UnmanagedResource? DraftResource2 { get; private set; }

        public UnmanagedResource? DraftResource3 { get; private set; }

        public UnmanagedResource? CompleteResource4 { get; private set; }

        public UnmanagedResource? CompleteResource5 { get; private set; }

        public ElementResource? ElementResource1 { get; private set; }

        public ServiceResource? ServiceResource1 { get; private set; }

        public VirtualFunctionResource? VirtualFunctionResource1 { get; private set; }

        public ResourcePool? ResourcePool1 { get; private set; } // Contains Resource 1 & 2

        public ResourcePool? ResourcePool2 { get; private set; } // Contains Resource 3, 4, and 5, VF1

        public ResourcePool? ResourcePool3 { get; private set; } // Contains no resources

        public ResourcePool? ResourcePool4 { get; private set; } // Contains no resources

        public ResourcePool? ResourcePool5 { get; private set; } // Contains no resources

        public Capability? Location { get; private set; }

        public Capability? Priority { get; private set; }

        public Capability? Resolution { get; private set; }

        public NumberCapacity? Frequency { get; private set; }

        public NumberCapacity? Reach { get; private set; }

        public RangeCapacity? Bandwidth { get; private set; }

        public ResourceProperty? Channel { get; private set; }

        public ResourceProperty? Color { get; private set; }

        public ResourceProperty? Format { get; private set; }

        public TextConfiguration? Region { get; private set; }

        public NumberConfiguration? Distance { get; private set; }

        public DiscreteTextConfiguration? ResolutionConfig { get; private set; }

        public DiscreteNumberConfiguration? PriorityConfig { get; private set; }

        private void CreateCapabilities()
        {
            Location = new Capability
            {
                Name = $"Location_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = true,
            };

            Location.SetDiscretes(["USA", "Mozambique", "Belgium"]);

            Priority = new Capability
            {
                Name = $"Priority_{Guid.NewGuid()}",
            };

            Priority.SetDiscretes(["Low", "Medium", "High"]);

            Resolution = new Capability
            {
                Name = $"Resolution_{Guid.NewGuid()}",
            };

            Resolution.SetDiscretes(["720p", "1080p", "4K"]);

            var capabilities = new Capability[]
            {
                Location,
                Priority,
                Resolution,
            };

            foreach (var capability in capabilities)
            {
                objectCreator.CreateCapability(capability);
            }

            Location = TestContext.Api.Capabilities.Read(Location.Id);
            Priority = TestContext.Api.Capabilities.Read(Priority.Id);
            Resolution = TestContext.Api.Capabilities.Read(Resolution.Id);
        }

        private void CreateDraftResources()
        {
            DraftResource1 = new UnmanagedResource
            {
                Name = $"Resource_Draft_1_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 5,
            };

            var locationResource1 = new CapabilitySettings(Location);
            locationResource1.SetDiscretes(["USA", "Belgium"]);

            var priorityResource1 = new CapabilitySettings(Priority);
            priorityResource1.SetDiscretes(["Low"]);

            var frequencyCapacity1 = new NumberCapacitySetting(Frequency)
            {
                Value = 20
            };

            var bandwidthCapacity1 = new RangeCapacitySetting(Bandwidth)
            {
                MinValue = 5000,
                MaxValue = 7500
            };

            DraftResource1.AddCapability(locationResource1);
            DraftResource1.AddCapability(priorityResource1);
            DraftResource1.AddCapacity(frequencyCapacity1);
            DraftResource1.AddCapacity(bandwidthCapacity1);
            DraftResource1.AssignToPool(ResourcePool1);

            DraftResource2 = new UnmanagedResource
            {
                Name = $"Resource_Draft_2_{Guid.NewGuid()}",
                IsFavorite = false,
                Concurrency = 10,
            };

            var channelProperty2 = new ResourcePropertySettings(Channel)
            {
                Value = "VRT"
            };

            var formatProperty2 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            var frequencyCapacity2 = new NumberCapacitySetting(Frequency)
            {
                Value = 20
            };

            var bandwidthCapacity2 = new RangeCapacitySetting(Bandwidth)
            {
                MinValue = 5000,
                MaxValue = 7500
            };

            DraftResource2.AddProperty(channelProperty2);
            DraftResource2.AddProperty(formatProperty2);
            DraftResource2.AddCapacity(frequencyCapacity2);
            DraftResource2.AddCapacity(bandwidthCapacity2);
            DraftResource2.AssignToPool(ResourcePool1);

            DraftResource3 = new UnmanagedResource
            {
                Name = $"Resource_Draft_3_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 15,
            };

            var resolutionResource3 = new CapabilitySettings(Resolution);
            resolutionResource3.SetDiscretes(["1080p", "4K"]);

            var formatProperty3 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            DraftResource3.AddCapability(resolutionResource3);
            DraftResource3.AddProperty(formatProperty3);
            DraftResource3.AssignToPool(ResourcePool2);

            var resourcesToCreate = new Resource[]
            {
                DraftResource1,
                DraftResource2,
                DraftResource3,
            };

            DraftResource1 = objectCreator.CreateResource(DraftResource1);
            DraftResource2 = objectCreator.CreateResource(DraftResource2);
            DraftResource3 = objectCreator.CreateResource(DraftResource3);
        }

        private void CreateCompleteResources()
        {
            CompleteResource4 = new UnmanagedResource
            {
                Name = $"Resource_Complete_4_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 5,
            };

            var resolutionResource4 = new CapabilitySettings(Resolution);
            resolutionResource4.SetDiscretes(["1080p", "4K"]);

            var formatProperty4 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            var frequencyCapacity4 = new NumberCapacitySetting(Frequency)
            {
                Value = 20
            };

            CompleteResource4.AddCapability(resolutionResource4);
            CompleteResource4.AddProperty(formatProperty4);
            CompleteResource4.AddCapacity(frequencyCapacity4);
            CompleteResource4.AssignToPool(ResourcePool2);

            CompleteResource5 = new UnmanagedResource
            {
                Name = $"Resource_Complete_5_{Guid.NewGuid()}",
                IsFavorite = false,
                Concurrency = 10,
            };

            var channelProperty5 = new ResourcePropertySettings(Channel)
            {
                Value = "VRT"
            };

            var formatProperty5 = new ResourcePropertySettings(Format)
            {
                Value = "16:9"
            };

            CompleteResource5.AddProperty(channelProperty5);
            CompleteResource5.AddProperty(formatProperty5);
            CompleteResource5.AssignToPool(ResourcePool2);

            // Create using objectCreator and keep returned instances
            CompleteResource4 = (UnmanagedResource)objectCreator.CreateResource(CompleteResource4);
            CompleteResource5 = (UnmanagedResource)objectCreator.CreateResource(CompleteResource5);

            // Mark as complete
            TestContext.Api.Resources.Complete(CompleteResource4);
            TestContext.Api.Resources.Complete(CompleteResource5);
        }

        private void CreateElementResources()
        {
            ElementResource1 = new ElementResource
            {
                Name = $"ElementResource_1_{Guid.NewGuid()}",
                IsFavorite = false,
                Concurrency = 4,
                AgentId = 100,
                ElementId = 200,
            };

            ElementResource1 = (ElementResource)objectCreator.CreateResource(ElementResource1);
        }

        private void CreateServiceResources()
        {
            ServiceResource1 = new ServiceResource
            {
                Name = $"ServiceResource_1_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 2,
                AgentId = 100,
                ServiceId = 20,
            };

            ServiceResource1 = (ServiceResource)objectCreator.CreateResource(ServiceResource1);
        }

        private void CreateVirtualFunctionResources()
        {
            VirtualFunctionResource1 = new VirtualFunctionResource
            {
                Name = $"VirtualFunction_1_{Guid.NewGuid()}",
                IsFavorite = true,
                Concurrency = 3,
                AgentId = 100,
                ElementId = 200,
                FunctionId = Guid.NewGuid(),
                FunctionTableIndex = "VF_Table_1",
            };

            var capabilitySettings1 = new CapabilitySettings(Resolution);
            capabilitySettings1.SetDiscretes(new[] { "720p", "1080p", "4K" });

            VirtualFunctionResource1.AddCapability(capabilitySettings1);
            VirtualFunctionResource1.AssignToPool(ResourcePool2);

            VirtualFunctionResource1 = (VirtualFunctionResource)objectCreator.CreateResource(VirtualFunctionResource1);
        }

        private void CreateCapacities()
        {
            Frequency = new NumberCapacity
            {
                Name = $"Frequency _{Guid.NewGuid()}",
                Units = "MHz",
                RangeMin = 1,
                RangeMax = 25,
                StepSize = 0.0001m,
                Decimals = 4,
            };

            Bandwidth = new RangeCapacity
            {
                Name = $"Band_{Guid.NewGuid()}",
                Units = "Hz",
                RangeMin = 1000,
                RangeMax = 30000,
                StepSize = 0.01m,
                Decimals = 2,
            };

            Reach = new NumberCapacity
            {
                Name = $"Reach _{Guid.NewGuid()}",
            };

            var capacities = new Capacity[]
            {
                Frequency,
                Bandwidth,
                Reach,
            };

            foreach (var capacity in capacities)
            {
                objectCreator.CreateCapacity(capacity);
            }

            Frequency = (NumberCapacity)TestContext.Api.Capacities.Read(Frequency.Id);
            Bandwidth = (RangeCapacity)TestContext.Api.Capacities.Read(Bandwidth.Id);
            Reach = (NumberCapacity)TestContext.Api.Capacities.Read(Reach.Id);
        }

        private void CreateConfigurations()
        {
            Region = new TextConfiguration
            {
                Name = $"Region_{Guid.NewGuid()}",
                IsMandatory = false,
                DefaultValue = "BEL",
            };

            Distance = new NumberConfiguration
            {
                Name = $"Distance_{Guid.NewGuid()}",
                IsMandatory = true,
                Units = "m",
                RangeMin = 0,
                RangeMax = 1000,
                StepSize = 1,
                Decimals = 0,
                DefaultValue = 100,
            };

            ResolutionConfig = new DiscreteTextConfiguration
            {
                Name = $"Resolution_{Guid.NewGuid()}",
                IsMandatory = false,
            };
            ResolutionConfig.AddDiscrete(new TextDiscreet("720p", "SD"));
            ResolutionConfig.AddDiscrete(new TextDiscreet("1080p", "HD"));
            ResolutionConfig.AddDiscrete(new TextDiscreet("4K", "UHD"));
            ResolutionConfig.DefaultValue = new TextDiscreet("1080p", "HD");

            PriorityConfig = new DiscreteNumberConfiguration
            {
                Name = $"Priority_{Guid.NewGuid()}",
                IsMandatory = false,
            };
            PriorityConfig.AddDiscrete(new NumberDiscreet(10, "Low"));
            PriorityConfig.AddDiscrete(new NumberDiscreet(50, "Medium"));
            PriorityConfig.AddDiscrete(new NumberDiscreet(100, "High"));
            PriorityConfig.DefaultValue = new NumberDiscreet(50, "Medium");

            var configurations = new Configuration[]
            {
                Region,
                Distance,
                ResolutionConfig,
                PriorityConfig,
            };

            foreach (var configuration in configurations)
            {
                objectCreator.CreateConfiguration(configuration);
            }

            Region = (TextConfiguration)TestContext.Api.Configurations.Read(Region.Id);
            Distance = (NumberConfiguration)TestContext.Api.Configurations.Read(Distance.Id);
            ResolutionConfig = (DiscreteTextConfiguration)TestContext.Api.Configurations.Read(ResolutionConfig.Id);
            PriorityConfig = (DiscreteNumberConfiguration)TestContext.Api.Configurations.Read(PriorityConfig.Id);
        }

        private void CreateProperties()
        {
            Channel = new ResourceProperty
            {
                Name = $"Channel_{Guid.NewGuid()}",
            };

            Color = new ResourceProperty
            {
                Name = $"Color_{Guid.NewGuid()}",
            };

            Format = new ResourceProperty
            {
                Name = $"Format_{Guid.NewGuid()}",
            };

            var properties = new ResourceProperty[]
            {
                Channel,
                Color,
                Format,
            };

            foreach (var property in properties)
            {
                objectCreator.CreateProperty(property);
            }

            Channel = TestContext.Api.ResourceProperties.Read(Channel.Id);
            Color = TestContext.Api.ResourceProperties.Read(Color.Id);
            Format = TestContext.Api.ResourceProperties.Read(Format.Id);
        }

        private void CreateResourcePools()
        {
            ResourcePool1 = new ResourcePool
            {
                Name = $"ResourcePool_Draft_1_{Guid.NewGuid()}",
                IconImage = "icon_1.png",
            }
            .AddCapability(new CapabilitySettings(Location!.Id).SetDiscretes(["Belgium"]))
            .AddCapability(new CapabilitySettings(Priority!.Id).SetDiscretes(["Low", "Medium", "High"]));

            ResourcePool2 = new ResourcePool
            {
                Name = $"ResourcePool_Complete_2_{Guid.NewGuid()}",
                IconImage = "icon_2.png",
            }
            .AddCapability(new CapabilitySettings(Location.Id).SetDiscretes(["Belgium"]))
            .AddCapability(new CapabilitySettings(Priority.Id).SetDiscretes(["Low", "Medium", "High"]));

            ResourcePool3 = new ResourcePool
            {
                Name = $"ResourcePool_Complete_3_{Guid.NewGuid()}",
                IconImage = "icon_3.png",
            }
            .AddCapability(new CapabilitySettings(Resolution!.Id).SetDiscretes(["4K"]))
            .AddCapability(new CapabilitySettings(Priority.Id).SetDiscretes(["Medium"]));

            ResourcePool4 = new ResourcePool
            {
                Name = $"ResourcePool_Complete_4_{Guid.NewGuid()}",
            }
            .AddLinkedResourcePool(new LinkedResourcePool(ResourcePool1) { SelectionType = ResourceSelectionType.Automatic })
            .AddLinkedResourcePool(new LinkedResourcePool(ResourcePool2) { SelectionType = ResourceSelectionType.Automatic });

            ResourcePool5 = new ResourcePool
            {
                Name = $"ResourcePool_Complete_5_{Guid.NewGuid()}",
                IconImage = "icon_5.jpeg",
            }
            .AddLinkedResourcePool(new LinkedResourcePool(ResourcePool1) { SelectionType = ResourceSelectionType.Manual })
            .AddLinkedResourcePool(new LinkedResourcePool(ResourcePool2) { SelectionType = ResourceSelectionType.Automatic });

            objectCreator.CreateResourcePool(ResourcePool1); // Draft pool

            objectCreator.CreateResourcePool(ResourcePool2);
            TestContext.Api.ResourcePools.Complete(ResourcePool2);

            objectCreator.CreateResourcePool(ResourcePool3);
            TestContext.Api.ResourcePools.Complete(ResourcePool3);

            objectCreator.CreateResourcePool(ResourcePool4);
            TestContext.Api.ResourcePools.Complete(ResourcePool4);

            objectCreator.CreateResourcePool(ResourcePool5);
            TestContext.Api.ResourcePools.Complete(ResourcePool5);

            ResourcePool1 = TestContext.Api.ResourcePools.Read(ResourcePool1.Id);
            ResourcePool2 = TestContext.Api.ResourcePools.Read(ResourcePool2.Id);
            ResourcePool3 = TestContext.Api.ResourcePools.Read(ResourcePool3.Id);
            ResourcePool4 = TestContext.Api.ResourcePools.Read(ResourcePool4.Id);
            ResourcePool5 = TestContext.Api.ResourcePools.Read(ResourcePool5.Id);
        }
    }
}
