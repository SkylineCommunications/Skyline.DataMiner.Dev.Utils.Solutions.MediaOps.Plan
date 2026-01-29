namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Collections.Concurrent;
    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class UnmanagedResourceTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public UnmanagedResourceTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

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

            objectCreator.CreateResource(unmanagedResource);

            var returnedResource = TestContext.Api.Resources.Read(id);
            expectedResult.ValidateUnmanagedResource(returnedResource);

            // Set resource to complete and validate result
            TestContext.Api.Resources.Complete(id);
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
            TestContext.Api.Resources.Deprecate(id);

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

            objectCreator.CreateResource(unmanagedResource1);
            try
            {
                objectCreator.CreateResource(unmanagedResource2);
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
            objectCreator.CreateResource(unmanagedResource1);
            try
            {
                objectCreator.CreateResource(unmanagedResource2);
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

            objectCreator.CreateResource(unmanagedResource1);
            objectCreator.CreateResource(unmanagedResource2);
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

        [TestMethod]
        public void ConcurrentUpdatesToSameResource_ShouldExecuteSequentially()
        {
            var resourceName = TestHelper.GetRandomName("UnmanagedResource_");
            var expectedResourceResult = new ExpectedUnmanagedResource(resourceName, 5, false, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Draft);
            var resource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = expectedResourceResult.Name,
                Concurrency = expectedResourceResult.Concurrency,
                IsFavorite = expectedResourceResult.IsFavorite,
            };

            objectCreator.CreateResource(resource);

            // Create multiple rapid updates
            var executionLog = new ConcurrentBag<(int TaskId, string Event, DateTime Timestamp, Exception? exception)>();
            var numberOfConcurrentUpdates = 20;
            var tasks = new List<Task>();

            for (int i = 0; i < numberOfConcurrentUpdates; i++)
            {
                int taskId = i;
                var task = Task.Run(() =>
                {
                    executionLog.Add((taskId, "Start", DateTime.UtcNow, null));

                    var res = TestContext.Api.Resources.Read(resource.Id);
                    res.Name = $"{resourceName}_Resource_Task{taskId}";

                    try
                    {
                        TestContext.Api.Resources.Update(res);
                        executionLog.Add((taskId, "Complete", DateTime.UtcNow, null));
                    }
                    catch (Exception e)
                    {
                        executionLog.Add((taskId, "Error", DateTime.UtcNow, e));
                    }
                });

                tasks.Add(task);

                Thread.Sleep(10); // Slight delay to increase chance of overlap
            }

            // Wait for all tasks
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(30));

            // Verify all completed successfully
            Assert.AreEqual(numberOfConcurrentUpdates * 2, executionLog.Count);

            // The final resource should have at least have one of the updates
            var finalResource = TestContext.Api.Resources.Read(resource.Id);
            Assert.IsTrue(finalResource.Name.StartsWith($"{resourceName}_Resource_Task"));

            // Check that if update fail, it's because of concurrent writes or because of lock not being granted
            var failedTasks = executionLog.Where(x => x.Event == "Error");
            Assert.IsTrue(failedTasks.All(x => x.exception != null));
            string[] possibleExceptionMessages = new string[]
            {
                "Value for field '13833c8f-6874-44e9-9aeb-9a9914e26771' has already been changed.",
                $"Failed to lock Resource {resource.Id}."
            };

            foreach (var failedTask in failedTasks)
            {
                Assert.IsTrue(possibleExceptionMessages.Contains(failedTask.exception!.Message));
            }

            // Check that no errors occurred (all updates completed)
            var completedTasks = executionLog.Where(x => x.Event == "Complete").Select(x => x.TaskId).Distinct();
            Assert.IsTrue(completedTasks.Count() > 0);
        }

        [TestMethod]
        public void ConcurrentUpdatesAreBlocked()
        {
            var resourceName = TestHelper.GetRandomName("UnmanagedResource_");
            var expectedResourceResult = new ExpectedUnmanagedResource(resourceName, 5, false, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Draft);
            var resource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = expectedResourceResult.Name,
                Concurrency = expectedResourceResult.Concurrency,
                IsFavorite = expectedResourceResult.IsFavorite,
            };

            objectCreator.CreateResource(resource);

            var updatedResourceName1 = $"{resourceName}_Update1";
            var updatedResourceName2 = $"{resourceName}_Update2";

            var resourceForUpdate1 = TestContext.Api.Resources.Read(resource.Id);
            var resourceForUpdate2 = TestContext.Api.Resources.Read(resource.Id);

            resourceForUpdate1.Name = updatedResourceName1;
            TestContext.Api.Resources.Update(resourceForUpdate1);

            resourceForUpdate2.Name = updatedResourceName2;
            Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Resources.Update(resourceForUpdate2));

            var finalResource = TestContext.Api.Resources.Read(resource.Id);
            Assert.AreEqual(updatedResourceName1, finalResource.Name);
        }

        [TestMethod]
        public void ReadWithEmptyListReturnsEmptyList()
        {
            var resources = TestContext.Api.Resources.Read(new List<Guid>());
            Assert.IsNotNull(resources);
            Assert.AreEqual(0, resources.Count());
        }

        [TestMethod]
        public void ResourceWithEmptyNameThrowsExceptionOnCreate()
        {
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Concurrency = 5,
                IsFavorite = true,
                Name = string.Empty,
            };

            MediaOpsException? me = null;
            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException e)
            {
                me = e;
            }

            Assert.IsTrue(me != null, "Exception not thrown");
            StringAssert.Contains(me.Message, "Name cannot be empty.");
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
