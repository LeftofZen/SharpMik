﻿using System;
using SharpMik.Common;
using SharpMik.Interfaces;

namespace SharpMik.Player
{
	/*
	 * Keeps tracks of user settings like pan separation and global volume.
	 * Also holds the selected driver and passes data to it.
	 * 
	 * Been trying to keep the user accessible variables hidden with accessors to access them.
	 * 
	 * Also strangly contains the MikMod_* functions that do alot of setting up.
	 * 
	 */
	public class ModDriver
	{
		#region private (static) variables
		private static IModDriver m_Driver;

		internal static byte md_numchn;
		internal static byte md_sngchn;
		internal static byte md_sfxchn;

		static byte md_hardchn;
		static byte md_softchn;
		static ushort md_bpm = 125;
		static byte[] sfxinfo;

		static byte md_pansep = 128;    /* 128 == 100% (full left/right) */
		static byte md_volume = 128;    /* global sound volume (0-128) */
		static byte md_musicvolume = 128;   /* volume of song */
		static byte md_sndfxvolume = 128;   /* volume of sound effects */

		static Sample[] md_sample;
		#endregion

		#region Accessors
		public static byte PanSeperation
		{
			get { return md_pansep; }
			set { md_pansep = value < 129 ? value : (byte)128; }
		}

		public static byte SoftwareChannel => md_softchn;

		public static int ChannelCount
		{
			get { return md_numchn; }
			set { md_numchn = (byte)value; }
		}

		public static ushort Mode { get; set; } = Constants.DMODE_STEREO | Constants.DMODE_16BITS |
						Constants.DMODE_SURROUND | Constants.DMODE_SOFT_MUSIC |
						Constants.DMODE_SOFT_SNDFX;

		public static ushort MixFrequency { get; set; } = 44100;
		public static ushort Bpm
		{
			get { return md_bpm; }
			set { md_bpm = value; }
		}

		public static byte Reverb { get; set; }

		public static byte SoundFXChannel
		{
			get { return md_sfxchn; }
			set { md_sfxchn = value; }
		}

		public static byte GlobalVolume
		{
			get { return md_volume; }
			set { md_volume = value < 129 ? value : (byte)128; }
		}

		public static byte MusicVolume
		{
			get { return md_musicvolume; }
			set { md_musicvolume = value < 129 ? value : (byte)128; }
		}

		public static IModDriver Driver => m_Driver;

		#endregion

		public static T LoadDriver<T>() where T : IModDriver, new()
		{
			m_Driver = new T();
			return (T)m_Driver;
		}

		internal static short MD_SampleLoad(SampleLoad s, int type)
		{
			short result = -1;

			if (m_Driver != null)
			{
				if (type == (int)MDTypes.MD_MUSIC)
				{
					type = (int)((Mode & Constants.DMODE_SOFT_MUSIC) == Constants.DMODE_SOFT_MUSIC ? MDDecodeTypes.MD_SOFTWARE : MDDecodeTypes.MD_HARDWARE);
				}
				else if (type == (int)MDTypes.MD_SNDFX)
				{
					type = (int)((Mode & Constants.DMODE_SOFT_SNDFX) == Constants.DMODE_SOFT_SNDFX ? MDDecodeTypes.MD_SOFTWARE : MDDecodeTypes.MD_HARDWARE);
				}

				SampleLoader.SL_Init(s);
				result = m_Driver.SampleLoad(s, type);
				SampleLoader.SL_Exit(s);
			}

			return result;
		}

		internal static short[] MD_GetSample(short handle)
		{
			if (m_Driver != null)
			{
				return m_Driver.GetSample(handle);
			}

			return null;
		}

		internal static short MD_SetSample(short[] sample)
		{
			if (m_Driver != null)
			{
				return m_Driver.SetSample(sample);
			}

			return -1;
		}

