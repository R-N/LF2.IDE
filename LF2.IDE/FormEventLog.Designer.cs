namespace LF2.IDE
{
	partial class FormEventLog
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormEventLog));
			this.scintilla = new FastColoredTextBoxNS.FastColoredTextBox();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copySelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.scintilla)).BeginInit();
			this.contextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// scintilla
			// 
			this.scintilla.ContextMenuStrip = this.contextMenuStrip;
			this.scintilla.Dock = System.Windows.Forms.DockStyle.Fill;
			this.scintilla.Font = new System.Drawing.Font("Consolas", 9.75F);
			this.scintilla.ForeColor = System.Drawing.Color.Black;
			this.scintilla.BackColor = System.Drawing.Color.White;
			this.scintilla.WordWrap = true;
			this.scintilla.ReadOnly = true;
			this.scintilla.ShowLineNumbers = true;
			this.scintilla.Location = new System.Drawing.Point(0, 0);
			this.scintilla.Name = "eventLog";
			this.scintilla.Size = new System.Drawing.Size(484, 216);
			this.scintilla.TabIndex = 1;
			this.scintilla.KeyDown += new System.Windows.Forms.KeyEventHandler(this.scintilla_KeyDown);
			this.scintilla.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.scintilla_MouseDoubleClick);
			// 
			// contextMenuStrip
			// 
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copySelectedToolStripMenuItem,
            this.copyAllToolStripMenuItem});
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(150, 48);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			// 
			// copySelectedToolStripMenuItem
			// 
			this.copySelectedToolStripMenuItem.Name = "copySelectedToolStripMenuItem";
			this.copySelectedToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
			this.copySelectedToolStripMenuItem.Text = "Copy Selected";
			this.copySelectedToolStripMenuItem.Click += new System.EventHandler(this.copySelectedToolStripMenuItem_Click);
			// 
			// copyAllToolStripMenuItem
			// 
			this.copyAllToolStripMenuItem.Name = "copyAllToolStripMenuItem";
			this.copyAllToolStripMenuItem.Size = new System.Drawing.Size(149, 22);
			this.copyAllToolStripMenuItem.Text = "Copy All";
			this.copyAllToolStripMenuItem.Click += new System.EventHandler(this.copyAllToolStripMenuItem_Click);
			// 
			// FormEventLog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(484, 216);
			this.Controls.Add(this.scintilla);
			this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)(((((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.DockLeft) 
            | WeifenLuo.WinFormsUI.Docking.DockAreas.DockRight) 
            | WeifenLuo.WinFormsUI.Docking.DockAreas.DockTop) 
            | WeifenLuo.WinFormsUI.Docking.DockAreas.DockBottom)));
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.HideOnClose = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormEventLog";
			this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.DockBottomAutoHide;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.TabText = "Event Log";
			this.Text = "Event Log";
			((System.ComponentModel.ISupportInitialize)(this.scintilla)).EndInit();
			this.contextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		public FastColoredTextBoxNS.FastColoredTextBox scintilla;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem copySelectedToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyAllToolStripMenuItem;
	}
}
