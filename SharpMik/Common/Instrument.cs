namespace SharpMik.Common
{
	public class Instrument
	{
		public Instrument()
		{
			samplenumber = new ushort[Constants.INSTNOTES];
			samplenote = new byte[Constants.INSTNOTES];
			volenv = new EnvPt[Constants.ENVPOINTS];
			panenv = new EnvPt[Constants.ENVPOINTS];
			pitenv = new EnvPt[Constants.ENVPOINTS];

			for (var i = 0; i < Constants.ENVPOINTS; i++)
			{
				volenv[i] = new EnvPt();
				panenv[i] = new EnvPt();
				pitenv[i] = new EnvPt();
			}
		}

		public string insname;

		public byte flags;
		public ushort[] samplenumber; // INSTNOTES
		public byte[] samplenote; // INSTNOTES

		public byte nnatype;
		public byte dca;              /* duplicate check action */
		public byte dct;              /* duplicate check type */
		public byte globvol;
		public ushort volfade;
		public short panning;          /* instrument-based panning var */

		public byte pitpansep;        /* pitch pan separation (0 to 255) */
		public byte pitpancenter;     /* pitch pan center (0 to 119) */
		public byte rvolvar;          /* random volume varations (0 - 100%) */
		public byte rpanvar;          /* random panning varations (0 - 100%) */

		/* volume envelope */
		public byte volflg;           /* bit 0: on 1: sustain 2: loop */
		public byte volpts;
		public byte volsusbeg;
		public byte volsusend;
		public byte volbeg;
		public byte volend;
		public EnvPt[] volenv; // ENVPOINTS
		/* panning envelope */
		public byte panflg;           /* bit 0: on 1: sustain 2: loop */
		public byte panpts;
		public byte pansusbeg;
		public byte pansusend;
		public byte panbeg;
		public byte panend;
		public EnvPt[] panenv; // ENVPOINTS
		/* pitch envelope */
		public byte pitflg;           /* bit 0: on 1: sustain 2: loop */
		public byte pitpts;
		public byte pitsusbeg;
		public byte pitsusend;
		public byte pitbeg;
		public byte pitend;
		public EnvPt[] pitenv; // ENVPOINTS
	}
}
