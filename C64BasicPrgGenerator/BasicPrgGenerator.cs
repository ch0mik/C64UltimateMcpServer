using System;
using RetroC64.Basic;   
using RetroC64.App;

namespace C64BasicPrgGenerator;

/// <summary>
/// Generates Commodore 64 BASIC PRG files from BASIC source code.
/// Uses RetroC64 SDK C64AppBasic for standard BASIC V2 tokenization.
/// </summary>
public class BasicPrgGenerator
{
    /// <summary>
    /// Generate a C64 BASIC PRG file from BASIC source code.
    /// </summary>
    /// <param name="basicSource">BASIC source code (e.g., "10 PRINT \"HELLO\"\n20 GOTO 10")</param>
    /// <returns>Binary PRG data ready for upload/execution</returns>
    /// <exception cref="ArgumentException">Thrown on BASIC syntax errors</exception>
    public byte[] GeneratePrg(string basicSource)
    {
        if (string.IsNullOrWhiteSpace(basicSource))
            throw new ArgumentException("BASIC source cannot be empty", nameof(basicSource));

        try
        {
            // Use RetroC64 C64AppBasic to generate PRG
            var app = new C64BasicCompiler();
            var prgData = app.Compile(basicSource);
            if (prgData.Length == 0)
                throw new ArgumentException("Failed to generate PRG from BASIC source");
            
            return prgData.ToArray();
        }
        catch (Exception ex) when (!(ex is ArgumentException))
        {
            throw new ArgumentException($"BASIC compilation error: {ex.Message}", nameof(basicSource), ex);
        }
    }
}
