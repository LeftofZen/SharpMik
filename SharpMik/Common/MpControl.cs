namespace SharpMik.Common
{
	public class MpControl
	{
		public MpControl()
		{
			main = new MpChannel();
			slave = null;
		}

		public MpChannel main;

		public MpVoice slave;    /* Audio Slave of current effects control channel */

		public byte slavechn;     /* Audio Slave of current effects control channel */
		public byte muted;        /* if set, channel not played */
		public ushort ultoffset;    /* fine sample offset memory */
		public byte anote;        /* the note that indexes the audible */
		public byte oldnote;
		public short ownper;
		public short ownvol;
		public byte dca;          /* duplicate check action */
		public byte dct;          /* duplicate check type */
		public byte[] row;          /* row currently playing on this channel */
		public int rowPos;
		public sbyte retrig;       /* retrig value (0 means don't retrig) */
		public uint speed;        /* what finetune to use */
		public short volume;       /* amiga volume (0 t/m 64) to play the sample at */

		public short tmpvolume;    /* tmp volume */
		public ushort tmpperiod;    /* tmp period */
		public ushort wantedperiod; /* period to slide to (with effect 3 or 5) */

		public byte arpmem;       /* arpeggio command memory */
		public byte pansspd;      /* panslide speed */
		public ushort slidespeed;
		public ushort portspeed;    /* noteslide speed (toneportamento) */

		public byte s3mtremor;    /* s3m tremor (effect I) counter */
		public byte s3mtronof;    /* s3m tremor ontime/offtime */
		public byte s3mvolslide;  /* last used volslide */
		public sbyte sliding;
		public byte s3mrtgspeed;  /* last used retrig speed */
		public byte s3mrtgslide;  /* last used retrig slide */

		public byte glissando;    /* glissando (0 means off) */
		public byte wavecontrol;

		public sbyte vibpos;       /* current vibrato position */
		public byte vibspd;       /* "" speed */
		public byte vibdepth;     /* "" depth */

		public sbyte trmpos;       /* current tremolo position */
		public byte trmspd;       /* "" speed */
		public byte trmdepth;     /* "" depth */

		public byte fslideupspd;
		public byte fslidednspd;
		public byte fportupspd;   /* fx E1 (extra fine portamento up) data */
		public byte fportdnspd;   /* fx E2 (extra fine portamento dn) data */
		public byte ffportupspd;  /* fx X1 (extra fine portamento up) data */
		public byte ffportdnspd;  /* fx X2 (extra fine portamento dn) data */

		public uint hioffset;     /* last used high order of sample offset */
		public ushort soffset;      /* last used low order of sample-offset (effect 9) */

		public byte sseffect;     /* last used Sxx effect */
		public byte ssdata;       /* last used Sxx data info */
		public byte chanvolslide; /* last used channel volume slide */

		public byte panbwave;     /* current panbrello waveform */
		public byte panbpos;      /* current panbrello position */
		public sbyte panbspd;      /* "" speed */
		public byte panbdepth;    /* "" depth */

		public ushort newsamp;      /* set to 1 upon a sample / inst change */
		public byte voleffect;    /* Volume Column Effect Memory as used by IT */
		public byte voldata;      /* Volume Column Data Memory */

		public short pat_reppos;   /* patternloop position */
		public ushort pat_repcnt;   /* times to loop */
	}
}