		/* Note: 'type' indicates whether the returned value should be for music or for sound effects. */
		public static uint MD_SampleSpace(int type)
		{
			if (type == (int)MDTypes.MD_MUSIC)
			{
				type = (int)((Mode & Constants.DMODE_SOFT_MUSIC) != 0 ? MDDecodeTypes.MD_SOFTWARE : MDDecodeTypes.MD_HARDWARE);
			}
			else if (type == (int)MDTypes.MD_SNDFX)
			{
				type = (int)((Mode & Constants.DMODE_SOFT_SNDFX) != 0 ? MDDecodeTypes.MD_SOFTWARE : MDDecodeTypes.MD_HARDWARE);
			}

			return m_Driver.FreeSampleSpace(type);
		}

		public static void Voice_Stop_internal(byte voice)
		{
			if ((voice < 0) || (voice >= md_numchn))
			{
				return;
			}

			if (voice >= md_sngchn)
			{
				/* It is a sound effects channel, so flag the voice as non-critical! */
				sfxinfo[voice - md_sngchn] = 0;
			}

			m_Driver.VoiceStop(voice);
		}

		internal static bool Voice_Stopped_internal(sbyte voice)
		{
			if ((voice < 0) || (voice >= md_numchn))
			{
				return false;
			}

			return m_Driver.VoiceStopped((byte)voice);
		}

		internal static void Voice_Play_internal(sbyte voice, Sample s, uint start)
		{
			uint repend;

			if ((voice < 0) || (voice >= md_numchn))
			{
				return;
			}

			md_sample[voice] = s;
			repend = s.loopend;

			if ((s.flags & Constants.SF_LOOP) == Constants.SF_LOOP)
			{
				/* repend can't be bigger than size */
				if (repend > s.length)
				{
					repend = s.length;
				}
			}

			m_Driver.VoicePlay((byte)voice, s.handle, start, s.length, s.loopstart, repend, s.flags);
		}

		internal static void Voice_SetVolume_internal(sbyte voice, ushort vol)
		{
			uint tmp;

			if ((voice < 0) || (voice >= md_numchn))
			{
				return;
			}

			/* range checks */
			if (md_musicvolume > 128)
			{
				md_musicvolume = 128;
			}

			if (md_sndfxvolume > 128)
			{
				md_sndfxvolume = 128;
			}

			if (md_volume > 128)
			{
				md_volume = 128;
			}

			tmp = (uint)(vol * md_volume * ((voice < md_sngchn) ? md_musicvolume : md_sndfxvolume));
			m_Driver.VoiceSetVolume((byte)voice, (ushort)(tmp / 16384));
		}

		internal static void Voice_SetPanning_internal(sbyte voice, uint pan)
		{
			if ((voice < 0) || (voice >= md_numchn))
			{
				return;
			}

			if (pan != Constants.PAN_SURROUND)
			{
				if (md_pansep > 128)
				{
					md_pansep = 128;
				}

				if ((Mode & Constants.DMODE_REVERSE) == Constants.DMODE_REVERSE)
				{
					pan = 255 - pan;
				}

				pan = (uint)(((short)(pan - 128) * md_pansep / 128) + 128);
			}

			m_Driver.VoiceSetPanning((byte)voice, pan);
		}

		internal static void Voice_SetFrequency_internal(sbyte voice, uint frq)
		{
			if ((voice < 0) || (voice >= md_numchn))
			{
				return;
			}

			if ((md_sample[voice] != null) && (md_sample[voice].divfactor != 0))
			{
				frq /= md_sample[voice].divfactor;
			}

			m_Driver.VoiceSetFrequency((byte)voice, frq);
		}

