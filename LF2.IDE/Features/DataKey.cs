using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LF2.IDE
{
	public static class DataKey
	{
		const string Standard = "odBearBecauseHeIsVeryGoodSiuHungIsAGo";
		const int Header = 123;
		static readonly string[] Tokens = { "<bmp_begin>", "<frame>", "<stage>", "<phase>", "layer:", "name:" };

		static bool Plausible(int c)
		{
			return c == 9 || c == 10 || c == 13 || (c >= 32 && c <= 126);
		}

		static bool Check(byte[] data, string key, int limit, bool token)
		{
			if (data.Length <= Header || string.IsNullOrEmpty(key) || key.Any(c => c > 255))
				return false;
			int n = Math.Min(data.Length - Header, limit), bad = 0;
			var text = token ? new StringBuilder(n) : null;
			for (int i = 0; i < n; i++)
			{
				int c = (data[Header + i] - key[i % key.Length]) & 255;
				if (!Plausible(c))
					bad++;
				if (token)
					text.Append((char)c);
			}
			if (bad * 100 > n * 2)
				return false;
			if (!token)
				return true;
			string decoded = text.ToString();
			return Tokens.Any(t => decoded.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		public static bool Decrypts(string datPath, string key)
		{
			try
			{
				return Check(File.ReadAllBytes(datPath), key, 4096, true);
			}
			catch (IOException)
			{
				return false;
			}
			catch (UnauthorizedAccessException)
			{
				return false;
			}
		}

		public static string Recover(string datPath)
		{
			byte[] data;
			try
			{
				data = File.ReadAllBytes(datPath);
			}
			catch (IOException)
			{
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
			int n = Math.Min(data.Length - Header, 32768);
			if (n < 512)
				return null;
			for (int length = 30; length <= 40; length++)
			{
				char[] key = new char[length];
				int totalBad = 0;
				for (int pos = 0; pos < length; pos++)
				{
					int[] counts = new int[256];
					for (int i = pos; i < n; i += length)
						counts[data[Header + i]]++;
					int bestBad = int.MaxValue, bestScore = int.MinValue, best = 0;
					for (int k = 1; k < 256; k++)
					{
						int bad = 0, score = 0;
						for (int c = 0; c < 256; c++)
						{
							if (counts[c] == 0)
								continue;
							int p = (c - k) & 255;
							if (!Plausible(p))
								bad += counts[c];
							else if (p == ' ' || p == ':' || p == '\r' || p == '\n' || p == '\t' ||
									 (p >= '0' && p <= '9') || (p >= 'a' && p <= 'z'))
								score += counts[c] * 2;
							else
								score += counts[c];
						}
						if (bad < bestBad || (bad == bestBad && score > bestScore))
						{
							bestBad = bad;
							bestScore = score;
							best = k;
						}
					}
					key[pos] = (char)best;
					totalBad += bestBad;
				}
				string candidate = new string(key);
				if (totalBad * 100 <= n * 2 && Check(data, candidate, 4096, true) &&
					Check(data, candidate, int.MaxValue, false))
					return candidate;
			}
			return null;
		}

		public static string DetectForFolder(string gameDir)
		{
			if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
				return null;
			var probes = new List<string>();
			try
			{
				string list = Platform.ResolvePath(gameDir, "data/data.txt");
				if (File.Exists(list))
					foreach (Match m in Regex.Matches(File.ReadAllText(list), @"(?im)\bfile\s*:\s*([^\s#]+\.dat)"))
					{
						string path = Platform.ResolvePath(gameDir, m.Groups[1].Value);
						if (File.Exists(path))
							probes.Add(path);
					}
				foreach (string name in new[] { "data", "bg" })
				{
					string dir = Platform.ResolvePath(gameDir, name);
					if (Directory.Exists(dir))
						probes.AddRange(Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
								.Where(p => string.Equals(
										   Path.GetExtension(p), ".dat", StringComparison.OrdinalIgnoreCase)));
				}
				foreach (string probe in probes.Distinct())
				{
					if (Decrypts(probe, Settings.Current.encryptionKey))
						return Settings.Current.encryptionKey;
					if (Decrypts(probe, Standard))
						return Standard;
					string key = Recover(probe);
					if (key != null)
						return key;
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			return null;
		}
	}
}
