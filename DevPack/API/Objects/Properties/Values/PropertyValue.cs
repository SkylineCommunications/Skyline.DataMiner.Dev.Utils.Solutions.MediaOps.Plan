namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Objects.Properties.Values
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public abstract class PropertyValue : TrackableObject
	{
	}

	public class CustomPropertyValue : PropertyValue
	{

	}

	public class FilePropertyValue
	{
		public FilePropertyValue(FileProperty property)
		{

		}

		public FilePropertyValue(Guid propertyId)
		{

		}
	}
}
