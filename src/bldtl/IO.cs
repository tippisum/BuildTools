using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CampAI.BuildTools {
	public class MemoryBuffer {
		public int Length {
			get { return length; }
			set {
				byte[] buffer;
				int cl;
				if (value < 0) { throw new ArgumentOutOfRangeException(); }
				cl = this.buffer.Length;
				if (value > cl) {
					buffer = new byte[value * 2];
					Buffer.BlockCopy(this.buffer, 0, buffer, 0, length);
					this.buffer = buffer;
				} else if (value < (cl >> 2)) {
					buffer = new byte[value];
					Buffer.BlockCopy(this.buffer, 0, buffer, 0, value);
					this.buffer = buffer;
				}
				if (position > value) { position = value; }
				length = value;
			}
		}
		public int Position {
			get { return position; }
			set {
				if (value < 0 || value >= length) { throw new ArgumentOutOfRangeException(); }
				position = value;
			}
		}

		public MemoryBuffer() { buffer = new byte[64]; }
		public MemoryBuffer(byte[] buffer) : this(buffer, 0, buffer.Length) { }
		public MemoryBuffer(byte[] buffer, int start, int count) {
			this.buffer = new byte[count];
			length = count;
			if (count > buffer.Length) { count = buffer.Length; }
			Buffer.BlockCopy(buffer, start, this.buffer, 0, count);
		}
		public MemoryBuffer(MemoryBuffer buffer) : this(buffer.buffer, 0, buffer.length) { }
		public MemoryBuffer(MemoryBuffer buffer, int start, int count) : this(buffer.buffer, start, count) { }

		public void Reset() {
			position = 0;
			length = 0;
			buffer = new byte[defaultSize];
		}

		public byte[] ToArray() {
			byte[] data;
			data = new byte[length];
			Buffer.BlockCopy(buffer, 0, data, 0, length);
			return data;
		}
		public void CopyFrom(Stream stream, int length) {
			int count;
			int offset = position;
			CheckWrite(offset, length);
			do {
				count = stream.Read(buffer, offset, length);
				length -= count;
				offset += count;
			} while (count > 0 && length > 0);
			position = offset;
		}
		public void CopyTo(Stream stream) { stream.Write(buffer, 0, length); }

		public void CopyFromZlib(Stream stream, int length) {
			DeflateStream f;
			int count;
			int offset;
			int r;
			offset = position;
			r = length;
			CheckWrite(offset, length);
			count = (stream.ReadByte() << 8) + stream.ReadByte();
			if (((count % 31) != 0) || ((count & 0x0F00) != 0x0800) || count >= 0x8000) {
				throw new FormatException();
			}
			f = new DeflateStream(stream, CompressionMode.Decompress);
			while (r > 0) {
				count = f.Read(buffer, offset, r);
				if (count == 0) { throw new ArgumentOutOfRangeException(); }
				r -= count;
				offset += count;
			}
			position = offset;
		}
		public void AlignTo(int alignment) { Position = (position + (alignment - 1)) & (-alignment); }
		public void AlignPadTo(int alignment) {
			int count;
			count = position;
			WriteZero(((count + (alignment - 1)) & (-alignment)) - count);
		}
		public void Seek(int offset) { Position = position + offset; }
		public void Read(byte[] buffer, int start, int count) {
			int offset;
			offset = position;
			Read(offset, buffer, start, count);
			position = offset + count;
		}
		public void Read(int offset, byte[] buffer, int start, int count) {
			CheckRead(offset, count);
			Buffer.BlockCopy(this.buffer, offset, buffer, start, count);
		}
		public byte ReadByte() {
			byte value;
			int offset;
			offset = position;
			value = ReadByte(offset);
			position = offset + 1;
			return value;
		}
		public byte ReadByte(int offset) {
			CheckRead(offset, 1);
			return buffer[offset];
		}
		public byte[] ReadBytes(int count) {
			byte[] value;
			int offset;
			offset = position;
			value = ReadBytes(offset, count);
			position = offset + count;
			return value;
		}
		public byte[] ReadBytes(int offset, int count) {
			byte[] value;
			CheckRead(offset, count);
			value = new byte[count];
			Buffer.BlockCopy(buffer, offset, value, 0, count);
			return value;
		}
		public short ReadInt16() {
			short value;
			int offset;
			offset = position;
			value = ReadInt16(offset);
			position = offset + 2;
			return value;
		}
		public short ReadInt16(int offset) {
			CheckRead(offset, 2);
			return (short)(buffer[offset]
				| buffer[offset + 1] << 8);
		}
		public int ReadInt32() {
			int value;
			int offset;
			offset = position;
			value = ReadInt32(offset);
			position = offset + 4;
			return value;
		}
		public int ReadInt32(int offset) {
			CheckRead(offset, 4);
			return buffer[offset]
				| buffer[offset + 1] << 8
				| buffer[offset + 2] << 16
				| buffer[offset + 3] << 24;
		}
		public long ReadInt64() {
			long value;
			int offset;
			offset = position;
			value = ReadInt64(offset);
			position = offset + 8;
			return value;
		}
		public long ReadInt64(int offset) {
			CheckRead(offset, 8);
			return buffer[offset]
				| buffer[offset + 1] << 8
				| buffer[offset + 2] << 16
				| buffer[offset + 3] << 24
				| buffer[offset + 4] << 32
				| buffer[offset + 5] << 40
				| buffer[offset + 6] << 48
				| buffer[offset + 7] << 56;
		}
		public sbyte ReadSByte() {
			sbyte value;
			int offset;
			offset = position;
			value = ReadSByte(offset);
			position = offset + 1;
			return value;
		}
		public sbyte ReadSByte(int offset) {
			CheckRead(offset, 1);
			return (sbyte)buffer[offset];
		}
		public ushort ReadUInt16() {
			ushort value;
			int offset;
			offset = position;
			value = ReadUInt16(offset);
			position = offset + 2;
			return value;
		}
		public ushort ReadUInt16(int offset) {
			CheckRead(offset, 2);
			return (ushort)(buffer[offset]
				| buffer[offset + 1] << 8);
		}
		public uint ReadUInt32() {
			uint value;
			int offset;
			offset = position;
			value = ReadUInt32(offset);
			position = offset + 4;
			return value;
		}
		public uint ReadUInt32(int offset) {
			CheckRead(offset, 4);
			return buffer[offset]
				| (uint)buffer[offset + 1] << 8
				| (uint)buffer[offset + 2] << 16
				| (uint)buffer[offset + 3] << 24;
		}
		public ulong ReadUInt64() {
			ulong value;
			int offset;
			offset = position;
			value = ReadUInt64(offset);
			position = offset + 8;
			return value;
		}
		public ulong ReadUInt64(int offset) {
			return buffer[offset]
				| (ulong)buffer[offset + 1] << 8
				| (ulong)buffer[offset + 2] << 16
				| (ulong)buffer[offset + 3] << 24
				| (ulong)buffer[offset + 4] << 32
				| (ulong)buffer[offset + 5] << 40
				| (ulong)buffer[offset + 6] << 48
				| (ulong)buffer[offset + 7] << 56;
		}
		public string ReadUTF8(int count) {
			string value;
			int offset;
			offset = position;
			value = ReadUTF8(offset, count);
			position = offset + count;
			return value;
		}
		public string ReadUTF8(int offset, int count) {
			string value;
			int i;
			CheckRead(offset, count);
			for (i = 0; i < count && buffer[offset + i] != 0; ++i) { }
			value = Encoding.UTF8.GetString(buffer, offset, i);
			return value;
		}
		public string ReadLenUTF8() { return ReadUTF8(ReadInt32()); }
		public string ReadUTF8z(int offset) {
			int i;
			for (i = offset; ; ++i) {
				if (i >= length) { throw new ArgumentOutOfRangeException(); }
				if (buffer[i] == 0) { break; }
			}
			return Encoding.UTF8.GetString(buffer, offset, i - offset);
		}
		public string ReadAlignUTF8z(int align) {
			string value;
			int offset;
			int i;
			offset = position;
			for (i = offset; ; ++i) {
				if (i >= length) { throw new ArgumentOutOfRangeException(); }
				if (buffer[i] == 0) { break; }
			}
			i -= offset;
			value = Encoding.UTF8.GetString(buffer, offset, i);
			i = offset + ((i + align) & (-align));
			if (i > length) { throw new ArgumentOutOfRangeException(); }
			position = i;
			return value;
		}
		public string ReadUTF16z(int offset) {
			int i;
			for (i = offset; ; i += 2) {
				if (i + 1 >= length) { throw new ArgumentOutOfRangeException(); }
				if (buffer[i] == 0 && buffer[i + 1] == 0) { break; }
			}
			return Encoding.Unicode.GetString(buffer, offset, i - offset);
		}
		public void Write(byte[] buffer) { Write(buffer, 0, buffer.Length); }
		public void Write(int offset, byte[] buffer) { Write(offset, buffer, 0, buffer.Length); }
		public void Write(byte[] buffer, int start, int count) {
			int offset;
			offset = position;
			Write(offset, buffer, start, count);
			position = offset + count;
		}
		public void Write(int offset, byte[] buffer, int start, int count) {
			if (buffer.Length < count) { throw new ArgumentOutOfRangeException(); }
			CheckWrite(offset, count);
			Buffer.BlockCopy(buffer, start, this.buffer, offset, count);
		}
		public void WriteBuffer(MemoryBuffer buffer) { WriteBuffer(buffer, 0, buffer.length); }
		public void WriteBuffer(MemoryBuffer buffer, int start, int count) {
			int offset;
			offset = position;
			WriteBuffer(offset, buffer, start, count);
			position = offset + count;
		}
		public void WriteBuffer(int offset, MemoryBuffer buffer, int start, int count) {
			if (buffer.length < count) { throw new ArgumentOutOfRangeException(); }
			CheckWrite(offset, count);
			Buffer.BlockCopy(buffer.buffer, start, this.buffer, offset, count);
		}
		public void WriteZero(int count) {
			int offset;
			offset = position;
			WriteZero(offset, count);
			position = offset + count;
		}
		public void WriteZero(int offset, int count) {
			CheckWrite(offset, count);
			while (count > 0) {
				buffer[offset] = 0;
				offset += 1;
				count -= 1;
			}
		}
		public void WriteByte(byte value) {
			int offset;
			offset = position;
			WriteByte(offset, value);
			position = offset + 1;
		}
		public void WriteByte(int offset, byte value) {
			CheckWrite(offset, 1);
			buffer[offset] = value;
		}
		public void WriteInt16(short value) {
			int offset;
			offset = position;
			WriteInt16(offset, value);
			position = offset + 2;
		}
		public void WriteInt16(int offset, short value) {
			CheckWrite(offset, 2);
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
		}
		public void WriteInt32(int value) {
			int offset;
			offset = position;
			WriteInt32(offset, value);
			position = offset + 4;
		}
		public void WriteInt32(int offset, int value) {
			CheckWrite(offset, 4);
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
		}
		public void WriteInt64(long value) {
			int offset;
			offset = position;
			WriteInt64(offset, value);
			position = offset + 8;
		}
		public void WriteInt64(int offset, long value) {
			CheckWrite(offset, 8);
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
			buffer[offset + 4] = (byte)(value >> 32);
			buffer[offset + 5] = (byte)(value >> 40);
			buffer[offset + 6] = (byte)(value >> 48);
			buffer[offset + 7] = (byte)(value >> 56);
		}
		public void WriteSByte(sbyte value) {
			int offset;
			offset = position;
			WriteSByte(offset, value);
			position = offset + 1;
		}
		public void WriteSByte(int offset, sbyte value) {
			CheckWrite(offset, 1);
			buffer[offset] = (byte)value;
		}
		public void WriteUInt16(ushort value) {
			int offset;
			offset = position;
			WriteUInt16(offset, value);
			position = offset + 2;
		}
		public void WriteUInt16(int offset, ushort value) {
			CheckWrite(offset, 2);
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
		}
		public void WriteUInt32(uint value) {
			int offset;
			offset = position;
			WriteUInt32(offset, value);
			position = offset + 4;
		}
		public void WriteUInt32(int offset, uint value) {
			CheckWrite(offset, 4);
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
		}
		public void WriteUInt64(ulong value) {
			int offset;
			offset = position;
			WriteUInt64(offset, value);
			position = offset + 8;
		}
		public void WriteUInt64(int offset, ulong value) {
			CheckWrite(offset, 8);
			buffer[offset] = (byte)value;
			buffer[offset + 1] = (byte)(value >> 8);
			buffer[offset + 2] = (byte)(value >> 16);
			buffer[offset + 3] = (byte)(value >> 24);
			buffer[offset + 4] = (byte)(value >> 32);
			buffer[offset + 5] = (byte)(value >> 40);
			buffer[offset + 6] = (byte)(value >> 48);
			buffer[offset + 7] = (byte)(value >> 56);
		}
		public void WriteUTF8(string value, int count) {
			int offset;
			offset = position;
			WriteUTF8(offset, value, count);
			position = offset + count;
		}
		public void WriteUTF8(int offset, string value, int count) {
			byte[] b;
			b = Encoding.UTF8.GetBytes(value);
			count -= b.Length;
			if (count < 0) { throw new OverflowException(); }
			Write(offset, b);
			WriteZero(offset + b.Length, count);
		}
		public void WriteUTF8z(string value) {
			Write(Encoding.UTF8.GetBytes(value));
			WriteByte(0);
		}
		public void WriteUTF16z(string value) {
			Write(Encoding.Unicode.GetBytes(value));
			WriteUInt16(0);
		}

		protected byte[] buffer;
		protected int position;
		protected int length;

		protected static readonly int defaultSize = 64;

		protected void CheckRead(int offset, int count) {
			if (offset < 0 || offset + count > length) { throw new ArgumentOutOfRangeException(); }
		}
		protected void CheckWrite(int offset, int count) {
			if (offset < 0) { throw new ArgumentOutOfRangeException(); }
			offset += count;
			if (offset > length) { Length = offset; }
		}
	}
}
