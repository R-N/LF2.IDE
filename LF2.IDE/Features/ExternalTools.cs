using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace LF2.IDE
{
	public class ExternalTool
	{
		public string Name { get; set; }
		public string Command { get; set; }
		public string Arguments { get; set; }
		public bool AskArguments { get; set; }
	}

	public static class ExternalTools
	{
		static string ToolsPath
		{
			get {
				return Path.Combine(Settings.SettingsDir, "tools.xml");
			}
		}
		public static void Populate(ToolStripMenuItem toolsMenu, MainForm main)
		{
			var added = new List<ToolStripItem>();
			Action rebuild = () =>
			{
				foreach (var item in added)
				{
					toolsMenu.DropDownItems.Remove(item);
					item.Dispose();
				}
				added.Clear();
				try
				{
					if (File.Exists(ToolsPath))
						using (var stream = File.OpenRead(ToolsPath))
						{
							var tools =
								(List<ExternalTool>)new XmlSerializer(typeof(List<ExternalTool>)).Deserialize(stream);
							foreach (var tool in tools)
							{
								var item = new ToolStripMenuItem(tool.Name);
								item.Click += (s, e) => Launch(tool, main);
								toolsMenu.DropDownItems.Add(item);
								added.Add(item);
							}
						}
				}
				catch (Exception ex)
				{
					var error = new ToolStripMenuItem("Cannot read tools.xml: " + ex.Message) { Enabled = false };
					toolsMenu.DropDownItems.Add(error);
					added.Add(error);
				}
				var configure = new ToolStripMenuItem("Configure External Tools…");
				configure.Click += (s, e) =>
				{
					try
					{
						if (!File.Exists(ToolsPath))
						{
							Directory.CreateDirectory(Settings.SettingsDir);
							using (var stream = new FileStream(ToolsPath, FileMode.CreateNew)) new XmlSerializer(
								typeof(List<ExternalTool>))
								.Serialize(stream, new List<ExternalTool> { new ExternalTool { Name = "Game",
									Command = "$(GameExe)", Arguments = "", AskArguments = false } });
						}
						main.Open(ToolsPath, false);
					}
					catch (Exception ex)
					{
						MessageBox.Show(main, ex.Message, "External tools");
					}
				};
				toolsMenu.DropDownItems.Add(configure);
				added.Add(configure);
			};
			toolsMenu.DropDownOpening += (s, e) => rebuild();
			rebuild();
		}
		static string Expand(string value, MainForm main)
		{
			string exe = Settings.Current.lfPath ?? "";
			return (value ?? "")
				.Replace("$(GameDir)", Path.GetDirectoryName(exe) ?? "")
				.Replace("$(GameExe)", exe)
				.Replace("$(File)", main.ActiveDocument == null ? "" : main.ActiveDocument.FilePath ?? "");
		}
		static void Launch(ExternalTool tool, MainForm main)
		{
			try
			{
				string command = Expand(tool.Command, main), arguments = Expand(tool.Arguments, main);
				if (tool.AskArguments && !Prompt(main, ref arguments))
					return;
				ProcessStartInfo info =
					!Platform.IsWindows && command.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
						? Platform.ExeStartInfo(command, arguments)
						: new ProcessStartInfo(command, arguments);
				Process.Start(info);
			}
			catch (Exception ex)
			{
				MessageBox.Show(main, ex.Message, "External tools");
			}
		}
		static bool Prompt(IWin32Window owner, ref string arguments)
		{
			using (var form = new Form { Text = "Tool arguments", ClientSize = new Size(500, 100),
				StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false })
			{
				var input = new TextBox { Text = arguments, Dock = DockStyle.Top };
				var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 35,
					FlowDirection = FlowDirection.RightToLeft };
				var ok = new Button { Text = "Run", DialogResult = DialogResult.OK };
				var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
				buttons.Controls.Add(ok);
				buttons.Controls.Add(cancel);
				form.Controls.Add(input);
				form.Controls.Add(buttons);
				form.AcceptButton = ok;
				form.CancelButton = cancel;
				if (form.ShowDialog(owner) != DialogResult.OK)
					return false;
				arguments = input.Text;
				return true;
			}
		}
	}
}
