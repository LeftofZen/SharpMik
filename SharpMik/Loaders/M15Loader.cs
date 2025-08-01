using SharpMik.Interfaces;
using SharpMik.Attributes;
using SharpMik.Common;

namespace SharpMik.Loaders
{
	[ModFileExtentions(".m15")]
	public class M15Loader : IModLoader
	{
		class M15SampleInfo
		{
			public string samplename;   /* 22 in module, 23 in memory */
			public ushort length;
			public byte finetune;
			public byte volume;
			public ushort reppos;
			public ushort replen;
		}

		class M15ModuleHeader
		{
			public M15ModuleHeader()
			{
				samples = new M15SampleInfo[15];

				for (var i = 0; i < samples.Length; i++)
				{
					samples[i] = new M15SampleInfo();
				}

				positions = new byte[128];
			}

			public string SongName;     /* the SongName.., 20 in module, 21 in memory */
			public M15SampleInfo[] samples;     /* all sampleinfo */
			public byte songlength;     /* number of patterns used */
			public byte magic1;         /* should be 127 */
			public byte[] positions;    /* which pattern to play at pos */
		}

		class M15ModNote
		{
			public byte a, b, c, d;
		}

		/*========== Loader variables */

		M15ModuleHeader mh;
		M15ModNote[] patbuf;
		bool ust_loader;        /* if TRUE, load as an ust module. */

		/* known file formats which can confuse the loader */
		const int REJECT = 2;
		static readonly string[] signatures =
		[
			"CAKEWALK",	/* cakewalk midi files */
			"SZDD"		/* Microsoft compressed files */
		];

		public M15Loader()
		{
			m_ModuleType = "15-instrument module";
			m_ModuleVersion = "MOD (15 instrument)";
		}

		/*========== Loader code */

		bool LoadModuleHeader(M15ModuleHeader mh)
		{
			int t, u;

			mh.SongName = m_Reader.Read_String(20);

			/* sanity check : title should contain printable characters and a bunch
			   of null chars */
			for (t = 0; t < mh.SongName.Length; t++)
			{
				if (mh.SongName[t] is not (char)0 and < (char)32)
				{
					return false;
				}
			}

			for (t = 0; (t < mh.SongName.Length) && (mh.SongName[t] != 0); t++)
			{
				;
			}

			if (t < 20)
			{
				for (; t < mh.SongName.Length; t++)
				{
					if (mh.SongName[t] != 0)
					{
						return false;
					}
				}
			}

			for (t = 0; t < 15; t++)
			{
				var s = mh.samples[t];

				s.samplename = m_Reader.Read_String(22);

				s.length = m_Reader.Read_Motorola_ushort();
				s.finetune = m_Reader.Read_byte();
				s.volume = m_Reader.Read_byte();
				s.reppos = m_Reader.Read_Motorola_ushort();
				s.replen = m_Reader.Read_Motorola_ushort();

				/* sanity check : sample title should contain printable characters and
				   a bunch of null chars */

				for (u = 0; u < s.samplename.Length; u++)
				{
					if (s.samplename[u] is not (char)0 and < (char)14)
					{
						return false;
					}
				}

				for (u = 0; (u < s.samplename.Length) && (s.samplename[u] != 0); u++)
				{
					;
				}

				if (u < 20)
				{
					for (; u < s.samplename.Length; u++)
					{
						if (s.samplename[u] != 0)
						{
							return false;
						}
					}
				}

				/* sanity check : finetune values */
				if ((s.finetune >> 4) != 0)
				{
					return false;
				}
			}

			mh.songlength = m_Reader.Read_byte();
			mh.magic1 = m_Reader.Read_byte();   /* should be 127 */

			/* sanity check : no more than 128 positions, restart position in range */
			if (mh.songlength is 0 or > 128)
			{
				return false;
			}
			/* values encountered so far are 0x6a and 0x78 */
			if (((mh.magic1 & 0xf8) != 0x78) && (mh.magic1 != 0x6a) && (mh.magic1 > mh.songlength))
			{
				return false;
			}

			m_Reader.Read_bytes(mh.positions, 128);

			/* sanity check : pattern range is 0..63 */
			for (t = 0; t < 128; t++)
			{
				if (mh.positions[t] > 63)
				{
					return false;
				}
			}

			return !m_Reader.isEOF();
		}

