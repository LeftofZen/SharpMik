using SharpMik.Attributes;
using SharpMik.Common;
using SharpMik.Interfaces;
using System.IO;

namespace SharpMik.Loaders
{
	[ModFileExtensions(".669")]
	public class _669Loader : IModLoader
	{
		class S69HEADER
		{
			public byte[] marker = new byte[2];
			public string message;
			public byte nos;
			public byte nop;
			public byte looporder;
			public byte[] orders = new byte[0x80];
			public byte[] tempos = new byte[0x80];
			public byte[] breaks = new byte[0x80];
		}

		/* sample information */
		class S69SAMPLE
		{
			public string filename;
			public int length;
			public int loopbeg;
			public int loopend;
		}

		/* encoded note */
		class S69NOTE
		{
			public byte a, b, c;
		}

		/*========== Loader variables */

		/* current pattern */
		S69NOTE[] s69pat;
		/* Module header */
		S69HEADER mh;

		/* file type identification */
		static readonly string[] S69_Version = [
				"Composer 669",
				"Extended 669"
			];

		public _669Loader()
		{
			m_ModuleType = "669";
			m_ModuleVersion = "669 (Composer 669, Unis 669)";
		}

		public override bool Init()
		{
			s69pat = new S69NOTE[64 * 8];

			for (var i = 0; i < s69pat.Length; i++)
			{
				s69pat[i] = new S69NOTE();
			}

			mh = new S69HEADER();

			return true;
		}

		public override bool Test()
		{
			var buf = new byte[0x80];
			if (!m_Reader.Read_bytes(buf, 2))
			{
				return false;
			}

			/* look for id */
			if ((buf[0] == 'i' && buf[1] == 'f') || (buf[0] == 'J' && buf[1] == 'N'))
			{

				int i;

				/* skip song message */
				_ = m_Reader.Seek(108, SeekOrigin.Current);

				/* sanity checks */
				if (m_Reader.Read_byte() > 64)
				{
					return false;
				}

				if (m_Reader.Read_byte() > 128)
				{
					return false;
				}

				if (m_Reader.Read_byte() > 127)
				{
					return false;
				}

				/* check order table */
				if (!m_Reader.Read_bytes(buf, 0x80))
				{
					return false;
				}

				for (i = 0; i < 0x80; i++)
				{
					if (buf[i] is >= 0x80 and not 0xff)
					{
						return false;
					}
				}

				/* check tempos table */
				if (!m_Reader.Read_bytes(buf, 0x80))
				{
					return false;
				}

				for (i = 0; i < 0x80; i++)
				{
					if (buf[i] is 0 or > 32)
					{
						return false;
					}
				}

				/* check pattern length table */
				if (!m_Reader.Read_bytes(buf, 0x80))
				{
					return false;
				}

				for (i = 0; i < 0x80; i++)
				{
					if (buf[i] > 0x3f)
					{
						return false;
					}
				}
			}
			else
			{
				return false;
			}

			return true;
		}

		public override bool Load(int curious)
		{
			int i;
			Sample current;
			S69SAMPLE sample;

			/* module header */
			_ = m_Reader.Read_bytes(mh.marker, 2);
			mh.message = m_Reader.Read_String(108);

			mh.nos = m_Reader.Read_byte();
			mh.nop = m_Reader.Read_byte();
			mh.looporder = m_Reader.Read_byte();
			_ = m_Reader.Read_bytes(mh.orders, 0x80);

			for (i = 0; i < 0x80; i++)
			{
				if (mh.orders[i] is >= 0x80 and not 0xff)
				{
					m_LoadError = MMERR_NOT_A_MODULE;
					return true;
				}
			}

			_ = m_Reader.Read_bytes(mh.tempos, 0x80);
			for (i = 0; i < 0x80; i++)
			{
				if (mh.tempos[i] is 0 or > 32)
				{
					m_LoadError = MMERR_NOT_A_MODULE;
					return true;
				}
			}

			_ = m_Reader.Read_bytes(mh.breaks, 0x80);

			for (i = 0; i < 0x80; i++)
			{
				if (mh.breaks[i] > 0x3f)
				{
					m_LoadError = MMERR_NOT_A_MODULE;
					return true;
				}
			}

			/* set module variables */
			m_Module.InitialSpeed = 4;
			m_Module.InitialTempo = 78;
			m_Module.SongName = mh.message[..36];
			if (mh.marker[0] == 'i' && mh.marker[1] == 'f')
			{
				m_Module.ModType = S69_Version[0];
			}
			else if (mh.marker[0] == 'J' && mh.marker[1] == 'N')
			{
				m_Module.ModType = S69_Version[1];
			}

			m_Module.NumChannels = 8;
			m_Module.NumPatterns = mh.nop;
			m_Module.NumInstruments = m_Module.NumSamples = mh.nos;
			m_Module.NumTracks = (ushort)(m_Module.NumChannels * m_Module.NumPatterns);
			m_Module.Flags = Constants.UF_XMPERIODS | Constants.UF_LINEAR;

			var message = new char[108];

			for (var j = 0; j < message.Length; j++)
			{
				if (j < mh.message.Length)
				{
					message[j] = mh.message[j];
				}
				else
				{
					message[j] = (char)0;
				}
			}

			for (i = 35; (i >= 0) && (message[i] == ' '); i--)
			{
				message[i] = (char)0;
			}

			for (i = 36 + 35; (i >= 36 + 0) && (message[i] == ' '); i--)
			{
				message[i] = (char)0;
			}

			for (i = 72 + 35; (i >= 72 + 0) && (message[i] == ' '); i--)
			{
				message[i] = (char)0;
			}

			if ((message[0] != 0) || (message[36] != 0) || (message[72] != 0))
			{
				m_Module.Comment = new string(message, 0, 36);
				m_Module.Comment += "\r";
				if (message[36] != 0)
				{
					m_Module.Comment += new string(message, 36, 36);
				}

				m_Module.Comment += "\r";

				if (mh.message[72] != 0)
				{
					m_Module.Comment += new string(message, 72, 36);
				}

				m_Module.Comment += "\r";
			}

			// hmm this differs, could be an issue.

			m_Module.Positions = new ushort[0x80];

			for (i = 0; i < 0x80; i++)
			{
				if (mh.orders[i] >= mh.nop)
				{
					break;
				}

				m_Module.Positions[i] = mh.orders[i];
			}

			m_Module.NumPositions = (ushort)i;
			m_Module.RestartPosition = (ushort)(mh.looporder < m_Module.NumPositions ? mh.looporder : 0);

			m_Module.AllocSamples();

			for (i = 0; i < m_Module.NumInstruments; i++)
			{
				current = m_Module.Samples[i];
				sample = new S69SAMPLE
				{
					/* sample information */
					filename = m_Reader.Read_String(13),
					length = m_Reader.Read_Intel_int(),
					loopbeg = m_Reader.Read_Intel_int(),
					loopend = m_Reader.Read_Intel_int()
				};

				if (sample.loopend == 0xfffff)
				{
					sample.loopend = 0;
				}

				if ((sample.length < 0) || (sample.loopbeg < -1) || (sample.loopend < -1))
				{
					m_LoadError = MMERR_LOADING_HEADER;
					return false;
				}

				current.samplename = sample.filename;
				current.seekpos = 0;
				current.speed = 0;
				current.length = (uint)sample.length;
				current.loopstart = (uint)sample.loopbeg;
				current.loopend = (uint)sample.loopend;
				current.flags = (ushort)((sample.loopbeg < sample.loopend) ? Constants.SF_LOOP : 0);
				current.volume = 64;
			}

			if (!S69_LoadPatterns())
			{
				return false;
			}

			return true;
		}

