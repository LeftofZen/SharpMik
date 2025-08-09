using SharpMik.Attributes;
using SharpMik.Common;
using SharpMik.Interfaces;
using System;

namespace SharpMik.Loaders
{
	[ModFileExtensions(".mtm")]
	public class MTMLoader : IModLoader
	{
		/*========== Module structure */

		class MTMHEADER
		{
			public string id;           /* MTM file marker */
			public byte version;            /* upper major, lower nibble minor version number */
			public string SongName;     /* ASCIIZ SongName */
			public ushort numtracks;        /* number of tracks saved */
			public byte lastpattern;        /* last pattern number saved */
			public byte lastorder;      /* last order number to play (songlength-1) */
			public ushort commentsize;      /* length of comment field */
			public byte numsamples;     /* number of samples saved  */
			public byte attribute;      /* attribute byte (unused) */
			public byte beatspertrack;
			public byte numchannels;        /* number of channels used  */
			public byte[] panpos = new byte[32];        /* voice pan positions */
		}

		class MTMSAMPLE
		{
			public string samplename;
			public uint length;
			public uint reppos;
			public uint repend;
			public byte finetune;
			public byte volume;
			public byte attribute;
		}

		class MTMNOTE
		{
			public byte a, b, c;
		}

		/*========== Loader variables */

		MTMHEADER mh;
		MTMNOTE[] mtmtrk;
		readonly ushort[] pat = new ushort[32];

		readonly string MTM_Version = "MTM";

		public MTMLoader()
			: base()
		{
			m_ModuleType = "MTM";
			m_ModuleVersion = "MTM (MultiTracker Module editor)";
		}

		public override bool Test()
		{
			var id = m_Reader.Read_String(3);

			return id == "MTM";
		}

		public override bool Init()
		{
			mh = new MTMHEADER();
			mtmtrk = new MTMNOTE[64];

			for (var i = 0; i < mtmtrk.Length; i++)
			{
				mtmtrk[i] = new MTMNOTE();
			}

			return true;
		}

		public override void Cleanup()
		{
			mh = null;
			mtmtrk = null;
		}

		byte[] MTM_Convert()
		{
			int t;
			byte a, b, inst, note, eff, dat;

			UniReset();
			for (t = 0; t < 64; t++)
			{
				a = mtmtrk[t].a;
				b = mtmtrk[t].b;
				inst = (byte)(((a & 0x3) << 4) | (b >> 4));
				note = (byte)(a >> 2);
				eff = (byte)(b & 0xf);
				dat = mtmtrk[t].c;

				if (inst != 0)
				{
					UniInstrument(inst - 1);
				}

				if (note != 0)
				{
					UniNote(note + (2 * Constants.Octave));
				}

				/* MTM bug workaround : when the effect is volslide, slide-up *always*
				   overrides slide-down. */
				if (eff == 0xa && (dat & 0xf0) != 0)
				{
					dat &= 0xf0;
				}

				/* Convert pattern jump from Dec to Hex */
				if (eff == 0xd)
				{
					dat = (byte)((((dat & 0xf0) >> 4) * 10) + (dat & 0xf));
				}

				UniPTEffect(eff, dat);
				UniNewline();
			}

			return UniDup();
		}

