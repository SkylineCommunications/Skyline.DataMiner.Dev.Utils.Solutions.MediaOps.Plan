namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	/// <summary>
	/// Compares objects by reference, ignoring any <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode"/> overrides.
	/// </summary>
	/// <remarks>
	/// Use this to key hash-based collections on objects whose value equality depends on mutable state (such as nodes),
	/// so an entry stays reachable after the object is modified.
	/// </remarks>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
	{
		public static readonly ReferenceEqualityComparer<T> Instance = new();

		private ReferenceEqualityComparer()
		{
		}

		public bool Equals(T x, T y) => ReferenceEquals(x, y);

		public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
