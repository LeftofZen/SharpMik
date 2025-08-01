using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpMik.Player;
using SharpMik.Drivers;

using System.IO;
using SharpMik.Common;

namespace SharpMilk
{
	public partial class MainForm : Form
	{
		Module m_Mod;
		bool m_Playing;
		MikMod m_Player;

		public MainForm()
		{
			InitializeComponent();
			// Ensure the form has minimize, maximize, and close buttons
			ControlBox = true;
			FormBorderStyle = FormBorderStyle.Sizable;
			MinimizeBox = true;
			MaximizeBox = true;

			m_Player = new MikMod();
			m_Player.PlayerStateChangeEvent += new ModPlayer.PlayerStateChangedEvent(m_Player_PlayerStateChangeEvent);

			tbSongPosition.Maximum = 99;
		}

		void m_Player_PlayerStateChangeEvent(ModPlayer.PlayerState state)
		{
			if (state == ModPlayer.PlayerState.Stopped)
			{
				Next();
			}
			else
			{
				var place = (int)(100.0f * MikMod.GetProgress());

				MethodInvoker action = delegate
				{
					tbSongPosition.Value = place;
				};
				tbSongPosition.BeginInvoke(action);
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			PlayPauseMod.Enabled = false;
			StopMod.Enabled = false;

			ModDriver.Mode = (ushort)(ModDriver.Mode | Constants.DMODE_NOISEREDUCTION);
			try
			{
				m_Player.Init<NaudioDriver>("");

				var start = DateTime.Now;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private void OpenMod_Click(object sender, EventArgs e)
		{
			var dialog = new OpenFileDialog();
			var extentions = Helpers.ModFileExtensions;
			var filters = "All (*.*)|*.*|";

			foreach (var item in extentions)
			{
				filters += "(*" + item + ")|*" + item + "|";
			}

			if (filters.Length > 0)
			{
				dialog.Filter = filters.Substring(0, filters.Length - 1);
			}

			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				m_Mod = m_Player.LoadModule(dialog.FileName);

				if (m_Mod != null)
				{
					tslCurrentlyPlaying.Text = m_Mod.SongName;
					pgCurrentlyPlaying.SelectedObject = m_Mod;
				}

				PlayPauseMod.Enabled = true;
				StopMod.Enabled = false;

				m_Playing = false;
				m_WasPlaying = false;
				PlayPauseMod.Image = SharpMikTester.Properties.Resources.PlayHS;
			}
		}

		bool m_WasPlaying;
		private void PlayPauseMod_Click(object sender, EventArgs e)
		{
			if (m_Playing)
			{
				m_Playing = false;
				m_WasPlaying = true;
				PlayPauseMod.Image = SharpMikTester.Properties.Resources.PlayHS;
				ModPlayer.Player_TogglePause();
			}
			else
			{
				m_Playing = true;
				PlayPauseMod.Image = SharpMikTester.Properties.Resources.PauseHS;

				if (m_WasPlaying)
				{
					ModPlayer.Player_TogglePause();
				}
				else
				{
					MikMod.Play(m_Mod);
					StopMod.Enabled = true;
					OpenMod.Enabled = false;
				}
			}
		}

		private void StopMod_Click(object sender, EventArgs e)
		{
			m_Playing = false;
			ModPlayer.Player_Stop();

			OpenMod.Enabled = true;
			StopMod.Enabled = false;
		}

		List<string> m_FileList = new();
		int place;

		private void toolStripButton1_Click(object sender, EventArgs e)
		{
			var dialog = new FolderBrowserDialog();
			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				var modFiles = Directory.EnumerateFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories);
				listBox1.Items.Clear();

				foreach (var name in modFiles)
				{
					if (Helpers.MatchesExtensions(name))
					{
						m_FileList.Add(name);
						var shortName = Path.GetFileNameWithoutExtension(name);
						listBox1.Items.Add(shortName + " (" + name + ")");
					}
				}

				place = 0;

				if (!Play())
				{
					Next();
				}
			}
		}

		private void Next()
		{
			if (m_FileList.Count > 0)
			{
				place++;

				if (place >= m_FileList.Count)
				{
					place = 0;
				}

				//m_Mod = m_Player.LoadModule(m_FileList[place]);

				if (!Play())
				{
					Next();
				}
			}
		}

		private void Prev()
		{
			place--;

			if (place < 0)
			{
				place = m_FileList.Count - 1;
			}

			if (!Play())
			{
				Prev();
			}
		}

		private bool Play()
		{
			if (m_FileList.Count == 0 || place < 0 || place >= m_FileList.Count)
			{
				return true;
			}

			m_Mod = m_Player.Play(m_FileList[place]);
			tslCurrentlyPlaying.Text = m_Mod.SongName;
			/*
			try
			{
				m_Mod = m_Player.LoadModule(m_FileList[place]);
			}
			catch
			{
				m_Mod = null;
				return false;
			}
			
			toolStripLabel1.Text = m_Mod.SongName;

			m_Player.Play(m_Mod);
			 * */
			return true;
		}

		void ExitApp()
		{
			ModPlayer.Player_Stop();
			ModDriver.MikMod_Exit();
		}

		private void tsbNext_Click(object sender, EventArgs e)
			=> Next();

		private void tsbPrevious_Click(object sender, EventArgs e)
			=> Prev();

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
			=> ExitApp();
	}
}
