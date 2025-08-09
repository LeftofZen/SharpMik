using SharpMik.Common;
using SharpMik.DSP;

namespace SharpMik.Interfaces
{
	public abstract class IModDriver
	{
		public IModDriver NextDriver { get; protected set; }
		public string Name { get; protected set; }
		public string Version { get; protected set; }
		public byte HardVoiceLimit { get; protected set; }
		public byte SoftVoiceLimit { get; protected set; }
		public bool AutoUpdating { get; protected set; }
		public Idsp DspProcessor { get; protected set; }

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
	}
}
