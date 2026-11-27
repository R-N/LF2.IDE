using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LF2.IDE
{
	public class FormFrameGenerator : Form
	{
		readonly MainForm main;
		readonly NumericUpDown count = Number(5);
		readonly NumericUpDown start = Number(0);
		readonly NumericUpDown pic = Number(60);
		readonly NumericUpDown state = Number(0);
		readonly NumericUpDown wait = Number(5);
		readonly NumericUpDown last = Number(999);
		readonly TextBox caption = new TextBox { Text = "standing", Width = 110 };
		readonly TextBox preview = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false,
			Dock = DockStyle.Fill, Font = new Font(FontFamily.GenericMonospace, 9) };
		readonly CheckBox[] enabled = new CheckBox[6];
		readonly TextBox[] blocks = new TextBox[6];
		readonly string[] names = { "bpoint", "wpoint", "bdy", "itr", "cpoint", "opoint" };

		static NumericUpDown Number(int value)
		{
			return new NumericUpDown { Minimum = -999999, Maximum = 999999, Value = value, Width = 68 };
		}
		public FormFrameGenerator(MainForm owner)
		{
			main = owner;
			Text = "Frame generator";
			ClientSize = new Size(820, 570);
			StartPosition = FormStartPosition.CenterParent;
			count.Minimum = 1;
			count.Maximum = 10000;
			var fields =
				new FlowLayoutPanel { Dock = DockStyle.Top, Height = 75, AutoScroll = true, Padding = new Padding(5) };
			AddField(fields, "Count", count);
			AddField(fields, "First frame", start);
			AddField(fields, "First pic", pic);
			AddField(fields, "Caption", caption);
			AddField(fields, "State", state);
			AddField(fields, "Wait", wait);
			AddField(fields, "Last next", last);
			var tabs = new TabControl { Dock = DockStyle.Top, Height = 140 };
			string[] defaults = { "x: 43  y: 38",
				"kind: 1  x: 27  y: 42  weaponact: 20  attacking: 0  cover: 0  dvx: 0  dvy: 0  dvz: 0",
				"kind: 0  x: 21  y: 18  w: 43  h: 62",
				"kind: 3  x: 40  y: 16  w: 25  h: 65\r\ncatchingact: 120 120  caughtact: 130 130",
				"kind: 1  x: 58  y: 38\r\ninjury: 15  vaction: 132  throwvz: -842150451  hurtable: 1  throwinjury: " +
				"-842150451  decrease: 3",
				"kind: 1  x: 73  y: 44  action: 50  dvx: 0  dvy: 0  oid: 204  facing: 0" };
			for (int i = 0; i < names.Length; i++)
			{
				var tab = new TabPage(names[i]);
				enabled[i] = new CheckBox { Text = "Include " + names[i], Dock = DockStyle.Top, Height = 24 };
				blocks[i] = new TextBox { Text = defaults[i], Dock = DockStyle.Fill, Multiline = true,
					ScrollBars = ScrollBars.Vertical };
				tab.Controls.Add(blocks[i]);
				tab.Controls.Add(enabled[i]);
				tabs.TabPages.Add(tab);
			}
			var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 38 };
			var generate = new Button { Text = "Generate preview", AutoSize = true };
			var insert = new Button { Text = "Insert", AutoSize = true };
			var close = new Button { Text = "Close", AutoSize = true };
			buttons.Controls.AddRange(new Control[] { generate, insert, close });
			Controls.Add(preview);
			Controls.Add(tabs);
			Controls.Add(fields);
			Controls.Add(buttons);
			generate.Click += (s, e) => Generate();
			insert.Click += (s, e) =>
			{
				var doc = main.ActiveDocument;
				if (doc == null)
				{
					MessageBox.Show(this, "Open a document first.");
					return;
				}
				var ed = doc.Editor;
				if (ed.ReadOnly)
				{
					MessageBox.Show(this, "The document is read-only.");
					return;
				}
				ed.Selection = ed.GetRange(ed.SelectionStart, ed.SelectionStart);
				ed.BeginAutoUndo();
				try
				{
					ed.InsertText(preview.Text);
				}
				finally
				{
					ed.EndAutoUndo();
				}
			};
			close.Click += (s, e) => Close();
			Generate();
		}
		static void AddField(FlowLayoutPanel panel, string label, Control field)
		{
			var group = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false,
				Width = field.Width + 8, Height = 55, Margin = new Padding(2) };
			group.Controls.Add(new Label { Text = label, AutoSize = true });
			group.Controls.Add(field);
			panel.Controls.Add(group);
		}
		void Generate()
		{
			var b = new StringBuilder();
			string nl = Environment.NewLine;
			for (int i = 0; i < (int)count.Value; i++)
			{
				int frame = (int)start.Value + i;
				b.Append("<frame> ").Append(frame).Append(' ').Append(caption.Text).Append(nl);
				b.Append("   pic: ")
					.Append((int)pic.Value + i)
					.Append("  next: ")
					.Append(i == (int)count.Value - 1 ? (int)last.Value : frame + 1)
					.Append("  state: ")
					.Append((int)state.Value)
					.Append("  wait: ")
					.Append((int)wait.Value)
					.Append("  dvx: 0  dvy: 0  dvz: 0  centerx: 39  centery: 79  hit_a: 0  hit_d: 0  hit_j: 0")
					.Append(nl);
				for (int j = 0; j < names.Length; j++)
					if (enabled[j].Checked)
						b.Append("   ")
							.Append(names[j])
							.Append(':')
							.Append(nl)
							.Append("      ")
							.Append(blocks[j].Text.Replace("\r\n", "\n").Replace("\n", nl + "      "))
							.Append(nl)
							.Append("   ")
							.Append(names[j])
							.Append("_end:")
							.Append(nl);
				b.Append("<frame_end>").Append(nl).Append(nl);
			}
			preview.Text = b.ToString();
		}
		public static void ShowGenerator(MainForm main)
		{
			new FormFrameGenerator(main).Show(main);
		}
	}
}
