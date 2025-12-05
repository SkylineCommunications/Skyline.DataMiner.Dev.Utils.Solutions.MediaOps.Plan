namespace RT_MediaOps.Plan.RST.Resources
{
    using System;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class UnmanagedResourcesTests : IDisposable
    {
        public UnmanagedResourcesTests()
        {
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        [TestMethod]
        public void HappyPathCrud()
        {
            // Create resource and validate result
            var id = Guid.NewGuid();
            var name = TestHelper.GetRandomName("UnmanagedResource_", id);
            var expectedResult = new ExpectedUnmanagedResource(name, 5, true, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Draft, id);
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource(id)
            {
                Name = expectedResult.Name,
                Concurrency = expectedResult.Concurrency,
                IsFavorite = expectedResult.IsFavorite,
            };

            var returnedId = TestContext.Api.Resources.Create(unmanagedResource);
            Assert.AreEqual(id, returnedId);

            var returnedResource = TestContext.Api.Resources.Read(id);
            expectedResult.ValidateUnmanagedResource(returnedResource);

            // Set resource to complete and validate result
            TestContext.Api.Resources.MoveTo(id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            returnedResource = TestContext.Api.Resources.Read(id);
            expectedResult.State = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete;
            expectedResult.ValidateUnmanagedResource(returnedResource);

            // Update resource and validate result
            expectedResult = new ExpectedUnmanagedResource(name + "_updated", 10, false, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete, id);
            returnedResource.Concurrency = expectedResult.Concurrency;
            returnedResource.IsFavorite = expectedResult.IsFavorite;
            returnedResource.Name = expectedResult.Name;
            TestContext.Api.Resources.Update(returnedResource);
            returnedResource = TestContext.Api.Resources.Read(id);
            expectedResult.ValidateUnmanagedResource(returnedResource);

            // Deprecate resource in order to be able to delete it
            TestContext.Api.Resources.MoveTo(id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Deprecated);

            // Delete resource and validate it is gone
            TestContext.Api.Resources.Delete(id);

            var retrievedResource = TestContext.Api.Resources.Read(id);
            Assert.IsNull(retrievedResource);
        }

        [TestMethod]
        public void CreateWithSameIdThrowsException()
        {
            string name = TestHelper.GetRandomName("UnmanagedResource_");
            var id = Guid.NewGuid();
            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource(id)
            {
                Name = name,
                Concurrency = 5,
                IsFavorite = true,
            };

            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource(id)
            {
                Name = name + "2",
                Concurrency = 5,
                IsFavorite = true,
            };

            TestContext.Api.Resources.Create(unmanagedResource1);
            try
            {
                TestContext.Api.Resources.Create(unmanagedResource2);
            }
            catch (MediaOpsException me)
            {
                StringAssert.Contains(me.Message, "ID is already in use.");
                TestContext.Api.Resources.Delete(unmanagedResource1);
                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void CreateWithSameNameThrowsException()
        {
            string name = TestHelper.GetRandomName("UnmanagedResource_");
            var id = Guid.NewGuid();
            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource(id)
            {
                Name = name,
                Concurrency = 5,
                IsFavorite = true,
            };
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = name,
                Concurrency = 5,
                IsFavorite = true,
            };
            TestContext.Api.Resources.Create(unmanagedResource1);
            try
            {
                TestContext.Api.Resources.Create(unmanagedResource2);
            }
            catch (MediaOpsException me)
            {
                StringAssert.Contains(me.Message, "Name is already in use.");
                TestContext.Api.Resources.Delete(unmanagedResource1);
                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateToSameNameThrowsException()
        {
            // Create two resources with different names and validate result
            var name1 = TestHelper.GetRandomName("UnmanagedResource_");
            var expectedResult1 = new ExpectedUnmanagedResource(name1, 5, true, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Draft);
            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = expectedResult1.Name,
                Concurrency = expectedResult1.Concurrency,
                IsFavorite = expectedResult1.IsFavorite,
            };

            var name2 = TestHelper.GetRandomName("UnmanagedResource_");
            var expectedResult2 = new ExpectedUnmanagedResource(name2, 5, true, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Draft);
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = expectedResult2.Name,
                Concurrency = expectedResult2.Concurrency,
                IsFavorite = expectedResult2.IsFavorite,
            };

            TestContext.Api.Resources.Create(unmanagedResource1);
            TestContext.Api.Resources.Create(unmanagedResource2);
            var returnedResource1 = TestContext.Api.Resources.Read(unmanagedResource1.Id);
            var returnedResource2 = TestContext.Api.Resources.Read(unmanagedResource2.Id);
            expectedResult1.ValidateUnmanagedResource(returnedResource1);
            expectedResult2.ValidateUnmanagedResource(returnedResource2);

            // Validate that exception is thrown when updating one of them to have the same name as the other
            MediaOpsException? me = null;
            returnedResource2.Name = name1;
            try
            {
                TestContext.Api.Resources.Update(returnedResource2);
            }
            catch (MediaOpsException e)
            {
                me = e;
            }

            Assert.IsTrue(me != null, "Exception not thrown");
            StringAssert.Contains(me.Message, "Name is already in use");
        }

        public void Dispose()
        {
        }

        private struct ExpectedUnmanagedResource
        {
            public ExpectedUnmanagedResource(string name, int concurrency, bool isFavorite, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState state, Guid? id = null)
            {
                Id = id;
                Name = name;
                Concurrency = concurrency;
                IsFavorite = isFavorite;
                State = state;
            }

            public Guid? Id { get; set; }
            public string Name { get; set; }
            public int Concurrency { get; set; }
            public bool IsFavorite { get; set; }
            public Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState State { get; set; }

            public void ValidateUnmanagedResource(Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource resource)
            {
                if (resource == null)
                {
                    Assert.Fail("Resource is null");
                }

                if (Id.HasValue)
                {
                    Assert.AreEqual(Id, resource.Id);
                }

                Assert.AreEqual(Name, resource.Name);
                Assert.AreEqual(Concurrency, resource.Concurrency);
                Assert.AreEqual(IsFavorite, resource.IsFavorite);
                Assert.AreEqual(State, resource.State);
            }
        }
    }
}
