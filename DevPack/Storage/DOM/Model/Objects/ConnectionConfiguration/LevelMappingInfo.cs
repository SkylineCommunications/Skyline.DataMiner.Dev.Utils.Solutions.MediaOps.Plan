namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM
{
	using System;
	using System.Collections.Generic;

	internal sealed class LevelMappingInfo : IEquatable<LevelMappingInfo>
	{
		public LevelMappingInfo(LevelInfo from, LevelInfo to)
		{
			From = from ?? throw new ArgumentNullException(nameof(from));
			To = to ?? throw new ArgumentNullException(nameof(to));
		}

		public LevelInfo From { get; set; }

		public LevelInfo To { get; set; }

		public static bool operator ==(LevelMappingInfo left, LevelMappingInfo right)
		{
			return EqualityComparer<LevelMappingInfo>.Default.Equals(left, right);
		}

		public static bool operator !=(LevelMappingInfo left, LevelMappingInfo right)
		{
			return !(left == right);
		}

		public bool Equals(LevelMappingInfo other)
		{
			if (other == null)
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return From == other.From && To == other.To;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as LevelMappingInfo);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = (hash * 23) + (From != null ? From.GetHashCode() : 0);
				hash = (hash * 23) + (To != null ? To.GetHashCode() : 0);

				return hash;
			}
		}
	}
}
