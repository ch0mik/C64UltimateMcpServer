using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Asm6502;

namespace C64AssemblyPrgGenerator;

/// <summary>
/// Compiles textual 6510 assembly source into C64 PRG bytes.
/// Supports a compact, MCP-friendly syntax:
/// .org / *=, labels, 6502/6510 mnemonics, .byte, .word, basic expressions (+/-).
/// </summary>
public class AssemblyPrgGenerator
{
    public AssemblyPrgBuildResult GeneratePrg(string assemblySource, bool basicRunLoader = false)
    {
        if (string.IsNullOrWhiteSpace(assemblySource))
        {
            throw new ArgumentException("Assembly source cannot be empty", nameof(assemblySource));
        }

        try
        {
            var normalized = assemblySource.Replace("\r\n", "\n");
            var lines = normalized.Split('\n');
            var compiler = new Text6510Compiler(lines, "<mcp-assembly-source>");
            var result = compiler.Compile();
            var prgBytes = basicRunLoader ? WrapWithBasicRunLoader(result) : result.PrgBytes;
            return new AssemblyPrgBuildResult(result.Origin, result.CodeSize, prgBytes, basicRunLoader);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Assembly compilation error: {ex.Message}", nameof(assemblySource), ex);
        }
    }

    private static byte[] WrapWithBasicRunLoader(CompileResult result)
    {
        const ushort basicLoadAddress = 0x0801;
        const ushort basicLineNumber = 10;

        var sysText = result.Origin.ToString(CultureInfo.InvariantCulture);
        var firstLineLength = 2 + 2 + 1 + sysText.Length + 1;
        var nextLineAddress = (ushort)(basicLoadAddress + firstLineLength);

        var payload = new List<byte>(result.CodeSize + 256)
        {
            (byte)(nextLineAddress & 0xFF),
            (byte)(nextLineAddress >> 8),
            (byte)(basicLineNumber & 0xFF),
            (byte)(basicLineNumber >> 8),
            0x9E // SYS token
        };

        foreach (var ch in sysText)
        {
            payload.Add((byte)ch);
        }

        payload.Add(0x00); // end of BASIC line
        payload.Add(0x00); // next line pointer low (no next line)
        payload.Add(0x00); // next line pointer high

        var codeAddress = result.Origin;
        var basicEndAddress = (ushort)(basicLoadAddress + payload.Count);
        if (codeAddress < basicEndAddress)
        {
            throw new InvalidOperationException(
                $"Cannot prepend BASIC loader: code starts at ${codeAddress:X4}, but loader ends at ${basicEndAddress:X4}. " +
                "Use a higher .org (e.g. $0810 or $C000), or disable basicRunLoader.");
        }

        var paddingCount = codeAddress - basicEndAddress;
        for (var i = 0; i < paddingCount; i++)
        {
            payload.Add(0x00);
        }

        for (var i = 2; i < result.PrgBytes.Length; i++)
        {
            payload.Add(result.PrgBytes[i]);
        }

        var wrapped = new byte[payload.Count + 2];
        wrapped[0] = (byte)(basicLoadAddress & 0xFF);
        wrapped[1] = (byte)(basicLoadAddress >> 8);
        payload.CopyTo(wrapped, 2);
        return wrapped;
    }
}

public sealed record AssemblyPrgBuildResult(ushort Origin, int CodeSize, byte[] PrgBytes, bool BasicRunLoader);

internal sealed class Text6510Compiler
{
    private readonly string[] _lines;
    private readonly string _sourcePath;
    private readonly Dictionary<string, ushort> _labels = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<byte> _code = [];
    private readonly List<Fixup> _fixups = [];
    private readonly Dictionary<(Mos6510Mnemonic Mnemonic, Mos6502AddressingMode Mode), byte> _opcodeMap;

    private ushort? _origin;
    private ushort _pc;

    public Text6510Compiler(string[] lines, string sourcePath)
    {
        _lines = lines;
        _sourcePath = sourcePath;
        _opcodeMap = BuildOpcodeMap();
    }

