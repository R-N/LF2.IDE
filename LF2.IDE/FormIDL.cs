using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LF2.IDE
{
	public partial class FormIDL : Form
	{
		public FormIDL(MainForm main)
		{
			mainForm = main;
			InitializeComponent();
		}

		MainForm mainForm;


		private void buttonLoad_Click(object sender, EventArgs e)
		{
			if (mainForm.ActiveDocument == null)
			{
				label_Result.Text = "No active document";
				return;
			}
			if (mainForm.ActiveDocument.DocumentType != DocumentType.BgData && mainForm.ActiveDocument.DocumentType != DocumentType.ObjectData && mainForm.ActiveDocument.DocumentType != DocumentType.StageData)
			{
				label_Result.Text = "Unsupported document type: " + mainForm.ActiveDocument.DocumentType;
				return;
			}
			int result = int.MinValue;
			try
			{
				if (comboBox_Process.SelectedIndex < 0 || 
					(comboBox_DataType.SelectedIndex < 0) || 
					(comboBox_DataType.SelectedIndex == 0 && (comboBox_ObjId.SelectedIndex < 0 || comboBox_ObjType.SelectedIndex < 0)) || 
					(comboBox_DataType.SelectedIndex == 2 && (comboBox_BgId.SelectedIndex < 0)))
					return;

				string dat = mainForm.ActiveDocument.Editor.Text;
				
				Process p = procs[comboBox_Process.SelectedIndex];
				if(p.HasExited)
				{
					RefreshProcessList();
					return;
				}
				p = Process.GetProcessById(p.Id);

				try
				{
					Clean();
					result =  IDL.InstantLoad(
						dat,
						p.Id,
						(DataType)comboBox_DataType.SelectedIndex,
						comboBox_DataType.SelectedIndex == 0 ? comboBox_ObjId.SelectedIndex :
							comboBox_DataType.SelectedIndex == 2 ? comboBox_BgId.SelectedIndex : -1,
						(ObjectType)comboBox_ObjType.SelectedIndex,
						this.Handle,
						logFunc);
				}
				finally
				{
					if (result == 0)
					{
						label_Result.Text = "Successful";
						//SetForegroundWindow(new HandleRef(p, p.MainWindowHandle));
						//this.Close();
					}
					else if (result == 1)
					{
						label_Result.Text = "Successful (warnings)";
						StepCaret();
					}
					else
					{
						label_Result.Text = "Error code: " + result;
						StepCaret();
					}
				}
			}
			catch(ApplicationException ex)
			{
				MessageBox.Show(ex.ToString(), "Instant Data Loading Error");
			}
			catch (Exception ex)
			{
				mainForm.formEventLog.Error(ex, "Instant Data Loading Error");
			}
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void button_Refresh_Click(object sender, EventArgs e)
		{
			RefreshProcessList();
		}

		private void RefreshProcessList()
		{
			procs = Platform.FindProcesses(Settings.Current.lfPath);
			comboBox_Process.Items.Clear();
			foreach (Process proc in procs)
				comboBox_Process.Items.Add("[" + proc.Id + "] " + Settings.Current.lfPath);

			if (comboBox_Process.Items.Count == 1)
				comboBox_Process.SelectedIndex = 0;
			else if (comboBox_Process.Items.Count > 1)
				comboBox_Process.SelectedIndex = -1;
		}

		List<Process> procs;
		public static DataTxt dataTxt;
		public static DateTime dataTxtLastModification = DateTime.MinValue;
		
		private void FormIDL_Load(object sender, EventArgs e)
		{
			form = this;
			FreshLoad();
		}

		public void FreshLoad()
		{
			DocumentForm doc = mainForm.ActiveDocument;
			if(doc == null)
			{
				return;
			}
			comboBox_ObjId.SelectedIndex = -1;
			comboBox_DataType.SelectedIndex = -1;
			comboBox_ObjType.SelectedIndex = -1;
			comboBox_BgId.SelectedIndex = -1;
			label_Result.Text = "";

			RefreshProcessList();
			
			switch (doc.DocumentType)
			{
				case DocumentType.ObjectData:
					comboBox_DataType.SelectedIndex = 0;
					comboBox_DataType.Enabled = true;
					break;
				case DocumentType.StageData:
					comboBox_DataType.SelectedIndex = 1;
					comboBox_DataType.Enabled = true;
					break;
				case DocumentType.BgData:
					comboBox_DataType.SelectedIndex = 2;
					comboBox_DataType.Enabled = true;
					break;
				default:
					label_Result.Text = "Unsupported document type: " + doc.DocumentType.ToString();
					comboBox_ObjId.Enabled = false;
					comboBox_DataType.Enabled = false;
					comboBox_ObjType.Enabled = false;
					comboBox_BgId.Enabled = false;
					break;
			}
			string lfDir = Path.GetDirectoryName(Settings.Current.lfPath), dataTxtFile = Platform.ResolvePath(lfDir, @"data\data.txt");
			if (!File.Exists(dataTxtFile))
			{
				MessageBox.Show("'data.txt' could not found. Make sure you set 'LF2 Path' correct in the settings menu.", Program.Title);
				this.Close();
				return;
			}

			comboBox_BgId.Items.Clear();
			comboBox_ObjId.Items.Clear();
				
			DateTime modification = File.GetLastWriteTime(dataTxtFile);
			if (modification > dataTxtLastModification)
			{
				dataTxtFile = File.ReadAllText(dataTxtFile);
				if (IDL.ReadDataTxt(dataTxtFile, out dataTxt.objects, out dataTxt.backgrounds, this.Handle) != 0)
					return;
				
				dataTxtLastModification = modification;
			}

			for (int i = 0; i < dataTxt.objects.Length; i++)
				comboBox_ObjId.Items.Add(string.Format("id: {0}  type: {1}  file: {2}", dataTxt.objects[i].id, (byte)dataTxt.objects[i].type, dataTxt.objects[i].file));

			for (int i = 0; i < dataTxt.backgrounds.Length; i++)
				comboBox_BgId.Items.Add(string.Format("id: {0}  file: {1}", dataTxt.backgrounds[i].id, dataTxt.backgrounds[i].file));

			for(int i = 0; i < dataTxt.objects.Length; i++)
			{
				if (doc.FilePath != null ?
					doc.FilePath.EndsWith(dataTxt.objects[i].file.Replace('\\', Path.DirectorySeparatorChar), StringComparison.InvariantCultureIgnoreCase) :
					doc.TabText.EndsWith(dataTxt.objects[i].file.Replace('\\', Path.DirectorySeparatorChar), StringComparison.InvariantCultureIgnoreCase))
				{
					comboBox_ObjId.SelectedIndex = i;
					comboBox_ObjType.SelectedIndex = (int)dataTxt.objects[i].type;
					comboBox_DataType.SelectedIndex = 0;
					break;
				}
			}

			for (int i = 0; i < dataTxt.backgrounds.Length; i++)
			{
				if (doc.FilePath != null ?
					doc.FilePath.EndsWith(dataTxt.backgrounds[i].file.Replace('\\', Path.DirectorySeparatorChar), StringComparison.InvariantCultureIgnoreCase) :
					doc.TabText.EndsWith(dataTxt.backgrounds[i].file.Replace('\\', Path.DirectorySeparatorChar), StringComparison.InvariantCultureIgnoreCase))
				{
					comboBox_BgId.SelectedIndex = i;
					comboBox_DataType.SelectedIndex = 2;
					break;
				}
			}
		}

		private void comboBox_DataType_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (comboBox_DataType.SelectedIndex == 0)
			{
				comboBox_ObjType.Enabled = true;
				comboBox_ObjId.Enabled = true;
				comboBox_BgId.Enabled = false;
			}
			else if (comboBox_DataType.SelectedIndex == 1)
			{
				comboBox_ObjType.Enabled = false;
				comboBox_ObjId.Enabled = false;
				comboBox_BgId.Enabled = false;
			}
			else if (comboBox_DataType.SelectedIndex == 2)
			{
				comboBox_ObjType.Enabled = false;
				comboBox_ObjId.Enabled = false;
				comboBox_BgId.Enabled = true;
			}
		}

		public static FormIDL form;

		static readonly string[] msgStr = { "[Info] ", "[Warning] ", "[Error] " };

		public static Logger logFunc = (msg, title, msgType) =>
		{
			try
			{
				if (msg != null)
				{
					if (title == null)
						form.Log(msgStr[(byte)msgType] + msg);
					else
						form.Log(msg, title);
				}
			}
			catch { }
		};

		public void Log(string logMessage, bool stepCaret = true)
		{
			if (!string.IsNullOrEmpty(scintilla.Text))
				scintilla.AppendText(Environment.NewLine);
			scintilla.AppendText(logMessage);
			StepCaret(stepCaret);
		}

		public void Log(string logMessage, string logTitle, bool stepCaret = true)
		{
			Log(logTitle, false);
			scintilla.AppendText(Environment.NewLine + logMessage);
			StepCaret(stepCaret);
		}

		private void StepCaret(bool condition = true)
		{
			StepCaret(scintilla.LinesCount - 1, condition);
		}

		private void StepCaret(int caretLine, bool condition = true)
		{
			if (condition)
			{
				scintilla.Navigate(Math.Max(0, Math.Min(caretLine, scintilla.LinesCount - 1)));
				scintilla.Refresh();
			}
		}

		public void Clean()
		{
			scintilla.ResetText();
			scintilla.Refresh();
		}


		private void copySelectedToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scintilla.Copy();
		}

		private void copyAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(scintilla.Text))
				Clipboard.SetText(scintilla.Text);
		}

		private void FormIDL_Activated(object sender, EventArgs e)
		{
			this.Opacity = 1;
		}

		private void FormIDL_Deactivate(object sender, EventArgs e)
		{
			if (!this.Disposing)
			{
				this.Opacity = 0.75;
			}
		}
	}
}
