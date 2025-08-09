namespace SharpMik.Common
{
	/*
	 * MikMod variable sizes and replaced with.
	 * 
	 * MikMod		C#			Size
	 * ------------------------------
	 * SBYTE		sbyte		1 byte signed
	 * UBYTE		byte		1 byte unsigned
	 * SWORD		short		2 byte signed
	 * UWORD		ushort		2 byte unsigned
	 * SLONG		int			4 byte signed
	 * ULONG		uint		4 byte unsigned
	 * SLONGLONG	long		8 byte signed
	 * ULONGLONG	ulong		8 byte unsigned
	 * 
	 */

	public class Filter
	{
		public byte filter;
		public byte inf;

		public void Clear()
		{
			filter = 0;
			inf = 0;
		}
	}
}
