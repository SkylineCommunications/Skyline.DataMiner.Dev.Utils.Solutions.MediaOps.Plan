namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System.Collections.Generic;

	internal interface ITemporaryCache<T>
	{
		void SetCache<T>(IEnumerable<T> objects);

		void AddToCache<T>(IEnumerable<T> objects);

		IEnumerable<T> GetFromCache<T>();
	}
}
