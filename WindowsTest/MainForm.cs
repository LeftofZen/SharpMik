using SharpMik.Common;
using SharpMik.Drivers;
using SharpMik.Player;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

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

				_ = tbSongPosition.BeginInvoke(() => tbSongPosition.Value = place);
				_ = lblCurrentPattern.BeginInvoke(() => lblCurrentPattern.Text = $"NumRowsOnCurrentPattern: {MikMod.NumRowsOnCurrentPattern}");
				_ = lblPatternRowNumber.BeginInvoke(() => lblPatternRowNumber.Text = $"Current Pattern Row Number: {MikMod.Playback_CurrentPatternRowNumber}");
				_ = lblSongPosition.BeginInvoke(() => lblSongPosition.Text = $"Current Pattern: {MikMod.Playback_CurrentPattern}");
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
				_ = m_Player.Init<NaudioDriver>("");

				var start = DateTime.Now;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		void OpenMod_Click(object sender, EventArgs e)
		{
			var dialog = new OpenFileDialog();
			var extensions = Helpers.ModFileExtensions;
			var filters = "All (*.*)|*.*|";

			foreach (var item in extensions)
			{
				filters += "(*" + item + ")|*" + item + "|";
			}

			if (filters.Length > 0)
			{
				dialog.Filter = filters[..^1];
			}

			var result = dialog.ShowDialog();

			if (result == DialogResult.OK)
			{
				m_Mod = m_Player.LoadModule(dialog.FileName);

				if (m_Mod != null)
				{
					tslCurrentlyPlaying.Text = m_Mod.SongName;
					pgCurrentlyPlaying.SelectedObject = m_Mod;

					UpdatePatternChannelList();
				}

				PlayPauseMod.Enabled = true;
				StopMod.Enabled = false;

				m_Playing = false;
				m_WasPlaying = false;
				PlayPauseMod.Image = SharpMikTester.Properties.Resources.PlayHS;
			}
		}

		bool m_WasPlaying;
		void PlayPauseMod_Click(object sender, EventArgs e)
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

		void StopMod_Click(object sender, EventArgs e)
		{
			m_Playing = false;
			ModPlayer.Player_Stop();

			OpenMod.Enabled = true;
			StopMod.Enabled = false;
		}

		List<string> m_FileList = [];
		int place;

		void toolStripButton1_Click(object sender, EventArgs e)
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
						_ = listBox1.Items.Add(shortName + " (" + name + ")");
					}
				}

				place = 0;

				if (!Play())
				{
					Next();
				}
			}
		}

		void Next()
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

		void Prev()
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

		bool Play()
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

		void tsbNext_Click(object sender, EventArgs e)
			=> Next();

		void tsbPrevious_Click(object sender, EventArgs e)
			=> Prev();

		void Form1_FormClosing(object sender, FormClosingEventArgs e)
			=> ExitApp();

		// Clear the list first
		void UpdatePatternChannelList()
		{
			listBox1.Items.Clear();

			if (m_Mod != null && m_Mod.Patterns != null && m_Mod.NumChannels > 0)
			{
				//for (int ch = 0; ch < m_Mod.NumChannels; ch++)
				//{
				//	// Each pattern's tracks are stored sequentially
				//	int patternIndex = ch; // For pattern 0, offset is 0
				//	ushort trackIndex = m_Mod.Patterns[patternIndex];
				//	listBox1.Items.Add($"Channel {ch}: Track {trackIndex}");
				//}
				var patternRows = m_Mod.Tracks[m_Mod.Patterns[0]];
				foreach (var row in patternRows)
				{
					_ = listBox1.Items.Add(row);
				}
			}
		}
	}
}
