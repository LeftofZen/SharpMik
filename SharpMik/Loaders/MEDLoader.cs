using System;
using SharpMik.Interfaces;
using System.IO;
using SharpMik.Attributes;
using SharpMik.Common;

namespace SharpMik.Loaders
{
	[ModFileExtentions(".med")]
	public class MEDLoader : IModLoader
	{

		/*========== Module information */
		class MEDHEADER
		{
			public uint id;
			public uint modlen;
			public uint MEDSONGP;               /* struct MEDSONG *song; */
			public ushort psecnum;              /* for the player routine, MMD2 only */
			public ushort pseq;                 /*  "   "   "   " */
			public uint MEDBlockPP;         /* struct MEDBlock **blockarr; */
			public uint reserved1;
			public uint MEDINSTHEADERPP;        /* struct MEDINSTHEADER **smplarr; */
			public uint reserved2;
			public uint MEDEXPP;                /* struct MEDEXP *expdata; */
			public uint reserved3;
			public ushort pstate;               /* some data for the player routine */
			public ushort pblock;
			public ushort pline;
			public ushort pseqnum;
			public short actplayline;
			public byte counter;
			public byte extra_songs;            /* number of songs - 1 */
		}

		class MEDSAMPLE
		{
			public ushort rep, replen;          /* offs: 0(s), 2(s) */
			public byte midich;             /* offs: 4(s) */
			public byte midipreset;         /* offs: 5(s) */
			public byte svol;                   /* offs: 6(s) */
			public sbyte strans;                /* offs: 7(s) */
		}

		class MEDSONG
		{
			public MEDSONG()
			{
				sample = new MEDSAMPLE[63];

				for (var i = 0; i < sample.Length; i++)
				{
					sample[i] = new MEDSAMPLE();
				}
			}
			public MEDSAMPLE[] sample;      /* 63 * 8 bytes = 504 bytes */
			public ushort numblocks;            /* offs: 504 */
			public ushort songlen;              /* offs: 506 */
			public byte[] playseq = new byte[256];          /* offs: 508 */
			public ushort deftempo;             /* offs: 764 */
			public sbyte playtransp;            /* offs: 766 */
			public byte flags;              /* offs: 767 */
			public byte flags2;             /* offs: 768 */
			public byte tempo2;             /* offs: 769 */
			public byte[] trkvol = new byte[16];            /* offs: 770 */
			public byte mastervol;          /* offs: 786 */
			public byte numsamples;         /* offs: 787 */
		}

		class MEDEXP
		{
			public uint nextmod;                /* pointer to next module */
			public uint exp_smp;                /* pointer to MEDINSTEXT array */
			public ushort s_ext_entries;
			public ushort s_ext_entrsz;
			public uint annotxt;                /* pointer to annotation text */
			public uint annolen;
			public uint iinfo;              /* pointer to MEDINSTINFO array */
			public ushort i_ext_entries;
			public ushort i_ext_entrsz;
			public uint jumpmask;
			public uint rgbtable;
			public uint channelsplit;
			public uint n_info;
			public uint SongName;               /* pointer to SongName */
			public uint songnamelen;
			public uint dumps;
			public uint[] reserved2 = new uint[7];
		}

		class MMD0NOTE
		{
			public byte a, b, c;

			public void Clear() => a = b = c = 0;
		}

		class MMD1NOTE
		{
			public byte a, b, c, d;

			public void Clear() => a = b = c = d = 0;
		}

		class MEDINSTHEADER
		{
			public uint length;
			public short type;
			/* Followed by actual data */
		}

		class MEDINSTEXT
		{
			public byte hold;
			public byte decay;
			public byte suppress_midi_off;
			public sbyte finetune;
		}

		class MEDINSTINFO
		{
			public byte[] name = new byte[40];
		}

		/*========== Loader variables */

		const int MMD0_string = 0x4D4D4430;
		const int MMD1_string = 0x4D4D4431;

