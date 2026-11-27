using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Microsoft.VisualBasic.ApplicationServices;

namespace LF2.IDE
{
	internal sealed class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
				CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
				CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
				CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

				MainForm.stopWatch = System.Diagnostics.Stopwatch.StartNew();
				Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(true);

				using (MainForm = new MainForm(args))
				{
					if (Platform.IsWindows)
						RunSingleInstance(MainForm);
					else
						Application.Run(MainForm); // Mono ships no Microsoft.VisualBasic
				}
			}
			catch (Exception ex)
			{
				string nl = Environment.NewLine;
				string log = Path.Combine(exeDir, "FatalError.log");
				File.AppendAllText(log, ex.ToString() + nl + nl, Encoding.Default);
				Platform.Open(log);
			}
			finally
			{
				MainForm.stopWatch.Reset();
			}
		}

		public static MainForm MainForm;

		// Separate method so the VisualBasic types are only JIT-compiled on Windows.
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		static void RunSingleInstance(MainForm form)
		{
			SingleInstanceApplication.Run(form, NewInstanceHandler);
		}

		public static void NewInstanceHandler(object sender, StartupNextInstanceEventArgs e)
		{
			e.BringToForeground = true;
			MainForm.NewInstance(e.CommandLine);
		}

		public class SingleInstanceApplication : WindowsFormsApplicationBase
		{
			private SingleInstanceApplication()
			{
				base.IsSingleInstance = true;
			}

			public static void Run(Form form, StartupNextInstanceEventHandler startupHandler)
			{
				SingleInstanceApplication app = new SingleInstanceApplication();
				app.MainForm = form;
				app.StartupNextInstance += startupHandler;
				app.Run(Environment.GetCommandLineArgs());
			}
		}

		public static readonly string exeDir = Path.GetDirectoryName(Application.ExecutablePath);

		public static readonly string langPath = Path.Combine(exeDir, "data.lang.xml");
		public static readonly string dockingPath = Path.Combine(Settings.SettingsDir, "docking.xml");
		public static readonly string nearDockingPath = Path.Combine(exeDir, "docking.xml");
		public static readonly string utilDir = Path.Combine(exeDir, "DataUtils");
		public static readonly string plugDir = Path.Combine(exeDir, "Plugins");
		public static readonly string templateDir = Path.Combine(exeDir, "Templates");
		public const string githubPage = "https://github.com/ahmetsait/LF2.IDE";
		public const string downloadPage = "https://github.com/ahmetsait/LF2.IDE/releases";
		public const string webPage = "http://www.lf-empire.de/forum/showthread.php?tid=9064";
		public const string updateInfoLink = "https://raw.githubusercontent.com/ahmetsait/LF2.IDE/master/UPDATE";

		public static readonly bool preRelease = false;

		public const string Title = "LF2 IDE";
	}
}
