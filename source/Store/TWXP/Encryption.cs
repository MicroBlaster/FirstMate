using System;

namespace TWXP
{
    public class Encryption
    {

        public Encryption()
        {

        }

        // Public declarations
        public void Encrypt(ref string Target)
        {
            int Chunks;
            int I;
            int X;
            int ChunkStart;
            byte J;
            byte B;
            byte C;
            byte ChunkKey;
            byte Last;
            byte CheckSum;
            string S;
            string Key;
            string Chunk;
            char P;
            // Encode string
            S = "";
            Last = 0x18;
            CheckSum = 0xF0;
            Key = FKey;
            ConvertKey(ref Key);
            for (I = 1; I <= Target.Length; I++)
            {
                B = (byte)(Target[I]);
                CheckSum = (byte)(CheckSum ^ B);
                C = B;
                for (X = 1; X <= Key.Length; X++)
                {
                    B = (byte)(B ^ (Key[X]));
                }
                if ((I % FShift == 0))
                {
                    // apply shift
                    B = (byte)(B ^ FShiftKey);
                }
                B = (byte)(B ^ Last);
                Last = C;
                S = S + (char)(B);
            }
            // add checksum
            S = S + (char)CheckSum;
            // Scramble string
            Target = "";
            Chunks = S.Length / FChunkSize;
            if ((Chunks * FChunkSize < S.Length))
            {
                Chunks = Chunks + 1;
            }
            for (I = 1; I <= Chunks; I++)
            {
                ChunkStart = (I - 1) * FChunkSize + 1;
                if ((ChunkStart + FChunkSize - 1 > S.Length))
                {
                    X = S.Length - ChunkStart + 1;
                }
                else
                {
                    X = FChunkSize;
                }
                Chunk = S.Substring(ChunkStart - 1, X);
                ChunkKey = ((byte)Chunk[1]);
                // record index of chunk and encrypt it -
                // do this by accessing 32-bit chunk index in memory and
                // encrypting it at low level
                P = (char)I;
                for (J = 1; J <= sizeof(int); J++)
                {
                    Chunk = ((char)(byte)P ^ FScrambleSeed ^ ChunkKey ^ J) + Chunk;
                    P = (char)(P + 1);
                }
                if (((new System.Random()).NextDouble() < 0.5) || (X < FChunkSize))
                {
                    Target = Target + Chunk;
                }
                else
                {
                    Target = Chunk + Target;
                }
            }
        }

        public void Decrypt(ref string Target)
        {
            int I;
            int X;
            int ChunkIndex;
            byte J;
            byte B;
            byte ChunkKey;
            byte CheckSum;
            byte Last;
            string S = "";
            string Chunk;
            string Key;
            string ChunkIdx;
            object P;
            // Unscramble string
            I = 1;
            X = 1;
            Key = FKey;
            ConvertKey(ref Key);
            while ((I < Target.Length))
            {
                // search for a chunk with the index we're looking for (X)
                // get this chunk
                Chunk = Target.Substring(I - 1, FChunkSize + sizeof(int));
                if ((Chunk.Length < sizeof(int) + 1))
                {
                    // invalid chunk - unscramble failed
                    throw new EEncryptError("Decryption failure");
                }
                // decode the chunk index
                ChunkKey = ((byte)Chunk[5]);
                P = (Chunk as string);
                ChunkIdx = "";
                for (J = 1; J <= sizeof(int); J++)
                {
                    P = (byte)P ^ (5 - J) ^ ChunkKey ^ FScrambleSeed;
                    ChunkIdx = (char)P + ChunkIdx;
                    P = ((int)P + 1 as object);
                }
                try
                {
                    ChunkIndex = int.Parse(ChunkIdx);
                    if ((ChunkIndex == X))
                    {
                        S = S + Chunk.Substring(sizeof(int) + 1 - 1, Chunk.Length);
                        I = 1;
                        X++;
                    }
                    else
                    {
                        I = I + Chunk.Length;
                    }
                }
                catch
                {
                    I = I + Chunk.Length;
                }
            }
            if ((S == ""))
            {
                throw new EEncryptError("Decryption failure");
            }
            // Decode string
            Last = 0x18;
            Target = "";
            CheckSum = 0xF0;
            for (I = 1; I < S.Length; I++)
            {
                B = (byte)(S[I]);
                B = (byte)(B ^ Last);
                if ((I % FShift == 0))
                {
                    // apply shift
                    B = (byte)(B ^ FShiftKey);
                }
                for (X = Key.Length; X >= 1; X--)
                {
                    B = (byte)(B ^ (int)(Key[X]));
                }
                Target = Target + (char)B;
                CheckSum = (byte)(CheckSum ^ B);
                Last = B;
            }
            if ((CheckSum != ((byte)S[S.Length])))
            {
                throw new EEncryptError("Decryption failure");
            }
        }

        protected string FKey = String.Empty;
        protected int FShift = 5;
        protected byte FShiftKey = 50;
        protected byte FChunkSize = 18;
        protected byte FScrambleSeed = 0;

        public string Key
        {
            get
            {
                return FKey;
            }
            set
            {
                FKey = value;
            }
        }
        public int Shift
        {
            get
            {
                return FShift;
            }
            set
            {
                FShift = value;
            }
        }
        public byte ShiftKey
        {
            get
            {
                return FShiftKey;
            }
            set
            {
                FShiftKey = value;
            }
        }
        public byte ChunkSize
        {
            get
            {
                return FChunkSize;
            }
            set
            {
                FChunkSize = value;
            }
        }
        public byte ChunkKey
        {
            get
            {
                return FScrambleSeed;
            }
            set
            {
                FScrambleSeed = value;
            }
        }

        public void ConvertKey(ref string Key)
        {
            string S = "";
            int I;
            string KeyChar;
            KeyChar = "";
            for (I = 1; I <= Key.Length; I++)
            {
                if ((Key[I] == ','))
                {
                    try
                    {
                        S = S + ((char)Convert.ToInt32(KeyChar));
                        KeyChar = "";
                    }
                    catch
                    {
                        throw new EEncryptError("Bad encryption key format");
                    }
                }
                else
                {
                    KeyChar = KeyChar + Key[I];
                }
            }
            try
            {
                S = S + ((char)Convert.ToInt32(KeyChar));
            }
            catch
            {
                throw new EEncryptError("Bad encryption key format");
            }
            Key = S;
        }
    }

    public class EEncryptError : Exception
    {
        public EEncryptError(String message)
            : base(message)
        {
        }
        public EEncryptError(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}