		MEDHEADER mh;
		MEDSONG ms;
		MEDEXP me;
		uint[] ba;
		MMD0NOTE[] mmd0pat;
		MMD1NOTE[] mmd1pat;

		static bool decimalvolumes;
		static bool bpmtempos;

		static readonly char[] MED_Version = "OctaMED (MMDx)".ToCharArray();

		public MEDLoader()
		{
			m_ModuleType = "MED";
			m_ModuleVersion = "MED (OctaMED)";
		}

		public override bool Test()
		{
			var id = m_Reader.Read_String(4);

			return id is "MMD0" or "MMD1";
		}

		public override bool Init()
		{
			me = new MEDEXP();
			mh = new MEDHEADER();
			ms = new MEDSONG();

			return true;
		}

		public override void Cleanup()
		{
			me = null;
			mh = null;
			ms = null;
			ba = null;
			mmd0pat = null;
			mmd1pat = null;
		}

		void EffectCvt(byte eff, byte dat)
		{
			switch (eff)
			{
				/* 0x0 0x1 0x2 0x3 0x4 PT effects */
				case 0x5:               /* PT vibrato with speed/depth nibbles swapped */
					UniPTEffect(0x4, (dat >> 4) | ((dat & 0xf) << 4));
					break;
				/* 0x6 0x7 not used */
				case 0x6:
				case 0x7:
					break;
				case 0x8:               /* midi hold/decay */
					break;
				case 0x9:
					if (bpmtempos)
					{
						if (dat == 0)
						{
							dat = m_Module.InitialSpeed;
						}

						UniEffect(Commands.UNI_S3MEFFECTA, dat);
					}
					else
					{
						if (dat <= 0x20)
						{
							if (dat == 0)
							{
								dat = m_Module.InitialSpeed;
							}
							else
							{
								dat /= 4;
							}

							UniPTEffect(0xf, dat);
						}
						else
						{
							UniEffect(Commands.UNI_MEDSPEED, dat * 125 / (33 * 4));
						}
					}

					break;
				/* 0xa 0xb PT effects */
				case 0xc:
					if (decimalvolumes)
					{
						dat = (byte)(((dat >> 4) * 10) + (dat & 0xf));
					}

					UniPTEffect(0xc, dat);
					break;
				case 0xd:               /* same as PT volslide */
					UniPTEffect(0xa, dat);
					break;
				case 0xe:               /* synth jmp - midi */
					break;
				case 0xf:
					switch (dat)
					{
						case 0:             /* patternbreak */
							UniPTEffect(0xd, 0);
							break;
						case 0xf1:          /* play note twice */
							UniWriteByte(Commands.UNI_MEDEFFECTF1);
							break;
						case 0xf2:          /* delay note */
							UniWriteByte(Commands.UNI_MEDEFFECTF2);
							break;
						case 0xf3:          /* play note three times */
							UniWriteByte(Commands.UNI_MEDEFFECTF3);
							break;
						case 0xfe:          /* stop playing */
							UniPTEffect(0xb, m_Module.NumPatterns);
							break;
						case 0xff:          /* note cut */
							UniPTEffect(0xc, 0);
							break;
						default:
							if (dat <= 10)
							{
								UniPTEffect(0xf, dat);
							}
							else if (dat <= 240)
							{
								if (bpmtempos)
								{
									UniPTEffect(0xf, (dat < 32) ? 32 : dat);
								}
								else
								{
									UniEffect(Commands.UNI_MEDSPEED, dat * 125 / 33);
								}
							}

							break;
					}

					break;
				default:                    /* all normal PT effects are handled here */
					UniPTEffect(eff, dat);
					break;
			}
		}

		byte[] MED_Convert1(int count, int col)
		{
			int t;
			byte inst, note, eff, dat;
			MMD1NOTE n;

			UniReset();
			for (t = 0; t < count; t++)
			{
				n = mmd1pat[(t * m_Module.NumChannels) + col];

				note = (byte)(n.a & 0x7f);
				inst = (byte)(n.b & 0x3f);
				eff = (byte)(n.c & 0xf);
				dat = n.d;

				if (inst != 0)
				{
					UniInstrument(inst - 1);
				}

				if (note != 0)
				{
					UniNote(note + (3 * Constants.Octave) - 1);
				}

				EffectCvt(eff, dat);
				UniNewline();
			}

			return UniDup();
		}

