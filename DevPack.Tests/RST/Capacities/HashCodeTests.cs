namespace RT_MediaOps.Plan.RST.Capacities
{
	using System;

	[TestClass]
	public sealed class HashCodeTests
	{
		[TestMethod]
		public void Capacity_TrackableObject_Name()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
			};

			var initialHash = capacity.GetHashCode();

			capacity.Name += "_Updated";

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void Capacity_TrackableObject_IsMandatory()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
				IsMandatory = false,
			};

			var initialHash = capacity.GetHashCode();

			capacity.IsMandatory = true;

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMandatory should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void Capacity_TrackableObject_Units()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
				Units = "MHz",
			};

			var initialHash = capacity.GetHashCode();

			capacity.Units = "kHz";

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Units should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void Capacity_TrackableObject_RangeMin()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
				RangeMin = 1,
			};

			var initialHash = capacity.GetHashCode();

			capacity.RangeMin = 2;

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing RangeMin should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void Capacity_TrackableObject_RangeMax()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
				RangeMax = 10,
			};

			var initialHash = capacity.GetHashCode();

			capacity.RangeMax = 20;

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing RangeMax should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void Capacity_TrackableObject_StepSize()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
				StepSize = 1,
			};

			var initialHash = capacity.GetHashCode();

			capacity.StepSize = 2;

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing StepSize should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void Capacity_TrackableObject_Decimals()
		{
			var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_Capacity",
				Decimals = 1,
			};

			var initialHash = capacity.GetHashCode();

			capacity.Decimals = 2;

			var updatedHash = capacity.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Decimals should affect the hash code for change tracking.");
		}
	}
}
