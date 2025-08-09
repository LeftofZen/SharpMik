using NAudio.Wave;
using SharpMik.Common;
using SharpMik.Player;
using System;

namespace SharpMik.Drivers
{
	class NAudioTrackerStream : WaveStream
	{
		readonly WaveFormat waveFormat;
		readonly NaudioDriver m_Driver;
		public NAudioTrackerStream(NaudioDriver driver)
		{
			var bitness = (ModDriver.Mode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS ? 16 : 8;
			var channels = (ModDriver.Mode & Constants.DMODE_STEREO) == Constants.DMODE_STEREO ? 2 : 1;

			waveFormat = new WaveFormat(ModDriver.MixFrequency, bitness, channels);

			m_Driver = driver;
		}

		public override long Position
		{
			get { return 0; }
			set {; }
		}

		public override long Length => 0;

		public override WaveFormat WaveFormat => waveFormat;

		public override int Read(byte[] buffer, int offset, int count) => m_Driver.GetBuffer(buffer, offset, count);
	}

	public class NaudioDriver : VirtualSoftwareDriver
	{
		IWavePlayer waveOut;
		NAudioTrackerStream m_NAudioStream;
		bool stopped;

		readonly object mutext = new();

		public NaudioDriver()
		{
			NextDriver = null;
			Name = "NAudio Driver";
			Version = "NAudio 1.0";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = true;
		}

		public override void CommandLine(string command)
		{

		}

		public int GetBuffer(byte[] buffer, int offset, int count)
		{
			lock (mutext)
			{
				uint done = 0;
				if (!stopped)
				{
					var buf = new sbyte[count];
					done = WriteBytes(buf, (uint)count);
					Buffer.BlockCopy(buf, 0, buffer, offset, count);
				}
				else
				{
					for (var i = 0; i < count; i++)
					{
						buffer[offset + i] = 0;
					}

					done = (uint)count;
				}

				return (int)done;
			}
		}

		public override bool IsPresent() => true;

		public override bool Init()
		{
			stopped = true;
			return base.Init();
		}

		public override void Exit()
		{

		}

		public override bool PlayStart()
		{
			lock (mutext)
			{
				if (waveOut == null)
				{
					waveOut = new DirectSoundOut(250);
					m_NAudioStream = new NAudioTrackerStream(this);
					waveOut.Init(m_NAudioStream);
				}

				waveOut.Play();

				stopped = false;

				return base.PlayStart();
			}
		}

		public override void PlayStop()
		{
			lock (mutext)
			{
				waveOut.Stop();
				waveOut.Dispose();
				waveOut = null;
				base.PlayStop();
			}
		}

		public override void Update()
		{

		}

		public override void Pause() => waveOut.Pause();

		public override void Resume() => waveOut.Play();
	}
}
