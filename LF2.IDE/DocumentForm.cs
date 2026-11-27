using FastColoredTextBoxNS;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using WeifenLuo.WinFormsUI.Docking;
using System.Diagnostics;
using System.Xml;

namespace LF2.IDE
{
	public sealed partial class DocumentForm : DockContent
	{
		private string _filePath;
		private bool _issaved;
		private DocumentType documentType = DocumentType.Default;
		private System.Timers.Timer syncTimer = new System.Timers.Timer(1000) { AutoReset = true };
		public bool justEdited = false;
		public object syncLock = new object();

		public List<SpriteSheet> spriteList = new List<SpriteSheet>(0);
		public Bitmap[] frames;
		public int frameIndexTag;
		public int frameIndexFrame;
		public int frameIndexShape;

		private MainForm mainForm;

		public DocumentType DocumentType
		{
			get { return documentType; }
		}

		public string FilePath
		{
			get { return _filePath; }
			set { _filePath = value; }
		}


		public bool Issaved
		{
			get { return _issaved; }
			set { _issaved = value; }
		}


		public FastColoredTextBox Editor { get { return scintilla; } }
		private AutocompleteMenu autocomplete;
		private string lineEnding = "\r\n";
		private string syntaxLanguage;
		private readonly List<KeyValuePair<Style, string>> syntaxRules = new List<KeyValuePair<Style, string>>();
		private readonly Style[] keywordStyles = {
			new TextStyle(Brushes.Blue, null, FontStyle.Regular),
			new TextStyle(Brushes.Green, null, FontStyle.Regular),
			new TextStyle(Brushes.DarkRed, null, FontStyle.Regular)
		};
		private static readonly string[] foldingNames = { "bmp_begin", "frame", "stage", "phase", "weapon_strength_list", "object", "bdy", "itr", "bpoint", "cpoint", "wpoint", "opoint" };
		private static readonly string[] foldingEnds = { "bmp_end", "frame_end", "stage_end", "phase_end", "weapon_strength_list_end", "object_end", "bdy_end", "itr_end", "bpoint_end", "cpoint_end", "wpoint_end", "opoint_end" };
		private sealed class NamedSnippet : SnippetAutocompleteItem
		{
			private readonly string shortcut;

			public NamedSnippet(string shortcut, string text) : base(text)
			{
				this.shortcut = shortcut;
				MenuText = shortcut + " (snippet)";
			}

			public override CompareResult Compare(string fragment)
			{
				return shortcut.StartsWith(fragment, StringComparison.OrdinalIgnoreCase) ? CompareResult.Visible : CompareResult.Hidden;
			}
		}



		#region Methods

		private void AddOrRemoveAsteric()
		{
			if (Editor.IsChanged)
			{
				if (!TabText.EndsWith(" *"))
					TabText += " *";
			}
			else
			{
				if (TabText.EndsWith(" *"))
					TabText = TabText.Substring(0, TabText.Length - 2);
			}
		}


		private void DocumentForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (Editor.IsChanged)
			{
				// Prompt if not saved
				string message = String.Format(
					CultureInfo.CurrentCulture,
					"The data in the {0} file has changed.{1}{2}Do you want to save the changes?",
					TabText.TrimEnd(' ', '*'),
					Environment.NewLine,
					Environment.NewLine);

				DialogResult dr = MessageBox.Show(this, message, Program.Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
				if (dr == DialogResult.Cancel)
				{
					// Stop closing
					e.Cancel = true;
					return;
				}
				else if (dr == DialogResult.Yes)
				{
					// Try to save before closing
					e.Cancel = !Save();
					return;
				}
			}

			// Close as normal
		}


		public bool ExportAsHtml()
		{
			using (SaveFileDialog dialog = new SaveFileDialog())
			{
				string fileName = (Text.EndsWith(" *") ? Text.Substring(0, Text.Length - 2) : Text);
				dialog.Filter = "HTML Files (*.html;*.htm)|*.html;*.htm|All Files (*.*)|*.*";
				dialog.FileName = fileName + ".html";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					ApplySyntax();
					File.WriteAllText(dialog.FileName, Editor.Html);

					return true;
				}
			}