		bool S69_LoadPatterns()
		{
			int track, row, channel;
			byte note, inst, vol, effect, lastfx, lastval;
			var tracks = 0;

			m_Module.AllocPatterns();
			m_Module.AllocTracks();

			for (track = 0; track < m_Module.NumPatterns; track++)
			{
				/* set pattern break locations */
				m_Module.PatternRows[track] = (ushort)(mh.breaks[track] + 1);

				/* load the 669 pattern */

				var place = 0;
				for (row = 0; row < 64; row++)
				{
					for (channel = 0; channel < 8; channel++, place++)
					{
						s69pat[place].a = m_Reader.Read_byte();
						s69pat[place].b = m_Reader.Read_byte();
						s69pat[place].c = m_Reader.Read_byte();
					}
				}

				if (m_Reader.isEOF())
				{
					m_LoadError = MMERR_LOADING_PATTERN;
					return false;
				}

				/* translate the pattern */
				for (channel = 0; channel < 8; channel++)
				{
					UniReset();
					/* set pattern tempo */
					UniPTEffect(0xf, 78);
					UniPTEffect(0xf, mh.tempos[track]);

					lastfx = 0xff;
					lastval = 0;

					for (row = 0; row <= mh.breaks[track]; row++)
					{
						int a, b, c;

						/* fetch the encoded note */
						a = s69pat[(row * 8) + channel].a;
						b = s69pat[(row * 8) + channel].b;
						c = s69pat[(row * 8) + channel].c;

						/* decode it */
						note = (byte)(a >> 2);
						inst = (byte)(((a & 0x3) << 4) | ((b & 0xf0) >> 4));
						vol = (byte)(b & 0xf);

						if (a < 0xff)
						{
							if (a < 0xfe)
							{
								UniInstrument(inst);
								UniNote(note + (2 * Constants.Octave));
								lastfx = 0xff; /* reset background effect memory */
							}

							UniPTEffect(0xc, vol << 2);
						}

						if ((c != 0xff) || (lastfx != 0xff))
						{
							if (c == 0xff)
							{
								c = lastfx;
								effect = lastval;
							}
							else
							{
								effect = (byte)(c & 0xf);
							}

							switch (c >> 4)
							{
								case 0: /* porta up */
									UniPTEffect(0x1, effect);
									lastfx = (byte)c;
									lastval = effect;
									break;
								case 1: /* porta down */
									UniPTEffect(0x2, effect);
									lastfx = (byte)c;
									lastval = effect;
									break;
								case 2: /* porta to note */
									UniPTEffect(0x3, effect);
									lastfx = (byte)c;
									lastval = effect;
									break;
								case 3: /* frequency adjust */
									/* DMP converts this effect to S3M FF1. Why not ? */
									UniEffect((int)Commands.UNI_S3MEFFECTF, 0xf0 | effect);
									break;
								case 4: /* vibrato */
									UniPTEffect(0x4, effect);
									lastfx = (byte)c;
									lastval = effect;
									break;
								case 5: /* set speed */
									if (effect != 0)
									{
										UniPTEffect(0xf, effect);
									}
									else
									  if (mh.marker[0] != 0x69)
									{
										m_LoadError = string.Format("\r669: unsupported super fast tempo at pat={0} row={1} chan={2}\n", track, row, channel);
									}

									break;
							}
						}

						UniNewline();
					}

					m_Module.Tracks[tracks++] = UniDup();
				}
			}

			return true;
		}

		public override void Cleanup()
		{
			mh = null;
			s69pat = null;
		}

		public override string LoadTitle()
		{
			_ = m_Reader.Seek(2, SeekOrigin.Begin);
			var name = m_Reader.Read_String(36);
			return name;
		}
	}
}
