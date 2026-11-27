using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FastColoredTextBoxNS;
using WeifenLuo.WinFormsUI.Docking;

namespace LF2.IDE
{
	public sealed class FormErrors : DockContent
	{
		readonly MainForm main;
		readonly ListView list =
		    new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false };
		readonly Timer debounce = new Timer { Interval = 500 };
		DocumentForm document;
		int version;
		public FormErrors(MainForm main)
		{
			this.main = main;
			Text = "Errors";
			list.Columns.Add("Line", 65);
			list.Columns.Add("Column", 65);
			list.Columns.Add("Message", 600);
			Controls.Add(list);
			if (!Platform.IsWindows)
			{
				// Mono keeps the original X11 window shape when a docked form grows.
				Resize += (s, e) =>
				{
					if (IsHandleCreated)
						BeginInvoke((Action)UpdateClip);
				};
				Shown += (s, e) => BeginInvoke((Action)UpdateClip);
			}
			list.DoubleClick += Navigate;
			list.KeyDown += Export;
			debounce.Tick += async (s, e) =>
			{
				debounce.Stop();
				var doc = document;
				if (doc == null)
					return;
				string text = doc.Editor.Text;
				int current = ++version;
				var issues = await Task.Run(() => DatValidator.Validate(text));
				if (IsDisposed || current != version || doc != document)
					return;
				list.BeginUpdate();
				list.Items.Clear();
				foreach (var issue in issues)
				{
					var item = new ListViewItem((issue.Line + 1).ToString());
					item.SubItems.Add((issue.Column + 1).ToString());
					item.SubItems.Add((issue.IsError ? "Error: " : "Warning: ") + issue.Message);
					item.Tag = issue;
					list.Items.Add(item);
				}
				list.EndUpdate();
			};
			main.DockPanel.ActiveDocumentChanged += ActiveChanged;
			ActiveChanged(this, EventArgs.Empty);
			FormClosed += (s, e) =>
			{
				main.DockPanel.ActiveDocumentChanged -= ActiveChanged;
				if (document != null)
					document.Editor.TextChanged -= Changed;
				debounce.Dispose();
				version++;
			};
		}
		void UpdateClip()
		{
			if (IsDisposed)
				return;
			var previous = Region;
			Region = new System.Drawing.Region(ClientRectangle);
			if (previous != null)
				previous.Dispose();
		}
		void ActiveChanged(object sender, EventArgs e)
		{
			if (document != null)
				document.Editor.TextChanged -= Changed;
			document = main.ActiveDocument;
			version++;
			list.Items.Clear();
			if (document != null)
			{
				document.Editor.TextChanged += Changed;
				debounce.Stop();
				debounce.Start();
			}
		}
		void Changed(object sender, TextChangedEventArgs e)
		{
			version++;
			debounce.Stop();
			debounce.Start();
		}
		void Navigate(object sender, EventArgs e)
		{
			if (document == null || list.SelectedItems.Count == 0)
				return;
			var issue = (DatIssue)list.SelectedItems[0].Tag;
			var editor = document.Editor;
			editor.Navigate(issue.Line);
			editor.Selection.Start = new Place(0, issue.Line);
			editor.Selection.End = new Place(editor.GetLineLength(issue.Line), issue.Line);
			editor.Focus();
		}
		void Export(object sender, KeyEventArgs e)
		{
			if (!e.Control || e.KeyCode != Keys.S)
				return;
			e.SuppressKeyPress = true;
			using (var dialog = new SaveFileDialog { Filter = "Text files|*.txt",
				DefaultExt = "txt" }) if (dialog.ShowDialog(this) == DialogResult.OK)
			{
				var text = new StringBuilder();
				foreach (ListViewItem item in list.Items)
					text.AppendLine(item.Text + ":" + item.SubItems[1].Text + " " + item.SubItems[2].Text);
				try
				{
					File.WriteAllText(dialog.FileName, text.ToString());
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Export errors");
				}
			}
		}
	}
}
