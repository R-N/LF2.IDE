using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace LF2.IDE
{
	public sealed class FormObjectBrowser : DockContent
	{
		sealed class Entry
		{
			public int Id;
			public int Type;
			public string File;
		}
		readonly MainForm main;
		readonly ComboBox filter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
		readonly ComboBox sheets = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
		readonly ListView list =
		    new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false };
		readonly PictureBox picture = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.DimGray };
		readonly PictureBox face =
		    new PictureBox { Dock = DockStyle.Bottom, Height = 80, SizeMode = PictureBoxSizeMode.Zoom };
		readonly Label status = new Label { Dock = DockStyle.Bottom, AutoSize = true };
		List<Entry> entries = new List<Entry>();
		SpriteData data;
		int version;
		readonly PictureBox small =
		    new PictureBox { Dock = DockStyle.Bottom, Height = 48, SizeMode = PictureBoxSizeMode.Zoom };
		static readonly string[] names = { "Character", "Weapon", "Heavy weapon", "Attack", "Throw weapon", "Criminal",
			"Drink", "Background" };
		public FormObjectBrowser(MainForm main)
		{
			this.main = main;
			Text = "Object browser";
			var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
			var refresh = new Button { Text = "Refresh", AutoSize = true };
			filter.Items.Add("All types");
			filter.Items.AddRange(names);
			filter.SelectedIndex = 0;
			bar.Controls.Add(filter);
			bar.Controls.Add(refresh);
			var split = new SplitContainer { Dock = DockStyle.Fill };
			list.Columns.Add("ID", 55);
			list.Columns.Add("Type", 100);
			list.Columns.Add("File", 250);
			split.Panel1.Controls.Add(list);
			var faces = new Panel { Dock = DockStyle.Right, Width = 64 };
			face.Dock = DockStyle.Top;
			face.Height = 64;
			small.Dock = DockStyle.Top;
			faces.Controls.Add(small);
			faces.Controls.Add(face);
			split.Panel2.Controls.Add(picture);
			split.Panel2.Controls.Add(faces);
			split.Panel2.Controls.Add(sheets);
			Controls.Add(split);
			Controls.Add(status);
			Controls.Add(bar);
			if (!Platform.IsWindows)
			{
				// Mono keeps the original X11 window shape when a docked form grows.
				Resize += (s, e) =>
				{
					if (IsHandleCreated)
						BeginInvoke((Action)UpdateClip);
				};
			}
			Shown += (s, e) => BeginInvoke((Action)(() =>
			{
				if (!Platform.IsWindows)
					UpdateClip();
				if (split.Width > 100)
					split.SplitterDistance = split.Width / 2;
			}));
			filter.SelectedIndexChanged += (s, e) => Populate();
			refresh.Click += async (s, e) => await LoadEntries();
			list.SelectedIndexChanged += async (s, e) => await SelectEntry();
			list.DoubleClick += (s, e) =>
			{
				if (list.SelectedItems.Count == 0)
					return;
				try
				{
					main.Open(Platform.ResolvePath(Path.GetDirectoryName(Settings.Current.lfPath),
					              ((Entry)list.SelectedItems[0].Tag).File),
					    true);
				}
				catch (Exception ex)
				{
					MessageBox.Show(this, ex.Message, "Open object");
				}
			};
			sheets.SelectedIndexChanged += (s, e) => picture.Invalidate();
			picture.Paint += PaintSheet;
			FormClosed += (s, e) =>
			{
				version++;
				face.Image = null;
				small.Image = null;
				if (data != null)
					data.Dispose();
			};
			Shown += async (s, e) => await LoadEntries();
		}
		void UpdateClip()
		{
			if (IsDisposed)
				return;
			var previous = Region;
			Region = new Region(ClientRectangle);
			if (previous != null)
				previous.Dispose();
		}
		async Task LoadEntries()
		{
			int gen = ++version;
			status.Text = "Loading objects…";
			try
			{
				string root = Path.GetDirectoryName(Settings.Current.lfPath);
				var result = await Task.Run(
				    () =>
				    {
					    var found = new List<Entry>();
					    int section = -1;
					    foreach (string raw in File.ReadAllLines(Platform.ResolvePath(root, @"data\data.txt")))
					    {
						    string line = raw.Split('#') [0].Trim();
						    if (line.Equals("<object>", StringComparison.OrdinalIgnoreCase))
						    {
							    section = 0;
							    continue;
						    }
						    if (line.Equals("<background>", StringComparison.OrdinalIgnoreCase))
						    {
							    section = 1;
							    continue;
						    }
						    if (line.StartsWith("<") && line.Contains("_end>"))
						    {
							    section = -1;
							    continue;
						    }
						    if (section < 0)
							    continue;
						    var id = Regex.Match(line, @"\bid\s*:\s*(\d+)", RegexOptions.IgnoreCase);
						    var type = Regex.Match(line, @"\btype\s*:\s*(\d+)", RegexOptions.IgnoreCase);
						    var file = Regex.Match(line, @"\bfile\s*:\s*(\S+)", RegexOptions.IgnoreCase);
						    if (id.Success && file.Success && (section == 1 || type.Success))
							    found.Add(new Entry { Id = int.Parse(id.Groups[1].Value),
								    Type = section == 1 ? 7 : int.Parse(type.Groups[1].Value),
								    File = file.Groups[1].Value });
					    }
					    return found;
				    });
				if (IsDisposed || gen != version)
					return;
				entries = result;
				Populate();
				status.Text = result.Count + " objects";
			}
			catch (Exception ex)
			{
				if (!IsDisposed && gen == version)
					status.Text = ex.Message;
			}
		}
		void Populate()
		{
			list.BeginUpdate();
			list.Items.Clear();
			foreach (var entry in entries)
			{
				if (filter.SelectedIndex > 0 && entry.Type != filter.SelectedIndex - 1)
					continue;
				var item = new ListViewItem(entry.Id.ToString());
				item.SubItems.Add(
				    entry.Type >= 0 && entry.Type < names.Length ? names[entry.Type] : entry.Type.ToString());
				item.SubItems.Add(entry.File);
				item.Tag = entry;
				list.Items.Add(item);
			}
			list.EndUpdate();
		}
		async Task SelectEntry()
		{
			int gen = ++version;
			face.Image = null;
			small.Image = null;
			if (data != null)
			{
				data.Dispose();
				data = null;
			}
			sheets.Items.Clear();
			picture.Invalidate();
			if (list.SelectedItems.Count == 0)
				return;
			var entry = (Entry)list.SelectedItems[0].Tag;
			status.Text = "Loading " + entry.File + "…";
			try
			{
				string root = Path.GetDirectoryName(Settings.Current.lfPath);
				var loaded = await Task.Run(
				    () => SpriteData.Load(LF2DataUtils.Decrypt(Platform.ResolvePath(root, entry.File)), root));
				if (IsDisposed || gen != version)
				{
					loaded.Dispose();
					return;
				}
				data = loaded;
				foreach (var sheet in data.Sheets)
					sheets.Items.Add(sheet.Path);
				if (sheets.Items.Count > 0)
					sheets.SelectedIndex = 0;
				face.Image = data.HeadImage;
				small.Image = data.SmallImage;
				status.Text = entry.File;
			}
			catch (Exception ex)
			{
				if (!IsDisposed && gen == version)
					status.Text = ex.Message;
			}
		}
		void PaintSheet(object sender, PaintEventArgs e)
		{
			if (data == null || sheets.SelectedIndex < 0 || sheets.SelectedIndex >= data.Sheets.Count)
				return;
			var image = data.Sheets[sheets.SelectedIndex].Image;
			float scale = Math.Min(
			    (float)picture.ClientSize.Width / image.Width, (float)picture.ClientSize.Height / image.Height);
			if (scale <= 0)
				return;
			e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
			e.Graphics.DrawImage(image, new Rectangle(0, 0, (int)(image.Width * scale), (int)(image.Height * scale)));
		}
	}
}