			return false;
		}


		public void SetTextFromFile(string text)
		{
			lineEnding = text.Contains("\r\n") ? "\r\n" : "\n";
			Editor.Text = text;
			Editor.ClearUndo();
			Editor.IsChanged = false;
			AddOrRemoveAsteric();
		}

		public bool Save()
		{
			if (String.IsNullOrEmpty(_filePath))
				return SaveAs();

			return Save(_filePath);
		}

		public bool Save(string filePath)
		{
			string text = Editor.Text.Replace("\r\n", "\n").Replace("\n", lineEnding);
			if (filePath.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
				LF2DataUtils.Encrypt(text, filePath);
			else
				File.WriteAllText(filePath, text, Platform.TextEncoding);
			Editor.IsChanged = false;
			AddOrRemoveAsteric();
			return true;
		}

		public bool SaveAs()
		{
			string tabText = TabText.TrimEnd(' ', '*');
			if (!string.IsNullOrEmpty(_filePath))
			{
				saveFileDialog.FileName = Path.GetFileName(_filePath);
				saveFileDialog.InitialDirectory = Path.GetDirectoryName(_filePath);
			}
			else
			{
				saveFileDialog.FileName = tabText;
			}
			switch (Path.GetExtension(tabText))
			{
				case ".dat":
					saveFileDialog.FilterIndex = 2;
					break;
				case ".txt":
					saveFileDialog.FilterIndex = 3;
					break;
				case ".as":
					saveFileDialog.FilterIndex = 4;
					break;
				case ".xml":
					saveFileDialog.FilterIndex = 5;
					break;
				case ".html":
				case ".htm":
					saveFileDialog.FilterIndex = 6;
					break;
				case ".cs":
					saveFileDialog.FilterIndex = 7;
					break;
				default:
					saveFileDialog.FilterIndex = 1;
					break;
			}
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				ToolTipText = _filePath = saveFileDialog.FileName;
				TabText = Path.GetFileName(_filePath);

				SetLanguage();

				return Save(_filePath);
			}

			return false;
		}


		private void scintilla_ModifiedChanged(object sender, EventArgs e)
		{
			AddOrRemoveAsteric();
		}

		public DocumentForm(MainForm main)
		{
			syncTimer.Elapsed += syncTimer_Elapsed;
			mainForm = main;
			InitializeComponent();
			autocomplete = new AutocompleteMenu(Editor);
			autocomplete.Opening += (sender, e) => e.Cancel = !Settings.Current.autoComplete || !auto;
		}

		void DocumentFormActivated(object sender, EventArgs e)
		{
			Editor.Focus();
		}

		void DocumentFormLoad(object sender, EventArgs e)
		{
			ToolTipText = _filePath ?? TabText;
			SetLanguage();
			Editor.WordWrap = Settings.Current.lineWrapping;

			if (Settings.Current.lfPath == null || !File.Exists(Settings.Current.lfPath))
				return;

			if (TabText.TrimEnd(' ', '*').EndsWith(".dat"))
			{
				try
				{
					if (ParseFiles(Path.GetDirectoryName(Settings.Current.lfPath)))
					    ParseFrames();
				}
				catch (Exception ex)
				{
					mainForm.formEventLog.Error(ex, "Parsing Error");
				}
			}
		}

