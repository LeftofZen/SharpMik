using SharpMik.Common;
using SharpMik.Drivers;
using System.IO;

namespace SharpMik.Player
{
	public class ModStream : Stream
	{
		public Module Module { get; }

		private MikMod Player { get; }
		private readonly PullStreamDriver m_PullStream;

		public ModStream(Stream toPlay)
		{
			Player = new MikMod();
			var result = false;
			m_PullStream = Player.Init<PullStreamDriver>("", out result);

			Module = Player.Play(toPlay);
		}

		public override bool CanRead => m_PullStream.Stream.CanRead;

		public override bool CanSeek => m_PullStream.Stream.CanSeek;

		public override bool CanWrite => m_PullStream.Stream.CanWrite;

		public override long Length => m_PullStream.Stream.Length;

		public override long Position
		{
			get
			{
				return m_PullStream.Stream.Position;
			}

			set
			{

			}
		}

		public override void Flush()
		{

		}

		public override int Read(byte[] buffer, int offset, int count) => m_PullStream.Stream.Read(buffer, offset, count);

		public override long Seek(long offset, SeekOrigin origin) => 0;

		public override void SetLength(long value)
		{

		}

		public override void Write(byte[] buffer, int offset, int count)
		{

		}
	}
}
