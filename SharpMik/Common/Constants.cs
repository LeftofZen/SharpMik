namespace SharpMik.Common
{
	public static class Constants
	{
		public const int Octave = 12;

		public const int UF_MAXMACRO = 0x10;
		public const int UF_MAXFILTER = 0x100;

		public const int FILT_CUT = 0x80;
		public const int FILT_RESONANT = 0x81;

		/* flags for S3MIT_ProcessCmd */
		public const int S3MIT_OLDSTYLE = 1;    /* behave as old scream tracker */
		public const int S3MIT_IT = 2;  /* behave as impulse tracker */
		public const int S3MIT_SCREAM = 4;  /* enforce scream tracker specific limits */

		/*========== Instruments */

		/* Instrument format flags */
		public const int IF_OWNPAN = 1;
		public const int IF_PITCHPAN = 2;

		/* Envelope flags: */
		public const int EF_ON = 1;
		public const int EF_SUSTAIN = 2;
		public const int EF_LOOP = 4;
		public const int EF_VOLENV = 8;

		/* New Note Action Flags */
		public const int NNA_CUT = 0;
		public const int NNA_CONTINUE = 1;
		public const int NNA_OFF = 2;
		public const int NNA_FADE = 3;

		public const int NNA_MASK = 3;

		public const int DCT_OFF = 0;
		public const int DCT_NOTE = 1;
		public const int DCT_SAMPLE = 2;
		public const int DCT_INST = 3;

		public const int DCA_CUT = 0;
		public const int DCA_OFF = 1;
		public const int DCA_FADE = 2;

		public const int KEY_KICK = 0;
		public const int KEY_OFF = 1;
		public const int KEY_FADE = 2;
		public const int KEY_KILL = KEY_OFF | KEY_FADE;

		public const int KICK_ABSENT = 0;
		public const int KICK_NOTE = 1;
		public const int KICK_KEYOFF = 2;
		public const int KICK_ENV = 4;

		public const int AV_IT = 1;   /* IT vs. XM vibrato info */

		public const int UF_MAXCHAN = 64;

		/* Sample format [loading and in-memory] flags: */
		public const int SF_16BITS = 0x0001;
		public const int SF_STEREO = 0x0002;
		public const int SF_SIGNED = 0x0004;
		public const int SF_BIG_ENDIAN = 0x0008;
		public const int SF_DELTA = 0x0010;
		public const int SF_ITPACKED = 0x0020;

		public const int SF_FORMATMASK = 0x003F;

		/* General Playback flags */

		public const int SF_LOOP = 0x0100;
		public const int SF_BIDI = 0x0200;
		public const int SF_REVERSE = 0x0400;
		public const int SF_SUSTAIN = 0x0800;

		public const int SF_PLAYBACKMASK = 0x0C00;

		/*========== Playing */

		public const int POS_NONE = -2;   /* no loop position defined */

		public const int LAST_PATTERN = ushort.MaxValue;    /* (ushort)-1 special ``end of song'' pattern */

		public const int INSTNOTES = 120;
		public const int ENVPOINTS = 32;

		/* Module flags */
		public const int UF_XMPERIODS = 0x0001; /* XM periods / finetuning */
		public const int UF_LINEAR = 0x0002; /* LINEAR periods (UF_XMPERIODS must be set) */
		public const int UF_INST = 0x0004; /* Instruments are used */
		public const int UF_NNA = 0x0008; /* IT: NNA used, set numvoices rather
															than NumChannels */
		public const int UF_S3MSLIDES = 0x0010; /* uses old S3M volume slides */
		public const int UF_BGSLIDES = 0x0020; /* continue volume slides in the background */
		public const int UF_HIGHBPM = 0x0040; /* MED: can use >255 bpm */
		public const int UF_NOWRAP = 0x0080; /* XM-type (i.e. illogical) pattern break
														semantics */
		public const int UF_ARPMEM = 0x0100; /* IT: need arpeggio memory */
		public const int UF_FT2QUIRKS = 0x0200;/* emulate some FT2 replay quirks */
		public const int UF_PANNING = 0x0400; /* module uses panning effects or have non-tracker default initial panning */

		/* Panning constants */
		public const int PAN_LEFT = 0;
		public const int PAN_HALFLEFT = 64;
		public const int PAN_CENTER = 128;
		public const int PAN_HALFRIGHT = 192;
		public const int PAN_RIGHT = 255;
		public const int PAN_SURROUND = 512; /* panning value for Dolby Surround */

		/* These ones take effect only after MikMod_Init or MikMod_Reset */
		public const int DMODE_16BITS = 0x0001; /* enable 16 bit output */
		public const int DMODE_STEREO = 0x0002; /* enable stereo output */
		public const int DMODE_SOFT_SNDFX = 0x0004; /* Process sound effects via software mixer */
		public const int DMODE_SOFT_MUSIC = 0x0008; /* Process music via software mixer */
		public const int DMODE_HQMIXER = 0x0010; /* Use high-quality (slower) software mixer */
		/* These take effect immediately. */
		public const int DMODE_SURROUND = 0x0100; /* enable surround sound */
		public const int DMODE_INTERP = 0x0200; /* enable interpolation */
		public const int DMODE_REVERSE = 0x0400; /* reverse stereo */
		public const int DMODE_NOISEREDUCTION = 0x1000; /* Low pass filtering */

		/* Module-only Playback Flags */

		public const int SF_OWNPAN = 0x1000;
		public const int SF_UST_LOOP = 0x2000;

		public const int SF_EXTRAPLAYBACKMASK = 0x3000;

		public const int MAXSAMPLEHANDLES = 1024; // missing in the original SharpMik repo

		public static short[] Npertab =
		[
			/* Octaves 6 . 0 */
			/* C    C#     D    D#     E     F    F#     G    G#     A    A#     B */
			0x6b0,0x650,0x5f4,0x5a0,0x54c,0x500,0x4b8,0x474,0x434,0x3f8,0x3c0,0x38a,
			0x358,0x328,0x2fa,0x2d0,0x2a6,0x280,0x25c,0x23a,0x21a,0x1fc,0x1e0,0x1c5,
			0x1ac,0x194,0x17d,0x168,0x153,0x140,0x12e,0x11d,0x10d,0x0fe,0x0f0,0x0e2,
			0x0d6,0x0ca,0x0be,0x0b4,0x0aa,0x0a0,0x097,0x08f,0x087,0x07f,0x078,0x071,
			0x06b,0x065,0x05f,0x05a,0x055,0x050,0x04b,0x047,0x043,0x03f,0x03c,0x038,
			0x035,0x032,0x02f,0x02d,0x02a,0x028,0x025,0x023,0x021,0x01f,0x01e,0x01c,
			0x01b,0x019,0x018,0x016,0x015,0x014,0x013,0x012,0x011,0x010,0x00f,0x00e
		];

		public static ushort[] finetune =
		[
			8363,8413,8463,8529,8581,8651,8723,8757,
			7895,7941,7985,8046,8107,8169,8232,8280
		];
	}
}
