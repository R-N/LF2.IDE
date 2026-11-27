using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LF2.IDE
{
	public sealed class DatIssue
	{
		public int Line { get; set; }
		public int Column { get; set; }
		public string Message { get; set; }
		public bool IsError { get; set; }
	}

	public static class DatValidator
	{
		static readonly Regex Tokens =
		    new Regex(@"(?<key>[\w]+(?:\([^)]*\))?):\s*(?<value>[^\s:]+)?", RegexOptions.Compiled);
		static readonly Regex Frame = new Regex(@"^<frame>\s*(\S+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		static readonly Dictionary<string, string> Ends =
		    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "<bmp_begin>", "<bmp_end>" },
			    { "<frame>", "<frame_end>" }, { "bdy:", "bdy_end:" }, { "itr:", "itr_end:" },
			    { "bpoint:", "bpoint_end:" }, { "cpoint:", "cpoint_end:" }, { "wpoint:", "wpoint_end:" },
			    { "opoint:", "opoint_end:" }, { "<stage>", "<stage_end>" }, { "<phase>", "<phase_end>" },
			    { "<weapon_strength_list>", "<weapon_strength_list_end>" }, { "layer:", "layer_end" } };
		sealed class Block
		{
			public string Open;
			public string End;
			public int Line;
			public int Column;
		}
		sealed class Reference
		{
			public int Number;
			public int Line;
			public int Column;
			public string Key;
		}
		public static List<DatIssue> Validate(string text)
		{
			var issues = new List<DatIssue>();
			var blocks = new List<Block>();
			var frames = new HashSet<int>();
			var references = new List<Reference>();
			string[] lines = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			Action<int, int, string, bool> add = (line, col, message, error) =>
			    issues.Add(new DatIssue { Line = line, Column = col, Message = message, IsError = error });
			for (int i = 0; i < lines.Length; i++)
			{
				string raw = lines[i];
				int comment = raw.IndexOf('#');
				if (comment >= 0)
					raw = raw.Substring(0, comment);
				string line = raw.Trim();
				if (line.Length == 0)
					continue;
				int col = raw.IndexOf(line, StringComparison.Ordinal);
				Match fm = Frame.Match(line);
				string opening = fm.Success ? "<frame>" : null;
				if (opening == null)
					foreach (var pair in Ends)
						if (line.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) ||
						    ((pair.Key == "<stage>" || pair.Key == "<phase>") &&
						        line.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase)))
						{
							opening = pair.Key;
							break;
						}
				if (opening != null)
				{
					bool child = opening == "bdy:" || opening == "itr:" || opening == "bpoint:" ||
					             opening == "cpoint:" || opening == "wpoint:" || opening == "opoint:" ||
					             opening == "layer:";
					while (blocks.Count > 0 &&
					       !(child && (blocks[blocks.Count - 1].Open == "<frame>" ||
					                      blocks[blocks.Count - 1].Open == "<stage>" ||
					                      blocks[blocks.Count - 1].Open == "<phase>")) &&
					       !(opening == "<phase>" && blocks[blocks.Count - 1].Open == "<stage>"))
					{
						var b = blocks[blocks.Count - 1];
						blocks.RemoveAt(blocks.Count - 1);
						add(i, col, "Expected " + b.End, true);
					}
					blocks.Add(new Block { Open = opening, End = Ends[opening], Line = i, Column = col });
					if (fm.Success)
					{
						int number;
						if (!int.TryParse(fm.Groups[1].Value, out number))
							add(i, col, "Frame number expected", true);
						else if (!frames.Add(number))
							add(i, col + fm.Groups[1].Index, "Duplicate frame " + number, true);
					}
					continue;
				}
				string close = null;
				foreach (var pair in Ends)
					if (line.Equals(pair.Value, StringComparison.OrdinalIgnoreCase) ||
					    (pair.Key == "layer:" && line.Equals("layer_end:", StringComparison.OrdinalIgnoreCase)))
					{
						close = pair.Value;
						break;
					}
				if (close != null)
				{
					int index = blocks.FindLastIndex(b => b.End.Equals(close, StringComparison.OrdinalIgnoreCase));
					if (index < 0)
						add(i, col, "Unexpected " + close, true);
					else
					{
						while (blocks.Count - 1 > index)
						{
							var b = blocks[blocks.Count - 1];
							blocks.RemoveAt(blocks.Count - 1);
							add(i, col, "Expected " + b.End, true);
						}
						blocks.RemoveAt(index);
					}
					continue;
				}
				if (blocks.Count == 0)
					continue;
				foreach (Match m in Tokens.Matches(raw))
				{
					string key = m.Groups["key"].Value.ToLowerInvariant();
					string value = m.Groups["value"].Value;
					int number;
					if (key == "name" || key == "head" || key == "small" || key == "music" || key.Contains("sound") ||
					    key.StartsWith("file(") || key == "file" || key == "pic_path")
						continue;
					double numeric;
					if (!int.TryParse(value, out number))
					{
						if (double.TryParse(value, System.Globalization.NumberStyles.Float,
						        System.Globalization.CultureInfo.InvariantCulture, out numeric))
							continue;
						add(i, m.Groups["value"].Success ? m.Groups["value"].Index : m.Index + m.Length,
						    "Number expected for " + key, true);
						continue;
					}
					if ((key == "next" || key.StartsWith("hit_")) && blocks.Exists(b => b.Open == "<frame>"))
						references.Add(
						    new Reference { Number = number, Line = i, Column = m.Groups["value"].Index, Key = key });
				}
			}
			foreach (var b in blocks)
				add(b.Line, b.Column, "Expected " + b.End, true);
			foreach (var r in references)
			{
				long n = Math.Abs((long)r.Number);
				if (n != 0 && n != 999 && n != 1000 && (n > int.MaxValue || !frames.Contains((int)n)))
					add(r.Line, r.Column, "Unknown frame " + r.Number + " referenced by " + r.Key, false);
			}
			issues.Sort((a, b) => a.Line != b.Line ? a.Line.CompareTo(b.Line) : a.Column.CompareTo(b.Column));
			return issues;
		}
	}
}
