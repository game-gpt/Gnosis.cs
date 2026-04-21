using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Gnosis.Assets.Formats;
using Gnosis.Assets.Formats.BcCompression;

namespace Gnosis.Testing.Assets.Formats;

[TestFixture]
public class TextureFormatHandlerTests
{
    private string _tempDir = null!;
    private TextureFormatHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GnosisTextureTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _handler = new TextureFormatHandler();
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    #region 加载 PNG 图像测试

    [Test]
    public async Task LoadTextureAsync_PngImage_ReturnsCorrectTextureData()
    {
        var pngPath = Path.Combine(_tempDir, "test.png");
        CreateTestPng(pngPath, 8, 8);

        var texture = await _handler.LoadTextureAsync(pngPath);

        Assert.That(texture.Width, Is.EqualTo(8));
        Assert.That(texture.Height, Is.EqualTo(8));
        Assert.That(texture.Format, Is.EqualTo(TextureFormat.R8G8B8A8_UNorm));
        Assert.That(texture.Dimension, Is.EqualTo(TextureDimension.Texture2D));
        Assert.That(texture.RawData.Length, Is.EqualTo(8 * 8 * 4));
        Assert.That(texture.Name, Is.EqualTo("test"));
    }

    [Test]
    public void LoadTextureAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "nonexistent.png");

        var ex = Assert.ThrowsAsync<FileNotFoundException>(() =>
            _handler.LoadTextureAsync(path));
        Assert.That(ex!.Message, Does.Contain("未找到纹理文件"));
    }

    #endregion

    #region 调整大小测试

    [Test]
    public async Task ResizeAsync_ValidTexture_ReturnsResizedTexture()
    {
        var texture = CreateSolidTexture(8, 8, 255, 0, 0, 255);

        var resized = await _handler.ResizeAsync(texture, 4, 4);

        Assert.That(resized.Width, Is.EqualTo(4));
        Assert.That(resized.Height, Is.EqualTo(4));
        Assert.That(resized.RawData.Length, Is.EqualTo(4 * 4 * 4));
        Assert.That(resized.Format, Is.EqualTo(TextureFormat.R8G8B8A8_UNorm));
    }

    [Test]
    public async Task ResizeAsync_WithNearestFilter_PreservesPixelColors()
    {
        var texture = CreateSolidTexture(4, 4, 128, 64, 32, 255);

        var resized = await _handler.ResizeAsync(texture, 4, 4, ResizeFilter.Nearest);

        Assert.That(resized.RawData[0], Is.EqualTo(128));
        Assert.That(resized.RawData[1], Is.EqualTo(64));
        Assert.That(resized.RawData[2], Is.EqualTo(32));
        Assert.That(resized.RawData[3], Is.EqualTo(255));
    }

    [Test]
    public void ResizeAsync_EmptyData_ThrowsArgumentException()
    {
        var texture = new TextureData
        {
            Width = 4,
            Height = 4,
            Format = TextureFormat.R8G8B8A8_UNorm,
            RawData = Array.Empty<byte>()
        };

        Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.ResizeAsync(texture, 2, 2));
    }

    [Test]
    public void ResizeAsync_InvalidDimensions_ThrowsArgumentException()
    {
        var texture = CreateSolidTexture(4, 4, 255, 0, 0, 255);

        Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.ResizeAsync(texture, 0, 0));
    }

    #endregion

    #region BC1 压缩测试

    [Test]
    public async Task CompressAsync_BC1_ReturnsCompressedData()
    {
        var texture = CreateSolidTexture(4, 4, 255, 0, 0, 255);

        var compressed = await _handler.CompressAsync(texture, TextureCompressionFormat.BC1);

        Assert.That(compressed.Format, Is.EqualTo(TextureFormat.BC1_RGB_UNorm));
        Assert.That(compressed.RawData.Length, Is.EqualTo(8));
    }

    [Test]
    public async Task CompressAsync_BC1_WithAlpha_ReturnsBc1RgbaFormat()
    {
        var texture = CreateSolidTexture(4, 4, 255, 0, 0, 128);

        var compressed = await _handler.CompressAsync(texture, TextureCompressionFormat.BC1);

        Assert.That(compressed.Format, Is.EqualTo(TextureFormat.BC1_RGBA_UNorm));
        Assert.That(compressed.RawData.Length, Is.EqualTo(8));
    }

    [Test]
    public void CompressBc1_Uniform4x4Block_ProducesValidOutput()
    {
        var rgbaData = CreateSolidRgbaData(4, 4, 255, 0, 0, 255);

        var result = BcCompressor.CompressBc1(rgbaData, 4, 4);

        Assert.That(result.Length, Is.EqualTo(8));
        Assert.That(result[0], Is.Not.EqualTo(0));
    }

    [Test]
    public void CompressBc1_8x8Texture_ProducesFourBlocks()
    {
        var rgbaData = CreateSolidRgbaData(8, 8, 0, 128, 255, 255);

        var result = BcCompressor.CompressBc1(rgbaData, 8, 8);

        Assert.That(result.Length, Is.EqualTo(32));
    }

    #endregion

    #region BC3 压缩测试

    [Test]
    public async Task CompressAsync_BC3_ReturnsCompressedData()
    {
        var texture = CreateSolidTexture(4, 4, 128, 64, 32, 200);

        var compressed = await _handler.CompressAsync(texture, TextureCompressionFormat.BC3);

        Assert.That(compressed.Format, Is.EqualTo(TextureFormat.BC3_UNorm));
        Assert.That(compressed.RawData.Length, Is.EqualTo(16));
    }

    [Test]
    public void CompressBc3_4x4Block_ProducesSixteenBytes()
    {
        var rgbaData = CreateSolidRgbaData(4, 4, 100, 150, 200, 255);

        var result = BcCompressor.CompressBc3(rgbaData, 4, 4);

        Assert.That(result.Length, Is.EqualTo(16));
    }

    [Test]
    public void CompressBc3_VaryingAlpha_EncodesAlphaBlock()
    {
        var rgbaData = new byte[4 * 4 * 4];
        for (int i = 0; i < 16; i++)
        {
            rgbaData[i * 4] = 200;
            rgbaData[i * 4 + 1] = 100;
            rgbaData[i * 4 + 2] = 50;
            rgbaData[i * 4 + 3] = (byte)(i * 17);
        }

        var result = BcCompressor.CompressBc3(rgbaData, 4, 4);

        Assert.That(result.Length, Is.EqualTo(16));
        Assert.That(result[0], Is.Not.EqualTo(result[1]));
    }

    #endregion

    #region BC4 压缩测试

    [Test]
    public void CompressBc4_4x4Block_ProducesEightBytes()
    {
        var rgbaData = CreateSolidRgbaData(4, 4, 128, 0, 0, 255);

        var result = BcCompressor.CompressBc4(rgbaData, 4, 4);

        Assert.That(result.Length, Is.EqualTo(8));
    }

    #endregion

    #region BC5 压缩测试

    [Test]
    public void CompressBc5_4x4Block_ProducesSixteenBytes()
    {
        var rgbaData = CreateSolidRgbaData(4, 4, 128, 64, 0, 255);

        var result = BcCompressor.CompressBc5(rgbaData, 4, 4);

        Assert.That(result.Length, Is.EqualTo(16));
    }

    #endregion

    #region BC7 压缩测试

    [Test]
    public void CompressBc7_4x4Block_ProducesSixteenBytes()
    {
        var rgbaData = CreateSolidRgbaData(4, 4, 200, 100, 50, 255);

        var result = BcCompressor.CompressBc7(rgbaData, 4, 4);

        Assert.That(result.Length, Is.EqualTo(16));
        Assert.That(result[0], Is.EqualTo(0x40));
    }

    #endregion

    #region 验证文件测试

    [Test]
    public async Task ValidateAsync_ValidPng_ReturnsTrue()
    {
        var pngPath = Path.Combine(_tempDir, "valid.png");
        CreateTestPng(pngPath, 4, 4);

        var result = await _handler.ValidateAsync(pngPath);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateAsync_InvalidPngMagic_ReturnsFalse()
    {
        var pngPath = Path.Combine(_tempDir, "invalid.png");
        await File.WriteAllBytesAsync(pngPath, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        var result = await _handler.ValidateAsync(pngPath);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateAsync_NonExistentFile_ReturnsFalse()
    {
        var result = await _handler.ValidateAsync(Path.Combine(_tempDir, "nonexistent.png"));

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateAsync_ValidJpeg_ReturnsTrue()
    {
        var jpegPath = Path.Combine(_tempDir, "valid.jpg");
        CreateTestJpeg(jpegPath, 4, 4);

        var result = await _handler.ValidateAsync(jpegPath);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ValidateAsync_EngineFormat_ReturnsTrue()
    {
        var enginePath = Path.Combine(_tempDir, "test.gnosis-texture");
        var texture = CreateSolidTexture(4, 4, 255, 0, 0, 255);
        await _handler.SaveTextureAsync(enginePath, texture);

        var result = await _handler.ValidateAsync(enginePath);

        Assert.That(result, Is.True);
    }

    #endregion

    #region 保存纹理测试

    [Test]
    public async Task SaveTextureAsync_EngineFormat_CanBeReloaded()
    {
        var enginePath = Path.Combine(_tempDir, "save_test.gnosis-texture");
        var original = CreateSolidTexture(4, 4, 128, 64, 32, 255);

        await _handler.SaveTextureAsync(enginePath, original);
        var loaded = await _handler.LoadTextureAsync(enginePath);

        Assert.That(loaded.Width, Is.EqualTo(original.Width));
        Assert.That(loaded.Height, Is.EqualTo(original.Height));
        Assert.That(loaded.Format, Is.EqualTo(original.Format));
    }

    [Test]
    public async Task SaveTextureAsync_PngFormat_CanBeReloaded()
    {
        var pngPath = Path.Combine(_tempDir, "save_test.png");
        var original = CreateSolidTexture(8, 8, 200, 100, 50, 255);

        await _handler.SaveTextureAsync(pngPath, original);
        var loaded = await _handler.LoadTextureAsync(pngPath);

        Assert.That(loaded.Width, Is.EqualTo(8));
        Assert.That(loaded.Height, Is.EqualTo(8));
        Assert.That(loaded.Format, Is.EqualTo(TextureFormat.R8G8B8A8_UNorm));
    }

    #endregion

    #region BcBlockEncoder 单元测试

    [Test]
    public void EncodeRgb565_PureRed_ReturnsCorrectValue()
    {
        var result = BcBlockEncoder.EncodeRgb565(255, 0, 0);

        Assert.That(result, Is.EqualTo(0xF800));
    }

    [Test]
    public void EncodeRgb565_PureGreen_ReturnsCorrectValue()
    {
        var result = BcBlockEncoder.EncodeRgb565(0, 255, 0);

        Assert.That(result, Is.EqualTo(0x07E0));
    }

    [Test]
    public void EncodeRgb565_PureBlue_ReturnsCorrectValue()
    {
        var result = BcBlockEncoder.EncodeRgb565(0, 0, 255);

        Assert.That(result, Is.EqualTo(0x001F));
    }

    [Test]
    public void DecodeRgb565_RoundTrip_PreservesColor()
    {
        ushort encoded = BcBlockEncoder.EncodeRgb565(200, 100, 50);
        BcBlockEncoder.DecodeRgb565(encoded, out byte r, out byte g, out byte b);

        Assert.That(Math.Abs(r - 200), Is.LessThanOrEqualTo(8));
        Assert.That(Math.Abs(g - 100), Is.LessThanOrEqualTo(4));
        Assert.That(Math.Abs(b - 50), Is.LessThanOrEqualTo(8));
    }

    [Test]
    public void ComputeAlphaPalette_A0GreaterThanA1_ProducesEightValues()
    {
        var palette = new byte[8];
        BcBlockEncoder.ComputeAlphaPalette(255, 0, palette);

        Assert.That(palette[0], Is.EqualTo(255));
        Assert.That(palette[1], Is.EqualTo(0));
        Assert.That(palette[7], Is.EqualTo(0));
    }

    [Test]
    public void ComputeAlphaPalette_A0LessThanA1_ProducesSixValuesPlusSpecial()
    {
        var palette = new byte[8];
        BcBlockEncoder.ComputeAlphaPalette(0, 255, palette);

        Assert.That(palette[0], Is.EqualTo(0));
        Assert.That(palette[1], Is.EqualTo(255));
        Assert.That(palette[6], Is.EqualTo(0));
        Assert.That(palette[7], Is.EqualTo(255));
    }

    #endregion

    #region 辅助方法

    private static TextureData CreateSolidTexture(int width, int height, byte r, byte g, byte b, byte a)
    {
        var rawData = CreateSolidRgbaData(width, height, r, g, b, a);

        return new TextureData
        {
            Name = "test",
            Width = width,
            Height = height,
            Depth = 1,
            MipLevels = 1,
            ArrayLayers = 1,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Dimension = TextureDimension.Texture2D,
            RawData = rawData
        };
    }

    private static byte[] CreateSolidRgbaData(int width, int height, byte r, byte g, byte b, byte a)
    {
        var data = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            data[i * 4] = r;
            data[i * 4 + 1] = g;
            data[i * 4 + 2] = b;
            data[i * 4 + 3] = a;
        }
        return data;
    }

    private static void CreateTestPng(string path, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32((byte)(x * 32), (byte)(y * 32), 128, 255);
            }
        }
        image.SaveAsPng(path);
    }

    private static void CreateTestJpeg(string path, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(200, 100, 50, 255);
            }
        }
        image.SaveAsJpeg(path);
    }

    #endregion
}
