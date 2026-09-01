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
	/// <seealso cref="BooleanResolvedValue"/>
	/// <seealso cref="DecimalResolvedValue"/>
	/// <seealso cref="DoubleResolvedValue"/>
	/// <seealso cref="NullResolvedValue"/>
	/// <seealso cref="StringResolvedValue"/>
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
		/// Determines whether this resolved value holds a boolean and, if so, returns it as a <see cref="BooleanResolvedValue"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the current resolved value as a <see cref="BooleanResolvedValue"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this resolved value is a <see cref="BooleanResolvedValue"/>; otherwise, <c>false</c>.</returns>
		public bool IsBooleanResolvedValue(out BooleanResolvedValue value)
		{
			value = this as BooleanResolvedValue;
			return value != null;
		}

		/// <summary>
		/// Determines whether this resolved value holds a decimal and, if so, returns it as a <see cref="DecimalResolvedValue"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the current resolved value as a <see cref="DecimalResolvedValue"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this resolved value is a <see cref="DecimalResolvedValue"/>; otherwise, <c>false</c>.</returns>
		public bool IsDecimalResolvedValue(out DecimalResolvedValue value)
		{
			value = this as DecimalResolvedValue;
			return value != null;
		}

		/// <summary>
		/// Determines whether this resolved value holds a double and, if so, returns it as a <see cref="DoubleResolvedValue"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the current resolved value as a <see cref="DoubleResolvedValue"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this resolved value is a <see cref="DoubleResolvedValue"/>; otherwise, <c>false</c>.</returns>
		public bool IsDoubleResolvedValue(out DoubleResolvedValue value)
		{
			value = this as DoubleResolvedValue;
			return value != null;
		}

		/// <summary>
		/// Determines whether this resolved value holds no value and, if so, returns it as a <see cref="NullResolvedValue"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the current resolved value as a <see cref="NullResolvedValue"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this resolved value is a <see cref="NullResolvedValue"/>; otherwise, <c>false</c>.</returns>
		public bool IsNullResolvedValue(out NullResolvedValue value)
		{
			value = this as NullResolvedValue;
			return value != null;
		}

		/// <summary>
		/// Determines whether this resolved value holds a string and, if so, returns it as a <see cref="StringResolvedValue"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the current resolved value as a <see cref="StringResolvedValue"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this resolved value is a <see cref="StringResolvedValue"/>; otherwise, <c>false</c>.</returns>
		public bool IsStringResolvedValue(out StringResolvedValue value)
		{
			value = this as StringResolvedValue;
			return value != null;
		}

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
