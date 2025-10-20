
using System.IO;
using System.IO.Compression;

public static class ByteArrayCompression {
	public static byte[] Compress(byte[] data) {
		using MemoryStream output = new();
		using (GZipStream gzip = new(output, CompressionLevel.Optimal)) {
			gzip.Write(data, 0, data.Length);
		}
		return output.ToArray();
	}

	public static byte[] Decompress(byte[] compressedData) {
		using MemoryStream input = new(compressedData);
		using GZipStream gzip = new(input, CompressionMode.Decompress);
		using MemoryStream output = new();
		gzip.CopyTo(output);
		return output.ToArray();
	}
}
