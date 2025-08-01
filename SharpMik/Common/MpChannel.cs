namespace SharpMik.Common
{
	public class MpChannel
	{
		public Instrument i;
		public Sample s;
		public byte sample;       /* which sample number */
		public byte note;         /* the audible note as heard, direct rep of period */
		public short outvolume;    /* output volume (vol + sampcol + instvol) */
		public sbyte chanvol;      /* channel's "global" volume */
		public ushort fadevol;      /* fading volume rate */
		public short panning;      /* panning position */
		public byte kick;         /* if true = sample has to be restarted */
		public byte kick_flag;   /* kick has been true */
		public ushort period;       /* period to play the sample at */
		public byte nna;          /* New note action type + master/slave flags */

		public byte volflg;       /* volume envelope settings */
		public byte panflg;       /* panning envelope settings */
		public byte pitflg;       /* pitch envelope settings */

		public byte keyoff;       /* if true = fade out and stuff */
		public short handle;       /* which sample-handle */
		public byte notedelay;    /* (used for note delay) */
		public long start;        /* The starting byte index in the sample */

		public MpChannel Clone() => (MpChannel)MemberwiseClone();

		public void CloneTo(MpChannel chan)
		{
			chan.i = i;
			chan.s = s;
			chan.sample = sample;
			chan.note = note;
			chan.outvolume = outvolume;
			chan.chanvol = chanvol;
			chan.fadevol = fadevol;
			chan.panning = panning;
			chan.kick = kick;
			chan.period = period;
			chan.nna = nna;

			chan.volflg = volflg;
			chan.panflg = panflg;
			chan.pitflg = pitflg;

			chan.keyoff = keyoff;
			chan.handle = handle;
			chan.notedelay = notedelay;
			chan.start = start;
		}
	}
}
