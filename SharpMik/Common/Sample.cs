namespace SharpMik.Common
{
	public class Sample
	{
		public short panning;     /* panning (0-255 or PAN_SURROUND) */
		public uint speed;       /* Base playing speed/frequency of note */
		public byte volume;      /* volume 0-64 */
		public ushort inflags;      /* sample format on disk */
		public ushort flags;       /* sample format in memory */
		public uint length;      /* length of sample (in samples!) */
		public uint loopstart;   /* repeat position (relative to start, in samples) */
		public uint loopend;     /* repeat end */
		public uint susbegin;    /* sustain loop begin (in samples) \  Not Supported */
		public uint susend;      /* sustain loop end                /      Yet! */

		/* Variables used by the module player only! (ignored for sound effects) */
		public byte globvol;     /* global volume */
		public byte vibflags;    /* autovibrato flag stuffs */
		public byte vibtype;     /* Vibratos moved from INSTRUMENT to SAMPLE */
		public byte vibsweep;
		public byte vibdepth;
		public byte vibrate;
		public string samplename;  /* name of the sample */

		/* Values used internally only */
		public ushort avibpos;     /* autovibrato pos [player use] */
		public byte divfactor;   /* for sample scaling, maintains proper period slides */
		public uint seekpos;     /* seek position in file */
		public short handle;      /* sample handle used by individual drivers */
	}
}
