using System;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LF2.IDE
{
	public partial class FormEventLog : WeifenLuo.WinFormsUI.Docking.DockContent
	{
		public FormEventLog()
		{
			InitializeComponent();
		}

		public void Error(Exception ex, string title)
		{
			string error = ex.ToString();

			if (this.InvokeRequired)
				this.BeginInvoke((Action)(() => MessageBox.Show(error, "[Async] " + title, MessageBoxButtons.OK, MessageBoxIcon.Error)));
			else
				MessageBox.Show(error, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
			try
			{
				Log(error, title, true);
			}
			catch { }
		}

		public void Log(string logMessage, string logTitle, bool stepCaret = true)
		{
			Action act = (Action)(() =>
				{
					int caret = scintilla.LinesCount - 1;
					Log(logTitle, false);
					scintilla.AppendText(Environment.NewLine + logMessage);
					StepCaret(stepCaret);
					StepCaret(caret, stepCaret);
				});

			try
			{
				if (this.InvokeRequired)
					this.BeginInvoke(act);
				else
					act();
			}
			catch (AccessViolationException) { }
		}

		public void Log(string logMessage, bool stepCaret = true)
		{
			Action<string> act = (Action<string>)((string msg) =>
				{
					if (!string.IsNullOrEmpty(scintilla.Text))
						scintilla.AppendText(Environment.NewLine);
					scintilla.AppendText(msg);
					StepCaret(stepCaret);
				});

			try
			{
				if (scintilla.InvokeRequired)
					scintilla.BeginInvoke(act, logMessage);
				else
					act(logMessage);
			}
			catch (AccessViolationException) { }
		}

		private void StepCaret(bool condition = true)
		{
			StepCaret(scintilla.LinesCount - 1, condition);
		}

		private void StepCaret(int caretLine, bool condition = true)
		{
			if (condition)
			{
				scintilla.Navigate(caretLine);
				scintilla.Refresh();
			}
		}

		public void Clean()
		{
			scintilla.ResetText();
			scintilla.Refresh();
		}

		void scintilla_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			Platform.Open(e.LinkText);
		}


		private void scintilla_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			string link = scintilla.Lines[scintilla.Selection.Start.iLine].Trim();
			if (link.StartsWith("http://"))
				scintilla_LinkClicked(scintilla, new LinkClickedEventArgs(link));
		}

		private void scintilla_KeyDown(object sender, KeyEventArgs e)
		{
			if (!(e.Modifiers == Keys.Control && e.KeyCode == Keys.C))
				e.SuppressKeyPress = e.Handled = true;
		}

		private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			//e.Cancel = true;
		}

		private void copySelectedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scintilla.Copy();
		}

		private void copyAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (scintilla.TextLength > 0)
				Clipboard.SetText(scintilla.Text);
		}
	}
}
