namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
	using System.Collections.Generic;

	internal class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>>
	{
		public static readonly DictionaryComparer<TKey, TValue> Default = new();

		public bool Equals(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
		{
			if (ReferenceEquals(x, y))
			{
				return true;
			}

			if (x == null || y == null || x.Count != y.Count)
			{
				return false;
			}

			var valueComparer = EqualityComparer<TValue>.Default;

			foreach (var kv in x)
			{
				if (!y.TryGetValue(kv.Key, out var value))
				{
					return false;
				}

				if (!valueComparer.Equals(kv.Value, value))
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(IDictionary<TKey, TValue> dictionary)
		{
			unchecked
			{
				int hash = 0;

				if (dictionary == null)
				{
					return hash;
				}

				foreach (var kvp in dictionary)
				{
					int keyHash = kvp.Key != null ? kvp.Key.GetHashCode() : 0;
					int valueHash = kvp.Value != null ? kvp.Value.GetHashCode() : 0;

					int pairHash = (keyHash * 397) ^ valueHash;
					hash += pairHash * 31;
				}

				return hash;
			}
		}
	}
}
