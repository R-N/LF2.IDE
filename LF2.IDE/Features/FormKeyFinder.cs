using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LF2.IDE
{
	public class FormKeyFinder : Form
	{
		readonly TextBox keyBox =
			new TextBox { Dock = DockStyle.Top, ReadOnly = true, BackColor = Color.White, ForeColor = Color.Black };
		readonly TextBox preview = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
			ScrollBars = ScrollBars.Both, WordWrap = false, Font = new Font(FontFamily.GenericMonospace, 9),
			BackColor = Color.White, ForeColor = Color.Black };
		readonly Label status =
			new Label { Dock = DockStyle.Top, Height = 25, Text = "Choose an encrypted data file." };
		readonly Button browse = new Button { Text = "Choose .dat…", AutoSize = true };
		readonly Button use =
			new Button { Text = "Use as decryption+encryption key", AutoSize = true, Enabled = false };
		readonly Button copy = new Button { Text = "Copy", AutoSize = true, Enabled = false };
		string key;

		public FormKeyFinder()
		{
			Text = "Find data key";
			ClientSize = new Size(720, 470);
			StartPosition = FormStartPosition.CenterParent;
			var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38 };
			buttons.Controls.AddRange(new Control[] { browse, use, copy });
			Controls.Add(preview);
			Controls.Add(keyBox);
			Controls.Add(status);
			Controls.Add(buttons);
			browse.Click += Choose;
			copy.Click += (s, e) => Clipboard.SetText(keyBox.Text);
			use.Click += (s, e) =>
			{
				try
				{
					Settings.Current.encryptionKey = key;
					Settings.Current.decryptionKey = key;
					Settings.Current.Save();
					status.Text = "Key saved.";
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Could not save key");
				}
			};
		}

		public static void ShowFinder(IWin32Window owner)
		{
			new FormKeyFinder().Show(owner);
		}

		void Choose(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog { Filter = "LF2 data (*.dat)|*.dat|All files (*.*)|*.*" })
			{
				string dir = Path.GetDirectoryName(Settings.Current.lfPath);
				if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					dialog.InitialDirectory = dir;
				if (dialog.ShowDialog(this) != DialogResult.OK)
					return;
				string path = dialog.FileName;
				key = null;
				keyBox.Clear();
				preview.Clear();
				use.Enabled = copy.Enabled = browse.Enabled = false;
				status.Text = "Recovering key…";
				var worker = new BackgroundWorker();
				worker.DoWork += (s, args) =>
				{
					string found = DataKey.Recover(path);
					byte[] bytes = found == null ? null : File.ReadAllBytes(path);
					args.Result = new object[] { found, bytes };
				};
				worker.RunWorkerCompleted += (s, args) =>
				{
					if (IsDisposed)
						return;
					browse.Enabled = true;
					if (args.Error != null)
					{
						status.Text = args.Error.Message;
						return;
					}
					var result = (object[])args.Result;
					key = (string)result[0];
					if (key == null)
					{
						status.Text = "No plausible key found.";
						return;
					}
					keyBox.Text = string.Concat(
						key.Select(c => c < 32 || c > 126 ? "\\x" + ((int)c).ToString("X2") : c.ToString()));
					byte[] bytes = (byte[])result[1];
					var text = new StringBuilder();
					int lines = 0;
					for (int i = 123; i < bytes.Length && lines < 40; i++)
					{
						char c = (char)((bytes[i] - key[(i - 123) % key.Length]) & 255);
						text.Append(c);
						if (c == '\n')
							lines++;
					}
					preview.Text = text.ToString();
					use.Enabled = copy.Enabled = true;
					status.Text = "Key recovered. Check the preview before using it.";
				};
				worker.RunWorkerAsync();
			}
		}
	}
}
