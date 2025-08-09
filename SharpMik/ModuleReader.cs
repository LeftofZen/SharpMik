using System.IO;

namespace SharpMik.IO
{

	/*
	 * Needs to be tidy up, removal of functions that are not needed any more
	 * And chaning of function headers to make more sense.
	 * 
	 * Also to not throw exceptions when hitting EOF, instead passing back how
	 * much data was read.
	 * 
	 */
	public class ModuleReader : BinaryReader
	{
		public ModuleReader(Stream baseStream)
			: base(baseStream)
		{

		}

		#region stream functions
		public bool Seek(int offset, SeekOrigin origin)
		{
			_ = BaseStream.Seek(offset, origin);
			return BaseStream.Position < BaseStream.Length;
		}

		public virtual int Tell()
		{
			try
			{
				return (int)BaseStream.Position;
			}
			catch (IOException)
			{
				return -1;
			}
		}

		public virtual bool isEOF()
		{
			try
			{
				return BaseStream.Position > BaseStream.Length;
			}
			catch (IOException)
			{
				return true;
			}
		}

		public void Rewind() => Seek(0, SeekOrigin.Begin);
		#endregion

		#region byte / sbyte functions
		public virtual byte Read_byte()
		{
			try
			{
				return ReadByte();
			}
			catch
			{
				return byte.MaxValue;
				//throw ioe1;
			}
		}

		public virtual sbyte Read_sbyte() => (sbyte)ReadByte();

		public virtual bool Read_bytes(byte[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_byte();
				number--;
			}

			return !isEOF();
		}

		public virtual bool Read_bytes(sbyte[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = (sbyte)Read_byte();
				number--;
			}

			return !isEOF();
		}

		public virtual bool Read_bytes(ushort[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_byte();
				number--;
			}

			return !isEOF();
		}

		public virtual bool Read_bytes(char[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = (char)Read_byte();
				number--;
			}

			return !isEOF();
		}

		public virtual bool Read_bytes(short[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_byte();
				number--;
			}

			return !isEOF();
		}
		#endregion

		#region short / ushort functions
		public virtual ushort Read_Motorola_ushort()
		{
			var b1 = ReadByte();
			var b2 = ReadByte();

			var ushort1 = (int)b1;
			var ushort2 = (int)b2;

			var result = (ushort)(ushort1 << 8);
			return (ushort)(result | ushort2);
		}

		public virtual ushort Read_Intel_ushort()
		{
			ushort result = Read_byte();
			result |= (ushort)(Read_byte() << 8);
			return result;
		}

		public virtual short Read_Motorola_short()
		{
			var result = (short)(Read_byte() << 8);
			result |= Read_byte();
			return result;
		}

		public virtual bool Read_Intel_ushorts(ushort[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_Intel_ushort();
				number--;
			}

			return !isEOF();
		}

		public virtual bool Read_Intel_ushorts(ushort[] buffer, int offset, int number)
		{
			var pos = 0;
			while (number > 0 && offset + pos < buffer.Length)
			{
				buffer[offset + pos++] = Read_Intel_ushort();
				number--;
			}

			return !isEOF();
		}

		public virtual short Read_Intel_short()
		{
			short result = Read_byte();
			result |= (short)(Read_byte() << 8);
			return result;
		}

		public virtual bool read_Motorola_shorts(short[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_Motorola_short();
				number--;
			}

			return !isEOF();
		}

		public virtual bool read_Intel_shorts(short[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_Intel_short();
				number--;
			}

			return !isEOF();
		}
		#endregion

		#region int / uint functions
		public virtual uint Read_Motorola_uint()
		{
			var result = (Read_Motorola_ushort()) << 16;
			result |= Read_Motorola_ushort();
			return (uint)result;
		}

		public virtual int Read_Motorola_uints(uint[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_Motorola_uint();
				number--;
			}

			return pos;
		}

		public virtual uint Read_Intel_uint()
		{
			uint result = Read_Intel_ushort();
			result |= ((uint)Read_Intel_ushort()) << 16;
			return result;
		}

		public virtual bool Read_Intel_uints(uint[] buffer, int number)
		{
			var pos = 0;
			while (number > 0)
			{
				buffer[pos++] = Read_Intel_uint();
				number--;
			}

			return !isEOF();
		}

		public virtual int Read_Motorola_int() => (int)Read_Motorola_uint();

		public virtual int Read_Intel_int() => (int)Read_Intel_uint();
		#endregion

		public string Read_String(int length)
		{
			var tmpBuffer = new byte[length];
			_ = Read(tmpBuffer, 0, length);
			return System.Text.Encoding.UTF8.GetString(tmpBuffer, 0, length).Trim(['\0']);
		}
	}
}