		public bool ParseFiles(string lfdir)
		{
			spriteList.Clear();
			string dat = scintilla.Text;
			int begin = dat.IndexOf("<bmp_begin>");
			if (begin < 0) return false;
			int end = dat.IndexOf("<bmp_end>", begin + 11);
			if (end < 0) return false;
			string script = dat.Substring(begin + 11, end - begin - 11);
			MatchCollection matches = Regex.Matches(script, SpriteSheet.regexPattern);
			if (matches.Count < 1) return false;
			for (int i = 0; i < matches.Count; i++)
			{
				string path = Platform.ResolvePath(lfdir, matches[i].Groups[3].Value.Trim());
				Bitmap img = HelperTools.GetClonedBitmap(path);
				img.Tag = Path.GetFileName(path);
				int si = int.Parse(matches[i].Groups[1].Value.Trim()),
					ei = int.Parse(matches[i].Groups[2].Value.Trim()),
					w = int.Parse(matches[i].Groups[4].Value.Trim()),
					h = int.Parse(matches[i].Groups[5].Value.Trim()),
					r = int.Parse(matches[i].Groups[6].Value.Trim()),
					c = int.Parse(matches[i].Groups[7].Value.Trim());
				SpriteSheet fm = new SpriteSheet(si, ei, img.Tag as string, img, w, h, c, r);
				spriteList.Add(fm);
			}
            return true;
		}

		public void ParseFrames()
		{
			var frames = new List<Bitmap>(256);

			for (int i = 0; i < spriteList.Count; i++)
			{
				SpriteSheet fm = spriteList[i];

				for (int r = 0; r < fm.row; r++)
				{
					for (int c = 0; c < fm.col; c++)
					{
						Bitmap btm = new Bitmap(fm.w, fm.h, PixelFormat.Format32bppRgb);
						using (Graphics g = Graphics.FromImage(btm))
							g.DrawImage(fm.sprite, new Rectangle(0, 0, fm.w, fm.h), new Rectangle(c * (fm.w + 1), r * (fm.h + 1), fm.w, fm.h), GraphicsUnit.Pixel);
						frames.Add(btm);
					}
				}
			}
			this.frames = frames.ToArray();
		}

		bool auto = false, smart = false;

		public void SetLanguage()
		{
			SetLanguageByExtension(Path.GetExtension(TabText.TrimEnd('*', ' ')));
		}

		public void SetLanguageByExtension(string ext)
		{
			switch (ext)
			{
				case ".dat":
					SetLanguage("dat");
					break;
				case ".txt":
					SetLanguage("txt");
					break;
				case ".as":
					SetLanguage("as");
					break;
				case ".xml":
					SetLanguage("xml");
					break;
				case ".html":
				case ".htm":
					SetLanguage("html");
					break;
				case ".cs":
					SetLanguage("cs");
					break;
				default:
					SetLanguage("default");
					break;
			}
		}