    public CompileResult Compile()
    {
        for (var i = 0; i < _lines.Length; i++)
        {
            ParseLine(_lines[i], i + 1);
        }

        ApplyFixups();

        if (_origin is null)
        {
            throw new InvalidOperationException("No origin defined. Add .org $xxxx or *= $xxxx");
        }

        var prg = new byte[_code.Count + 2];
        prg[0] = (byte)(_origin.Value & 0xFF);
        prg[1] = (byte)(_origin.Value >> 8);
        _code.CopyTo(prg, 2);

        return new CompileResult(_origin.Value, _code.Count, prg);
    }

    private static Dictionary<(Mos6510Mnemonic, Mos6502AddressingMode), byte> BuildOpcodeMap()
    {
        var map = new Dictionary<(Mos6510Mnemonic, Mos6502AddressingMode), byte>();

        for (var op = 0; op <= 0xFF; op++)
        {
            var opcode = (Mos6510OpCode)op;
            var mnemonic = opcode.ToMnemonic();
            var mode = opcode.ToAddressingMode();
            if (mnemonic == Mos6510Mnemonic.Unknown || mode == Mos6502AddressingMode.Unknown)
            {
                continue;
            }

            var key = (mnemonic, mode);
            if (!map.ContainsKey(key))
            {
                map[key] = (byte)op;
            }
        }

        return map;
    }

    private void ParseLine(string rawLine, int lineNumber)
    {
        var line = StripComment(rawLine).Trim();
        if (line.Length == 0)
        {
            return;
        }

        while (true)
        {
            var colon = line.IndexOf(':');
            if (colon <= 0)
            {
                break;
            }

            var maybeLabel = line[..colon].Trim();
            if (!IsIdentifier(maybeLabel))
            {
                break;
            }

            DefineLabel(maybeLabel, lineNumber);
            line = line[(colon + 1)..].Trim();
            if (line.Length == 0)
            {
                return;
            }
        }

        if (line.StartsWith(".org", StringComparison.OrdinalIgnoreCase))
        {
            SetOrigin(ParseExpression(line[4..].Trim(), lineNumber), lineNumber);
            return;
        }

        if (line.StartsWith("*=", StringComparison.OrdinalIgnoreCase))
        {
            SetOrigin(ParseExpression(line[2..].Trim(), lineNumber), lineNumber);
            return;
        }

        if (line.StartsWith(".byte", StringComparison.OrdinalIgnoreCase))
        {
            EmitDataList(line[5..].Trim(), isWord: false, lineNumber);
            return;
        }

        if (line.StartsWith(".word", StringComparison.OrdinalIgnoreCase))
        {
            EmitDataList(line[5..].Trim(), isWord: true, lineNumber);
            return;
        }

        ParseInstruction(line, lineNumber);
    }

    private void ParseInstruction(string line, int lineNumber)
    {
        var parts = line.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
        var mnemonicText = parts[0].Trim();

        if (!Enum.TryParse<Mos6510Mnemonic>(mnemonicText, true, out var mnemonic) || mnemonic == Mos6510Mnemonic.Unknown)
        {
            throw Error(lineNumber, $"Unknown mnemonic '{mnemonicText}'");
        }

        var operandText = parts.Length > 1 ? parts[1].Trim() : string.Empty;
        var parsed = ParseOperand(mnemonic, operandText, lineNumber);

        if (!_opcodeMap.TryGetValue((mnemonic, parsed.Mode), out var opcode))
        {
            throw Error(lineNumber, $"Addressing mode {parsed.Mode} is not valid for {mnemonic}");
        }

        EmitByte(opcode);

        if (parsed.Mode == Mos6502AddressingMode.Implied || parsed.Mode == Mos6502AddressingMode.Accumulator)
        {
            return;
        }

        switch (parsed.Mode)
        {
            case Mos6502AddressingMode.Immediate:
            case Mos6502AddressingMode.ZeroPage:
            case Mos6502AddressingMode.ZeroPageX:
            case Mos6502AddressingMode.ZeroPageY:
            case Mos6502AddressingMode.IndirectX:
            case Mos6502AddressingMode.IndirectY:
            case Mos6502AddressingMode.Relative:
                EmitValueOrFixup(parsed.Expression!, 1, parsed.Mode == Mos6502AddressingMode.Relative, lineNumber);
                break;

            case Mos6502AddressingMode.Absolute:
            case Mos6502AddressingMode.AbsoluteX:
            case Mos6502AddressingMode.AbsoluteY:
            case Mos6502AddressingMode.Indirect:
                EmitValueOrFixup(parsed.Expression!, 2, false, lineNumber);
                break;

            default:
                throw Error(lineNumber, $"Unsupported addressing mode {parsed.Mode}");
        }
    }

