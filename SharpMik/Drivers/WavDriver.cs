using SharpMik.Common;
using SharpMik.Extensions;
using SharpMik.Player;
using System.IO;

namespace SharpMik.Drivers
{
	public class WavDriver : VirtualSoftwareDriver
	{
		BinaryWriter fileStream;

		string fileName = "music.wav";

		sbyte[] audiobuffer;

		public static uint BUFFERSIZE = 32768;
		uint dumpsize;

		public WavDriver()
		{
			NextDriver = null;
			Name = "Disk Wav Writer";
			Version = "Wav disk writer (music.wav) v1.0";
			HardVoiceLimit = 0;
			SoftVoiceLimit = 255;
			AutoUpdating = false;
		}

		public override void CommandLine(string command)
		{
			if (!string.IsNullOrEmpty(command))
			{
				fileName = command;
			}
		}

		public override bool IsPresent() => true;

		public override bool Init()
		{
			var stream = new FileStream(fileName, FileMode.Create);
			fileStream = new BinaryWriter(stream);
			audiobuffer = new sbyte[BUFFERSIZE];

			ModDriver.Mode = (ushort)(ModDriver.Mode | Constants.DMODE_SOFT_MUSIC | Constants.DMODE_SOFT_SNDFX);

			PutHeader();
			return base.Init();
		}

		public override void Exit()
		{
			PutHeader();
			base.Exit();
			//putheader();
			fileStream.Close();
			fileStream.Dispose();
			fileStream = null;
		}

		int loc;

		public override void Update()
		{
			var done = WriteBytes(audiobuffer, BUFFERSIZE);
			fileStream.Write(audiobuffer, 0, (int)done);
			dumpsize += done;
			loc++;
		}

		void PutHeader()
		{
			_ = fileStream.Seek(0, SeekOrigin.Begin);
			fileStream.Write("RIFF".ToCharArray());
			fileStream.Write(dumpsize + 44);
			fileStream.Write("WAVEfmt ".ToCharArray());
			fileStream.Write((uint)16);
			fileStream.Write((ushort)1);
			var channelCount = (ushort)((ModDriver.Mode & Constants.DMODE_STEREO) == Constants.DMODE_STEREO ? 2 : 1);
			var numberOfBytes = (ushort)((ModDriver.Mode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS ? 2 : 1);

			fileStream.Write(channelCount);
			fileStream.Write((uint)ModDriver.MixFrequency);
			var blah = ModDriver.MixFrequency * channelCount * numberOfBytes;
			fileStream.Write((uint)blah);
			fileStream.Write((ushort)(channelCount * numberOfBytes));
			fileStream.Write((ushort)((ModDriver.Mode & Constants.DMODE_16BITS) == Constants.DMODE_16BITS ? 16 : 8));
			fileStream.Write("data".ToCharArray());
			fileStream.Write(dumpsize);
		}
	}
}
