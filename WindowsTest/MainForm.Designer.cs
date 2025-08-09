namespace SharpMilk
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			menuToolStrip = new System.Windows.Forms.ToolStrip();
			OpenMod = new System.Windows.Forms.ToolStripButton();
			toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			PlayPauseMod = new System.Windows.Forms.ToolStripButton();
			StopMod = new System.Windows.Forms.ToolStripButton();
			tsbPrevious = new System.Windows.Forms.ToolStripButton();
			tsbNext = new System.Windows.Forms.ToolStripButton();
			toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			tslCurrentlyPlaying = new System.Windows.Forms.ToolStripLabel();
			listBox1 = new System.Windows.Forms.ListBox();
			tbSongPosition = new System.Windows.Forms.TrackBar();
			pgCurrentlyPlaying = new System.Windows.Forms.PropertyGrid();
			scMain = new System.Windows.Forms.SplitContainer();
			lblSongPosition = new System.Windows.Forms.Label();
			lblPatternRowNumber = new System.Windows.Forms.Label();
			lblCurrentPattern = new System.Windows.Forms.Label();
			menuToolStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)tbSongPosition).BeginInit();
			((System.ComponentModel.ISupportInitialize)scMain).BeginInit();
			scMain.Panel1.SuspendLayout();
			scMain.Panel2.SuspendLayout();
			scMain.SuspendLayout();
			SuspendLayout();
			// 
			// menuToolStrip
			// 
			menuToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			menuToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { OpenMod, toolStripButton1, toolStripSeparator1, PlayPauseMod, StopMod, tsbPrevious, tsbNext, toolStripSeparator2, tslCurrentlyPlaying });
			menuToolStrip.Location = new System.Drawing.Point(0, 0);
			menuToolStrip.Name = "menuToolStrip";
			menuToolStrip.Size = new System.Drawing.Size(784, 25);
			menuToolStrip.TabIndex = 0;
			menuToolStrip.Text = "toolStrip1";
			// 
			// OpenMod
			// 
			OpenMod.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			OpenMod.Image = SharpMikTester.Properties.Resources.openHS;
			OpenMod.ImageTransparentColor = System.Drawing.Color.Magenta;
			OpenMod.Name = "OpenMod";
			OpenMod.Size = new System.Drawing.Size(23, 22);
			OpenMod.Text = "Open";
			OpenMod.Click += OpenMod_Click;
			// 
			// toolStripButton1
			// 
			toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton1.Image = SharpMikTester.Properties.Resources._042b_AddCategory_16x16_72;
			toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton1.Name = "toolStripButton1";
			toolStripButton1.Size = new System.Drawing.Size(23, 22);
			toolStripButton1.Text = "toolStripButton1";
			toolStripButton1.Click += toolStripButton1_Click;
			// 
			// toolStripSeparator1
			// 
			toolStripSeparator1.Name = "toolStripSeparator1";
			toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// PlayPauseMod
			// 
			PlayPauseMod.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			PlayPauseMod.Image = SharpMikTester.Properties.Resources.PlayHS;
			PlayPauseMod.ImageTransparentColor = System.Drawing.Color.Magenta;
			PlayPauseMod.Name = "PlayPauseMod";
			PlayPauseMod.Size = new System.Drawing.Size(23, 22);
			PlayPauseMod.Text = "Play";
			PlayPauseMod.Click += PlayPauseMod_Click;
			// 
			// StopMod
			// 
			StopMod.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			StopMod.Image = SharpMikTester.Properties.Resources.StopHS;
			StopMod.ImageTransparentColor = System.Drawing.Color.Magenta;
			StopMod.Name = "StopMod";
			StopMod.Size = new System.Drawing.Size(23, 22);
			StopMod.Text = "toolStripButton3";
			StopMod.Click += StopMod_Click;
			// 
			// tsbPrevious
			// 
			tsbPrevious.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			tsbPrevious.Image = SharpMikTester.Properties.Resources.NavBack;
			tsbPrevious.ImageTransparentColor = System.Drawing.Color.Magenta;
			tsbPrevious.Name = "tsbPrevious";
			tsbPrevious.RightToLeft = System.Windows.Forms.RightToLeft.No;
			tsbPrevious.Size = new System.Drawing.Size(23, 22);
			tsbPrevious.Text = "toolStripButton2";
			tsbPrevious.Click += tsbPrevious_Click;
			// 
			// tsbNext
			// 
			tsbNext.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			tsbNext.Image = SharpMikTester.Properties.Resources.NavForward;
			tsbNext.ImageTransparentColor = System.Drawing.Color.Magenta;
			tsbNext.Name = "tsbNext";
			tsbNext.Size = new System.Drawing.Size(23, 22);
			tsbNext.Text = "toolStripButton3";
			tsbNext.Click += tsbNext_Click;
			// 
			// toolStripSeparator2
			// 
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// tslCurrentlyPlaying
			// 
			tslCurrentlyPlaying.AutoSize = false;
			tslCurrentlyPlaying.Name = "tslCurrentlyPlaying";
			tslCurrentlyPlaying.Size = new System.Drawing.Size(200, 22);
			// 
			// listBox1
			// 
			listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			listBox1.FormattingEnabled = true;
			listBox1.Location = new System.Drawing.Point(0, 45);
			listBox1.Margin = new System.Windows.Forms.Padding(0);
			listBox1.Name = "listBox1";
			listBox1.Size = new System.Drawing.Size(515, 446);
			listBox1.TabIndex = 2;
			// 
			// tbSongPosition
			// 
			tbSongPosition.Dock = System.Windows.Forms.DockStyle.Top;
			tbSongPosition.Enabled = false;
			tbSongPosition.Location = new System.Drawing.Point(0, 25);
			tbSongPosition.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			tbSongPosition.Name = "tbSongPosition";
			tbSongPosition.Size = new System.Drawing.Size(784, 45);
			tbSongPosition.TabIndex = 3;
			tbSongPosition.TickStyle = System.Windows.Forms.TickStyle.None;
			// 
			// pgCurrentlyPlaying
			// 
			pgCurrentlyPlaying.BackColor = System.Drawing.SystemColors.Control;
			pgCurrentlyPlaying.Dock = System.Windows.Forms.DockStyle.Fill;
			pgCurrentlyPlaying.HelpVisible = false;
			pgCurrentlyPlaying.Location = new System.Drawing.Point(0, 0);
			pgCurrentlyPlaying.Name = "pgCurrentlyPlaying";
			pgCurrentlyPlaying.Size = new System.Drawing.Size(261, 491);
			pgCurrentlyPlaying.TabIndex = 4;
			pgCurrentlyPlaying.ToolbarVisible = false;
			// 
			// scMain
			// 
			scMain.Dock = System.Windows.Forms.DockStyle.Fill;
			scMain.Location = new System.Drawing.Point(0, 70);
			scMain.Name = "scMain";
			// 
			// scMain.Panel1
			// 
			scMain.Panel1.Controls.Add(listBox1);
			scMain.Panel1.Controls.Add(lblSongPosition);
			scMain.Panel1.Controls.Add(lblPatternRowNumber);
			scMain.Panel1.Controls.Add(lblCurrentPattern);
			// 
			// scMain.Panel2
			// 
			scMain.Panel2.Controls.Add(pgCurrentlyPlaying);
			scMain.Size = new System.Drawing.Size(784, 491);
			scMain.SplitterDistance = 515;
			scMain.SplitterWidth = 8;
			scMain.TabIndex = 5;
			// 
			// lblSongPosition
			// 
			lblSongPosition.AutoSize = true;
			lblSongPosition.Dock = System.Windows.Forms.DockStyle.Top;
			lblSongPosition.Location = new System.Drawing.Point(0, 30);
			lblSongPosition.Margin = new System.Windows.Forms.Padding(4);
			lblSongPosition.Name = "lblSongPosition";
			lblSongPosition.Size = new System.Drawing.Size(38, 15);
			lblSongPosition.TabIndex = 5;
			lblSongPosition.Text = "label1";
			// 
			// lblPatternRowNumber
			// 
			lblPatternRowNumber.AutoSize = true;
			lblPatternRowNumber.Dock = System.Windows.Forms.DockStyle.Top;
			lblPatternRowNumber.Location = new System.Drawing.Point(0, 15);
			lblPatternRowNumber.Margin = new System.Windows.Forms.Padding(4);
			lblPatternRowNumber.Name = "lblPatternRowNumber";
			lblPatternRowNumber.Size = new System.Drawing.Size(38, 15);
			lblPatternRowNumber.TabIndex = 4;
			lblPatternRowNumber.Text = "label1";
			// 
			// lblCurrentPattern
			// 
			lblCurrentPattern.AutoSize = true;
			lblCurrentPattern.Dock = System.Windows.Forms.DockStyle.Top;
			lblCurrentPattern.Location = new System.Drawing.Point(0, 0);
			lblCurrentPattern.Margin = new System.Windows.Forms.Padding(4);
			lblCurrentPattern.Name = "lblCurrentPattern";
			lblCurrentPattern.Size = new System.Drawing.Size(38, 15);
			lblCurrentPattern.TabIndex = 3;
			lblCurrentPattern.Text = "label1";
			// 
			// MainForm
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(784, 561);
			Controls.Add(scMain);
			Controls.Add(tbSongPosition);
			Controls.Add(menuToolStrip);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "MainForm";
			Text = "SharpMik";
			FormClosing += Form1_FormClosing;
			menuToolStrip.ResumeLayout(false);
			menuToolStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)tbSongPosition).EndInit();
			scMain.Panel1.ResumeLayout(false);
			scMain.Panel1.PerformLayout();
			scMain.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)scMain).EndInit();
			scMain.ResumeLayout(false);
			ResumeLayout(false);
			PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip menuToolStrip;
		private System.Windows.Forms.ToolStripButton OpenMod;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton PlayPauseMod;
		private System.Windows.Forms.ToolStripButton StopMod;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripLabel tslCurrentlyPlaying;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripButton tsbPrevious;
		private System.Windows.Forms.ToolStripButton tsbNext;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.TrackBar tbSongPosition;
		private System.Windows.Forms.PropertyGrid pgCurrentlyPlaying;
		private System.Windows.Forms.SplitContainer scMain;
		private System.Windows.Forms.Label lblCurrentPattern;
		private System.Windows.Forms.Label lblPatternRowNumber;
		private System.Windows.Forms.Label lblSongPosition;
	}
}

