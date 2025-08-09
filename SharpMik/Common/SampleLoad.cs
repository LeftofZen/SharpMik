using SharpMik.IO;

namespace SharpMik.Common
{
	/*========== Samples */

	/* This is a handle of sorts attached to any sample registered with
	   SL_RegisterSample.  Generally, this only need be used or changed by the
	   loaders and drivers of mikmod. */
	public class SampleLoad
	{
		public SampleLoad next;

		public uint length;       /* length of sample (in samples!) */
		public uint loopstart;    /* repeat position (relative to start, in samples) */
		public uint loopend;      /* repeat end */
		public uint infmt, outfmt;
		public int scalefactor;
		public Sample sample;
		public ModuleReader reader;
	}
}
