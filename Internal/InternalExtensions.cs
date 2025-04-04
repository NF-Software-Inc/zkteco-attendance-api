using System.Collections.Generic;

namespace zkteco_attendance_api.Internal
{
	/// <summary>
	/// Contains internal extension methods.
	/// </summary>
	internal static class InternalExtensions
	{
		/// <summary>
		/// Splits the provided source into arrays of the specified size.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The collection to split.</param>
		/// <param name="partitionSize">The amount of items to return in each array.</param>
		/// <param name="includePartial">Specifies whether to return an incomplete group at the end.</param>
		internal static IEnumerable<T[]> Partition<T>(this IEnumerable<T> source, int partitionSize, bool includePartial = true)
		{
			var buffer = new T[partitionSize];
			var n = 0;

			foreach (var item in source)
			{
				buffer[n] = item;
				n += 1;

				if (n == partitionSize)
				{
					yield return buffer;

					buffer = new T[partitionSize];
					n = 0;
				}
			}

			if (n > 0 && includePartial)
				yield return buffer;
		}
	}
}