		public override bool Load(int curious)
		{
			int t, u;
			MTMSAMPLE s;
			Sample q;

			/* try to read module header  */
			mh.id = m_Reader.Read_String(3);
			mh.version = m_Reader.Read_byte();

			mh.SongName = m_Reader.Read_String(20);
			mh.numtracks = m_Reader.Read_Intel_ushort();
			mh.lastpattern = m_Reader.Read_byte();
			mh.lastorder = m_Reader.Read_byte();
			mh.commentsize = m_Reader.Read_Intel_ushort();
			mh.numsamples = m_Reader.Read_byte();
			mh.attribute = m_Reader.Read_byte();
			mh.beatspertrack = m_Reader.Read_byte();
			mh.numchannels = m_Reader.Read_byte();
			_ = m_Reader.Read_bytes(mh.panpos, 32);

			if (m_Reader.isEOF())
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			/* set module variables */
			m_Module.InitialSpeed = 6;
			m_Module.InitialTempo = 125;
			m_Module.ModType = MTM_Version;
			m_Module.NumChannels = mh.numchannels;
			m_Module.NumTracks = (ushort)(mh.numtracks + 1);           /* get number of channels */
			m_Module.SongName = mh.SongName; /* make a cstr of SongName */
			m_Module.NumPositions = (ushort)(mh.lastorder + 1);           /* copy the songlength */
			m_Module.NumPatterns = (ushort)(mh.lastpattern + 1);
			m_Module.RestartPosition = 0;
			m_Module.Flags |= Constants.UF_PANNING;
			for (t = 0; t < 32; t++)
			{
				m_Module.Panning[t] = (ushort)(mh.panpos[t] << 4);
			}

			m_Module.NumInstruments = m_Module.NumSamples = mh.numsamples;

			m_Module.AllocSamples();

			for (t = 0; t < m_Module.NumInstruments; t++)
			{
				s = new MTMSAMPLE();

				q = m_Module.Samples[t];
				/* try to read sample info */
				s.samplename = m_Reader.Read_String(22);
				s.length = m_Reader.Read_Intel_uint();
				s.reppos = m_Reader.Read_Intel_uint();
				s.repend = m_Reader.Read_Intel_uint();
				s.finetune = m_Reader.Read_byte();
				s.volume = m_Reader.Read_byte();
				s.attribute = m_Reader.Read_byte();

				if (m_Reader.isEOF())
				{
					m_LoadError = MMERR_LOADING_SAMPLEINFO;
					return false;
				}

				q.samplename = s.samplename;
				q.seekpos = 0;
				q.speed = Constants.finetune[s.finetune];
				q.length = s.length;
				q.loopstart = s.reppos;
				q.loopend = s.repend;
				q.volume = s.volume;
				if ((s.repend - s.reppos) > 2)
				{
					q.flags |= Constants.SF_LOOP;
				}

				if ((s.attribute & 1) != 0)
				{
					/* If the sample is 16-bits, convert the length and replen
					   byte-values into sample-values */
					q.flags |= Constants.SF_16BITS;
					q.length >>= 1;
					q.loopstart >>= 1;
					q.loopend >>= 1;
				}
			}

			m_Module.Positions = new ushort[m_Module.NumPositions];

			for (t = 0; t < m_Module.NumPositions; t++)
			{
				m_Module.Positions[t] = m_Reader.Read_byte();
			}

			for (; t < 128; t++)
			{
				_ = m_Reader.Read_byte();
			}

			if (m_Reader.isEOF())
			{
				m_LoadError = MMERR_LOADING_HEADER;
				return false;
			}

			m_Module.AllocTracks();
			m_Module.AllocPatterns();

			m_Module.Tracks[0] = MTM_Convert();     /* track 0 is empty */
			for (t = 1; t < m_Module.NumTracks; t++)
			{
				for (var i = 0; i < 64; i++)
				{
					mtmtrk[i].a = m_Reader.Read_byte();
					mtmtrk[i].b = m_Reader.Read_byte();
					mtmtrk[i].c = m_Reader.Read_byte();
				}

				if (m_Reader.isEOF())
				{
					m_LoadError = MMERR_LOADING_TRACK;
					return false;
				}

				if ((m_Module.Tracks[t] = MTM_Convert()) == null)
				{
					return false;
				}
			}

			for (t = 0; t < m_Module.NumPatterns; t++)
			{
				_ = m_Reader.Read_Intel_ushorts(pat, 32);
				for (u = 0; u < m_Module.NumChannels; u++)
				{
					m_Module.Patterns[((long)t * m_Module.NumChannels) + u] = pat[u];
				}
			}

			/* read comment field */
			if (mh.commentsize != 0)
			{
				if (!ReadLinedComment(mh.commentsize, 40))
				{
					return false;
				}
			}

			return true;
		}

		public override string LoadTitle() => throw new NotImplementedException();
	}
}
