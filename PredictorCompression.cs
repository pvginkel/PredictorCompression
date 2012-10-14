// Taken from http://tools.ietf.org/html/draft-ietf-pppext-predictor-00.
// See the IETF draft for more information about the algorithm.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class PredictorCompression
{
    private ushort _hash;
    private readonly byte[] _guessTable = new byte[65536];

    private void Hash(byte x)
    {
        _hash = (ushort)((_hash << 4) ^ x);
    }

    public byte[] Compress(byte[] data)
    {
        using (var stream = new MemoryStream())
        {
            int len = data.Length;
            int source = 0;

            while (len > 0)
            {
                long flagdest = stream.Position;
                stream.WriteByte(0);

                byte flags = 0; /* All guess wrong initially */

                for (int bitmask = 1, i = 0; i < 8 && len > 0; i++, bitmask <<= 1)
                {
                    if (_guessTable[_hash] == data[source])
                    {
                        flags |= (byte)bitmask;  /* Guess was right - don't output */
                    }
                    else
                    {
                        _guessTable[_hash] = data[source];
                        stream.WriteByte(data[source]); /* Guess wrong, output char */
                    }

                    Hash(data[source++]);
                    len--;
                }

                long dest = stream.Position;
                stream.Position = flagdest;
                stream.WriteByte(flags);
                stream.Position = dest;
            }

            return stream.ToArray();
        }
    }

    public byte[] Decompress(byte[] data)
    {
        int len = data.Length;
        int source = 0;

        using (var stream = new MemoryStream())
        {
            byte flags;

            while (len >= 9)
            {
                flags = data[source++];

                for (int i = 0, bitmask = 1; i < 8; i++, bitmask <<= 1)
                {
                    if ((flags & bitmask) != 0)
                    {
                        byte value = _guessTable[_hash];
                        stream.WriteByte(value); /* Guess correct */
                        Hash(value);
                    }
                    else
                    {
                        _guessTable[_hash] = data[source];
                        stream.WriteByte(data[source]);
                        Hash(data[source++]);
                        len--;
                    }
                }

                len--;
            }

            while (len > 0)
            {
                flags = data[source++];
                len--;

                for (int i = 0, bitmask = 1; i < 8; i++, bitmask <<= 1)
                {
                    if ((flags & bitmask) != 0)
                    {
                        byte value = _guessTable[_hash];
                        stream.WriteByte(value); /* Guess correct */
                        Hash(value);
                    }
                    else
                    {
                        if (len == 0)
                            break; /* we seem to be really done -- cabo */

                        _guessTable[_hash] = data[source]; /* Guess wrong */
                        stream.WriteByte(data[source]);
                        Hash(data[source++]);
                        len--;
                    }
                }
            }

            return stream.ToArray();
        }
    }
}
