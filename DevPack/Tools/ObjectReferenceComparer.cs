namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Tools
{
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	/// <summary>
	/// Compares objects by reference, ignoring any <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode"/>
	/// overrides. Useful for keying collections on objects whose equality depends on lazily read data.
	/// </summary>
	/// <typeparam name="T">The type of the objects.</typeparam>
	internal sealed class ObjectReferenceComparer<T> : IEqualityComparer<T>
		where T : class
	{
		internal static readonly ObjectReferenceComparer<T> Instance = new ObjectReferenceComparer<T>();

		private ObjectReferenceComparer()
		{
		}

		public bool Equals(T x, T y) => ReferenceEquals(x, y);

		public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
