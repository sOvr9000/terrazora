
using System;

public class BitArray {
	public readonly byte[] bytes;
	public readonly int bitCount;

	public int curBitIndex { get; private set; }

	public BitArray(int bitCount) {
		this.bitCount = bitCount;
		bytes = new byte[(bitCount + 7) / 8]; // round up
		curBitIndex = 0;
	}

	public bool this[int index] {
		get {
			if (index >= bitCount) throw new IndexOutOfRangeException();

			int byteIndex = index / 8;
			int bitIndex = 7 - index % 8;
			return (bytes[byteIndex] & (1 << bitIndex)) != 0;
		}
		set {
			if (index >= bitCount) throw new IndexOutOfRangeException();

			int byteIndex = index / 8;
			int bitIndex = 7 - index % 8;

			if (value)
				bytes[byteIndex] |= (byte) (1 << bitIndex);
			else
				bytes[byteIndex] &= (byte) ~(1 << bitIndex);
		}
	}

	public byte[] ToCompressed() {
		// Include bit count
		byte[] toCompress = new byte[bytes.Length + 4];
		bytes.CopyTo(toCompress, 4);

		toCompress[0] = (byte) ((bitCount >> 24) & 0xFF);
		toCompress[1] = (byte) ((bitCount >> 16) & 0xFF);
		toCompress[2] = (byte) ((bitCount >> 8) & 0xFF);
		toCompress[3] = (byte) (bitCount & 0xFF);

		return ByteArrayCompression.Compress(toCompress);
	}

	public static BitArray FromCompressed(byte[] compressed) {
		byte[] decompressed = ByteArrayCompression.Decompress(compressed);
		int bitCount = (decompressed[0] << 24) + (decompressed[1] << 16) + (decompressed[2] << 8) + decompressed[3];

		BitArray bitArray = new BitArray(bitCount);
		//decompressed.TakeLast(decompressed.Length - 4).ToArray().CopyTo(bitArray.bytes, 0);
		Array.Copy(decompressed, 4, bitArray.bytes, 0, bitArray.bytes.Length);

		return bitArray;
	}