    private ParsedOperand ParseOperand(Mos6510Mnemonic mnemonic, string operandText, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(operandText))
        {
            return new ParsedOperand(Mos6502AddressingMode.Implied, null);
        }

        var op = operandText.Trim();
        if (string.Equals(op, "A", StringComparison.OrdinalIgnoreCase))
        {
            return new ParsedOperand(Mos6502AddressingMode.Accumulator, null);
        }

        if (op.StartsWith("#", StringComparison.Ordinal))
        {
            var expr = ParseExpression(op[1..].Trim(), lineNumber);
            return new ParsedOperand(Mos6502AddressingMode.Immediate, expr);
        }

        if (IsBranchMnemonic(mnemonic))
        {
            var expr = ParseExpression(op, lineNumber);
            return new ParsedOperand(Mos6502AddressingMode.Relative, expr);
        }

        if (op.StartsWith("(", StringComparison.Ordinal) && op.EndsWith(",X)", StringComparison.OrdinalIgnoreCase))
        {
            var expr = ParseExpression(op[1..^3].Trim(), lineNumber);
            return new ParsedOperand(Mos6502AddressingMode.IndirectX, expr);
        }

        if (op.StartsWith("(", StringComparison.Ordinal) && op.EndsWith("),Y", StringComparison.OrdinalIgnoreCase))
        {
            var expr = ParseExpression(op[1..^3].Trim(), lineNumber);
            return new ParsedOperand(Mos6502AddressingMode.IndirectY, expr);
        }

        if (op.StartsWith("(", StringComparison.Ordinal) && op.EndsWith(")", StringComparison.Ordinal))
        {
            var expr = ParseExpression(op[1..^1].Trim(), lineNumber);
            return new ParsedOperand(Mos6502AddressingMode.Indirect, expr);
        }

        var idx = op.LastIndexOf(',');
        if (idx > 0)
        {
            var left = op[..idx].Trim();
            var right = op[(idx + 1)..].Trim();
            var expr = ParseExpression(left, lineNumber);

            if (right.Equals("X", StringComparison.OrdinalIgnoreCase))
            {
                var mode = PreferZpOrAbs(mnemonic, expr, Mos6502AddressingMode.ZeroPageX, Mos6502AddressingMode.AbsoluteX);
                return new ParsedOperand(mode, expr);
            }

            if (right.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                var mode = PreferZpOrAbs(mnemonic, expr, Mos6502AddressingMode.ZeroPageY, Mos6502AddressingMode.AbsoluteY);
                return new ParsedOperand(mode, expr);
            }

            throw Error(lineNumber, $"Invalid index register '{right}' in operand '{operandText}'");
        }

