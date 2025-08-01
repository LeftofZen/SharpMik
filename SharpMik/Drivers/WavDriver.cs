using System.IO;
using SharpMik.Player;
using SharpMik.Extentions;
using SharpMik.Common;

namespace SharpMik.Drivers
{
	public class WavDriver : VirtualSoftwareDriver
	{
		BinaryWriter m_FileStream;

		string m_FileName = "music.wav";

		sbyte[] m_Audiobuffer;

		public static uint BUFFERSIZE = 32768;
		uint dumpsize;

		public WavDriver()
		{
			m_Next = null;
			m_Name = "Disk Wav Writer";
			m_Version = "Wav disk writer (music.wav) v1.0";
			m_HardVoiceLimit = 0;
			m_SoftVoiceLimit = 255;
			m_AutoUpdating = false;
		}

		public override void CommandLine(string command)
		{
			if (!string.IsNullOrEmpty(command))
			{
				m_FileName = command;
			}
		}

		public override bool IsPresent() => true;

		public override bool Init()
		{
			var stream = new FileStream(m_FileName, FileMode.Create);
			m_FileStream = new BinaryWriter(stream);
			m_Audiobuffer = new sbyte[BUFFERSIZE];

			ModDriver.Mode = (ushort)(ModDriver.Mode | Constants.DMODE_SOFT_MUSIC | Constants.DMODE_SOFT_SNDFX);

			putheader();
			return base.Init();
		}

		public override void Exit()
		{
			putheader();
			base.Exit();
			//putheader();
			m_FileStream.Close();
			m_FileStream.Dispose();
			m_FileStream = null;
		}

		int loc;

		public override void Update()
		{
			var done = WriteBytes(m_Audiobuffer, BUFFERSIZE);
			m_FileStream.Write(m_Audiobuffer, 0, (int)done);
			dumpsize += done;
			loc++;
		}

		void putheader()
		{
			m_FileStream.Seek(0, SeekOrigin.Begin);
			m_FileStream.Write("RIFF".ToCharArray());
			m_FileStream.Write(dumpsize + 44);
			m_FileStream.Write("WAVEfmt ".ToCharArray());
			m_FileStream.Write((uint)16);
			m_FileStream.Write((ushort)1);
			var channelCount = (ushort)((ModDriver.Mode & Constants.DMODE_STEREO) == Constants.DMODE_STEREO ? 2 : 1);
			var numberOfBytes = (ushort)((ModDriver.Mode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS ? 2 : 1);

			m_FileStream.Write(channelCount);
			m_FileStream.Write((uint)ModDriver.MixFrequency);
			var blah = ModDriver.MixFrequency * channelCount * numberOfBytes;
			m_FileStream.Write((uint)blah);
			m_FileStream.Write((ushort)(channelCount * numberOfBytes));
			m_FileStream.Write((ushort)((ModDriver.Mode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS ? 16 : 8));
			m_FileStream.Write("data".ToCharArray());
			m_FileStream.Write(dumpsize);
		}
	}
}
