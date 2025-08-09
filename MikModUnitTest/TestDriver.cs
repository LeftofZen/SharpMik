using SharpMik.Drivers;

namespace MikModUnitTest
{
	public class TestDriver : VirtualSoftwareDriver
	{
		byte[] m_CWav;
		sbyte[] m_Audiobuffer;
		public const uint BUFFERSIZE = 32768;

		long m_Place;

		public TestDriver()
		{
			NextDriver = null;
			Name = "Test Driver";
			Version = "Test Driver";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = false;
		}

		public bool Failed { get; private set; }

		public void SetCWav(byte[] data) => m_CWav = data;

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
			m_Place = 44;
			Failed = false;
			return base.PlayStart();
		}

		public override void Exit()
		{

		}

		public override void Update()
		{
			var done = WriteBytes(m_Audiobuffer, BUFFERSIZE);

			for (uint i = 0; i < done; i++)
			{
				if ((byte)m_Audiobuffer[i] != m_CWav[m_Place])
				{
					Failed = true;
					return;
				}

				m_Place++;
			}
		}

	}
}
