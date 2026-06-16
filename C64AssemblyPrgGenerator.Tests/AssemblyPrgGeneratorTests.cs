using System;
using System.IO;
using C64AssemblyPrgGenerator;
using Xunit;

namespace C64AssemblyPrgGenerator.Tests;

public class AssemblyPrgGeneratorTests
{
    [Fact]
    public void GeneratePrg_ValidHelloSysSource_ReturnsExpectedPrgBytes()
    {
        const string source = """
            .org $C000
            start:
                lda #$01
                sta $D020
                rts
            """;

        var sut = new AssemblyPrgGenerator();
        var result = sut.GeneratePrg(source);

        Assert.Equal((ushort)0xC000, result.Origin);
        Assert.False(result.BasicRunLoader);
        Assert.True(result.PrgBytes.Length >= 8);
        Assert.Equal(0x00, result.PrgBytes[0]); // load address lo
        Assert.Equal(0xC0, result.PrgBytes[1]); // load address hi

        // A9 01 8D 20 D0 60
        var payload = new byte[] { 0xA9, 0x01, 0x8D, 0x20, 0xD0, 0x60 };
        for (var i = 0; i < payload.Length; i++)
        {
            Assert.Equal(payload[i], result.PrgBytes[i + 2]);
        }
    }

    [Fact]
    public void GeneratePrg_WithBasicRunLoader_UsesBasicLoadAddress()
    {
        const string source = """
            .org $C000
            start:
                rts
            """;

        var sut = new AssemblyPrgGenerator();
        var result = sut.GeneratePrg(source, basicRunLoader: true);

        Assert.True(result.BasicRunLoader);
        Assert.Equal((ushort)0xC000, result.Origin);
        Assert.True(result.PrgBytes.Length > 10);
        Assert.Equal(0x01, result.PrgBytes[0]); // BASIC start $0801 lo
        Assert.Equal(0x08, result.PrgBytes[1]); // BASIC start $0801 hi
        Assert.Contains((byte)0x9E, result.PrgBytes); // SYS token
    }

    [Fact]
    public void GeneratePrg_TextScrollLikeSource_CanBeSavedAsPrgFile()
    {
        const string source = """
            .org $1000
            start:
                lda #$20
                ldx #$00
            fill:
                sta $0400,x
                inx
                cpx #$28
                bne fill
                rts
            """;

        var sut = new AssemblyPrgGenerator();
        var result = sut.GeneratePrg(source);

        var tempPath = Path.Combine(Path.GetTempPath(), $"asmgen-{Guid.NewGuid():N}.prg");
        try
        {
            File.WriteAllBytes(tempPath, result.PrgBytes);

            Assert.True(File.Exists(tempPath));
            Assert.True(new FileInfo(tempPath).Length > 2);
            Assert.Equal(result.PrgBytes.Length, new FileInfo(tempPath).Length);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
