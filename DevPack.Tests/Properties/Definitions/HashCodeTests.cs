namespace RT_MediaOps.Plan.Properties.Definitions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class HashCodeTests
	{
		[TestMethod]
		public void BooleanProperty_TrackableObject_Name()
		{
			var propertyId = Guid.NewGuid();

			var property = new BooleanProperty(propertyId)
			{
				Name = $"{propertyId}_Property",
			};

			var initialHash = property.GetHashCode();

			property.Name += "_Updated";

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void BooleanProperty_TrackableObject_DefaultValue()
		{
			var property = new BooleanProperty
			{
				Name = "MyBool",
				DefaultValue = false,
			};

			var initialHash = property.GetHashCode();

			property.DefaultValue = true;

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing DefaultValue should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void StringProperty_TrackableObject_Name()
		{
			var propertyId = Guid.NewGuid();

			var property = new StringProperty(propertyId)
			{
				Name = $"{propertyId}_Property",
			};

			var initialHash = property.GetHashCode();

			property.Name += "_Updated";

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void StringProperty_TrackableObject_DefaultValue()
		{
			var property = new StringProperty
			{
				Name = "MyString",
				DefaultValue = "initial",
			};

			var initialHash = property.GetHashCode();

			property.DefaultValue = "updated";

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing DefaultValue should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void StringProperty_TrackableObject_SizeLimit()
		{
			var property = new StringProperty
			{
				Name = "MyString",
				SizeLimit = 100,
			};

			var initialHash = property.GetHashCode();

			property.SizeLimit = 200;

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing SizeLimit should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void StringProperty_TrackableObject_IsMultiLine()
		{
			var property = new StringProperty
			{
				Name = "MyString",
				IsMultiLine = false,
			};

			var initialHash = property.GetHashCode();

			property.IsMultiLine = true;

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMultiLine should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void DiscreteProperty_TrackableObject_Name()
		{
			var propertyId = Guid.NewGuid();

			var property = new DiscreteProperty(propertyId)
			{
				Name = $"{propertyId}_Property",
			};

			var initialHash = property.GetHashCode();

			property.Name += "_Updated";

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void DiscreteProperty_TrackableObject_DefaultValue()
		{
			var property = new DiscreteProperty
			{
				Name = "MyDiscrete",
				DefaultValue = "A",
			};

			var initialHash = property.GetHashCode();

			property.DefaultValue = "B";

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing DefaultValue should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void DiscreteProperty_TrackableObject_Discretes()
		{
			var property = new DiscreteProperty
			{
				Name = "MyDiscrete",
			};
			property.SetDiscretes(new[] { "A", "B" });

			var initialHash = property.GetHashCode();

			property.AddDiscrete("C");

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Discretes should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void FileProperty_TrackableObject_Name()
		{
			var propertyId = Guid.NewGuid();

			var property = new FileProperty(propertyId)
			{
				Name = $"{propertyId}_Property",
			};

			var initialHash = property.GetHashCode();

			property.Name += "_Updated";

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void FileProperty_TrackableObject_SizeLimit()
		{
			var property = new FileProperty
			{
				Name = "MyFile",
				HasSizeLimit = true,
				SizeLimit = 100,
			};

			var initialHash = property.GetHashCode();

			property.SizeLimit = 200;

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing SizeLimit should affect the hash code for change tracking.");
		}

		[TestMethod]
		public void FileProperty_TrackableObject_AllowMultiple()
		{
			var property = new FileProperty
			{
				Name = "MyFile",
				AllowMultiple = false,
			};

			var initialHash = property.GetHashCode();

			property.AllowMultiple = true;

			var updatedHash = property.GetHashCode();

			Assert.AreNotEqual(initialHash, updatedHash, "Changing AllowMultiple should affect the hash code for change tracking.");
		}
	}
}
