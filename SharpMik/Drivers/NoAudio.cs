namespace SharpMik.Drivers
{
	public class NoAudio : VirtualSoftwareDriver
	{
		readonly uint BUFFERSIZE = 32768;
		readonly sbyte[] m_buffer;
		public NoAudio()
		{
			m_Next = null;
			m_Name = "No Audio Driver";
			m_Version = "No Audio v1.0";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = false;
			m_buffer = new sbyte[BUFFERSIZE];
		}

		public override void CommandLine(string command)
		{

		}

		public override bool IsPresent() => true;

		public override bool Init() => base.Init();

		public override void Exit() => base.Exit();

		public override void Update()
		{
			var done = WriteBytes(m_buffer, BUFFERSIZE);
		}
	}
}
