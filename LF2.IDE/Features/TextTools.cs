using System;
using System.Text.RegularExpressions;
using FastColoredTextBoxNS;

namespace LF2.IDE
{
	public static class TextTools
	{
		public static void DeleteEmptyLines(FastColoredTextBox ed)
		{
			Apply(ed, @"(?m)^[ \t]*(?:\r?\n|$)", "");
		}

		public static void TrimTrailingSpaces(FastColoredTextBox ed)
		{
			Apply(ed, @"[ \t]+(?=\r?$)", "");
		}

		static void Apply(FastColoredTextBox ed, string pattern, string replacement)
		{
			if (ed == null || ed.ReadOnly) return;
			string text = ed.Text;
			string result = Regex.Replace(text, pattern, replacement, RegexOptions.Multiline);
			if (result == text) return;
			int caret = ed.SelectionStart;
			int newCaret = caret;
			foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Multiline))
			{
				if (match.Index >= caret) break;
				newCaret -= Math.Min(match.Length, caret - match.Index);
			}
			ed.BeginAutoUndo();
			try { ed.Selection = ed.GetRange(0, text.Length); ed.InsertText(result); }
			finally { ed.EndAutoUndo(); }
			ed.Selection = ed.GetRange(Math.Min(newCaret, ed.Text.Length), Math.Min(newCaret, ed.Text.Length));
			ed.DoSelectionVisible();
		}
	}
}