		/* Checks the patterns in the modfile for UST / 15-inst indications.
		   For example, if an effect 3xx is found, it is assumed that the song 
		   is 15-inst.  If a 1xx effect has dat greater than 0x20, it is UST.   

		   Returns:  0 indecisive; 1 = UST; 2 = 15-inst                               */
		int CheckPatternType(int numpat)
		{
			int t;
			byte eff, dat;

			for (t = 0; t < numpat * (64U * 4); t++)
			{
				/* Load the pattern into the temp buffer and scan it */
				m_Reader.Read_byte();
				m_Reader.Read_byte();

				eff = m_Reader.Read_byte();
				dat = m_Reader.Read_byte();

				switch (eff)
				{
					case 1:
						if (dat > 0x1f)
						{
							return 1;
						}

						if (dat < 0x3)
						{
							return 2;
						}

						break;
					case 2:
						if (dat > 0x1f)
						{
							return 1;
						}

						return 2;
					case 3:
						if (dat != 0)
						{
							return 2;
						}

						break;
					default:
						return 2;
				}
			}

			return 0;
		}

		public override bool Init()
		{
			mh = new M15ModuleHeader();

			return true;
		}

		public override bool Test()
		{
			int t, numpat;
			mh = new M15ModuleHeader();

			ust_loader = false;
			if (!LoadModuleHeader(mh))
			{
				return false;
			}

			/* reject other file types */
			for (t = 0; t < REJECT; t++)
			{
				if (mh.SongName == signatures[t])
				{
					mh = null;
					return false;
				}
			}

			if (mh.magic1 > 127)
			{
				return false;
			}

			if ((mh.songlength == 0) || (mh.songlength > mh.magic1))
			{
				return false;
			}

			for (t = 0; t < 15; t++)
			{
				/* all finetunes should be zero */
				if (mh.samples[t].finetune != 0)
				{
					return false;
				}

				/* all volumes should be <= 64 */
				if (mh.samples[t].volume > 64)
				{
					return false;
				}

				/* all instrument names should begin with s, st-, or a number */
				if (mh.samples[t].samplename.Length > 0 && (mh.samples[t].samplename[0] == 's' || mh.samples[t].samplename[0] == 'S'))
				{
					if (!string.IsNullOrEmpty(mh.samples[t].samplename) && !mh.samples[t].samplename.StartsWith("st-") && !mh.samples[t].samplename.StartsWith("ST-"))
					{
						ust_loader = true;
					}
				}
				else
				{
					if (mh.samples[t].samplename.Length == 0 || (mh.samples[t].samplename.Length > 0 && !char.IsDigit(mh.samples[t].samplename[0])))
					{
						ust_loader = true;
					}
				}

				if (mh.samples[t].length > 4999 || mh.samples[t].reppos > 9999)
				{
					ust_loader = false;
					if (mh.samples[t].length > 32768)
					{
						return false;
					}
				}

				/* if loop information is incorrect as words, but correct as bytes,
				   this is likely to be an ust-style module */
				if ((mh.samples[t].reppos + mh.samples[t].replen > mh.samples[t].length) &&
				   (mh.samples[t].reppos + mh.samples[t].replen < (mh.samples[t].length << 1)))
				{
					ust_loader = true;
					return true;
				}

				if (!ust_loader)
				{
					return true;
				}
			}

			for (numpat = 0, t = 0; t < mh.songlength; t++)
			{
				if (mh.positions[t] > numpat)
				{
					numpat = mh.positions[t];
				}
			}

			numpat++;
			switch (CheckPatternType(numpat))
			{
				case 0:   /* indecisive, so check more clues... */
					break;
				case 1:
					ust_loader = true;
					break;
				case 2:
					ust_loader = false;
					break;
			}

			return true;
		}

		/*
		Old (amiga) noteinfo:

		 _____byte 1_____   byte2_    _____byte 3_____   byte4_
		/                \ /      \  /                \ /      \
		0000          0000-00000000  0000          0000-00000000

		Upper four    12 bits for    Lower four    Effect command.
		bits of sam-  note period.   bits of sam-
		ple number.                  ple number.
		*/

