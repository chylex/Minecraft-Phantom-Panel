using System.Buffers.Binary;
using System.Text;

namespace Phantom.Utils.Cryptography;

// MIT License
// 
// Copyright (c) 2020 Niklas Mollenhauer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// 
// ---------------------------------------------------------------------
// This is a modified version of https://github.com/nikeee/dotnet-base24
// ---------------------------------------------------------------------

sealed class Base24 {
	private readonly string alphabet;
	private readonly uint alphabetLength;
	private readonly Dictionary<char, uint> decodeMap;
	
	public Base24(string alphabet) {
		this.alphabet = alphabet;
		this.alphabetLength = (uint) alphabet.Length;
		this.decodeMap = new Dictionary<char, uint>(alphabet.Length);
		
		for (int i = 0; i < alphabet.Length; ++i) {
			this.decodeMap[alphabet[i]] = (uint) i;
		}
	}
	
	public string Encode(ReadOnlySpan<byte> data) {
		if (data.Length == 0) {
			return string.Empty;
		}
		
		if (data.Length % 4 != 0) {
			throw new ArgumentException("The data length must be multiple of 4 bytes (32 bits).");
		}
		
		var encodedDataLength = (data.Length / 4) * 7;
		var result = new StringBuilder(encodedDataLength);
		
		Span<char> subResult = stackalloc char[7];
		
		for (int i = 0; i < data.Length; i += 4) {
			uint value = BinaryPrimitives.ReadUInt32LittleEndian(data[i..]);
			
			for (int k = 6; k >= 0; --k) {
				uint idx = value % alphabetLength;
				value /= alphabetLength;
				subResult[k] = alphabet[(int) idx];
			}
			
			result.Append(subResult);
		}
		
		return result.ToString();
	}
	
	public byte[] Decode(ReadOnlySpan<char> data) {
		if (data == null) {
			throw new ArgumentNullException(nameof(data));
		}
		
		if (data.Length % 7 != 0) {
			throw new ArgumentException("The data length must be multiple of 7 chars.");
		}
		
		var decodedDataLength = (data.Length / 7) * 4;
		var result = new byte[decodedDataLength];
		
		for (int i = 0; i < data.Length / 7; ++i) {
			var subData = data.Slice(i * 7, 7);
			uint value = 0;
			
			foreach (char c in subData) {
				value = (alphabetLength * value) + decodeMap[c];
			}
			
			var resultIndex = i * 4;
			BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(resultIndex), value);
		}
		
		return result;
	}
}
