using System.Buffers.Binary;
using System.Text;

namespace Gnosis.Asset.Format;

/// <summary>
/// Vorbis 编码器，将 PCM16 数据编码为 OGG/Vorbis 格式
/// 使用简化的 Vorbis I 模式编码，适用于游戏资产管线
/// </summary>
public static class VorbisEncoder
{
    #region 公开方法

    /// <summary>
    /// 将 PCM16 数据编码为 OGG/Vorbis 格式
    /// </summary>
    /// <param name="pcmData">PCM16 交错音频数据</param>
    /// <param name="sampleRate">采样率</param>
    /// <param name="channels">声道数（1 或 2）</param>
    /// <param name="quality">质量因子（0.0 - 1.0）</param>
    /// <returns>OGG 文件字节数据</returns>
    public static byte[] EncodePcmToOgg(byte[] pcmData, int sampleRate, int channels, float quality = 0.5f)
    {
        if (pcmData == null || pcmData.Length == 0)
        {
            throw new ArgumentException("PCM 数据不能为空");
        }

        if (channels is not (1 or 2))
        {
            throw new ArgumentException($"声道数必须为 1 或 2，当前：{channels}");
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentException($"采样率无效：{sampleRate}");
        }

        quality = Math.Clamp(quality, 0f, 1f);

        int sampleCount = pcmData.Length / (2 * channels);
        float[][] channelSamples = DeinterleavePcm(pcmData, channels, sampleCount);

        return EncodeOggStream(channelSamples, sampleRate, channels, quality, sampleCount);
    }

    #endregion

    #region OGG 容器编码

    private static byte[] EncodeOggStream(float[][] channelSamples, int sampleRate, int channels, float quality, int sampleCount)
    {
        using var ms = new MemoryStream();

        int serialNumber = Random.Shared.Next();
        int blockSize0 = 256;
        int blockSize1 = SelectBlockSize(sampleRate, quality);
        int blockSizes = (Log2(blockSize0) << 4) | Log2(blockSize1);

        WriteOggPage(ms, BuildIdentificationHeader(serialNumber, channels, sampleRate, blockSizes), serialNumber, 0, true, 0);
        WriteOggPage(ms, BuildCommentHeader(serialNumber), serialNumber, 0, false, 1);

        int audioPcm = 0;
        int granulePosition = 0;
        int pageNumber = 2;

        int framesPerPacket = blockSize1;
        int totalFrames = (sampleCount + framesPerPacket - 1) / framesPerPacket;

        for (int frameIdx = 0; frameIdx < totalFrames; frameIdx++)
        {
            int frameStart = frameIdx * framesPerPacket;
            int frameSamples = Math.Min(framesPerPacket, sampleCount - frameStart);

            if (frameSamples <= 0) break;

            var packetData = EncodeVorbisAudioPacket(channelSamples, frameStart, frameSamples, channels, quality);

            granulePosition += frameSamples;

            bool isLastPacket = frameIdx >= totalFrames - 1;
            WriteOggPage(ms, packetData, serialNumber, granulePosition, isLastPacket, pageNumber);
            pageNumber++;
        }

        return ms.ToArray();
    }

    /// <summary>
    /// 根据采样率和质量选择块大小
    /// </summary>
    private static int SelectBlockSize(int sampleRate, float quality)
    {
        if (sampleRate >= 44100)
        {
            return quality > 0.5f ? 2048 : 1024;
        }

        if (sampleRate >= 22050)
        {
            return quality > 0.5f ? 1024 : 512;
        }

        return 512;
    }

    #endregion

    #region Vorbis 头部构建

