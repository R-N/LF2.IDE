using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gif.Components;
using WeifenLuo.WinFormsUI.Docking;

namespace LF2.IDE
{
	internal sealed class SpriteData : IDisposable
	{
		internal sealed class Frame
		{
			public int Number;
			public int Pic;
			public int Wait;
			public int Next;
			public int X;
			public int Y;
		}
		internal sealed class Sheet
		{
			public int First;
			public int Last;
			public int Width;
			public int Height;
			public int Columns;
			public int Rows;
			public string Path;
			public Bitmap Image;
		}
		public readonly Dictionary<int, Frame> Frames = new Dictionary<int, Frame>();
		public readonly List<Sheet> Sheets = new List<Sheet>();
		public string Head;
		public string Small;
		public Bitmap HeadImage;
		public Bitmap SmallImage;
		static readonly Regex framePattern = new Regex(@"^\s*<frame>\s+(-?\d+)", RegexOptions.IgnoreCase);
		static readonly Regex sheetPattern =
		    new Regex(@"file\s*\(\s*(\d+)\s*-\s*(\d+)\s*\)\s*:\s*(\S+)", RegexOptions.IgnoreCase);
		static readonly Regex pair = new Regex(@"([\w]+)\s*:\s*([^\s]+)");
		public static SpriteData Load(string text, string root)
		{
			var data = new SpriteData();
			Frame frame = null;
			bool header = false;
			try
			{
				foreach (string original in text.Replace("\r", "").Split('\n'))
				{
					string line = original.Split('#')[0];
					string trimmed = line.Trim();
					if (trimmed.Equals("<bmp_begin>", StringComparison.OrdinalIgnoreCase))
					{
						header = true;
						continue;
					}
					if (trimmed.Equals("<bmp_end>", StringComparison.OrdinalIgnoreCase))
					{
						header = false;
						continue;
					}
					var fm = framePattern.Match(line);
					if (fm.Success)
					{
						int number = int.Parse(fm.Groups[1].Value);
						frame = new Frame { Number = number };
						data.Frames[number] = frame;
						continue;
					}
					if (trimmed.Equals("<frame_end>", StringComparison.OrdinalIgnoreCase))
					{
						frame = null;
						continue;
					}
					if (header)
					{
						var sm = sheetPattern.Match(line);
						if (sm.Success)
						{
							var sheet = new Sheet { First = int.Parse(sm.Groups[1].Value),
								Last = int.Parse(sm.Groups[2].Value), Path = sm.Groups[3].Value };
							foreach (Match p in pair.Matches(line))
							{
								int n;
								if (!int.TryParse(p.Groups[2].Value, out n))
									continue;
								switch (p.Groups[1].Value.ToLowerInvariant())
								{
								case "w":
									sheet.Width = n;
									break;
								case "h":
									sheet.Height = n;
									break;
								case "row":
									sheet.Columns = n;
									break;
								case "col":
									sheet.Rows = n;
									break;
								}
							}
							string path = Platform.ResolvePath(root, sheet.Path);
							using (var image = Image.FromFile(path)) sheet.Image = new Bitmap(image);
							data.Sheets.Add(sheet);
						}
						else
							foreach (Match p in pair.Matches(line))
							{
								if (p.Groups[1].Value.Equals("head", StringComparison.OrdinalIgnoreCase))
									data.Head = p.Groups[2].Value;
								if (p.Groups[1].Value.Equals("small", StringComparison.OrdinalIgnoreCase))
									data.Small = p.Groups[2].Value;
							}
					}
					else if (frame != null)
						foreach (Match p in pair.Matches(line))
						{
							int n;
							if (!int.TryParse(p.Groups[2].Value, out n))
								continue;
							switch (p.Groups[1].Value.ToLowerInvariant())
							{
							case "pic":
								frame.Pic = n;
								break;
							case "wait":
								frame.Wait = n;
								break;
							case "next":
								frame.Next = n;
								break;
							case "centerx":
								frame.X = n;
								break;
							case "centery":
								frame.Y = n;
								break;
							}
						}
					else if (!header && trimmed.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) &&
					         !trimmed.Contains(":") &&
					         !data.Sheets.Any(s => s.Path.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
					{
						var sheet = new Sheet { Path = trimmed };
						using (var image = Image.FromFile(Platform.ResolvePath(root, trimmed))) sheet.Image =
						    new Bitmap(image);
						data.Sheets.Add(sheet);
					}
				}
				if (data.Head != null)
					using (var image = Image.FromFile(Platform.ResolvePath(root, data.Head))) data.HeadImage =
					    new Bitmap(image);
				if (data.Small != null)
					using (var image = Image.FromFile(Platform.ResolvePath(root, data.Small))) data.SmallImage =
					    new Bitmap(image);
				return data;
			}
			catch
			{
				data.Dispose();
				throw;
			}
		}
		public Bitmap Crop(int pic)
		{
			var s = Sheets.FirstOrDefault(x => pic >= x.First && pic <= x.Last);
			if (s == null || s.Columns <= 0 || s.Width <= 0 || s.Height <= 0)
				return null;
			int index = pic - s.First;
			var rect =
			    new Rectangle(index % s.Columns * (s.Width + 1), index / s.Columns * (s.Height + 1), s.Width, s.Height);
			if (!new Rectangle(Point.Empty, s.Image.Size).Contains(rect))
				return null;
			var bmp = s.Image.Clone(rect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			bmp.MakeTransparent(Color.Black);
			bmp.MakeTransparent(Color.Magenta);
			return bmp;
		}
		public void Dispose()
		{
			foreach (var s in Sheets)
				if (s.Image != null)
					s.Image.Dispose();
			if (HeadImage != null)
				HeadImage.Dispose();
			if (SmallImage != null)
				SmallImage.Dispose();
		}
	}
	public sealed class FormSpriteAnimator : DockContent
	{
		readonly MainForm main;
		readonly NumericUpDown start = new NumericUpDown { Maximum = 9999, Width = 65 };
		readonly NumericUpDown speed = new NumericUpDown { Minimum = 1, Maximum = 300, Value = 30, Width = 65 };
		readonly CheckBox loop = new CheckBox { Text = "Loop", Checked = true, AutoSize = true };
		readonly PictureBox canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.DimGray };
		readonly Timer timer = new Timer();
		SpriteData data;
		int current;
		int remaining;
		int generation;
		bool mirror;
		bool paused;
		readonly List<Tuple<int, bool>> played = new List<Tuple<int, bool>>();
		string sourceText;
		string sourceRoot;
		public FormSpriteAnimator(MainForm main)
		{
			this.main = main;
			Text = "Sprite animator";
			var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
			var play = new Button { Text = "Play", AutoSize = true };
			var pause = new Button { Text = "Pause", AutoSize = true };
			var stop = new Button { Text = "Stop", AutoSize = true };
			var export = new Button { Text = "Export GIF…", AutoSize = true };
			bar.Controls.Add(new Label { Text = "Start frame", AutoSize = true, Margin = new Padding(3, 8, 3, 0) });
			bar.Controls.Add(start);
			bar.Controls.Add(play);
			bar.Controls.Add(pause);
			bar.Controls.Add(stop);
			bar.Controls.Add(new Label { Text = "FPS", AutoSize = true, Margin = new Padding(3, 8, 3, 0) });
			bar.Controls.Add(speed);
			bar.Controls.Add(loop);
			bar.Controls.Add(export);
			Controls.Add(canvas);
			Controls.Add(bar);
			canvas.Paint += (s, e) =>
			{
				if (data != null && data.Frames.ContainsKey(current))
					Draw(e.Graphics, data, data.Frames[current], mirror, canvas.ClientSize);
			};
			speed.ValueChanged += (s, e) => timer.Interval = Math.Max(1, 1000 / (int)speed.Value);
			timer.Interval = 33;
			timer.Tick += Tick;
			play.Click += async (s, e) => await Play();
			pause.Click += (s, e) =>
			{
				paused = !paused;
				timer.Enabled = !paused && data != null;
				pause.Text = paused ? "Resume" : "Pause";
			};
			stop.Click += (s, e) =>
			{
				generation++;
				timer.Stop();
				current = (int)start.Value;
				mirror = false;
				canvas.Invalidate();
			};
			export.Click += async (s, e) => await Export();
			main.DockPanel.ActiveDocumentChanged += DocumentChanged;
			FormClosed += (s, e) =>
			{
				main.DockPanel.ActiveDocumentChanged -= DocumentChanged;
				generation++;
				timer.Dispose();
				if (data != null)
					data.Dispose();
			};
		}
		void DocumentChanged(object s, EventArgs e)
		{
			generation++;
			timer.Stop();
			played.Clear();
			if (data != null)
			{
				data.Dispose();
				data = null;
			}
			canvas.Invalidate();
		}
		async Task Play()
		{
			var doc = main.ActiveDocument;
			if (doc == null)
				return;
			timer.Stop();
			int gen = ++generation;
			string text = doc.Editor.Text;
			string root = Path.GetDirectoryName(Settings.Current.lfPath);
			try
			{
				var loaded = await Task.Run(() => SpriteData.Load(text, root));
				if (IsDisposed || gen != generation)
				{
					loaded.Dispose();
					return;
				}
				if (data != null)
					data.Dispose();
				data = loaded;
				sourceText = text;
				sourceRoot = root;
				current = (int)start.Value;
				mirror = false;
				played.Clear();
				if (!data.Frames.ContainsKey(current))
					throw new InvalidOperationException("Start frame does not exist.");
				remaining = Math.Max(0, data.Frames[current].Wait) + 1;
				played.Add(Tuple.Create(current, mirror));
				paused = false;
				timer.Start();
				canvas.Invalidate();
			}
			catch (Exception ex)
			{
				if (!IsDisposed)
					MessageBox.Show(this, ex.Message, "Sprite animator");
			}
		}
		void Tick(object s, EventArgs e)
		{
			if (data == null || !data.Frames.ContainsKey(current))
			{
				timer.Stop();
				return;
			}
			if (--remaining > 0)
				return;
			int next = data.Frames[current].Next;
			if (next == 1000)
			{
				timer.Stop();
				return;
			}
			if (next == 999)
				next = 0;
			mirror = next < 0;
			next = Math.Abs(next);
			bool seen = played.Any(x => x.Item1 == next && x.Item2 == mirror);
			if (!data.Frames.ContainsKey(next) || (!loop.Checked && seen))
			{
				timer.Stop();
				return;
			}
			current = next;
			remaining = Math.Max(0, data.Frames[current].Wait) + 1;
			if (!seen)
				played.Add(Tuple.Create(current, mirror));
			canvas.Invalidate();
		}
		static void Draw(Graphics g, SpriteData data, SpriteData.Frame frame, bool mirror, Size size)
		{
			using (var bmp = data.Crop(frame.Pic))
			{
				if (bmp == null)
					return;
				g.InterpolationMode = InterpolationMode.NearestNeighbor;
				int x = size.Width / 2 - (mirror ? bmp.Width - frame.X : frame.X), y = size.Height / 2 - frame.Y;
				if (mirror)
					bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
				g.DrawImageUnscaled(bmp, x, y);
			}
		}
		async Task Export()
		{
			if (data == null || played.Count == 0)
				return;
			using (var dialog = new SaveFileDialog { Filter = "GIF image|*.gif", DefaultExt = "gif" })
			{
				if (dialog.ShowDialog(this) != DialogResult.OK)
					return;
				var frames = played.ToArray();
				string text = sourceText, root = sourceRoot;
				int fps = (int)speed.Value;
				bool repeat = loop.Checked;
				Size size = canvas.ClientSize;
				string path = dialog.FileName;
				try
				{
					await Task.Run(
					    () =>
					    {
						    using (var source = SpriteData.Load(text, root))
						    {
							    var gif = new AnimatedGifEncoder();
							    if (!gif.Start(path))
								    throw new IOException("Cannot create GIF.");
							    if (repeat)
								    gif.SetRepeat(0);
							    foreach (var item in frames)
							    {
								    var frame = source.Frames[item.Item1];
								    using (var bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height)))
								    {
									    using (var g = Graphics.FromImage(bitmap))
									    {
										    g.Clear(Color.Magenta);
										    Draw(g, source, frame, item.Item2, size);
									    }
									    gif.SetTransparent(Color.Magenta);
									    gif.SetDelay((int)(1000.0 * (Math.Max(0, frame.Wait) + 1) / fps));
									    if (!gif.AddFrame(bitmap))
										    throw new IOException("Cannot encode GIF frame.");
								    }
							    }
							    if (!gif.Finish())
								    throw new IOException("Cannot finish GIF.");
						    }
					    });
				}
				catch (Exception ex)
				{
					if (!IsDisposed)
						MessageBox.Show(this, ex.Message, "Export GIF");
				}
			}
		}
	}
}