		byte[] MED_Convert0(int count, int col)
		{
			int t;
			byte a, b, inst, note, eff, dat;
			MMD0NOTE n;

			UniReset();
			for (t = 0; t < count; t++)
			{
				n = mmd0pat[(t * m_Module.NumChannels) + col];
				a = n.a;
				b = n.b;

				note = (byte)(a & 0x3f);
				a >>= 6;
				a = (byte)(((a & 1) << 1) | (a >> 1));
				inst = (byte)((b >> 4) | (a << 4));
				eff = (byte)(b & 0xf);
				dat = n.c;

				if (inst != 0)
				{
					UniInstrument(inst - 1);
				}

				if (note != 0)
				{
					UniNote(note + (3 * Constants.Octave) - 1);
				}

				EffectCvt(eff, dat);
				UniNewline();
			}

			return UniDup();
		}

		bool LoadMEDPatterns()
		{
			int t, row, col;
			ushort numtracks, numlines, maxlines = 0, track = 0;
			MMD0NOTE mmdp;

			/* first, scan patterns to see how many channels are used */
			for (t = 0; t < m_Module.NumPatterns; t++)
			{
				m_Reader.Seek((int)ba[t], SeekOrigin.Begin);

				numtracks = m_Reader.Read_byte();
				numlines = m_Reader.Read_byte();

				if (numtracks > m_Module.NumChannels)
				{
					m_Module.NumChannels = (byte)numtracks;
				}

				if (numlines > maxlines)
				{
					maxlines = numlines;
				}
			}

			m_Module.NumTracks = (ushort)(m_Module.NumPatterns * m_Module.NumChannels);
			m_Module.AllocTracks();
			m_Module.AllocPatterns();

			mmd0pat = new MMD0NOTE[m_Module.NumChannels * (maxlines + 1)];
			for (var i = 0; i < mmd0pat.Length; i++)
			{
				mmd0pat[i] = new MMD0NOTE();
			}

			/* second read: read and convert patterns */
			for (t = 0; t < m_Module.NumPatterns; t++)
			{
				m_Reader.Seek((int)ba[t], SeekOrigin.Begin);
				numtracks = m_Reader.Read_byte();
				numlines = m_Reader.Read_byte();

				m_Module.PatternRows[t] = ++numlines;
				for (var i = 0; i < mmd0pat.Length; i++)
				{
					mmd0pat[i].Clear();
				}

				var place = 0;
				for (row = numlines; row != 0; row--)
				{
					for (col = numtracks; col != 0; col--)
					{
						mmdp = mmd0pat[place];
						mmdp.a = m_Reader.Read_byte();
						mmdp.b = m_Reader.Read_byte();
						mmdp.c = m_Reader.Read_byte();
						place++;
					}
				}

				for (col = 0; col < m_Module.NumChannels; col++)
				{
					m_Module.Tracks[track++] = MED_Convert0(numlines, col);
				}
			}

			return true;
		}

