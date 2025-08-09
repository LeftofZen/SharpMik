using SharpMik.Common;
using SharpMik.Interfaces;
using System;
using System.IO;

namespace SharpMik.Player
{
	public class MikMod
	{
		string commandLine;

		public event ModPlayer.PlayerStateChangedEvent PlayerStateChangeEvent;

		public bool HasError => !string.IsNullOrEmpty(error);

		public static int Playback_CurrentPattern => ModPlayer.s_Module.Playback_SongPosition;
		public static int Playback_CurrentPatternRowNumber => ModPlayer.s_Module.Playback_PatternRowNumber;
		public static int NumRowsOnCurrentPattern => ModPlayer.s_Module.NumRowsOnCurrentPattern;

		string error;
		public string ErrorMessage
		{
			get
			{
				var error = this.error;
				this.error = null;
				return error;
			}
			set { error = value; }
		}

		public MikMod() => ModPlayer.PlayStateChangedHandle += new ModPlayer.PlayerStateChangedEvent(ModPlayer_PlayStateChangedHandle);

		void ModPlayer_PlayStateChangedHandle(ModPlayer.PlayerState state) => PlayerStateChangeEvent?.Invoke(state);

		public static float GetProgress()
		{
			if (ModPlayer.s_Module != null)
			{
				float current = (ModPlayer.s_Module.Playback_SongPosition * ModPlayer.s_Module.NumRowsOnCurrentPattern) + ModPlayer.s_Module.Playback_PatternRowNumber;
				float total = ModPlayer.s_Module.NumPositions * ModPlayer.s_Module.NumRowsOnCurrentPattern;

				return current / total;
			}

			return 0.0f;
		}

		public bool Init<T>() where T : IModDriver, new() => Init<T>("");

		public bool Init<T>(string command) where T : IModDriver, new()
		{
			commandLine = command;
			_ = ModDriver.LoadDriver<T>();

			return ModDriver.MikMod_Init(command);
		}

		public T Init<T>(string command, out bool result) where T : IModDriver, new()
		{
			commandLine = command;
			var driver = ModDriver.LoadDriver<T>();

			result = ModDriver.MikMod_Init(command);

			return driver;
		}

		public void Reset() => ModDriver.MikMod_Reset(commandLine);

		public static void Exit() => ModDriver.MikMod_Exit();

		public Module LoadModule(string fileName)
		{
			error = null;
			if (ModDriver.Driver != null)
			{
				try
				{
					return ModuleLoader.Load(fileName);
				}
				catch (Exception ex)
				{
					error = ex.Message;
				}
			}
			else
			{
				error = "A Driver needs to be set before loading a module";
			}

			return null;
		}

		public Module LoadModule(Stream stream)
		{
			error = null;
			if (ModDriver.Driver != null)
			{
				try
				{
					return ModuleLoader.Load(stream, 128, 0);
				}
				catch (Exception ex)
				{
					error = ex.Message;
				}
			}
			else
			{
				error = "A Driver needs to be set before loading a module";
			}

			return null;
		}

		public void UnLoadModule(Module mod)
		{
			// Make sure the mod is stopped before unloading.
			Stop();
			ModuleLoader.UnLoad(mod);
		}

		public static void UnLoadCurrent()
		{
			if (ModPlayer.s_Module != null)
			{
				ModuleLoader.UnLoad(ModPlayer.s_Module);
			}
		}

		public Module Play(string name)
		{
			var mod = LoadModule(name);

			if (mod != null)
			{
				Play(mod);
			}

			return mod;
		}

		public Module Play(Stream stream)
		{
			var mod = LoadModule(stream);

			if (mod != null)
			{
				Play(mod);
			}

			return mod;
		}

		public static void Play(Module mod) => ModPlayer.Player_Start(mod);

		public static bool IsPlaying() => ModPlayer.Player_Active();

		public static void Stop() => ModPlayer.Player_Stop();

		public static void TogglePause() => ModPlayer.Player_Paused();

		public static void SetPosition(int position) => ModPlayer.Player_SetPosition((ushort)position);

		// Fast forward will mute all the channels and mute the driver then update mikmod till it reaches the song position that is requested
		// then it will unmute and unpause the audio after.
		// this makes sure that no sound is heard while fast forwarding.
		// the bonus of fast forwarding over setting the position is that it will know the real state of the mod.
		public static void FastForwardTo(int position)
		{
			_ = ModPlayer.Player_Mute_Channel(MuteOptions.MuteAll, null);
			ModDriver.Driver_Pause(true);
			while (ModPlayer.s_Module.Playback_SongPosition != position)
			{
				ModDriver.MikMod_Update();
			}

			ModDriver.Driver_Pause(false);
			_ = ModPlayer.Player_UnMute_Channel(MuteOptions.MuteAll, null);
		}

		public static void MuteChannel(int channel) => ModPlayer.Player_Mute_Channel(channel);

		public static void MuteChannel(MuteOptions option, params int[] list) => ModPlayer.Player_Mute_Channel(option, list);

		public static void UnMuteChannel(int channel) => ModPlayer.Player_UnMute_Channel(channel);

		public static void UnMuteChannel(MuteOptions option, params int[] list) => ModPlayer.Player_UnMute_Channel(option, list);

		/// <summary>
		/// Depending on the driver this might need to be called, it should be safe to call even if the driver is auto updating.
		/// </summary>
		public static void Update()
		{
			if (ModDriver.Driver?.AutoUpdating == false)
			{
				ModDriver.MikMod_Update();
			}
		}
	}
}
