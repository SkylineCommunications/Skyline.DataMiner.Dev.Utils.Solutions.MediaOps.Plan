namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the result of a <see cref="ReferenceResolver"/> resolve call.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use <see cref="IsResolved"/> to determine whether the resolve succeeded.
	/// When <see langword="true"/>, cast to the appropriate concrete subtype
	/// (<see cref="StringResolvedValue"/>, <see cref="DecimalResolvedValue"/>, <see cref="BooleanResolvedValue"/>,
	/// or <see cref="NullResolvedValue"/>) to access a strongly-typed value.
	/// When <see langword="false"/>, inspect <see cref="UnresolvedReference"/> for diagnostics.
	/// </para>
	/// </remarks>
	public abstract class ResolvedValue
	{
		/// <summary>Initializes a new instance of the <see cref="ResolvedValue"/> class.</summary>
		protected ResolvedValue()
		{
		}

		/// <summary>
		/// Gets the <see cref="DataReference"/> that could not be resolved any further.
		/// Only valid when <see cref="IsResolved"/> is <see langword="false"/>.
		/// </summary>
		public DataReference UnresolvedReference { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the reference was fully resolved.
		/// When <see langword="false"/>, inspect <see cref="UnresolvedReference"/> for details.
		/// </summary>
		public bool IsResolved => UnresolvedReference == null;

		/// <summary>
		/// Creates an unresolved <see cref="ResolvedValue"/> wrapping a <see cref="DataReference"/>
		/// that could not be resolved any further.
		/// </summary>
		/// <param name="reference">The reference that could not be resolved.</param>
		/// <returns>
		/// A <see cref="NullResolvedValue"/> whose <see cref="IsResolved"/> is <see langword="false"/>
		/// and whose <see cref="UnresolvedReference"/> is set to <paramref name="reference"/>.
		/// </returns>
		public static ResolvedValue FromUnresolvedReference(DataReference reference)
		{
			return new NullResolvedValue { UnresolvedReference = reference };
		}

		/// <summary>
		/// Returns the resolved value as <see cref="object"/>.
		/// </summary>
		public virtual object GetRawValue()
		{
			return null;
		}
	}
}
