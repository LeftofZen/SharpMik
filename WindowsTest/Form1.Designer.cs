namespace SharpMilk
{
	partial class Form1
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
			toolStrip1 = new System.Windows.Forms.ToolStrip();
			OpenMod = new System.Windows.Forms.ToolStripButton();
			toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			PlayPauseMod = new System.Windows.Forms.ToolStripButton();
			StopMod = new System.Windows.Forms.ToolStripButton();
			toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			CloseApp = new System.Windows.Forms.ToolStripButton();
			listBox1 = new System.Windows.Forms.ListBox();
			trackBar1 = new System.Windows.Forms.TrackBar();
			toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)trackBar1).BeginInit();
			SuspendLayout();
			// 
			// toolStrip1
			// 
			toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { OpenMod, toolStripButton1, toolStripSeparator1, PlayPauseMod, StopMod, toolStripButton2, toolStripButton3, toolStripSeparator2, toolStripLabel1, toolStripSeparator3, CloseApp });
			toolStrip1.Location = new System.Drawing.Point(0, 0);
			toolStrip1.Name = "toolStrip1";
			toolStrip1.Size = new System.Drawing.Size(595, 25);
			toolStrip1.TabIndex = 0;
			toolStrip1.Text = "toolStrip1";
			toolStrip1.ItemClicked += toolStrip1_ItemClicked;
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
			// toolStripButton2
			// 
			toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton2.Image = SharpMikTester.Properties.Resources.NavBack;
			toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton2.Name = "toolStripButton2";
			toolStripButton2.RightToLeft = System.Windows.Forms.RightToLeft.No;
			toolStripButton2.Size = new System.Drawing.Size(23, 22);
			toolStripButton2.Text = "toolStripButton2";
			toolStripButton2.Click += toolStripButton2_Click;
			// 
			// toolStripButton3
			// 
			toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			toolStripButton3.Image = SharpMikTester.Properties.Resources.NavForward;
			toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			toolStripButton3.Name = "toolStripButton3";
			toolStripButton3.Size = new System.Drawing.Size(23, 22);
			toolStripButton3.Text = "toolStripButton3";
			toolStripButton3.Click += toolStripButton3_Click;
			// 
			// toolStripSeparator2
			// 
			toolStripSeparator2.Name = "toolStripSeparator2";
			toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripLabel1
			// 
			toolStripLabel1.AutoSize = false;
			toolStripLabel1.Name = "toolStripLabel1";
			toolStripLabel1.Size = new System.Drawing.Size(200, 22);
			// 
			// toolStripSeparator3
			// 
			toolStripSeparator3.Name = "toolStripSeparator3";
			toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// CloseApp
			// 
			CloseApp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			CloseApp.Image = SharpMikTester.Properties.Resources._305_Close_16x16_72;
			CloseApp.ImageTransparentColor = System.Drawing.Color.Magenta;
			CloseApp.Name = "CloseApp";
			CloseApp.Size = new System.Drawing.Size(23, 22);
			CloseApp.Text = "toolStripButton4";
			CloseApp.Click += CloseApp_Click;
			// 
			// listBox1
			// 
			listBox1.FormattingEnabled = true;
			listBox1.Location = new System.Drawing.Point(0, 68);
			listBox1.Margin = new System.Windows.Forms.Padding(0);
			listBox1.Name = "listBox1";
			listBox1.Size = new System.Drawing.Size(593, 244);
			listBox1.TabIndex = 2;
			// 
			// trackBar1
			// 
			trackBar1.Enabled = false;
			trackBar1.Location = new System.Drawing.Point(0, 32);
			trackBar1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			trackBar1.Name = "trackBar1";
			trackBar1.Size = new System.Drawing.Size(594, 45);
			trackBar1.TabIndex = 3;
			trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
			// 
			// Form1
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(595, 309);
			Controls.Add(listBox1);
			Controls.Add(trackBar1);
			Controls.Add(toolStrip1);
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "Form1";
			Text = "SharpMik";
			toolStrip1.ResumeLayout(false);
			toolStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)trackBar1).EndInit();
			ResumeLayout(false);
			PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton OpenMod;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton PlayPauseMod;
		private System.Windows.Forms.ToolStripButton StopMod;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton CloseApp;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.TrackBar trackBar1;
	}
}

