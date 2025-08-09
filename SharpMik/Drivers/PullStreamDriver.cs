using System;
using System.IO;

namespace SharpMik.Drivers
{

	public class PullAudioStream : Stream
	{
		readonly PullStreamDriver m_StreamDriver;

		public PullAudioStream(PullStreamDriver driver) => m_StreamDriver = driver;

		public override bool CanRead => m_StreamDriver.IsPlaying;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override long Length => -1;

		public override long Position
		{
			get
			{
				return 0;
			}

			set
			{

			}
		}

		public override void Flush()
		{

		}

		public override int Read(byte[] buffer, int offset, int count) => (int)m_StreamDriver.GetData(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => 0;

		public override void SetLength(long value)
		{

		}

		public override void Write(byte[] buffer, int offset, int count)
		{

		}
	}

	public class PullStreamDriver : VirtualSoftwareDriver
	{
		sbyte[] m_TempBuffer;

		public bool IsPlaying { get; set; }

		public Stream Stream { get; set; }

		public PullStreamDriver()
		{
			NextDriver = null;
			Name = "Pull Audio Stream";
			Version = "1";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = true;
			IsPlaying = false;
			Stream = new PullAudioStream(this);

		}

		public override void CommandLine(string command)
		{

		}

		public override bool IsPresent() => true;

		public override bool PlayStart()
		{
			IsPlaying = true;
			return base.PlayStart();
		}

		public override void PlayStop()
		{
			base.PlayStop();
			IsPlaying = false;
		}

		public override void Pause()
		{
			base.Pause();
			IsPlaying = !IsPlaying;
		}

		public uint GetData(byte[] buffer, int offset, int count)
		{
			if (m_TempBuffer == null)
			{
				m_TempBuffer = new sbyte[count];
			}
			else if (m_TempBuffer.Length < count)
			{
				Array.Resize(ref m_TempBuffer, count);
			}

			var done = WriteBytes(m_TempBuffer, (uint)count);
			Array.Copy(m_TempBuffer, buffer, done);

			return done;
		}
	}
}
