namespace SharpMik.Common
{
	/* This structure is used to query current playing voices status */
	public class VoiceInfo
	{
		public Instrument i;            /* Current channel instrument */
		public Sample s;            /* Current channel sample */
		public short panning;      /* panning position */
		public sbyte volume;       /* channel's "global" volume (0..64) */
		public ushort period;       /* period to play the sample at */
		public byte kick;         /* if true = sample has been restarted */
	}
}