		bool LoadMMD1Patterns()
		{
			int t, row, col;
			ushort numtracks, numlines, maxlines = 0, track = 0;
			MMD1NOTE mmdp;

			/* first, scan patterns to see how many channels are used */
			for (t = 0; t < m_Module.NumPatterns; t++)
			{
				m_Reader.Seek((int)ba[t], SeekOrigin.Begin);
				numtracks = m_Reader.Read_Motorola_ushort();
				numlines = m_Reader.Read_Motorola_ushort();
				if (numtracks > m_Module.NumChannels)
				{
					m_Module.NumChannels = (byte)numtracks;
				}

				if (numlines > maxlines)
				{
					maxlines = numlines;
				}
			}

			m_Module.NumTracks = (ushort)(m_Module.NumPatterns * m_Module.NumChannels);
			m_Module.AllocTracks();
			m_Module.AllocPatterns();

			mmd1pat = new MMD1NOTE[m_Module.NumChannels * (maxlines + 1)];

			for (var i = 0; i < mmd1pat.Length; i++)
			{
				mmd1pat[i] = new MMD1NOTE();
			}

			/* second read: really read and convert patterns */
			for (t = 0; t < m_Module.NumPatterns; t++)
			{
				m_Reader.Seek((int)ba[t], SeekOrigin.Begin);
				numtracks = m_Reader.Read_Motorola_ushort();
				numlines = m_Reader.Read_Motorola_ushort();
				m_Reader.Seek(sizeof(uint), SeekOrigin.Current);

				m_Module.PatternRows[t] = ++numlines;
				var place = 0;
				for (var i = 0; i < mmd1pat.Length; i++)
				{
					mmd1pat[i].Clear();
				}

				for (row = numlines; row != 0; row--)
				{
					for (col = numtracks; col != 0; col--)
					{
						mmdp = mmd1pat[place];
						mmdp.a = m_Reader.Read_byte();
						mmdp.b = m_Reader.Read_byte();
						mmdp.c = m_Reader.Read_byte();
						mmdp.d = m_Reader.Read_byte();
						place++;
					}
				}

				for (col = 0; col < m_Module.NumChannels; col++)
				{
					m_Module.Tracks[track++] = MED_Convert1(numlines, col);
				}
			}

			return true;
		}

