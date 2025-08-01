namespace SharpMik.Common
{
	/* Used by NNA only player (audio control.  AUDTMP is used for full effects
	   control). */
	public class MpVoice
	{
		public MpVoice()
		{
			venv = new EnvPr();
			penv = new EnvPr();
			cenv = new EnvPr();

			main = new MpChannel();
		}

		public MpChannel main;

		public EnvPr venv;
		public EnvPr penv;
		public EnvPr cenv;

		public ushort avibpos;      /* autovibrato pos */
		public ushort aswppos;      /* autovibrato sweep pos */

		public uint totalvol;     /* total volume of channel (before global mixings) */

		public bool mflag;
		public short masterchn;
		public ushort masterperiod;

		public MpControl master;       /* index of "master" effects channel */
	}
}
