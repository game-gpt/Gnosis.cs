namespace Gnosis.Editor.Widget;

internal static class BitmapFont
{
    public const int GlyphSize = 8;

    private static readonly byte[][] _glyphs = new byte[128][];

    static BitmapFont()
    {
        for (int i = 0; i < 128; i++)
        {
            _glyphs[i] = i >= 0x20 && i < 0x7F
                ? BuildGlyph((char)i)
                : BuildGlyph('\0');
        }
    }

    public static byte[] GetGlyph(char ch)
    {
        int index = ch & 0x7F;

        if (index < 0x20 || index >= 0x7F)
        {
            return _glyphs[0];
        }

        return _glyphs[index];
    }

    private static byte[] BuildGlyph(char ch)
    {
        byte[] glyph = new byte[8];

        if (ch == '\0')
        {
            glyph[0] = 0x7E;
            glyph[1] = 0x42;
            glyph[2] = 0x42;
            glyph[3] = 0x42;
            glyph[4] = 0x42;
            glyph[5] = 0x42;
            glyph[6] = 0x7E;
            glyph[7] = 0x00;
            return glyph;
        }

        switch (ch)
        {
            case ' ':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x00; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '!':
                glyph[0] = 0x18; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x00; glyph[6] = 0x18; glyph[7] = 0x00;
                break;
            case '"':
                glyph[0] = 0x6C; glyph[1] = 0x6C; glyph[2] = 0x24; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '#':
                glyph[0] = 0x6C; glyph[1] = 0x6C; glyph[2] = 0xFE; glyph[3] = 0x6C;
                glyph[4] = 0xFE; glyph[5] = 0x6C; glyph[6] = 0x6C; glyph[7] = 0x00;
                break;
            case '$':
                glyph[0] = 0x18; glyph[1] = 0x7E; glyph[2] = 0xC0; glyph[3] = 0x7C;
                glyph[4] = 0x06; glyph[5] = 0xFC; glyph[6] = 0x18; glyph[7] = 0x00;
                break;
            case '%':
                glyph[0] = 0xC6; glyph[1] = 0xCC; glyph[2] = 0x18; glyph[3] = 0x30;
                glyph[4] = 0x60; glyph[5] = 0xC6; glyph[6] = 0x86; glyph[7] = 0x00;
                break;
            case '&':
                glyph[0] = 0x38; glyph[1] = 0x6C; glyph[2] = 0x38; glyph[3] = 0x76;
                glyph[4] = 0xDC; glyph[5] = 0xCC; glyph[6] = 0x76; glyph[7] = 0x00;
                break;
            case '\'':
                glyph[0] = 0x18; glyph[1] = 0x18; glyph[2] = 0x30; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '(':
                glyph[0] = 0x0C; glyph[1] = 0x18; glyph[2] = 0x30; glyph[3] = 0x30;
                glyph[4] = 0x30; glyph[5] = 0x18; glyph[6] = 0x0C; glyph[7] = 0x00;
                break;
            case ')':
                glyph[0] = 0x30; glyph[1] = 0x18; glyph[2] = 0x0C; glyph[3] = 0x0C;
                glyph[4] = 0x0C; glyph[5] = 0x18; glyph[6] = 0x30; glyph[7] = 0x00;
                break;
            case '*':
                glyph[0] = 0x00; glyph[1] = 0x66; glyph[2] = 0x3C; glyph[3] = 0xFF;
                glyph[4] = 0x3C; glyph[5] = 0x66; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '+':
                glyph[0] = 0x00; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x7E;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case ',':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x00; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x18; glyph[6] = 0x18; glyph[7] = 0x30;
                break;
            case '-':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x00; glyph[3] = 0x7E;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '.':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x00; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x18; glyph[6] = 0x18; glyph[7] = 0x00;
                break;
            case '/':
                glyph[0] = 0x06; glyph[1] = 0x0C; glyph[2] = 0x18; glyph[3] = 0x30;
                glyph[4] = 0x60; glyph[5] = 0xC0; glyph[6] = 0x80; glyph[7] = 0x00;
                break;
            case '0':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xCE; glyph[3] = 0xDE;
                glyph[4] = 0xF6; glyph[5] = 0xE6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case '1':
                glyph[0] = 0x18; glyph[1] = 0x38; glyph[2] = 0x78; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x7E; glyph[7] = 0x00;
                break;
            case '2':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0x06; glyph[3] = 0x1C;
                glyph[4] = 0x30; glyph[5] = 0x66; glyph[6] = 0xFE; glyph[7] = 0x00;
                break;
            case '3':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0x06; glyph[3] = 0x3C;
                glyph[4] = 0x06; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case '4':
                glyph[0] = 0x1C; glyph[1] = 0x3C; glyph[2] = 0x6C; glyph[3] = 0xCC;
                glyph[4] = 0xFE; glyph[5] = 0x0C; glyph[6] = 0x1E; glyph[7] = 0x00;
                break;
            case '5':
                glyph[0] = 0xFE; glyph[1] = 0xC0; glyph[2] = 0xFC; glyph[3] = 0x06;
                glyph[4] = 0x06; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case '6':
                glyph[0] = 0x38; glyph[1] = 0x60; glyph[2] = 0xC0; glyph[3] = 0xFC;
                glyph[4] = 0xC6; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case '7':
                glyph[0] = 0xFE; glyph[1] = 0xC6; glyph[2] = 0x0C; glyph[3] = 0x18;
                glyph[4] = 0x30; glyph[5] = 0x30; glyph[6] = 0x30; glyph[7] = 0x00;
                break;
            case '8':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0x7C;
                glyph[4] = 0xC6; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case '9':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0x7E;
                glyph[4] = 0x06; glyph[5] = 0x0C; glyph[6] = 0x78; glyph[7] = 0x00;
                break;
            case ':':
                glyph[0] = 0x00; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x18; glyph[6] = 0x18; glyph[7] = 0x00;
                break;
            case ';':
                glyph[0] = 0x00; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x18; glyph[6] = 0x18; glyph[7] = 0x30;
                break;
            case '<':
                glyph[0] = 0x0C; glyph[1] = 0x18; glyph[2] = 0x30; glyph[3] = 0x60;
                glyph[4] = 0x30; glyph[5] = 0x18; glyph[6] = 0x0C; glyph[7] = 0x00;
                break;
            case '=':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x7E; glyph[3] = 0x00;
                glyph[4] = 0x7E; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '>':
                glyph[0] = 0x60; glyph[1] = 0x30; glyph[2] = 0x18; glyph[3] = 0x0C;
                glyph[4] = 0x18; glyph[5] = 0x30; glyph[6] = 0x60; glyph[7] = 0x00;
                break;
            case '?':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0x0C; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x00; glyph[6] = 0x18; glyph[7] = 0x00;
                break;
            case '@':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xDE; glyph[3] = 0xDE;
                glyph[4] = 0xDE; glyph[5] = 0xC0; glyph[6] = 0x78; glyph[7] = 0x00;
                break;
            case 'A':
                glyph[0] = 0x38; glyph[1] = 0x6C; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0xFE; glyph[5] = 0xC6; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'B':
                glyph[0] = 0xFC; glyph[1] = 0x66; glyph[2] = 0x66; glyph[3] = 0x7C;
                glyph[4] = 0x66; glyph[5] = 0x66; glyph[6] = 0xFC; glyph[7] = 0x00;
                break;
            case 'C':
                glyph[0] = 0x3C; glyph[1] = 0x66; glyph[2] = 0xC0; glyph[3] = 0xC0;
                glyph[4] = 0xC0; glyph[5] = 0x66; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case 'D':
                glyph[0] = 0xF8; glyph[1] = 0x6C; glyph[2] = 0x66; glyph[3] = 0x66;
                glyph[4] = 0x66; glyph[5] = 0x6C; glyph[6] = 0xF8; glyph[7] = 0x00;
                break;
            case 'E':
                glyph[0] = 0xFE; glyph[1] = 0x62; glyph[2] = 0x68; glyph[3] = 0x78;
                glyph[4] = 0x68; glyph[5] = 0x62; glyph[6] = 0xFE; glyph[7] = 0x00;
                break;
            case 'F':
                glyph[0] = 0xFE; glyph[1] = 0x62; glyph[2] = 0x68; glyph[3] = 0x78;
                glyph[4] = 0x68; glyph[5] = 0x60; glyph[6] = 0xF0; glyph[7] = 0x00;
                break;
            case 'G':
                glyph[0] = 0x3C; glyph[1] = 0x66; glyph[2] = 0xC0; glyph[3] = 0xC0;
                glyph[4] = 0xCE; glyph[5] = 0x66; glyph[6] = 0x3E; glyph[7] = 0x00;
                break;
            case 'H':
                glyph[0] = 0xC6; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0xFE;
                glyph[4] = 0xC6; glyph[5] = 0xC6; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'I':
                glyph[0] = 0x3C; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case 'J':
                glyph[0] = 0x1E; glyph[1] = 0x0C; glyph[2] = 0x0C; glyph[3] = 0x0C;
                glyph[4] = 0xCC; glyph[5] = 0xCC; glyph[6] = 0x78; glyph[7] = 0x00;
                break;
            case 'K':
                glyph[0] = 0xE6; glyph[1] = 0x66; glyph[2] = 0x6C; glyph[3] = 0x78;
                glyph[4] = 0x6C; glyph[5] = 0x66; glyph[6] = 0xE6; glyph[7] = 0x00;
                break;
            case 'L':
                glyph[0] = 0xF0; glyph[1] = 0x60; glyph[2] = 0x60; glyph[3] = 0x60;
                glyph[4] = 0x62; glyph[5] = 0x66; glyph[6] = 0xFE; glyph[7] = 0x00;
                break;
            case 'M':
                glyph[0] = 0xC6; glyph[1] = 0xEE; glyph[2] = 0xFE; glyph[3] = 0xFE;
                glyph[4] = 0xD6; glyph[5] = 0xC6; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'N':
                glyph[0] = 0xC6; glyph[1] = 0xE6; glyph[2] = 0xF6; glyph[3] = 0xDE;
                glyph[4] = 0xCE; glyph[5] = 0xC6; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'O':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0xC6; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case 'P':
                glyph[0] = 0xFC; glyph[1] = 0x66; glyph[2] = 0x66; glyph[3] = 0x7C;
                glyph[4] = 0x60; glyph[5] = 0x60; glyph[6] = 0xF0; glyph[7] = 0x00;
                break;
            case 'Q':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0xD6; glyph[5] = 0xDE; glyph[6] = 0x7C; glyph[7] = 0x06;
                break;
            case 'R':
                glyph[0] = 0xFC; glyph[1] = 0x66; glyph[2] = 0x66; glyph[3] = 0x7C;
                glyph[4] = 0x6C; glyph[5] = 0x66; glyph[6] = 0xE6; glyph[7] = 0x00;
                break;
            case 'S':
                glyph[0] = 0x7C; glyph[1] = 0xC6; glyph[2] = 0xC0; glyph[3] = 0x7C;
                glyph[4] = 0x06; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case 'T':
                glyph[0] = 0x7E; glyph[1] = 0x5A; glyph[2] = 0x18; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case 'U':
                glyph[0] = 0xC6; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0xC6; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case 'V':
                glyph[0] = 0xC6; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0x6C; glyph[5] = 0x38; glyph[6] = 0x10; glyph[7] = 0x00;
                break;
            case 'W':
                glyph[0] = 0xC6; glyph[1] = 0xC6; glyph[2] = 0xC6; glyph[3] = 0xD6;
                glyph[4] = 0xFE; glyph[5] = 0xEE; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'X':
                glyph[0] = 0xC6; glyph[1] = 0x6C; glyph[2] = 0x38; glyph[3] = 0x38;
                glyph[4] = 0x6C; glyph[5] = 0xC6; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'Y':
                glyph[0] = 0x66; glyph[1] = 0x66; glyph[2] = 0x66; glyph[3] = 0x3C;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case 'Z':
                glyph[0] = 0xFE; glyph[1] = 0xC6; glyph[2] = 0x8C; glyph[3] = 0x18;
                glyph[4] = 0x32; glyph[5] = 0x66; glyph[6] = 0xFE; glyph[7] = 0x00;
                break;
            case '[':
                glyph[0] = 0x3C; glyph[1] = 0x30; glyph[2] = 0x30; glyph[3] = 0x30;
                glyph[4] = 0x30; glyph[5] = 0x30; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case '\\':
                glyph[0] = 0xC0; glyph[1] = 0x60; glyph[2] = 0x30; glyph[3] = 0x18;
                glyph[4] = 0x0C; glyph[5] = 0x06; glyph[6] = 0x02; glyph[7] = 0x00;
                break;
            case ']':
                glyph[0] = 0x3C; glyph[1] = 0x0C; glyph[2] = 0x0C; glyph[3] = 0x0C;
                glyph[4] = 0x0C; glyph[5] = 0x0C; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case '^':
                glyph[0] = 0x10; glyph[1] = 0x38; glyph[2] = 0x6C; glyph[3] = 0xC6;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case '_':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x00; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0xFE;
                break;
            case '`':
                glyph[0] = 0x30; glyph[1] = 0x18; glyph[2] = 0x00; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            case 'a':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x78; glyph[3] = 0x0C;
                glyph[4] = 0x7C; glyph[5] = 0xCC; glyph[6] = 0x76; glyph[7] = 0x00;
                break;
            case 'b':
                glyph[0] = 0xE0; glyph[1] = 0x60; glyph[2] = 0x7C; glyph[3] = 0x66;
                glyph[4] = 0x66; glyph[5] = 0x66; glyph[6] = 0xDC; glyph[7] = 0x00;
                break;
            case 'c':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x7C; glyph[3] = 0xC6;
                glyph[4] = 0xC0; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case 'd':
                glyph[0] = 0x1C; glyph[1] = 0x0C; glyph[2] = 0x7C; glyph[3] = 0xCC;
                glyph[4] = 0xCC; glyph[5] = 0xCC; glyph[6] = 0x76; glyph[7] = 0x00;
                break;
            case 'e':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x7C; glyph[3] = 0xC6;
                glyph[4] = 0xFE; glyph[5] = 0xC0; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case 'f':
                glyph[0] = 0x1C; glyph[1] = 0x36; glyph[2] = 0x30; glyph[3] = 0x78;
                glyph[4] = 0x30; glyph[5] = 0x30; glyph[6] = 0x78; glyph[7] = 0x00;
                break;
            case 'g':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x76; glyph[3] = 0xCC;
                glyph[4] = 0xCC; glyph[5] = 0x7C; glyph[6] = 0x0C; glyph[7] = 0xF8;
                break;
            case 'h':
                glyph[0] = 0xE0; glyph[1] = 0x60; glyph[2] = 0x6C; glyph[3] = 0x76;
                glyph[4] = 0x66; glyph[5] = 0x66; glyph[6] = 0xE6; glyph[7] = 0x00;
                break;
            case 'i':
                glyph[0] = 0x18; glyph[1] = 0x00; glyph[2] = 0x38; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case 'j':
                glyph[0] = 0x06; glyph[1] = 0x00; glyph[2] = 0x06; glyph[3] = 0x06;
                glyph[4] = 0x06; glyph[5] = 0x66; glyph[6] = 0x66; glyph[7] = 0x3C;
                break;
            case 'k':
                glyph[0] = 0xE0; glyph[1] = 0x60; glyph[2] = 0x66; glyph[3] = 0x6C;
                glyph[4] = 0x78; glyph[5] = 0x6C; glyph[6] = 0xE6; glyph[7] = 0x00;
                break;
            case 'l':
                glyph[0] = 0x38; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x3C; glyph[7] = 0x00;
                break;
            case 'm':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xEC; glyph[3] = 0xFE;
                glyph[4] = 0xD6; glyph[5] = 0xD6; glyph[6] = 0xD6; glyph[7] = 0x00;
                break;
            case 'n':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xDC; glyph[3] = 0x66;
                glyph[4] = 0x66; glyph[5] = 0x66; glyph[6] = 0x66; glyph[7] = 0x00;
                break;
            case 'o':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x7C; glyph[3] = 0xC6;
                glyph[4] = 0xC6; glyph[5] = 0xC6; glyph[6] = 0x7C; glyph[7] = 0x00;
                break;
            case 'p':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xDC; glyph[3] = 0x66;
                glyph[4] = 0x66; glyph[5] = 0x7C; glyph[6] = 0x60; glyph[7] = 0xF0;
                break;
            case 'q':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x76; glyph[3] = 0xCC;
                glyph[4] = 0xCC; glyph[5] = 0x7C; glyph[6] = 0x0C; glyph[7] = 0x1E;
                break;
            case 'r':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xDC; glyph[3] = 0x76;
                glyph[4] = 0x60; glyph[5] = 0x60; glyph[6] = 0xF0; glyph[7] = 0x00;
                break;
            case 's':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0x7E; glyph[3] = 0xC0;
                glyph[4] = 0x7C; glyph[5] = 0x06; glyph[6] = 0xFC; glyph[7] = 0x00;
                break;
            case 't':
                glyph[0] = 0x30; glyph[1] = 0x30; glyph[2] = 0x78; glyph[3] = 0x30;
                glyph[4] = 0x30; glyph[5] = 0x36; glyph[6] = 0x1C; glyph[7] = 0x00;
                break;
            case 'u':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xCC; glyph[3] = 0xCC;
                glyph[4] = 0xCC; glyph[5] = 0xCC; glyph[6] = 0x76; glyph[7] = 0x00;
                break;
            case 'v':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0xC6; glyph[5] = 0x6C; glyph[6] = 0x38; glyph[7] = 0x00;
                break;
            case 'w':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xC6; glyph[3] = 0xD6;
                glyph[4] = 0xD6; glyph[5] = 0xFE; glyph[6] = 0x6C; glyph[7] = 0x00;
                break;
            case 'x':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xC6; glyph[3] = 0x6C;
                glyph[4] = 0x38; glyph[5] = 0x6C; glyph[6] = 0xC6; glyph[7] = 0x00;
                break;
            case 'y':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xC6; glyph[3] = 0xC6;
                glyph[4] = 0xCE; glyph[5] = 0x76; glyph[6] = 0x06; glyph[7] = 0x7C;
                break;
            case 'z':
                glyph[0] = 0x00; glyph[1] = 0x00; glyph[2] = 0xFC; glyph[3] = 0x98;
                glyph[4] = 0x30; glyph[5] = 0x64; glyph[6] = 0xFC; glyph[7] = 0x00;
                break;
            case '{':
                glyph[0] = 0x0E; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x70;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x0E; glyph[7] = 0x00;
                break;
            case '|':
                glyph[0] = 0x18; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x18;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x18; glyph[7] = 0x18;
                break;
            case '}':
                glyph[0] = 0x70; glyph[1] = 0x18; glyph[2] = 0x18; glyph[3] = 0x0E;
                glyph[4] = 0x18; glyph[5] = 0x18; glyph[6] = 0x70; glyph[7] = 0x00;
                break;
            case '~':
                glyph[0] = 0x76; glyph[1] = 0xDC; glyph[2] = 0x00; glyph[3] = 0x00;
                glyph[4] = 0x00; glyph[5] = 0x00; glyph[6] = 0x00; glyph[7] = 0x00;
                break;
            default:
                glyph[0] = 0x7E; glyph[1] = 0x42; glyph[2] = 0x42; glyph[3] = 0x42;
                glyph[4] = 0x42; glyph[5] = 0x42; glyph[6] = 0x7E; glyph[7] = 0x00;
                break;
        }

        return glyph;
    }
}
