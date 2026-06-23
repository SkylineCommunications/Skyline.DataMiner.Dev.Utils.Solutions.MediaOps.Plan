namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	internal sealed class LevelInfo : IEquatable<LevelInfo>
	{
		public long Number { get; set; }

		public static bool operator ==(LevelInfo left, LevelInfo right)
		{
			return EqualityComparer<LevelInfo>.Default.Equals(left, right);
		}

		public static bool operator !=(LevelInfo left, LevelInfo right)
		{
			return !(left == right);
		}

		public bool Equals(LevelInfo other)
		{
			if (other == null)
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return Number == other.Number;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as LevelInfo);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + Number.GetHashCode();

				return hash;
			}
		}
	}
}