		public void SetLanguage(string language)
		{
			language = language.ToLowerInvariant();
			syntaxLanguage = null;
			documentType = DocumentType.Default;
			auto = false;
			Editor.Language = Language.Custom;
			Editor.TabLength = 4;
			Editor.AutoIndent = true;
			switch (language)
			{
				case "dat":
					auto = true;
					if (Editor.Text.Contains("<stage>"))
					{
						documentType = DocumentType.StageData;
						syntaxLanguage = "stage_dat";
						Editor.TabLength = 8;
					}
					else if (Editor.Text.Contains("zboundary:"))
					{
						documentType = DocumentType.BgData;
						syntaxLanguage = "bg_dat";
						Editor.TabLength = 2;
					}
					else
					{
						documentType = DocumentType.ObjectData;
						syntaxLanguage = "object_dat";
						Editor.TabLength = 3;
					}
					Icon = Properties.Resources.DocumentDAT;
					break;
				case "txt":
					if (string.Equals(TabText.TrimEnd(' ', '*'), "data.txt", StringComparison.OrdinalIgnoreCase))
					{
						documentType = DocumentType.DataTxt;
						syntaxLanguage = "data_txt";
						auto = true;
						Icon = Properties.Resources.DocumentDAT;
						break;
					}
					goto default;
				case "as":
					documentType = DocumentType.AngelScript;
					Editor.Language = Language.CSharp;
					Icon = Properties.Resources.DocumentAS;
					break;
				case "cs":
					documentType = DocumentType.CSharp;
					Editor.Language = Language.CSharp;
					Icon = Properties.Resources.DocumentCS;
					break;
				case "html":
				case "xml":
					documentType = DocumentType.XML;
					Editor.Language = language == "xml" ? Language.XML : Language.HTML;
					Editor.TabLength = 2;
					Icon = Properties.Resources.DocumentXML;
					break;
				default:
					Icon = Properties.Resources.Document;
					break;
			}
			syntaxRules.Clear();
			if (syntaxLanguage != null && File.Exists(Program.langPath))
			{
				var xml = new XmlDocument();
				xml.Load(Program.langPath);
				var node = xml.SelectSingleNode("/Languages/Language[@Name='" + syntaxLanguage + "']");
				if (node != null)
				{
					var colors = keywordStyles;
					foreach (XmlNode keywords in node.SelectNodes("Lexer/Keywords"))
					{
						int index;
						if (!int.TryParse(keywords.Attributes["List"].Value, out index) || index < 0 || index >= colors.Length)
							continue;
						var words = Regex.Split(keywords.InnerText.Trim(), @"\s+").Where(w => w.Length != 0).Select(Regex.Escape);
						syntaxRules.Add(new KeyValuePair<Style, string>(colors[index], @"\b(?:" + string.Join("|", words.ToArray()) + @")\b"));
					}
					var list = node.SelectSingleNode("AutoComplete/List");
					var items = new List<AutocompleteItem>();
					if (list != null)
						foreach (string word in Regex.Split(list.InnerText.Trim(), @"\s+"))
							if (word.Length != 0) items.Add(new AutocompleteItem(word));
					foreach (XmlNode snippet in node.SelectNodes("Snippets/Snippet"))
					{
						string text = Regex.Replace(snippet.InnerText, @"\$([^$]+)\$", m => m.Groups[1].Value == "end" ? "^" : m.Groups[1].Value);
						if (!text.Contains("^")) text += "^";
						items.Add(new NamedSnippet(snippet.Attributes["Shortcut"].Value, text));
					}
					autocomplete.Items.SetAutocompleteItems(items);
				}
			}
			ApplySyntax();
		}

		private void ApplySyntax()
		{
			if (syntaxLanguage == null)
				return;
			Editor.Range.ClearStyle(StyleIndex.All);
			foreach (var rule in syntaxRules)
				Editor.Range.SetStyle(rule.Key, rule.Value, RegexOptions.IgnoreCase);
			Editor.Range.ClearFoldingMarkers();
			for (int i = 0; i < foldingNames.Length; i++)
			{
				string start = @"(?im)^[ \t]*" + (i < 6 ? "<" + foldingNames[i] + ">" : foldingNames[i] + ":");
				string end = @"(?im)^[ \t]*" + (i < 6 ? "<" + foldingEnds[i] + ">" : foldingEnds[i] + ":");
				Editor.Range.SetFoldingMarkers(start, end);
			}
		}

		public void FoldLevel(int level, bool fold)
		{
			ApplySyntax();
			if (level == 0)
			{
				if (fold) Editor.CollapseAllFoldingBlocks();
				else Editor.ExpandAllFoldingBlocks();
				return;
			}
			int depth = 0;
			for (int i = 0; i < Editor.LinesCount; i++)
			{
				if (!string.IsNullOrEmpty(Editor[i].FoldingEndMarker))
					depth = Math.Max(0, depth - 1);
				if (!string.IsNullOrEmpty(Editor[i].FoldingStartMarker))
				{
					depth++;
					if (depth == level)
					{
						if (fold) Editor.CollapseFoldingBlock(i);
						else if (Editor.LineInfos[i].VisibleState == FastColoredTextBoxNS.VisibleState.StartOfHiddenBlock) Editor.ExpandFoldedBlock(i);
					}
				}
			}
		}

		void EditorTextChangedDelayed(object sender, TextChangedEventArgs e)
		{
			ApplySyntax();
		}

		void ScintillaTextChanged(object sender, TextChangedEventArgs e)
		{
			AddOrRemoveAsteric();
			if (Monitor.TryEnter(syncLock))
			{
				try
				{
					justEdited = true;
					syncTimer.Start();
				}
				finally { Monitor.Exit(syncLock); }
			}
			else
				syncTimer.Stop();
		}

