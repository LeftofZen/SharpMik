using SharpMik.Extensions;
using System.IO;

namespace SharpMik.Drivers
{

	public class PushStreamDriver : VirtualSoftwareDriver
	{
		sbyte[] m_Audiobuffer;

		public static uint BUFFERSIZE = 32768;

		public MemoryStream MemoryStream { get; private set; }

		public PushStreamDriver()
		{
			NextDriver = null;
			Name = "Mem Writer";
			Version = "Mem stream writer";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = false;
		}

		public override void CommandLine(string command)
		{
		}

		public override bool IsPresent() => true;

		public override bool Init()
		{
			m_Audiobuffer = new sbyte[BUFFERSIZE];

			return base.Init();
		}

		public override void PlayStop() => base.PlayStop();

		public override bool PlayStart()
		{
			MemoryStream = new MemoryStream();
			return base.PlayStart();
		}

		public override void Exit()
		{

		}

		public override void Update()
		{
			var done = WriteBytes(m_Audiobuffer, BUFFERSIZE);
			MemoryStream.Write(m_Audiobuffer, 0, (int)done);
		}
	}
}
