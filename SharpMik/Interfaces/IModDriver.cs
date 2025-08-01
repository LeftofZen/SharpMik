﻿using SharpMik.Common;
using SharpMik.DSP;

namespace SharpMik.Interfaces
{
	public abstract class IModDriver
	{
		#region protected variables
		protected IModDriver m_Next;
		protected string m_Name;
		protected string m_Version;

		protected byte m_HardVoiceLimit;
		protected byte m_SoftVoiceLimit;

		protected bool m_AutoUpdating;

		protected Idsp m_DspProcessor;
		#endregion

		#region public accessors
		public IModDriver NextDriver => m_Next;

		public string Name => m_Name;

		public string Version => m_Version;

		public byte HardVoiceLimit => m_HardVoiceLimit;

		public byte SoftVoiceLimit => m_SoftVoiceLimit;

		public bool AutoUpdating => m_AutoUpdating;

		public Idsp DspProcessor
		{
			get { return m_DspProcessor; }
			set { m_DspProcessor = value; }
		}
		#endregion

		#region abstract functions
		public abstract void CommandLine(string command);
		public abstract bool IsPresent();
		public abstract short SampleLoad(SampleLoad sample, int type);
		public abstract void SampleUnload(short handle);

		public abstract short[] GetSample(short handle);
		public abstract short SetSample(short[] sample);

		public abstract uint FreeSampleSpace(int value);
		public abstract uint RealSampleLength(int value, Sample sample);
		public abstract bool Init();
		public abstract void Exit();
		public abstract bool Reset();
		public abstract bool SetNumVoices();
		public abstract bool PlayStart();
		public abstract void PlayStop();
		public abstract void Update();

		public abstract void Pause();
		public abstract void Resume();

		public abstract void VoiceSetVolume(byte voice, ushort volume);
		public abstract ushort VoiceGetVolume(byte voice);
		public abstract void VoiceSetFrequency(byte voice, uint freq);
		public abstract uint VoiceGetFrequency(byte voice);
		public abstract void VoiceSetPanning(byte voice, uint panning);
		public abstract uint VoiceGetPanning(byte voice);
		public abstract void VoicePlay(byte voice, short handle, uint start, uint size, uint reppos, uint repend, ushort flags);
		public abstract void VoiceStop(byte voice);
		public abstract bool VoiceStopped(byte voice);
		public abstract int VoiceGetPosition(byte voice);
		public abstract uint VoiceRealVolume(byte voice);
		#endregion
	}
}
