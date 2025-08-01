﻿using System;
using SharpMik.Interfaces;
using SharpMik.SoftwareMixers;
using SharpMik.Player;
using SharpMik.Common;

namespace SharpMik.Drivers
{
	public class VirtualSoftwareDriver : IModDriver
	{
		CommonSoftwareMixer m_SoftwareMixer;

		public override void CommandLine(string command)
		{

		}

		public override bool Init()
		{
			SetupMixer();
			return m_SoftwareMixer.Init();
		}

		private void SetupMixer()
		{
			if (m_SoftwareMixer != null)
			{
				if ((ModDriver.Mode & Constants.DMODE_HQMIXER) != 0)
				{
					if (m_SoftwareMixer is HQSoftwareMixer)
					{
						return;
					}
				}
				else
				{
					if (m_SoftwareMixer is LQSoftwareMixer)
					{
						return;
					}
				}

				m_SoftwareMixer.DeInit();
				m_SoftwareMixer = null;
			}

			if ((ModDriver.Mode & Constants.DMODE_HQMIXER) != 0)
			{
				m_SoftwareMixer = new HQSoftwareMixer();
			}
			else
			{
				m_SoftwareMixer = new LQSoftwareMixer();
			}
		}

		public virtual uint WriteBytes(sbyte[] buf, uint todo) => m_SoftwareMixer.WriteBytes(buf, todo);

		public override bool IsPresent() => throw new NotImplementedException();

		public override short SampleLoad(SampleLoad sample, int type) => m_SoftwareMixer.SampleLoad(sample, type);

		public override void SampleUnload(short handle) => m_SoftwareMixer.SampleUnload(handle);

		public override short[] GetSample(short handle) => throw new NotImplementedException();

		public override short SetSample(short[] sample) => throw new NotImplementedException();

		public override uint FreeSampleSpace(int value) => m_SoftwareMixer.FreeSampleSpace(value);

		public override uint RealSampleLength(int value, Sample sample) => CommonSoftwareMixer.RealSampleLength(value, sample);

		public override void Exit() => m_SoftwareMixer.DeInit();

		public override bool Reset() => false;

		public override bool SetNumVoices() => m_SoftwareMixer.SetNumVoices();

		public override bool PlayStart() => m_SoftwareMixer.PlayStart();

		public override void PlayStop()
		{

		}

		public override void Update()
		{

		}

		public override void Pause()
		{

		}

		public override void Resume()
		{

		}

		public override void VoiceSetVolume(byte voice, ushort volume) => m_SoftwareMixer.VoiceSetVolume(voice, volume);

		public override ushort VoiceGetVolume(byte voice) => m_SoftwareMixer.VoiceGetVolume(voice);

		public override void VoiceSetFrequency(byte voice, uint freq) => m_SoftwareMixer.VoiceSetFrequency(voice, freq);

		public override uint VoiceGetFrequency(byte voice) => m_SoftwareMixer.VoiceGetFrequency(voice);

		public override void VoiceSetPanning(byte voice, uint panning) => m_SoftwareMixer.VoiceSetPanning(voice, panning);

		public override uint VoiceGetPanning(byte voice) => m_SoftwareMixer.VoiceGetPanning(voice);

		public override void VoicePlay(byte voice, short handle, uint start, uint size, uint reppos, uint repend, ushort flags) => m_SoftwareMixer.VoicePlay(voice, handle, start, size, reppos, repend, flags);

		public override void VoiceStop(byte voice) => m_SoftwareMixer.VoiceStop(voice);

		public override bool VoiceStopped(byte voice) => m_SoftwareMixer.VoiceStopped(voice);

		public override int VoiceGetPosition(byte voice) => m_SoftwareMixer.VoiceGetPosition(voice);

		public override uint VoiceRealVolume(byte voice) => m_SoftwareMixer.VoiceRealVolume(voice);
	}
}
