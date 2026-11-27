using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LF2.IDE
{
	/// <summary>
	/// The few places where Windows and Linux (Mono) differ: path lookup, shell actions and launching the game.
	/// LF2 runs under Wine on Linux, so the game is started through <c>wine</c> and found by its Wine process name.
	/// </summary>
	public static class Platform
	{
		public static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

		/// <summary>
		/// Encoding for LF2's single-byte text files. Windows keeps the ANSI code page; Mono's Encoding.Default is UTF-8,
		/// which would corrupt bytes above 127, so Linux uses Latin-1 (byte-preserving).
		/// </summary>
		public static readonly System.Text.Encoding TextEncoding = IsWindows ? System.Text.Encoding.Default : System.Text.Encoding.GetEncoding(28591);

		/// <summary>
		/// Resolves an LF2-style relative path (backslashes, any letter case, e.g. <c>sprite\sys\Davis_0.bmp</c>)
		/// under <paramref name="root"/>. Each segment is matched case-insensitively on case-sensitive file systems.
		/// Returns the best-effort combined path when nothing matches, so callers can still report it.
		/// </summary>
		public static string ResolvePath(string root, string relative)
		{
			string[] parts = relative.Trim().Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
			string path = root;
			for (int i = 0; i < parts.Length; i++)
			{
				string next = Path.Combine(path, parts[i]);
				if (!IsWindows && !File.Exists(next) && !Directory.Exists(next) && Directory.Exists(path))
				{
					string match = Directory.EnumerateFileSystemEntries(path)
						.FirstOrDefault(e => string.Equals(Path.GetFileName(e), parts[i], StringComparison.OrdinalIgnoreCase));
					if (match != null)
						next = match;
				}
				path = next;
			}
			return path;
		}

		/// <summary>Opens a file, folder or URL with the desktop's default handler.</summary>
		public static void Open(string target)
		{
			if (IsWindows)
				Process.Start(target);
			else
				Process.Start(new ProcessStartInfo("xdg-open", Quote(target)) { UseShellExecute = false });
		}

		/// <summary>Shows a file in the file manager (selected on Windows; its folder elsewhere).</summary>
		public static void Reveal(string path)
		{
			if (IsWindows)
				Process.Start("explorer", "/select, \"" + path + "\"");
			else
				Open(Directory.Exists(path) ? path : Path.GetDirectoryName(path));
		}

		/// <summary>Start info for running a Windows executable (LF2 itself, or tools in the game folder).</summary>
		public static ProcessStartInfo ExeStartInfo(string exePath, string arguments = "")
		{
			var psi = IsWindows
				? new ProcessStartInfo(exePath, arguments)
				: new ProcessStartInfo("wine", Quote(exePath) + (string.IsNullOrEmpty(arguments) ? "" : " " + arguments));
			psi.UseShellExecute = false;
			psi.WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(exePath));
			return psi;
		}

		/// <summary>Starts the configured game command, or LF2 through the platform default.</summary>
		public static ProcessStartInfo GameStartInfo()
		{
			string exe = Path.GetFullPath(Settings.Current.lfPath);
			string command = (Settings.Current.runCommand ?? "").Trim();
			if (command.Length == 0) return ExeStartInfo(exe);
			command = command.Replace("$(GameDir)", Path.GetDirectoryName(exe)).Replace("$(GameExe)", exe);
			string file;
			int end;
			if (command[0] == '"' || command[0] == '\'')
			{
				char quote = command[0];
				end = command.IndexOf(quote, 1);
				if (end < 0) throw new ArgumentException("Unclosed quote in game command.");
				file = command.Substring(1, end - 1);
				end++;
			}
			else
			{
				end = 0;
				while (end < command.Length && !char.IsWhiteSpace(command[end])) end++;
				file = command.Substring(0, end);
			}
			return new ProcessStartInfo(file, command.Substring(end).TrimStart())
			{
				UseShellExecute = false,
				WorkingDirectory = Path.GetDirectoryName(exe)
			};
		}

		/// <summary>Running processes of the given executable. Under Wine the process name is the .exe file name.</summary>
		public static List<Process> FindProcesses(string exePath)
		{
			string name = Path.GetFileName(exePath);
			if (IsWindows)
				return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name)).ToList();

			var found = new List<Process>();
			// /proc/<pid>/comm holds at most 15 characters of the name.
			string comm = name.Length > 15 ? name.Substring(0, 15) : name;
			foreach (string dir in Directory.EnumerateDirectories("/proc"))
			{
				int pid;
				if (!int.TryParse(Path.GetFileName(dir), out pid))
					continue;
				try
				{
					if (string.Equals(File.ReadAllText(Path.Combine(dir, "comm")).Trim(), comm, StringComparison.OrdinalIgnoreCase))
						found.Add(Process.GetProcessById(pid));
				}
				catch (Exception) { } // process exited or is not ours
			}
			return found;
		}

		static string Quote(string s) { return "\"" + s.Replace("\"", "\\\"") + "\""; }
	}
}
