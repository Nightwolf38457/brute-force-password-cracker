using System;
using System.Collections.Generic;

namespace BruteForce
{
	/// <summary>
	/// Generates all possible combinations of characters from length 1 up to maxLength.
	/// Does NOT know the target password length in advance.
	/// </summary>
	public class BruteForceGenerator
	{
		public static readonly char[] CHARSET =
			"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

		public const int MAX_LENGTH = 6;
		private readonly int _base;

		public BruteForceGenerator()
		{
			_base = CHARSET.Length;
		}

		/// <summary>
		/// Returns the total number of combinations for lengths 1..maxLen.
		/// </summary>
		public long GetTotalCombinations(int maxLen = MAX_LENGTH)
		{
			long total = 0;
			long power = 1;
			for (int l = 1; l <= maxLen; l++)
			{
				power *= _base;
				total += power;
			}
			return total;
		}

		/// <summary>
		/// Gets the combination string at a given global index.
		/// Index 0 = "a", index 61 = "9", index 62 = "aa", etc.
		/// </summary>
		public string GetCombinationAt(long globalIndex)
		{
			long offset = 0;
			long power = 1;
			int length = 1;

			while (length <= MAX_LENGTH)
			{
				power = Pow(_base, length);
				if (globalIndex < offset + power)
					break;
				offset += power;
				length++;
			}

			if (length > MAX_LENGTH)
				return null;

			long indexWithinLength = globalIndex - offset;
			char[] result = new char[length];
			for (int i = length - 1; i >= 0; i--)
			{
				result[i] = CHARSET[indexWithinLength % _base];
				indexWithinLength /= _base;
			}
			return new string(result);
		}

		/// <summary>
		/// Generates all combinations sequentially (used for single-thread mode).
		/// </summary>
		public IEnumerable<string> GenerateCombinations(int maxLen = MAX_LENGTH)
		{
			for (int length = 1; length <= maxLen; length++)
			{
				foreach (string combo in GenerateForLength(length))
					yield return combo;
			}
		}

		private IEnumerable<string> GenerateForLength(int length)
		{
			long total = Pow(_base, length);
			char[] buffer = new char[length];
			for (long i = 0; i < total; i++)
			{
				long idx = i;
				for (int j = length - 1; j >= 0; j--)
				{
					buffer[j] = CHARSET[idx % _base];
					idx /= _base;
				}
				yield return new string(buffer);
			}
		}

		private long Pow(long b, int exp)
		{
			long result = 1;
			for (int i = 0; i < exp; i++) result *= b;
			return result;
		}
	}
}