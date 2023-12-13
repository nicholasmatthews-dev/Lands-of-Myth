using System;
using System.Collections.Generic;
using System.Text;

public static class SerializationHelper {
	/// <summary>
	/// Converts a list of integers with specified bit length into a list of
	/// integers with a different bit length.
	/// </summary>
	/// <param name="input">The list of integers to be converted.</param>
	/// <param name="inputBits">The length in bits of the integers in the input list.</param>
	/// <param name="outputBits">The length of bits for the integers in the output list.</param>
	/// <param name="expectFullWords">If <c>True</c> then any remaining bits less than a 
	/// full output word will be discarded.</param>
	/// <returns>A list of integers in which each contains <c>outputBits</c> bits.</returns>
	/// <exception cref="ArgumentException">If the list is empty, or if input or output 
	/// bits are less than 1.</exception>
    public static List<int> ConvertBetweenCodes
	(
		List<int> input, 
		int inputBits, 
		int outputBits,
		bool expectFullWords = false
	){
		if (inputBits < 1){
			throw new ArgumentException("Input bits " + inputBits + " is less than 1.");
		}
		if (outputBits < 1){
			throw new ArgumentException("Output bits " + outputBits + " is less than 1.");
		}
		if (input.Count <= 0){
			throw new ArgumentException("Invalid input list size.");
		}
		int outputSize;
		if (inputBits * input.Count % outputBits == 0){
			outputSize = input.Count * inputBits / outputBits;
		}
		else if (!expectFullWords) {
			outputSize = input.Count * inputBits / outputBits + 1;
		}
		else {
			outputSize = input.Count * inputBits / outputBits;
		}
		List<int> output = new List<int>(outputSize);
		int currentWordB = 0;
		int currentWordA = input[0];
		int wordACount = 0;
		int wordBOffSet = outputBits - 1;
		int wordAOffSet = inputBits - 1;
		while (wordACount < input.Count){
			// There are more bits in the code than a single byte.
			if (wordBOffSet < wordAOffSet){
				// Shift the current wordA to align with the current wordB head, deleting all lower order bits.
				int tempCode = currentWordA;
				tempCode >>= wordAOffSet - wordBOffSet;
				
				// Add the bits into the current wordB and pass it into the array
				currentWordB += tempCode;
				output.Add(currentWordB);
				currentWordB = 0;
				
				// Shift the bits in tempCode back into place, then remove them
				tempCode <<= wordAOffSet - wordBOffSet;
				currentWordA -= tempCode;

				// Move the heads of the current wordB and wordA back into place
				wordAOffSet -= wordBOffSet + 1;
				wordBOffSet = outputBits - 1;
			}
			// The current wordA contains exactly the remaining bits for the current wordB (how nice!).
			else if (wordBOffSet == wordAOffSet){
				// Add the bits into the current byte and pass it into the array
				currentWordB += currentWordA;
				output.Add(currentWordB);
				currentWordB = 0;

				// No need to pull out a new value if we are at the end of the list
				if (wordACount == input.Count - 1){
					break;
				}

				// Get the next code and reset the offsets.
				wordACount++;
				currentWordA = input[wordACount];
				wordAOffSet = inputBits - 1;
				wordBOffSet = outputBits - 1;
			}
			// There aren't enough bits available to fill a whole wordB
			else if (wordBOffSet > wordAOffSet){
				// Shift the current wordA to align with the wordB offset
				currentWordA <<= wordBOffSet - wordAOffSet;

				// Adds the available bits to the wordB and shifts the wordB offset
				currentWordB += currentWordA;
				wordBOffSet -= wordAOffSet + 1;

				// If this is the last code, add the byte as is and break
				if (wordACount == input.Count - 1 && !expectFullWords){
					output.Add(currentWordB);
					break;
				}
				// Don't add the partial word at the end of the input list
				else if (wordACount == input.Count - 1){
					break;
				}

				// Retrieve the next code in the list and update the code offset
				wordACount++;
				currentWordA = input[wordACount];
				wordAOffSet = inputBits - 1;
			}
		}
		return output;
	}

	public static int RepresentativeBits(int input){
		return (int)Math.Ceiling(Math.Log2(input));
	}

	public static void AppendBytes(ref byte[] bytes, byte[] toAdd, ref int byteHead){
		for (int i = 0; i < toAdd.Length; i++){
			bytes[byteHead] = toAdd[i];
			byteHead++;
		}
	}

	public static byte[] ReadBytes(byte[] bytes, int toRead, ref int byteHead){
		byte[] output = new byte[toRead];
		for (int i = 0; i < toRead; i++){
			output[i] = bytes[byteHead];
			byteHead++;
		}
		return output;
	}

	public static void StoreInt(ref byte[] bytes, int toAdd, ref int byteHead){
		AppendBytes(ref bytes, BitConverter.GetBytes(toAdd), ref byteHead);
	}

	public static int ReadInt(byte[] bytes, ref int byteHead){
		return BitConverter.ToInt32(ReadBytes(bytes, 4, ref byteHead));
	}

	public static void StoreString(ref byte[] bytes, string toAdd, ref int byteHead){
		StoreInt(ref bytes, toAdd.Length, ref byteHead);
		AppendBytes(ref bytes, Encoding.ASCII.GetBytes(toAdd), ref byteHead);
	}

	public static string ReadString(byte[] bytes, ref int byteHead){
		int stringLength = ReadInt(bytes, ref byteHead);
		byte[] stringBytes = ReadBytes(bytes, stringLength, ref byteHead);
		return Encoding.ASCII.GetString(stringBytes);
	}

}