        {
            var expr = ParseExpression(op, lineNumber);
            var mode = PreferZpOrAbs(mnemonic, expr, Mos6502AddressingMode.ZeroPage, Mos6502AddressingMode.Absolute);
            return new ParsedOperand(mode, expr);
        }
    }

    private Mos6502AddressingMode PreferZpOrAbs(Mos6510Mnemonic mnemonic, Expression expr, Mos6502AddressingMode zp, Mos6502AddressingMode abs)
    {
        var hasZp = _opcodeMap.ContainsKey((mnemonic, zp));
        var hasAbs = _opcodeMap.ContainsKey((mnemonic, abs));

        if (expr.TryResolve(_labels, out var value))
        {
            if (value <= 0xFF && hasZp)
            {
                return zp;
            }

            if (hasAbs)
            {
                return abs;
            }

            if (hasZp)
            {
                return zp;
            }
        }
        else
        {
            if (hasAbs)
            {
                return abs;
            }

            if (hasZp)
            {
                return zp;
            }
        }

        return abs;
    }

    private void EmitDataList(string operandList, bool isWord, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(operandList))
        {
            throw Error(lineNumber, "Missing data list");
        }

        var items = operandList.Split(',');
        foreach (var item in items)
        {
            var expr = ParseExpression(item.Trim(), lineNumber);
            EmitValueOrFixup(expr, isWord ? 2 : 1, false, lineNumber);
        }
    }

    private void SetOrigin(Expression expression, int lineNumber)
    {
        if (!expression.TryResolve(_labels, out var value))
        {
            throw Error(lineNumber, "Origin must be a constant value");
        }

        if (value < 0 || value > 0xFFFF)
        {
            throw Error(lineNumber, $"Origin out of range: {value}");
        }

        var addr = (ushort)value;

        if (_origin is null)
        {
            _origin = addr;
            _pc = addr;
            return;
        }

        if (addr < _pc)
        {
            throw Error(lineNumber, $".org cannot move backwards (${_pc:X4} -> ${addr:X4})");
        }

        while (_pc < addr)
        {
            EmitByte(0x00);
        }
    }

    private void DefineLabel(string label, int lineNumber)
    {
        EnsureOrigin(lineNumber);

        if (_labels.ContainsKey(label))
        {
            throw Error(lineNumber, $"Label '{label}' already defined");
        }

        _labels[label] = _pc;
    }

    private void EmitValueOrFixup(Expression expression, int size, bool isRelative, int lineNumber)
    {
        EnsureOrigin(lineNumber);

        var offset = _code.Count;
        var operandAddress = _pc;
        for (var i = 0; i < size; i++)
        {
            EmitByte(0x00);
        }

        if (expression.TryResolve(_labels, out var value))
        {
            WriteValue(value, size, isRelative, offset, lineNumber, operandAddress);
            return;
        }

        _fixups.Add(new Fixup(offset, operandAddress, size, isRelative, expression, lineNumber));
    }

    private void ApplyFixups()
    {
        foreach (var f in _fixups)
        {
            if (!f.Expression.TryResolve(_labels, out var value))
            {
                throw Error(f.LineNumber, $"Unresolved symbol in expression '{f.Expression.Raw}'");
            }

            WriteValue(value, f.Size, f.IsRelative, f.Offset, f.LineNumber, f.AddressAtOperand);
        }
    }

    private void WriteValue(int value, int size, bool isRelative, int codeOffset, int lineNumber, ushort? addressAtOperand = null)
    {
        if (isRelative)
        {
            var operandAddress = addressAtOperand ?? (ushort)(_origin!.Value + codeOffset);
            var nextInstruction = operandAddress + 1;
            var delta = value - nextInstruction;
            if (delta < sbyte.MinValue || delta > sbyte.MaxValue)
            {
                throw Error(lineNumber, $"Branch target out of range: delta {delta}");
            }

            _code[codeOffset] = unchecked((byte)(sbyte)delta);
            return;
        }

        if (size == 1)
        {
            if (value < 0 || value > 0xFF)
            {
                throw Error(lineNumber, $"Byte value out of range: {value}");
            }

            _code[codeOffset] = (byte)value;
            return;
        }

        if (size == 2)
        {
            if (value < 0 || value > 0xFFFF)
            {
                throw Error(lineNumber, $"Word value out of range: {value}");
            }

            _code[codeOffset] = (byte)(value & 0xFF);
            _code[codeOffset + 1] = (byte)(value >> 8);
            return;
        }

        throw new InvalidOperationException($"Unsupported operand size {size}");
    }

    private static bool IsBranchMnemonic(Mos6510Mnemonic mnemonic)
        => mnemonic is Mos6510Mnemonic.BCC or Mos6510Mnemonic.BCS or Mos6510Mnemonic.BEQ or Mos6510Mnemonic.BMI
            or Mos6510Mnemonic.BNE or Mos6510Mnemonic.BPL or Mos6510Mnemonic.BVC or Mos6510Mnemonic.BVS;

    private Expression ParseExpression(string text, int lineNumber)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw Error(lineNumber, "Missing expression");
        }

        var trimmed = text.Trim();

        var plus = trimmed.LastIndexOf('+');
        if (plus > 0)
        {
            var lhs = ParseExpression(trimmed[..plus], lineNumber);
            var rhs = ParseExpression(trimmed[(plus + 1)..], lineNumber);
            return Expression.Add(lhs, rhs, trimmed);
        }

        var minus = FindBinaryMinus(trimmed);
        if (minus > 0)
        {
            var lhs = ParseExpression(trimmed[..minus], lineNumber);
            var rhs = ParseExpression(trimmed[(minus + 1)..], lineNumber);
            return Expression.Sub(lhs, rhs, trimmed);
        }

        if (TryParseNumber(trimmed, out var value))
        {
            return Expression.Const(value, trimmed);
        }

        if (!IsIdentifier(trimmed))
        {
            throw Error(lineNumber, $"Invalid expression '{trimmed}'");
        }

        return Expression.Symbol(trimmed, trimmed);
    }

    private static int FindBinaryMinus(string input)
    {
        for (var i = input.Length - 1; i >= 1; i--)
        {
            if (input[i] != '-')
            {
                continue;
            }

            var prev = input[i - 1];
            if (prev == '+' || prev == '-' || prev == '*' || prev == '/' || prev == '(')
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private static bool TryParseNumber(string text, out int value)
    {
        var s = text.Trim();
        if (s.Length == 0)
        {
            value = 0;
            return false;
        }

        if (s.StartsWith("$", StringComparison.Ordinal))
        {
            return int.TryParse(s[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        if (s.StartsWith("%", StringComparison.Ordinal))
        {
            try
            {
                value = Convert.ToInt32(s[1..], 2);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(s[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }

        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool IsIdentifier(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!(char.IsLetter(text[0]) || text[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < text.Length; i++)
        {
            var c = text[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private void EmitByte(byte value)
    {
        EnsureOrigin(0);
        _code.Add(value);
        _pc++;
    }

    private void EnsureOrigin(int lineNumber)
    {
        if (_origin is not null)
        {
            return;
        }

        throw Error(lineNumber, "No origin set. Add .org $xxxx before code/data");
    }

    private static string StripComment(string line)
    {
        var idx = line.IndexOf(';');
        return idx < 0 ? line : line[..idx];
    }

    private Exception Error(int lineNumber, string message)
        => new InvalidOperationException($"{_sourcePath}:{lineNumber}: {message}");

    private sealed record ParsedOperand(Mos6502AddressingMode Mode, Expression? Expression);

    private sealed record Fixup(int Offset, ushort AddressAtOperand, int Size, bool IsRelative, Expression Expression, int LineNumber);

    private sealed class Expression
    {
        private readonly Func<IReadOnlyDictionary<string, ushort>, (bool Ok, int Value)> _resolve;

        private Expression(Func<IReadOnlyDictionary<string, ushort>, (bool Ok, int Value)> resolve, string raw)
        {
            _resolve = resolve;
            Raw = raw;
        }

        public string Raw { get; }

        public bool TryResolve(IReadOnlyDictionary<string, ushort> labels, out int value)
        {
            var (ok, v) = _resolve(labels);
            value = v;
            return ok;
        }

        public static Expression Const(int value, string raw)
            => new(_ => (true, value), raw);

        public static Expression Symbol(string name, string raw)
            => new(labels => labels.TryGetValue(name, out var v) ? (true, v) : (false, 0), raw);

        public static Expression Add(Expression lhs, Expression rhs, string raw)
            => new(labels =>
            {
                var okL = lhs.TryResolve(labels, out var lv);
                var okR = rhs.TryResolve(labels, out var rv);
                return okL && okR ? (true, lv + rv) : (false, 0);
            }, raw);

        public static Expression Sub(Expression lhs, Expression rhs, string raw)
            => new(labels =>
            {
                var okL = lhs.TryResolve(labels, out var lv);
                var okR = rhs.TryResolve(labels, out var rv);
                return okL && okR ? (true, lv - rv) : (false, 0);
            }, raw);
    }
}

internal sealed record CompileResult(ushort Origin, int CodeSize, byte[] PrgBytes);