		public void SetMarginAuto()
		{
			Editor.ShowLineNumbers = true;
		}

		#endregion Methods

		private void scintilla_SelectionChanged(object sender, EventArgs e)
		{
			Place caret = Editor.Selection.Start;
			toolStripStatusLabel_Line.Text = (caret.iLine + 1).ToString();
			toolStripStatusLabel_Col.Text = (caret.iChar + 1).ToString();
			toolStripStatusLabel_Ch.Text = (caret.iChar + 1).ToString();
			toolStripStatusLabel_SelLen.Text = Editor.Selection.Length.ToString();
			int sel = Math.Abs(Editor.Selection.End.iLine - caret.iLine);
			toolStripStatusLabel_SelLines.Text = (Editor.Selection.Length > 0 ? sel + 1 : 0).ToString();
			if (!justEdited)
				SyncToDesign(auto: true);
			mainForm.UpdateNavigationControls();
		}

		private void scintilla_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Z && e.Modifiers == (Keys.Control | Keys.Shift))
			{
				Editor.Redo();
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		// God save us from ever needing to write this kind of creepy code
		public bool SyncToDesign(bool auto = false)
		{
			if (documentType != DocumentType.ObjectData || auto && !mainForm.formDesign.checkBoxLinkage.Checked || !Monitor.TryEnter(syncLock))
				return false;
			try
			{
				int caret = Editor.PlaceToPosition(Editor.Selection.Start);
				int line = Editor.Selection.Start.iLine;
				int lineEnd = Editor.PlaceToPosition(new Place(Editor.Lines[line].Length, line));
				if (Editor.Text.Length == 0)
					return false;
				int fs = Editor.Text.LastIndexOf("<frame>", Math.Min(lineEnd, Editor.Text.Length - 1));
				if (fs < 0)
					return false;
				int fe = Editor.Text.IndexOf("<frame_end>", fs + 7);
				if (fe < 0 || fe + 11 < caret)
					return false;
				var fr = new FastColoredTextBoxNS.Range(Editor, Editor.PositionToPlace(fs), Editor.PositionToPlace(fe + 11));
				{
					var frame = LF2DataUtils.ReadFrame(fr.Text);
					mainForm.formDesign.EditIn();
					if (frame.pic.HasValue && frame.pic.Value <= mainForm.formDesign.numericUpDown_ImageIndex.Maximum)
						mainForm.formDesign.numericUpDown_ImageIndex.Value = frame.pic.Value;
					mainForm.formDesign.textBox_caption.Text = frame.caption;
					if (frame.bdys != null)
					{
						var bi = mainForm.formDesign.tagBox.ActiveBdyIndex;
						mainForm.formDesign.tagBox.BdyTags = new List<TagBox.Bdy>(frame.bdys.Select<LF2DataUtils.Bdy, TagBox.Bdy>((LF2DataUtils.Bdy bdy) => (TagBox.Bdy)bdy));
						if (mainForm.formDesign.tagBox.BdyTags.Count == 1)
							mainForm.formDesign.tagBox.ActiveBdyIndex = 0;
						else if (bi.HasValue && bi.Value < mainForm.formDesign.tagBox.BdyTags.Count)
							mainForm.formDesign.tagBox.ActiveBdyIndex = bi;
						else
							mainForm.formDesign.tagBox.ActiveBdyIndex = null;
						if (mainForm.formDesign.tagBox.ActiveBdyIndex.HasValue)
						{
							LF2DataUtils.Bdy bdy = frame.bdys[mainForm.formDesign.tagBox.ActiveBdyIndex.Value];
							if (bdy != null)
							{
								mainForm.formDesign.bdy_h.Text = bdy.h.HasValue ? bdy.h.ToString() : "";
								mainForm.formDesign.bdy_kind.Text = bdy.kind.HasValue ? bdy.kind.ToString() : "";
								mainForm.formDesign.bdy_w.Text = bdy.w.HasValue ? bdy.w.ToString() : "";
								mainForm.formDesign.bdy_x.Text = bdy.x.HasValue ? bdy.x.ToString() : "";
								mainForm.formDesign.bdy_y.Text = bdy.y.HasValue ? bdy.y.ToString() : "";
							}
						}
					}
					else
					{
						mainForm.formDesign.tagBox.ClearBdyList();
						mainForm.formDesign.bdy_h.Text = 
						mainForm.formDesign.bdy_kind.Text = 
						mainForm.formDesign.bdy_w.Text = 
						mainForm.formDesign.bdy_x.Text = 
						mainForm.formDesign.bdy_y.Text = "";
					}
					if (frame.itrs != null)
					{
						var ti = mainForm.formDesign.tagBox.ActiveItrIndex;
						mainForm.formDesign.tagBox.ItrTags = new List<TagBox.Itr>(frame.itrs.Select<LF2DataUtils.Itr, TagBox.Itr>((LF2DataUtils.Itr itr) => (TagBox.Itr)itr));
						if (mainForm.formDesign.tagBox.ItrTags.Count == 1)
							mainForm.formDesign.tagBox.ActiveItrIndex = 0;
						else if (ti.HasValue && ti.Value < mainForm.formDesign.tagBox.ItrTags.Count)
							mainForm.formDesign.tagBox.ActiveItrIndex = ti;
						else
							mainForm.formDesign.tagBox.ActiveItrIndex = null;
						if (mainForm.formDesign.tagBox.ActiveItrIndex.HasValue)
						{
							LF2DataUtils.Itr itr = frame.itrs[mainForm.formDesign.tagBox.ActiveItrIndex.Value];
							if (itr != null)
							{
								mainForm.formDesign.itr_arest.Text = itr.arest.HasValue ? itr.arest.ToString() : "";
								mainForm.formDesign.itr_bdefend.Text = itr.bdefend.HasValue ? itr.bdefend.ToString() : "";
								mainForm.formDesign.itr_catchingact1.Text = itr.catchingact1.HasValue ? itr.catchingact1.ToString() : "";
								mainForm.formDesign.itr_catchingact2.Text = itr.catchingact2.HasValue ? itr.catchingact2.ToString() : "";
								mainForm.formDesign.itr_caughtact1.Text = itr.caughtact1.HasValue ? itr.caughtact1.ToString() : "";
								mainForm.formDesign.itr_caughtact2.Text = itr.caughtact2.HasValue ? itr.caughtact2.ToString() : "";
								mainForm.formDesign.itr_dvx.Text = itr.dvx.HasValue ? itr.dvx.ToString() : "";
								mainForm.formDesign.itr_dvy.Text = itr.dvy.HasValue ? itr.dvy.ToString() : "";
								mainForm.formDesign.itr_effect.Text = itr.effect.HasValue ? itr.effect.ToString() : "";
								mainForm.formDesign.itr_fall.Text = itr.fall.HasValue ? itr.fall.ToString() : "";
								mainForm.formDesign.itr_h.Text = itr.h.HasValue ? itr.h.ToString() : "";
								mainForm.formDesign.itr_injury.Text = itr.injury.HasValue ? itr.injury.ToString() : "";
								mainForm.formDesign.itr_kind.Text = itr.kind.HasValue ? itr.kind.ToString() : "";
								mainForm.formDesign.itr_vrest.Text = itr.vrest.HasValue ? itr.vrest.ToString() : "";
								mainForm.formDesign.itr_w.Text = itr.w.HasValue ? itr.w.ToString() : "";
								mainForm.formDesign.itr_x.Text = itr.x.HasValue ? itr.x.ToString() : "";
								mainForm.formDesign.itr_y.Text = itr.y.HasValue ? itr.y.ToString() : "";
								mainForm.formDesign.itr_zwidth.Text = itr.zwidth.HasValue ? itr.zwidth.ToString() : "";
							}
						}
					}
					else
					{
						mainForm.formDesign.tagBox.ClearItrList();
					}
					if (frame.centerx.HasValue && frame.centery.HasValue)
					{
						mainForm.formDesign.tagBox.TagData.center = new Point(frame.centerx.Value, frame.centery.Value);
					}
					else
					{
						mainForm.formDesign.tagBox.TagData.center = null;
					}
					if (frame.bpoint != null)
					{
						mainForm.formDesign.tagBox.TagData.bpoint = (Point)frame.bpoint;
					}
					else
					{
						mainForm.formDesign.tagBox.TagData.bpoint = null;
					}
					if (frame.cpoint != null)
					{
						mainForm.formDesign.tagBox.TagData.cpoint = (TagBox.CPoint)frame.cpoint;
					}
					else
					{
						mainForm.formDesign.tagBox.TagData.cpoint = null;
					}
					if (frame.opoint != null)
					{
						mainForm.formDesign.tagBox.TagData.opoint = (TagBox.OPoint)frame.opoint;
					}
					else
					{
						mainForm.formDesign.tagBox.TagData.opoint = null;
					}
					if (frame.wpoint != null)
					{
						mainForm.formDesign.tagBox.TagData.wpoint = (TagBox.WPoint)frame.wpoint;
					}
					else
					{
						mainForm.formDesign.tagBox.TagData.wpoint = null;
					}
					mainForm.formDesign.RefreshAllTextBoxes();
				}
				mainForm.formDesign.EditReset();
				mainForm.formDesign.tagBox.Refresh();
				return true;
			}
			catch
			{
				mainForm.formDesign.EditReset();
				mainForm.formDesign.tagBox.Invalidate();
				return false;
			}
			finally
			{
				Monitor.Exit(syncLock);
			}
		}

		private void syncTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Action func = (Action)(() =>
			{
				justEdited = false;
				syncTimer.Stop();
				SyncToDesign(true);
			});
			if (this.InvokeRequired)
				this.BeginInvoke(func);
			else
				func();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Save();
		}

		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void closeAllButThisToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var doc = this.DockPanel.DocumentsToArray();
			for (int i = this.DockPanel.DocumentsCount - 1; i >= 0; i--)
			{
				if (doc[i] != this)
					(doc[i] as DocumentForm).Close();
			}
		}

		private void closeAllToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var doc = this.DockPanel.DocumentsToArray();
			for (int i = this.DockPanel.DocumentsCount - 1; i >= 0; i--)
			{
				if (doc[i] != this)
					(doc[i] as DocumentForm).Close();
			}
			Close();
		}

		private void copyFullPathToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_filePath != null)
				System.Windows.Forms.Clipboard.SetText(_filePath, TextDataFormat.UnicodeText);
		}

		private void copyFileNameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (_filePath != null)
				System.Windows.Forms.Clipboard.SetText(Path.GetFileName(_filePath), TextDataFormat.UnicodeText);
		}

		private void copyFileItselfToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (File.Exists(_filePath))
			{
				var fdl = new System.Collections.Specialized.StringCollection();
				fdl.Add(_filePath);
				System.Windows.Forms.Clipboard.SetFileDropList(fdl);
			}
		}

		private void openInExplorerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (File.Exists(_filePath))
			{
				Platform.Reveal(_filePath);
			}
		}


		private void scintilla_MouseMove(object sender, MouseEventArgs e)
		{
			if (mainForm.ContainsFocus && mainForm.formDesign.ContainsFocus)
			{
				this.Focus();
				ActiveControl = scintilla;
				scintilla.Focus();
			}
		}

		public int selectionStart;
		public int selectionEnd;
		public int firstVisibleLine;

		private void DocumentForm_Shown(object sender, EventArgs e)
		{
			scintilla.Selection.Start = scintilla.PositionToPlace(Math.Min(this.selectionStart, scintilla.TextLength));
			scintilla.Selection.End = scintilla.PositionToPlace(Math.Min(this.selectionEnd, scintilla.TextLength));
			scintilla.Navigate(Math.Min(this.firstVisibleLine, scintilla.LinesCount - 1));
		}
	}

	public enum DocumentType
	{
		Default,
		ObjectData,
		StageData,
		BgData,
		DataTxt,
		AngelScript,
		CSharp,
		XML
	}
}