		static void LimitHardVoices(int limit)
		{
			var t = 0;

			if ((Mode & Constants.DMODE_SOFT_SNDFX) != Constants.DMODE_SOFT_SNDFX && (md_sfxchn > limit))
			{
				md_sfxchn = (byte)limit;
			}

			if ((Mode & Constants.DMODE_SOFT_MUSIC) != Constants.DMODE_SOFT_MUSIC && (md_sngchn > limit))
			{
				md_sngchn = (byte)limit;
			}

			if ((Mode & Constants.DMODE_SOFT_SNDFX) != Constants.DMODE_SOFT_SNDFX)
			{
				md_hardchn = md_sfxchn;
			}
			else
			{
				md_hardchn = 0;
			}

			if ((Mode & Constants.DMODE_SOFT_MUSIC) != Constants.DMODE_SOFT_MUSIC)
			{
				md_hardchn += md_sngchn;
			}

			while (md_hardchn > limit)
			{
				if ((++t & 1) == 1)
				{
					if ((Mode & Constants.DMODE_SOFT_SNDFX) != Constants.DMODE_SOFT_SNDFX && (md_sfxchn > 4))
					{
						md_sfxchn--;
					}
				}
				else
				{
					if ((Mode & Constants.DMODE_SOFT_MUSIC) != Constants.DMODE_SOFT_MUSIC && (md_sngchn > 8))
					{
						md_sngchn--;
					}
				}

				if ((Mode & Constants.DMODE_SOFT_SNDFX) != Constants.DMODE_SOFT_SNDFX)
				{
					md_hardchn = md_sfxchn;
				}
				else
				{
					md_hardchn = 0;
				}

				if ((Mode & Constants.DMODE_SOFT_MUSIC) != Constants.DMODE_SOFT_MUSIC)
				{
					md_hardchn += md_sngchn;
				}
			}

			md_numchn = (byte)(md_hardchn + md_softchn);
		}

		static void LimitSoftVoices(int limit)
		{
			var t = 0;

			if ((Mode & Constants.DMODE_SOFT_SNDFX) == Constants.DMODE_SOFT_SNDFX && (md_sfxchn > limit))
			{
				md_sfxchn = (byte)limit;
			}

			if ((Mode & Constants.DMODE_SOFT_MUSIC) == Constants.DMODE_SOFT_MUSIC && (md_sngchn > limit))
			{
				md_sngchn = (byte)limit;
			}

			if ((Mode & Constants.DMODE_SOFT_SNDFX) == Constants.DMODE_SOFT_SNDFX)
			{
				md_softchn = md_sfxchn;
			}
			else
			{
				md_softchn = 0;
			}

			if ((Mode & Constants.DMODE_SOFT_MUSIC) == Constants.DMODE_SOFT_MUSIC)
			{
				md_softchn += md_sngchn;
			}

			while (md_softchn > limit)
			{
				if ((++t & 1) == 1)
				{
					if (((Mode & Constants.DMODE_SOFT_SNDFX) == Constants.DMODE_SOFT_SNDFX) && (md_sfxchn > 4))
					{
						md_sfxchn--;
					}
				}
				else
				{
					if (((Mode & Constants.DMODE_SOFT_MUSIC) == Constants.DMODE_SOFT_MUSIC) && (md_sngchn > 8))
					{
						md_sngchn--;
					}
				}

				if ((Mode & Constants.DMODE_SOFT_SNDFX) != Constants.DMODE_SOFT_SNDFX)
				{
					md_softchn = md_sfxchn;
				}
				else
				{
					md_softchn = 0;
				}

				if ((Mode & Constants.DMODE_SOFT_MUSIC) != Constants.DMODE_SOFT_MUSIC)
				{
					md_softchn += md_sngchn;
				}
			}

			md_numchn = (byte)(md_hardchn + md_softchn);
		}

		// should be moved into own file..
		#region milkmod stuff 
		internal static bool isplaying;
		static bool initialized;

		static bool MikMod_Active_internal() => isplaying;

		public static bool MikMod_Active() => MikMod_Active_internal();

		static bool MikMod_EnableOutput_internal()
		{
			if (!isplaying)
			{
				if (m_Driver.PlayStart())
				{
					return true;
				}

				isplaying = true;
			}

			return false;
		}

