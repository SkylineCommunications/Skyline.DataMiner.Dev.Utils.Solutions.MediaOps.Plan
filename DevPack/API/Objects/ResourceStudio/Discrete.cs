namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a value paired with a display name.
	/// </summary>
	/// <typeparam name="T">The type of the value to associate with a display name.</typeparam>
	/// <seealso cref="NumberDiscrete"/>
	/// <seealso cref="TextDiscrete"/>
	public class Discrete<T> : IEquatable<Discrete<T>>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Discrete{T}"/> class.
		/// </summary>
		protected Discrete()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Discrete{T}"/> class.
		/// </summary>
		/// <param name="value">Value of the Discrete.</param>
		/// <param name="displayName">DisplayName of the Discrete.</param>
		protected Discrete(T value, string displayName)
		{
			Value = value;
			DisplayName = displayName;
		}

		/// <summary>
		/// Value of the Discrete.
		/// </summary>
		public T Value { get; set; }

		/// <summary>
		/// DisplayName of the Discrete.
		/// </summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// Determines whether two <see cref="Discrete{T}"/> instances are equal.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>true if the instances are equal; otherwise, false.</returns>
		public static bool operator ==(Discrete<T> left, Discrete<T> right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}

			if (left is null || right is null)
			{
				return false;
			}

			return left.Equals(right);
		}

		/// <summary>
		/// Determines whether two <see cref="Discrete{T}"/> instances are not equal.
		/// </summary>
		/// <param name="left">The left instance to compare.</param>
		/// <param name="right">The right instance to compare.</param>
		/// <returns>true if the instances are not equal; otherwise, false.</returns>
		public static bool operator !=(Discrete<T> left, Discrete<T> right)
		{
			return !(left == right);
		}

		/// <summary>
		/// Returns a string that represents the current Discrete.
		/// </summary>
		/// <returns>String representation of the current Discrete instance.</returns>
		public override string ToString()
		{
			return $"{DisplayName} ({Value})";
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current Discrete.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance.</param>
		/// <returns>true if the object is a <see cref="Discrete{T}"/> and if the display name and value of both objects are equal; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not Discrete<T> other)
			{
				return false;
			}

			return Equals(other);
		}

		/// <summary>
		/// Determines whether the current instance and the specified <see cref="Discrete{T}"/> object have the same
		/// display name and value.
		/// </summary>
		/// <param name="other">The <see cref="Discrete{T}"/> object to compare with the current instance.</param>
		/// <returns>true if the display name and value of both objects are equal; otherwise, false.</returns>
		public virtual bool Equals(Discrete<T> other)
		{
			if (other == null)
			{
				return false;
			}

			return String.Equals(DisplayName, other.DisplayName) && EqualityComparer<T>.Default.Equals(Value, other.Value);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (Value != null ? Value.GetHashCode() : 0);
				hash = (hash * 23) + (DisplayName != null ? DisplayName.GetHashCode() : 0);

				return hash;
			}
		}
	}
}
