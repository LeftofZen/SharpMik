namespace SharpMik.Common
{
	public class EnvPr
	{
		public byte flg;          /* envelope flag */
		public byte pts;          /* number of envelope points */
		public byte susbeg;       /* envelope sustain index begin */
		public byte susend;       /* envelope sustain index end */
		public byte beg;          /* envelope loop begin */
		public byte end;          /* envelope loop end */
		public short p;            /* current envelope counter */
		public ushort a;            /* envelope index a */
		public ushort b;            /* envelope index b */
		public EnvPt[] env;          /* envelope points */
	}
}
