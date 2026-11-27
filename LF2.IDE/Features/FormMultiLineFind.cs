using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FastColoredTextBoxNS;

namespace LF2.IDE
{
	public class FormMultiLineFind : Form
	{
		readonly MainForm main;
		readonly TextBox find = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
			AcceptsReturn = true };
		readonly TextBox replacement = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical,
			Dock = DockStyle.Fill, AcceptsReturn = true };
		readonly CheckBox matchCase = new CheckBox { Text = "Match case", AutoSize = true };
		readonly CheckBox whitespace = new CheckBox { Text = "Ignore whitespace differences", AutoSize = true };
		readonly CheckBox up = new CheckBox { Text = "Search upward", AutoSize = true };
		readonly Label status = new Label { AutoSize = true };

		public FormMultiLineFind(MainForm owner)
		{
			main = owner;
			Text = "Multiline find and replace";
			ClientSize = new Size(610, 410);
			StartPosition = FormStartPosition.CenterParent;
			var layout =
				new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 7, Padding = new Padding(8) };
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			layout.Controls.Add(new Label { Text = "Find (? matches any character)", AutoSize = true }, 0, 0);
			layout.Controls.Add(find, 0, 1);
			layout.Controls.Add(new Label { Text = "Replace with", AutoSize = true }, 0, 2);
			layout.Controls.Add(replacement, 0, 3);
			var options = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
			options.Controls.AddRange(new Control[] { matchCase, whitespace, up });
			layout.Controls.Add(options, 0, 4);
			var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
			var next = new Button { Text = "Find Next", AutoSize = true };
			var replace = new Button { Text = "Replace", AutoSize = true };
			var all = new Button { Text = "Replace All", AutoSize = true };
			var close = new Button { Text = "Close", AutoSize = true };
			buttons.Controls.AddRange(new Control[] { next, replace, all, close });
			layout.Controls.Add(buttons, 0, 5);
			layout.Controls.Add(status, 0, 6);
			Controls.Add(layout);
			next.Click += (s, e) => FindNext();
			replace.Click += (s, e) => ReplaceOne();
			all.Click += (s, e) => ReplaceAll();
			close.Click += (s, e) => Close();
		}
		Regex Pattern()
		{
			if (find.Text.Length == 0)
			{
				status.Text = "Enter text to find.";
				return null;
			}
			var pattern = new StringBuilder();
			for (int i = 0; i < find.Text.Length; i++)
			{
				char c = find.Text[i];
				if (whitespace.Checked && char.IsWhiteSpace(c))
					continue;
				if (whitespace.Checked && pattern.Length > 0)
					pattern.Append(@"\s*");
				if (c == '?')
					pattern.Append('.');
				else
					pattern.Append(Regex.Escape(c.ToString()));
			}
			if (pattern.Length == 0)
			{
				status.Text = "Enter non-whitespace text to find.";
				return null;
			}
			return new Regex(pattern.ToString(),
				RegexOptions.Singleline | (matchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase));
		}
		FastColoredTextBox Editor()
		{
			if (main.ActiveDocument == null)
			{
				status.Text = "Open a document first.";
				return null;
			}
			return main.ActiveDocument.Editor;
		}
		void FindNext()
		{
			var ed = Editor();
			var regex = Pattern();
			if (ed == null || regex == null)
				return;
			string text = ed.Text;
			int start = ed.SelectionStart, end = start + ed.SelectionLength;
			Match chosen = null;
			if (up.Checked)
			{
				foreach (Match m in regex.Matches(text))
				{
					if (m.Index + m.Length > start)
						break;
					chosen = m;
				}
			}
			else
			{
				var m = regex.Match(text, Math.Min(end, text.Length));
				if (m.Success)
					chosen = m;
			}
			if (chosen == null)
			{
				status.Text = "No more matches.";
				return;
			}
			ed.Selection = ed.GetRange(chosen.Index, chosen.Index + chosen.Length);
			ed.DoSelectionVisible();
			status.Text = "Match found.";
		}
		void ReplaceOne()
		{
			var ed = Editor();
			var regex = Pattern();
			if (ed == null || regex == null)
				return;
			if (ed.ReadOnly)
			{
				status.Text = "Document is read-only.";
				return;
			}
			string selected = ed.SelectedText;
			Match m = regex.Match(selected);
			if (!m.Success || m.Index != 0 || m.Length != selected.Length)
			{
				FindNext();
				return;
			}
			ed.InsertText(replacement.Text);
			FindNext();
		}
		void ReplaceAll()
		{
			var ed = Editor();
			var regex = Pattern();
			if (ed == null || regex == null)
				return;
			if (ed.ReadOnly)
			{
				status.Text = "Document is read-only.";
				return;
			}
			string text = ed.Text;
			var matches = regex.Matches(text);
			int count = matches.Count;
			if (count == 0)
			{
				status.Text = "No matches.";
				return;
			}
			ed.BeginAutoUndo();
			try
			{
				for (int i = count - 1; i >= 0; i--)
				{
					Match m = matches[i];
					ed.Selection = ed.GetRange(m.Index, m.Index + m.Length);
					ed.InsertText(replacement.Text);
				}
			}
			finally
			{
				ed.EndAutoUndo();
			}
			status.Text = count + " replacements.";
		}
		public static void ShowFind(MainForm main)
		{
			new FormMultiLineFind(main).Show(main);
		}
	}
}