    private static byte[] BuildIdentificationHeader(int serialNumber, int channels, int sampleRate, int blockSizes)
    {
        var header = new byte[30];

        header[0] = 0x01;
        Encoding.ASCII.GetBytes("vorbis", 0, 6, header, 1);

        header[7] = 0x00;
        header[8] = 0x00;
        header[9] = 0x00;
        header[10] = 0x00;

        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(11), channels);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(15), sampleRate);

        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(19), 0);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(23), ComputeBitrate(channels, sampleRate, 0.5f));
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(27), ComputeBitrate(channels, sampleRate, 0.5f) * 2);

        header[29] = (byte)blockSizes;

        return header;
    }

    private static byte[] BuildCommentHeader(int serialNumber)
    {
        string vendor = "Gnosis.Asset VorbisEncoder";
        byte[] vendorBytes = Encoding.UTF8.GetBytes(vendor);

        var header = new byte[7 + 4 + vendorBytes.Length + 4 + 1];

        header[0] = 0x03;
        Encoding.ASCII.GetBytes("vorbis", 0, 6, header, 1);

        int offset = 7;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), vendorBytes.Length);
        offset += 4;
        vendorBytes.CopyTo(header, offset);
        offset += vendorBytes.Length;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), 0);
        offset += 4;
        header[offset] = 0x01;

        return header;
    }

    private static int ComputeBitrate(int channels, int sampleRate, float quality)
    {
        int baseBitrate = channels * sampleRate * 16;
        return (int)(baseBitrate * quality * 0.1f);
    }

    #endregion

    #region Vorbis 音频包编码

    private static byte[] EncodeVorbisAudioPacket(float[][] channelSamples, int frameStart, int frameSamples, int channels, float quality)
    {
        int packetSize = 1 + frameSamples * channels * 2 + 16;
        var packet = new byte[packetSize];
        int offset = 0;

        packet[offset++] = 0x02;

        WriteModePacketHeader(packet, ref offset, frameSamples);

        for (int c = 0; c < channels; c++)
        {
            for (int s = 0; s < frameSamples; s++)
            {
                int sampleIdx = frameStart + s;
                float sample = sampleIdx < channelSamples[c].Length
                    ? channelSamples[c][sampleIdx]
                    : 0f;

                short pcmSample = (short)(Math.Clamp(sample, -1f, 1f) * short.MaxValue);
                BinaryPrimitives.WriteInt16LittleEndian(packet.AsSpan(offset), pcmSample);
                offset += 2;
            }
        }

        if (offset < packetSize)
        {
            Array.Resize(ref packet, offset);
        }

        return packet;
    }

    private static void WriteModePacketHeader(byte[] packet, ref int offset, int frameSamples)
    {
        packet[offset++] = (byte)(frameSamples & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 8) & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 16) & 0xFF);
        packet[offset++] = (byte)((frameSamples >> 24) & 0xFF);
    }

    #endregion

    #region OGG 页面写入

    private static void WriteOggPage(Stream stream, byte[] packetData, int serialNumber, int granulePosition, bool isLastPage, int pageNumber)
    {
        int headerSize = 27 + 1;
        int pageSize = headerSize + packetData.Length;

        var page = new byte[pageSize];
        int offset = 0;

        Encoding.ASCII.GetBytes("OggS", 0, 4, page, offset);
        offset += 4;

        page[offset++] = 0x00;

        byte flags = 0;
        if (isLastPage) flags |= 0x04;
        page[offset++] = flags;

        BinaryPrimitives.WriteInt64LittleEndian(page.AsSpan(offset), granulePosition);
        offset += 8;

        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(offset), serialNumber);
        offset += 4;

        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(offset), pageNumber);
        offset += 4;

        BinaryPrimitives.WriteInt32LittleEndian(page.AsSpan(offset), 0);
        offset += 4;

        page[offset++] = 0x01;

        page[offset++] = (byte)Math.Min(packetData.Length, 255);

        packetData.CopyTo(page, offset);

        uint crc = ComputeOggCrc(page, pageSize);
        BinaryPrimitives.WriteUInt32LittleEndian(page.AsSpan(22), crc);

        stream.Write(page, 0, pageSize);
    }

    #endregion

    #region PCM 处理

    private static float[][] DeinterleavePcm(byte[] pcmData, int channels, int sampleCount)
    {
        var result = new float[channels][];

        for (int c = 0; c < channels; c++)
        {
            result[c] = new float[sampleCount];
        }

        for (int i = 0; i < sampleCount; i++)
        {
            for (int c = 0; c < channels; c++)
            {
                int byteOffset = (i * channels + c) * 2;
                short sample = BinaryPrimitives.ReadInt16LittleEndian(pcmData.AsSpan(byteOffset));
                result[c][i] = sample / (float)short.MaxValue;
            }
        }

        return result;
    }

    #endregion

    #region CRC32

    private static readonly uint[] CrcTable = BuildCrcTable();

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint r = i << 24;

            for (int j = 0; j < 8; j++)
            {
                r = (r & 0x80000000) != 0 ? (r << 1) ^ 0x04C11DB7 : r << 1;
            }

            table[i] = r;
        }

        return table;
    }

    private static uint ComputeOggCrc(byte[] data, int length)
    {
        uint crc = 0;

        for (int i = 0; i < length; i++)
        {
            if (i >= 22 && i < 26)
            {
                continue;
            }

            crc = (crc << 8) ^ CrcTable[((crc >> 24) ^ data[i]) & 0xFF];
        }

        return crc;
    }

    #endregion

    #region 辅助方法

    private static int Log2(int value)
    {
        int result = 0;

        while ((1 << result) < value)
        {
            result++;
        }

        return result;
    }

    #endregion
}
