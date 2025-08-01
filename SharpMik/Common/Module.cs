namespace SharpMik.Common
{
	public class Module
	{
		public Module()
		{
			Panning = new ushort[Constants.UF_MAXCHAN];
			ChannelVolume = new byte[Constants.UF_MAXCHAN];
		}

		/* general module information */
		public string SongName { get; set; }    /* name of the song */
		public string ModType { get; set; }     /* string type of module loaded */
		public string Comment { get; set; }     /* module comments */

		public ushort Flags { get; set; }       /* See module flags above */
		public byte NumChannels { get; set; }      /* number of module channels */
		public byte NumVoices { get; set; }   /* max # voices used for full NNA playback */
		public ushort NumPositions { get; set; }      /* number of positions in this song */
		public ushort NumPatterns { get; set; }      /* number of patterns in this song */
		public ushort NumInstruments { get; set; }      /* number of instruments */
		public ushort NumSamples { get; set; }      /* number of samples */
		public Instrument[] Instruments { get; set; } /* all instruments */
		public Sample[] Samples { get; set; }     /* all samples */
		public byte RealChannels { get; set; }     /* real number of channels used */
		public byte TotalChannels { get; set; }    /* total number of channels used (incl NNAs) */

		/* playback settings */
		public ushort RestartPosition { get; set; }      /* restart position */
		public byte InitialSpeed { get; set; }   /* initial song speed */
		public ushort InitialTempo { get; set; }   /* initial song tempo */
		public byte InitialVolume { get; set; }  /* initial global volume (0 - 128) */
		public ushort[] Panning { get; set; } /* panning positions */
		public byte[] ChannelVolume { get; set; } /* channel positions */
		public ushort bpm { get; set; }         /* current beats-per-minute speed */
		public ushort SongSpeed { get; set; }      /* current song speed */
		public short Volume { get; set; }      /* song volume (0-128) (or user volume) */

		public bool ExtendedSpeedFlag { get; set; }      /* extended speed flag (default enabled) */
		public bool PanFlag { get; set; }     /* panning flag (default enabled) */
		public bool Wrap { get; set; }        /* wrap module ? (default disabled) */
		public bool Loop { get; set; }        /* allow module to loop ? (default enabled) */
		public bool Fadeout { get; set; }     /* volume fade out during last pattern */

		public ushort PatternRowNumber { get; set; }      /* current row number */
		public short SongPosition { get; set; }      /* current song position */
		public uint SongTime { get; set; }     /* current song time in 2^-10 seconds */

		public short RelativeSpeedFactor { get; set; }      /* relative speed factor */

		/* internal module representation */
		public ushort NumTracks { get; set; }      /* number of tracks */
		public byte[][] Tracks;       /* array of numtrk pointers to tracks */
		public ushort[] Patterns { get; set; }    /* array of Patterns */
		public ushort[] PatternRows { get; set; }    /* array of number of rows for each pattern */
		public ushort[] Positions { get; set; }   /* all positions */

		public bool Forbid { get; set; }      /* if true, no player update! */
		public ushort NumRowsOnCurrentPattern { get; set; }      /* number of rows on current pattern */
		public ushort vbtick { get; set; }      /* tick counter (counts from 0 to sngspd) */
		public ushort SongRemainder { get; set; }/* used for song time computation */

		public MpControl[] Control { get; set; }     /* Effects Channel info (size pf.NumChannels) */
		public MpVoice[] Voice { get; set; }       /* Audio Voice information (size md_numchn) */

		public byte GlobalSlide { get; set; } /* global volume slide rate */
		public byte pat_repcrazy { get; set; }/* module has just looped to position -1 */
		public ushort PatternNewStartPosition { get; set; }      /* position where to start a new pattern */
		public byte PatterDelayCounter1 { get; set; }      /* patterndelay counter (command memory) */
		public byte PatternDelayCounter2 { get; set; }     /* patterndelay counter (real one) */
		public short PositionJumpFlag { get; set; }      /* flag to indicate a jump is needed... */
		public ushort BpmLimit { get; set; }  /* threshold to detect bpm or speed values */

		public void AllocSamples()
		{
			Samples = new Sample[NumSamples];

			for (var i = 0; i < NumSamples; i++)
			{
				Samples[i] = new Sample
				{
					panning = 128,
					handle = -1,
					globvol = 64,
					volume = 64
				};
			}
		}

		public void AllocPatterns()
		{
			ushort tracks = 0;
			Patterns = new ushort[(NumPatterns + 1) * NumChannels];
			PatternRows = new ushort[NumPatterns + 1];

			for (var t = 0; t <= NumPatterns; t++)
			{
				PatternRows[t] = 64;
				for (var s = 0; s < NumChannels; s++)
				{
					Patterns[(t * NumChannels) + s] = tracks++;
				}
			}
		}

		public void AllocTracks()
			=> Tracks = new byte[NumTracks][];

		public bool AllocInstruments()
		{
			if (NumInstruments == 0)
			{
				return false;
			}

			Instruments = new Instrument[NumInstruments];

			for (ushort i = 0; i < Instruments.Length; i++)
			{
				Instruments[i] = new Instrument();
				for (byte n = 0; n < Constants.INSTNOTES; n++)
				{
					Instruments[i].samplenote[n] = n;
					Instruments[i].samplenumber[n] = i;
				}

				Instruments[i].globvol = 64;
			}

			return true;
		}
	}
}
