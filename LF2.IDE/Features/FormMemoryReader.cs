using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace LF2.IDE
{
	public sealed class FormMemoryReader : DockContent
	{
		sealed class ProcessChoice
		{
			public int Id;
			public string Name;
			public override string ToString()
			{
				return Name + " (" + Id + ")";
			}
		}
		readonly ComboBox processes = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
		readonly NumericUpDown interval =
		    new NumericUpDown { Minimum = 100, Maximum = 60000, Value = 1000, Width = 80 };
		readonly TextBox addresses = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both,
			Dock = DockStyle.Fill, AcceptsReturn = true, WordWrap = false };
		readonly ListView values = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
		readonly Label status = new Label { Dock = DockStyle.Bottom, AutoSize = true };
		readonly Timer timer = new Timer();
		ProcessMemory memory;
		int generation;
		int processGeneration;
		bool busy;
		public FormMemoryReader(MainForm main)
		{
			Text = "Memory reader";
			var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
			var refresh = new Button { Text = "Refresh processes", AutoSize = true };
			var open = new Button { Text = "Open", AutoSize = true };
			bar.Controls.Add(processes);
			bar.Controls.Add(refresh);
			bar.Controls.Add(open);
			bar.Controls.Add(new Label { Text = "Refresh (ms):", AutoSize = true, Margin = new Padding(5, 8, 2, 0) });
			bar.Controls.Add(interval);
			var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
				SplitterDistance = 140 };
			split.Panel1.Controls.Add(addresses);
			split.Panel2.Controls.Add(values);
			values.Columns.Add("Name", 170);
			values.Columns.Add("Address", 120);
			values.Columns.Add("Type", 80);
			values.Columns.Add("Value", 180);
			Controls.Add(split);
			Controls.Add(status);
			Controls.Add(bar);
			refresh.Click += async (s, e) => await LoadProcesses();
			open.Click += (s, e) => OpenProcess();
			interval.ValueChanged += (s, e) => timer.Interval = (int)interval.Value;
			timer.Interval = (int)interval.Value;
			timer.Tick += async (s, e) => await RefreshValues();
			timer.Start();
			Shown += async (s, e) => await LoadProcesses();
			FormClosed += (s, e) =>
			{
				timer.Stop();
				timer.Dispose();
				generation++;
				processGeneration++;
				if (memory != null)
					memory.Dispose();
			};
		}
		async Task LoadProcesses()
		{
			int selected = processes.SelectedItem is ProcessChoice ? ((ProcessChoice)processes.SelectedItem).Id : -1;
			int request = ++processGeneration;
			string game = Settings.Current.lfPath;
			var choices = await Task.Run(
			    () =>
			    {
				    var found = new List<ProcessChoice>();
				    var ids = new HashSet<int>();
				    try
				    {
					    foreach (var p in Platform.FindProcesses(game))
					    {
						    try
						    {
							    if (ids.Add(p.Id))
								    found.Add(new ProcessChoice { Id = p.Id, Name = p.ProcessName + " [game]" });
						    }
						    catch
						    {
						    }
						    finally
						    {
							    p.Dispose();
						    }
					    }
				    }
				    catch
				    {
				    }
				    foreach (var p in Process.GetProcesses())
				    {
					    try
					    {
						    if (ids.Add(p.Id))
							    found.Add(new ProcessChoice { Id = p.Id, Name = p.ProcessName });
					    }
					    catch
					    {
					    }
					    finally
					    {
						    p.Dispose();
					    }
				    }
				    return found;
			    });
			if (IsDisposed || request != processGeneration)
				return;
			processes.Items.Clear();
			foreach (var choice in choices)
				processes.Items.Add(choice);
			int index = choices.FindIndex(c => c.Id == selected);
			if (choices.Count > 0)
				processes.SelectedIndex = index < 0 ? 0 : index;
		}
		void OpenProcess()
		{
			var choice = processes.SelectedItem as ProcessChoice;
			if (choice == null)
				return;
			try
			{
				var next = ProcessMemory.Open(choice.Id, false);
				generation++;
				if (memory != null)
					memory.Dispose();
				memory = next;
				status.Text = "Opened " + choice;
			}
			catch (Exception ex)
			{
				status.Text = "Could not open process: " + ex.Message;
				MessageBox.Show(this, status.Text, "Memory reader");
			}
		}
		async Task RefreshValues()
		{
			if (memory == null || busy)
				return;
			busy = true;
			int current = generation;
			int pid = memory.Pid;
			string text = addresses.Text;
			try
			{
				var rows = await Task.Run(() => ReadValues(pid, text));
				if (IsDisposed || current != generation)
					return;
				values.BeginUpdate();
				values.Items.Clear();
				foreach (var row in rows)
					values.Items.Add(new ListViewItem(row));
				values.EndUpdate();
			}
			catch (Exception ex)
			{
				if (!IsDisposed && current == generation)
					status.Text = ex.Message;
			}
			finally
			{
				busy = false;
			}
		}
		static List<string[]> ReadValues(int pid, string text)
		{
			var rows = new List<string[]>();
			using (var mem = ProcessMemory.Open(pid, false)) foreach (string raw in text.Split('\n'))
			{
				string line = raw.Trim();
				if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("$"))
					continue;
				string[] parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 3)
				{
					rows.Add(new[] { line, "", "", "Expected: name address type" });
					continue;
				}
				string result;
				try
				{
					string addr = parts[1];
					int pointers = addr.Count(c => c == '*');
					addr = addr.Replace("*", "");
					if (addr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
						addr = addr.Substring(2);
					long a = long.Parse(addr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					for (int i = 0; i < pointers; i++)
						a = (uint)mem.ReadInt32(a);
					switch (parts[2].ToLowerInvariant())
					{
					case "byte":
						result = mem.ReadByte(a).ToString();
						break;
					case "word":
						result = ((ushort)mem.ReadInt16(a)).ToString();
						break;
					case "dword":
						result = ((uint)mem.ReadInt32(a)).ToString();
						break;
					default:
						result = "Unknown type";
						break;
					}
				}
				catch (Exception ex)
				{
					result = ex.Message;
				}
				rows.Add(new[] { parts[0], parts[1], parts[2], result });
			}
			return rows;
		}
	}
}