	public void SetByte(int startBit, byte value) {
		if (startBit + 8 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		int offset = startBit & 0x7; // mod 8
		if (offset == 0) {
			bytes[startBit / 8] = value; // WILL overwrite, unlike the rest of the logic
			return;
		}

		byte firstByte = (byte) (value >> offset); // first overlap of value with a byte in the array
		byte lastByte = (byte) (value & ((1 << offset) - 1)); // second overlap of value with the next byte in the array
		lastByte <<= 8 - offset; // ensure that the carry-over bits come from the left, not the right

		// assume that all relevant bits in the current array are zero (if they aren't, unexpected results can occur)
		int idx = startBit / 8;
		bytes[idx] |= firstByte;
		bytes[idx + 1] |= lastByte;
	}

	public void SetByte(byte value) {
		SetByte(curBitIndex, value);
		curBitIndex += 8;
	}

	public void SetShort(int startBit, short value) {
		if (startBit + 16 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		byte firstHalf = (byte) (value >> 8);
		byte lastHalf = (byte) (value & byte.MaxValue);
		SetByte(startBit, firstHalf);
		SetByte(startBit + 8, lastHalf);

		// WIP below, a more efficient implementation of above, optimizing out repeated calculations
		//int offset = startBit & 0x7;
		//byte firstByte = (byte) ((value >> (8 + offset)) >> 8);
		//byte lastByte = (byte) ((value & ((1 << (8 + offset)) - 1)) >> 8);
		//lastByte <<= 8 - offset;

		//byte middleByte = (byte) (value & ((1 << offset) - 1));

		//// assume that all relevant bits in the current array are zero (if they aren't, unexpected results can occur)
		//int idx = startBit / 8;
		//bytes[idx] |= firstByte;
		//bytes[idx + 1] |= middleByte;
		//bytes[idx + 2] |= lastByte;
	}

	public void SetShort(short value) {
		SetShort(curBitIndex, value);
		curBitIndex += 16;
	}

	public void SetInt(int startBit, int value) {
		if (startBit + 32 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		short firstHalf = (short) (value >> 16);
		short lastHalf = (short) (value & short.MaxValue);
		SetShort(startBit, firstHalf);
		SetShort(startBit + 16, lastHalf);
	}

	public void SetInt(int value) {
		SetInt(curBitIndex, value);
		curBitIndex += 32;
	}

	public void SetLong(int startBit, long value) {
		if (startBit + 64 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		int firstHalf = (int) (value >> 32);
		int lastHalf = (int) (value & int.MaxValue);
		SetInt(startBit, firstHalf);
		SetInt(startBit + 32, lastHalf);
	}

	public void SetLong(long value) {
		SetLong(curBitIndex, value);
		curBitIndex += 64;
	}

	public byte GetByte(int startBit) {
		if (startBit + 8 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		int offset = startBit & 0x7;
		int idx = startBit / 8;
		if (offset == 0) {
			return bytes[idx];
		}

		byte firstMask = (byte) ((1 << (8 - offset)) - 1);
		byte lastMask = (byte) (((1 << offset) - 1) << (8 - offset));

		byte arrayValue1 = bytes[idx];
		byte arrayValue2 = bytes[idx + 1];

		byte combinedValue = (byte) (((arrayValue1 & firstMask) << offset) | ((arrayValue2 & lastMask) >> (8 - offset)));

		return combinedValue;
	}

	public bool HasNextByte() {
		return curBitIndex + 8 <= bitCount;
	}

	public byte GetNextByte() {
		byte val = GetByte(curBitIndex);
		curBitIndex += 8;
		return val;
	}

	public short GetShort(int startBit) {
		if (startBit + 16 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		byte firstHalf = GetByte(startBit);
		byte lastHalf = GetByte(startBit + 8);
		return (short) ((firstHalf << 8) | lastHalf);
	}

	public bool HasNextShort() {
		return curBitIndex + 16 <= bitCount;
	}

	public short GetNextShort() {
		short val = GetShort(curBitIndex);
		curBitIndex += 16;
		return val;
	}

	public int GetInt(int startBit) {
		if (startBit + 32 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		short firstHalf = GetShort(startBit);
		short lastHalf = GetShort(startBit + 16);
		return (int) ((long) (firstHalf << 16) | (long) lastHalf);
	}

	public bool HasNextInt() {
		return curBitIndex + 32 <= bitCount;
	}

	public int GetNextInt() {
		int val = GetInt(curBitIndex);
		curBitIndex += 32;
		return val;
	}

	public long GetLong(int startBit) {
		if (startBit + 64 > bitCount) throw new ArgumentOutOfRangeException(nameof(startBit));

		int firstHalf = GetInt(startBit);
		int lastHalf = GetInt(startBit + 32);
		return ((long) firstHalf << 32) | (long) lastHalf;
	}

	public bool HasNextLong() {
		return curBitIndex + 64 <= bitCount;
	}

	public long GetNextLong() {
		long val = GetLong(curBitIndex);
		curBitIndex += 64;
		return val;
	}

	public bool HasNextBits(int bits) {
		return curBitIndex + bits <= bitCount;
	}

	public bool HasNextBits(int numBytes, int numShorts, int numInts, int numLongs) {
		return HasNextBits(numBytes * 8 + numShorts * 16 + numInts * 32 + numLongs * 64);
	}

	public override string ToString() {
		bool[] bits = GetBitArray();
		string s = "";

		int i = 0;
		foreach (bool bit in bits) {
			if (i > 0) {
				if (i % 8 == 0) {
					s += " | ";
				} else if (i % 4 == 0) {
					s += " ";
				}
			}
			s += bit ? "1" : "0";
			i++;
		}

		return s;
	}

	public bool[] GetBitArray() {
		bool[] bits = new bool[bitCount];

		int i = 0;
		foreach (byte b in bytes) {
			byte mask = 0b10000000;
			for (int j = 0; j < 8; j++) {
				if (i >= bitCount) break;
				bits[i++] = (b & mask) != 0;
				mask >>= 1;
			}
		}

		return bits;
	}
}
