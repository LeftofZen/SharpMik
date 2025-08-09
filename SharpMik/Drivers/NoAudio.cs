namespace SharpMik.Drivers
{
	public class NoAudio : VirtualSoftwareDriver
	{
		readonly uint BUFFERSIZE = 32768;
		readonly sbyte[] m_buffer;

		public NoAudio()
		{
			NextDriver = null;
			Name = "No Audio Driver";
			Version = "No Audio v1.0";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = false;
			m_buffer = new sbyte[BUFFERSIZE];
		}

		public override void CommandLine(string command)
		{ }

		public override bool IsPresent() => true;

		public override bool Init() => base.Init();

		public override void Exit() => base.Exit();

		public override void Update() => _ = WriteBytes(m_buffer, BUFFERSIZE);
	}
}