		byte M15_ConvertNote(M15ModNote[] n, int place, byte lasteffect)
		{
			byte instrument, effect, effdat, note;
			ushort period;
			byte lastnote = 0;

			/* decode the 4 bytes that make up a single note */
			instrument = (byte)(n[place].c >> 4);
			period = (ushort)(((n[place].a & 0xf) << 8) + n[place].b);
			effect = (byte)(n[place].c & 0xf);
			effdat = n[place].d;

			/* Convert the period to a note number */
			note = 0;
			if (period != 0)
			{
				for (note = 0; note < Constants.Npertab.Length; note++)
				{
					if (period >= Constants.Npertab[note])
					{
						break;
					}
				}

				if (note == Constants.Npertab.Length)
				{
					note = 0;
				}
				else
				{
					note++;
				}
			}

			if (instrument != 0)
			{
				/* if instrument does not exist, note cut */
				if ((instrument > 15) || (mh.samples[instrument - 1].length == 0))
				{
					//UniPTEffect(0xc,0);
					UniPTEffect(0xc, 0);
					if (effect == 0xc)
					{
						effect = effdat = 0;
					}
				}
				else
				{
					/* if we had a note, then change instrument... */
					if (note != 0)
					{
						UniInstrument(instrument - 1);
					}
					/* ...otherwise, only adjust volume... */
					else
					{
						/* ...unless an effect was specified, which forces a new note
						   to be played */
						if (effect != 0 || effdat != 0)
						{
							UniInstrument(instrument - 1);
							note = lastnote;
						}
						else
						{
							UniPTEffect(0xc, (byte)(mh.samples[instrument - 1].volume & 0x7f));
						}
					}
				}
			}

			if (note != 0)
			{
				UniNote(note + (2 * Constants.Octave) - 1);
				lastnote = note;
			}

			/* Convert pattern jump from Dec to Hex */
			if (effect == 0xd)
			{
				effdat = (byte)((((effdat & 0xf0) >> 4) * 10) + (effdat & 0xf));
			}

			/* Volume slide, up has priority */
			if ((effect == 0xa) && (effdat & 0xf) != 0 && (effdat & 0xf0) != 0)
			{
				effdat &= 0xf0;
			}

			/* Handle ``heavy'' volumes correctly */
			if ((effect == 0xc) && (effdat > 0x40))
			{
				effdat = 0x40;
			}

			if (ust_loader)
			{
				switch (effect)
				{
					case 0:
					case 3:
						break;
					case 1:
						UniPTEffect(0, effdat);
						break;
					case 2:
						if ((effdat & 0xf) != 0)
						{
							UniPTEffect(1, (byte)(effdat & 0xf));
						}
						else
							if ((effdat >> 2) != 0)
						{
							UniPTEffect(2, (byte)(effdat >> 2));
						}

						break;
					default:
						UniPTEffect(effect, effdat);
						break;
				}
			}
			else
			{
				/* An isolated 100, 200 or 300 effect should be ignored (no
				   "standalone" porta memory in mod files). However, a sequence such
				   as 1XX, 100, 100, 100 is fine. */
				if ((effdat == 0) && ((effect == 1) || (effect == 2) || (effect == 3)) && (lasteffect < 0x10) && (effect != lasteffect))
				{
					effect = 0;
				}

				UniPTEffect(effect, effdat);
			}

			if (effect == 8)
			{
				m_Module.Flags |= Constants.UF_PANNING;
			}

			return effect;
		}

		//byte *M15_ConvertTrack(MODNOTE* n)
		void M15_ConvertTrack(M15ModNote[] n, int startlocation, ref byte[][] tracks, int track)
		{
			int t;
			byte lasteffect = 0x10; /* non existant effect */

			UniReset();
			var place = startlocation;

			for (t = 0; t < 64; t++)
			{
				lasteffect = M15_ConvertNote(n, place, lasteffect);

				UniNewline();
				place += 4;
			}

			tracks[track] = UniDup();
		}