		public static bool MikMod_EnableOutput() => MikMod_EnableOutput_internal();

		public static void MikMod_Update()
		{
			if (isplaying)
			{
				if (ModPlayer.s_Module != null || (!ModPlayer.s_Module.Forbid))
				{
					m_Driver.Update();
				}
			}
		}

		public static void Driver_Pause(bool pause)
		{
			if (isplaying)
			{
				if (pause)
				{
					m_Driver.Pause();
				}
				else
				{
					m_Driver.Resume();
				}
			}
		}

		static internal void MikMod_DisableOutput_internal()
		{
			if (isplaying && m_Driver != null)
			{
				isplaying = false;
				m_Driver.PlayStop();
			}
		}

		public static bool MikMod_SetNumVoices_internal(int music, int sfx)
		{
			var resume = false;
			int t, oldchn = 0;

			if ((music == 0) && (sfx == 0))
			{
				return true;
			}

			if (isplaying)
			{
				MikMod_DisableOutput_internal();
				oldchn = md_numchn;
				resume = true;
			}

			if (sfxinfo != null)
			{
				sfxinfo = null;
			}

			if (md_sample != null)
			{
				md_sample = null;
			}

			if (music != -1)
			{
				md_sngchn = (byte)music;
			}

			if (sfx != -1)
			{
				md_sfxchn = (byte)sfx;
			}

			md_numchn = (byte)(md_sngchn + md_sfxchn);

			LimitHardVoices(m_Driver.HardVoiceLimit);
			LimitSoftVoices(m_Driver.SoftVoiceLimit);

			if (m_Driver.SetNumVoices())
			{
				MikMod_Exit_internal();
				md_numchn = md_softchn = md_hardchn = md_sfxchn = md_sngchn = 0;
				return true;
			}

			if ((md_sngchn + md_sfxchn) != 0)
			{
				md_sample = new Sample[md_sngchn + md_sfxchn];
				for (var i = 0; i < md_sngchn + md_sfxchn; i++)
				{
					md_sample[i] = new Sample();
				}
			}

			if (md_sfxchn != 0)
			{
				sfxinfo = new byte[md_sfxchn];
			}

			/* make sure the player doesn't start with garbage */
			for (t = oldchn; t < md_numchn; t++)
			{
				Voice_Stop_internal((byte)t);
			}

			if (resume)
			{
				MikMod_EnableOutput_internal();
			}

			return false;
		}

		static void MikMod_Exit_internal()
		{
			MikMod_DisableOutput_internal();
			m_Driver.Exit();
			md_numchn = md_sfxchn = md_sngchn = 0;

			if (sfxinfo != null)
			{
				sfxinfo = null;
			}

			if (md_sample != null)
			{
				md_sample = null;
			}

			initialized = false;
		}

		public static void MikMod_Exit() => MikMod_Exit_internal();

		static bool _mm_init(string cmdline)
		{
			m_Driver.CommandLine(cmdline);

			if (m_Driver.IsPresent())
			{
				if (m_Driver.Init())
				{
					MikMod_Exit_internal();
					m_Driver = null;
					throw new Exception("Failed to init driver!");
				}
			}
			else
			{
				throw new Exception("Driver not present!");
			}

			initialized = true;

			return false;
		}

		public static bool MikMod_Init(string cmdline) => _mm_init(cmdline);

		public static bool MikMod_Reset(string cmdline) => _mm_reset(cmdline);

		static bool _mm_reset(string cmdline)
		{
			var wasplaying = false;

			if (!initialized)
			{
				return _mm_init(cmdline);
			}

			if (isplaying)
			{
				wasplaying = true;
				m_Driver.PlayStop();
			}

			if (m_Driver.Reset())
			{
				MikMod_Exit_internal();
				return true;
			}

			if (wasplaying)
			{
				m_Driver.PlayStart();
			}

			return false;
		}

		#endregion
	}
}
