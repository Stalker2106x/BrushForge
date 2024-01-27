using Godot;
using System;
using System.IO;
using System.Linq;
using System.Text;

public partial class WAV : AudioStreamWav
{
    string chunkID;
    UInt32 chunkSize;
    string format;
    string subChunk1ID;
    UInt32 subChunk1Size;
    UInt16 audioFormat;
    UInt16 channelCount;
    UInt32 sampleRate;
    UInt32 byteRate;
    UInt16 blockAlign;
    UInt16 bitsPerSample;
    string subChunk2ID;
    UInt32 subChunk2Size;
    Byte[] data;

    public UInt32 ReadUInt32BE(BinaryReader reader)
    {
        Byte[] bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes);
    }

    public bool Load(String path)
    {
        FileStream fs = File.OpenRead(path);
        BinaryReader reader = new BinaryReader(fs);

        chunkID = Encoding.UTF8.GetString(reader.ReadBytes(4));
        chunkSize = reader.ReadUInt32();

        format = Encoding.UTF8.GetString(reader.ReadBytes(4));
        subChunk1ID = Encoding.UTF8.GetString(reader.ReadBytes(4));
        subChunk1Size = reader.ReadUInt32();
        audioFormat = reader.ReadUInt16();
        channelCount = reader.ReadUInt16();
        Stereo = channelCount > 1 ? true : false; // Set stereo
        sampleRate = reader.ReadUInt32();
        MixRate = (int)sampleRate; // Set sample rate
        byteRate = reader.ReadUInt32();
        Format = audioFormat == 1 ? FormatEnum.Format8Bits : FormatEnum.Format16Bits; // Set format
        blockAlign = reader.ReadUInt16();
        bitsPerSample = reader.ReadUInt16();


        UInt16 dummy = reader.ReadUInt16();

        subChunk2ID = Encoding.UTF8.GetString(reader.ReadBytes(4));
        subChunk2Size = reader.ReadUInt32();
        data = reader.ReadBytes((int)subChunk2Size);  // Set data
        Data = data.Select(x => (byte)(x - 128)).ToArray(); //Sign bytes for godot
        LoopMode = LoopModeEnum.Forward; //Set loop mode
        LoopEnd = (int)subChunk2Size;

        return true;
    }
}