		/* Loads all patterns of a modfile and converts them into the 3 byte format. */
		bool M15_LoadPatterns()
		{
			int t, s, tracks = 0;

			m_Module.AllocPatterns();
			m_Module.AllocTracks();

			/* Allocate temporary buffer for loading and converting the patterns */
			patbuf = new M15ModNote[64 * 4];

			for (var i = 0; i < patbuf.Length; i++)
			{
				patbuf[i] = new M15ModNote();
			}

			for (t = 0; t < m_Module.NumPatterns; t++)
			{

				/* Load the pattern into the temp buffer and convert it */
				for (s = 0; s < (64U * 4); s++)
				{
					patbuf[s].a = m_Reader.Read_byte();
					patbuf[s].b = m_Reader.Read_byte();
					patbuf[s].c = m_Reader.Read_byte();
					patbuf[s].d = m_Reader.Read_byte();
				}

				for (s = 0; s < 4; s++)
				{
					M15_ConvertTrack(patbuf, s, ref m_Module.Tracks, tracks++);
				}
				//if(!(of.tracks[tracks++]=M15_ConvertTrack(patbuf+s))) return 0;
			}

			return true;
		}

		public override bool Load(int curious)
		{
			int t, scan;
			Sample q;
			M15SampleInfo s;

			/* try to read module header */
			if (!LoadModuleHeader(mh))
			{
				m_LoadError = "Error loading header";
				return false;
			}

			if (ust_loader)
			{
				m_Module.ModType = "Ultimate Soundtracker";
			}
			else
			{
				m_Module.ModType = "Soundtracker";
			}

			/* set module variables */
			m_Module.InitialSpeed = 6;
			m_Module.InitialTempo = 125;
			m_Module.NumChannels = 4;
			m_Module.SongName = mh.SongName;
			m_Module.NumPositions = mh.songlength;
			m_Module.PatternNewStartPosition = 0;

			/* Count the number of patterns */
			m_Module.NumPatterns = 0;
			for (t = 0; t < m_Module.NumPositions; t++)
			{
				if (mh.positions[t] > m_Module.NumPatterns)
				{
					m_Module.NumPatterns = mh.positions[t];
				}
			}
			/* since some old modules embed extra patterns, we have to check the
   whole list to get the samples' file offsets right - however we can find
   garbage here, so check carefully */
			scan = 1;
			for (t = m_Module.NumPositions; t < 128; t++)
			{
				if (mh.positions[t] >= 0x80)
				{
					scan = 0;
				}
			}

			if (scan != 0)
			{
				for (t = m_Module.NumPositions; t < 128; t++)
				{
					if (mh.positions[t] > m_Module.NumPatterns)
					{
						m_Module.NumPatterns = mh.positions[t];
					}

					if ((curious != 0) && (mh.positions[t] != 0))
					{
						m_Module.NumPositions = (ushort)(t + 1);
					}
				}
			}

			m_Module.NumPatterns++;
			m_Module.NumTracks = (ushort)(m_Module.NumPatterns * m_Module.NumChannels);

			m_Module.Positions = new ushort[m_Module.NumPositions];

			for (t = 0; t < m_Module.NumPositions; t++)
			{
				m_Module.Positions[t] = mh.positions[t];
			}

			/* Finally, init the sampleinfo structures */
			m_Module.NumInstruments = m_Module.NumSamples = 15;
			m_Module.AllocSamples();

			for (t = 0; t < m_Module.NumInstruments; t++)
			{
				s = mh.samples[t];
				q = m_Module.Samples[t];

				/* convert the samplename */
				q.samplename = s.samplename;

				/* init the sampleinfo variables and convert the size pointers */
				q.speed = Constants.finetune[s.finetune & 0xf];
				q.volume = s.volume;
				if (ust_loader)
				{
					q.loopstart = s.reppos;
				}
				else
				{
					q.loopstart = (uint)s.reppos << 1;
				}

				q.loopend = (uint)(q.loopstart + (s.replen << 1));
				q.length = (uint)(s.length << 1);

				q.flags = Constants.SF_SIGNED;
				if (ust_loader)
				{
					q.flags |= Constants.SF_UST_LOOP;
				}

				if (s.replen > 2)
				{
					q.flags |= Constants.SF_LOOP;
				}
			}

			if (!M15_LoadPatterns())
			{
				return false;
			}

			ust_loader = false;

			return true;
		}

		public override void Cleanup()
		{
			patbuf = null;
			mh = null;
		}

		public override string LoadTitle()
		{
			m_Reader.Seek(20, System.IO.SeekOrigin.Begin);

			var title = m_Reader.Read_String(20);

			return title;
		}
	}
}