		public override bool Load(int curious)
		{
			int t;
			var sa = new uint[64];
			MEDINSTHEADER s;
			Sample q;
			MEDSAMPLE mss;

			/* try to read module header */
			mh.id = m_Reader.Read_Motorola_uint();
			mh.modlen = m_Reader.Read_Motorola_uint();
			mh.MEDSONGP = m_Reader.Read_Motorola_uint();
			mh.psecnum = m_Reader.Read_Motorola_ushort();
			mh.pseq = m_Reader.Read_Motorola_ushort();
			mh.MEDBlockPP = m_Reader.Read_Motorola_uint();
			mh.reserved1 = m_Reader.Read_Motorola_uint();
			mh.MEDINSTHEADERPP = m_Reader.Read_Motorola_uint();
			mh.reserved2 = m_Reader.Read_Motorola_uint();
			mh.MEDEXPP = m_Reader.Read_Motorola_uint();
			mh.reserved3 = m_Reader.Read_Motorola_uint();
			mh.pstate = m_Reader.Read_Motorola_ushort();
			mh.pblock = m_Reader.Read_Motorola_ushort();
			mh.pline = m_Reader.Read_Motorola_ushort();
			mh.pseqnum = m_Reader.Read_Motorola_ushort();
			mh.actplayline = m_Reader.Read_Motorola_short();
			mh.counter = m_Reader.Read_byte();
			mh.extra_songs = m_Reader.Read_byte();

			/* Seek to MEDSONG struct */
			m_Reader.Seek((int)mh.MEDSONGP, SeekOrigin.Begin);

			/* Load the MED Song Header */
			//mss = ms.sample;			/* load the sample data first */
			var place = 0;
			for (t = 63; t != 0; t--)
			{
				mss = ms.sample[place];

				mss.rep = m_Reader.Read_Motorola_ushort();
				mss.replen = m_Reader.Read_Motorola_ushort();
				mss.midich = m_Reader.Read_byte();
				mss.midipreset = m_Reader.Read_byte();
				mss.svol = m_Reader.Read_byte();
				mss.strans = m_Reader.Read_sbyte();
				place++;
			}

			ms.numblocks = m_Reader.Read_Motorola_ushort();
			ms.songlen = m_Reader.Read_Motorola_ushort();
			m_Reader.Read_bytes(ms.playseq, 256);
			ms.deftempo = m_Reader.Read_Motorola_ushort();
			ms.playtransp = m_Reader.Read_sbyte();
			ms.flags = m_Reader.Read_byte();
			ms.flags2 = m_Reader.Read_byte();
			ms.tempo2 = m_Reader.Read_byte();
			m_Reader.Read_bytes(ms.trkvol, 16);
			ms.mastervol = m_Reader.Read_byte();
			ms.numsamples = m_Reader.Read_byte();

			/* check for a bad header */
			if (m_Reader.isEOF())
			{

				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			/* load extension structure */
			if (mh.MEDEXPP != 0)
			{
				m_Reader.Seek((int)mh.MEDEXPP, SeekOrigin.Begin);
				me.nextmod = m_Reader.Read_Motorola_uint();
				me.exp_smp = m_Reader.Read_Motorola_uint();
				me.s_ext_entries = m_Reader.Read_Motorola_ushort();
				me.s_ext_entrsz = m_Reader.Read_Motorola_ushort();
				me.annotxt = m_Reader.Read_Motorola_uint();
				me.annolen = m_Reader.Read_Motorola_uint();
				me.iinfo = m_Reader.Read_Motorola_uint();
				me.i_ext_entries = m_Reader.Read_Motorola_ushort();
				me.i_ext_entrsz = m_Reader.Read_Motorola_ushort();
				me.jumpmask = m_Reader.Read_Motorola_uint();
				me.rgbtable = m_Reader.Read_Motorola_uint();
				me.channelsplit = m_Reader.Read_Motorola_uint();
				me.n_info = m_Reader.Read_Motorola_uint();
				me.SongName = m_Reader.Read_Motorola_uint();
				me.songnamelen = m_Reader.Read_Motorola_uint();
				me.dumps = m_Reader.Read_Motorola_uint();
			}

			/* seek to and read the samplepointer array */
			m_Reader.Seek((int)mh.MEDINSTHEADERPP, SeekOrigin.Begin);
			m_Reader.Read_Motorola_uints(sa, ms.numsamples);

			/* alloc and read the blockpointer array */
			ba = new uint[ms.numblocks];
			m_Reader.Seek((int)mh.MEDBlockPP, SeekOrigin.Begin);
			m_Reader.Read_Motorola_uints(ba, ms.numblocks);

			/* copy song positions */
			m_Module.Positions = new ushort[ms.songlen];

			for (t = 0; t < ms.songlen; t++)
			{
				m_Module.Positions[t] = ms.playseq[t];
			}

			decimalvolumes = (ms.flags & 0x10) == 0;
			bpmtempos = (ms.flags2 & 0x20) != 0;

			if (bpmtempos)
			{
				var bpmlen = (ms.flags2 & 0x1f) + 1;
				m_Module.InitialSpeed = ms.tempo2;
				m_Module.InitialTempo = (ushort)(ms.deftempo * bpmlen / 4);

				if (bpmlen != 4)
				{
					/* Let's do some math : compute GCD of BPM beat length and speed */
					int a, b;

					a = bpmlen;
					b = ms.tempo2;

					if (a > b)
					{
						t = b;
						b = a;
						a = t;
					}

					while ((a != b) && (a != 0))
					{
						t = a;
						a = b - a;
						b = t;
						if (a > b)
						{
							t = b;
							b = a;
							a = t;
						}
					}

					m_Module.InitialSpeed /= (byte)b;
					m_Module.InitialTempo = (ushort)(ms.deftempo * bpmlen / (4 * b));
				}
			}
			else
			{
				m_Module.InitialSpeed = ms.tempo2;
				m_Module.InitialTempo = (ushort)(ms.deftempo != 0 ? ms.deftempo * 125 / 33 : 128);
				if (ms.deftempo is <= 10 and not 0)
				{
					m_Module.InitialTempo = (ushort)(m_Module.InitialTempo * 33 / 6);
				}

				m_Module.Flags |= Constants.UF_HIGHBPM;
			}

			MED_Version[12] = (char)mh.id;
			m_Module.ModType = new string(MED_Version);
			m_Module.NumChannels = 0;                /* will be counted later */
			m_Module.NumPatterns = ms.numblocks;
			m_Module.NumPositions = ms.songlen;
			m_Module.NumInstruments = ms.numsamples;
			m_Module.NumSamples = m_Module.NumInstruments;
			m_Module.RestartPosition = 0;
			if ((mh.MEDEXPP != 0) && (me.SongName != 0) && (me.songnamelen != 0))
			{
				m_Reader.Seek((int)me.SongName, SeekOrigin.Begin);
				m_Module.SongName = m_Reader.Read_String((int)me.songnamelen);
			}
			else
			{
				m_Module.SongName = "";
			}

			if ((mh.MEDEXPP != 0) && (me.annotxt != 0) && (me.annolen != 0))
			{
				m_Reader.Seek((int)me.annotxt, SeekOrigin.Begin);
				ReadComment((ushort)me.annolen);
			}

			m_Module.AllocSamples();
			for (t = 0; t < m_Module.NumInstruments; t++)
			{
				s = new MEDINSTHEADER();
				q = m_Module.Samples[t];
				q.flags = Constants.SF_SIGNED;
				q.volume = 64;
				if (sa[t] != 0)
				{
					m_Reader.Seek((int)sa[t], SeekOrigin.Begin);

					s.length = m_Reader.Read_Motorola_uint();
					s.type = m_Reader.Read_Motorola_short();

					if (s.type != 0)
					{
						if (curious == 0)
						{
							m_LoadError = MMERR_MED_SYNTHSAMPLES;
							return false;
						}

						s.length = 0;
					}

					if (m_Reader.isEOF())
					{
						m_LoadError = MMERR_LOADING_SAMPLEINFO;
						return false;
					}

					q.length = s.length;
					q.seekpos = (uint)m_Reader.Tell();
					q.loopstart = (uint)(ms.sample[t].rep << 1);
					q.loopend = (uint)(q.loopstart + (ms.sample[t].replen << 1));

					if (ms.sample[t].replen > 1)
					{
						q.flags |= Constants.SF_LOOP;
					}

					/* don't load sample if length>='MMD0'...
					   such kluges make libmikmod's code unique !!! */
					if (q.length >= MMD0_string)
					{
						q.length = 0;
					}
				}
				else
				{
					q.length = 0;
				}

				if ((mh.MEDEXPP != 0) && (me.exp_smp != 0) && (t < me.s_ext_entries) && (me.s_ext_entrsz >= 4))
				{
					var ie = new MEDINSTEXT();
					m_Reader.Seek((int)(me.exp_smp + (t * me.s_ext_entrsz)), SeekOrigin.Begin);
					ie.hold = m_Reader.Read_byte();
					ie.decay = m_Reader.Read_byte();
					ie.suppress_midi_off = m_Reader.Read_byte();
					ie.finetune = m_Reader.Read_sbyte();

					q.speed = Constants.finetune[ie.finetune & 0xf];
				}
				else
				{
					q.speed = 8363;
				}

				if ((mh.MEDEXPP != 0) && (me.iinfo != 0) && (t < me.i_ext_entries) && (me.i_ext_entrsz >= 40))
				{
					m_Reader.Seek((int)(me.iinfo + (t * me.i_ext_entrsz)), SeekOrigin.Begin);
					q.samplename = m_Reader.Read_String(40);
				}
				else
				{
					q.samplename = "";
				}
			}

			if (mh.id == MMD0_string)
			{
				if (!LoadMEDPatterns())
				{
					m_LoadError = MMERR_LOADING_PATTERN;
					return false;
				}
			}
			else if (mh.id == MMD1_string)
			{
				if (!LoadMMD1Patterns())
				{
					m_LoadError = MMERR_LOADING_PATTERN;
					return false;
				}
			}
			else
			{
				m_LoadError = MMERR_NOT_A_MODULE;
				return false;
			}

			return true;
		}

		public override string LoadTitle() => throw new NotImplementedException();
	}
}
