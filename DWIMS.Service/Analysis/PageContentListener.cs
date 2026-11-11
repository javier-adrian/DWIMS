using System.Reflection.Metadata.Ecma335;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace DWIMS.Service.Analysis;

public sealed class PageContentListener(double pageWidth, double pageHeight) : IEventListener
{
    private const double MinSegmentLength = 20;

    private readonly List<WordBox> _words = new();
    private readonly List<RuleSegment> _segments = new();
    private List<Glyph> _pending = new();
    
    public IReadOnlyList<WordBox> Words => _words;
    public IReadOnlyList<RuleSegment> Segments => _segments;

    private readonly record struct Glyph(
        char ch,
        double Left,
        double Right,
        double Top,
        double Bottom);
    
    public void EventOccurred(IEventData data, EventType type)
    {
        switch (data)
        {
            case TextRenderInfo text: CollectText(text); break;
            case PathRenderInfo path: CollectSegments(path); break;
        }
    }

    public ICollection<EventType>? GetSupportedEvents() => null;

    private void CollectText(TextRenderInfo text)
    {
        var glyphs = new List<Glyph>();

        foreach (var character in text.GetCharacterRenderInfos())
        {
            var ch = character.GetText();

            if (string.IsNullOrEmpty(ch)) continue;
            
            var baseline = character.GetBaseline();
            var ascent = character.GetAscentLine();
            var descent = character.GetDescentLine();

            var xIndex = 0;
            var yIndex = 1;
            var zIndex = 2;
            
            var left = Math.Min(baseline.GetStartPoint().Get(xIndex),  baseline.GetEndPoint().Get(xIndex));
            var right = Math.Max(baseline.GetStartPoint().Get(xIndex), baseline.GetEndPoint().Get(xIndex));
            var topPdf = Math.Max(ascent.GetStartPoint().Get(yIndex), ascent.GetEndPoint().Get(yIndex));
            var bottomPdf = Math.Min(descent.GetStartPoint().Get(yIndex), descent.GetEndPoint().Get(yIndex));
            
            if (right - left <= 0 || topPdf - bottomPdf <= 0) continue;

            var glyph = new Glyph(ch[0], left, right, pageHeight - topPdf, pageHeight - bottomPdf);
            var glyphHeight = Math.Max(glyph.Bottom - glyph.Top, 1);
            
            if (char.IsWhiteSpace(glyph.ch)) { FlushPending(); continue; }

            if (_pending.Count > 0)
            {
                var last = _pending[^1];

                var verticalJump = Math.Abs(glyph.Top - last.Top) > 0.4 * glyphHeight;
                var backwards = glyph.Left < last.Right - 2;
                var gapTooWide = glyph.Left - last.Right >
                                 Math.Max(1.5 * Math.Max(glyph.Right - glyph.Left, 1),
                                     0.5 * glyphHeight);
                
                var afterColon = last.ch == ':';
                
                if (verticalJump || backwards || gapTooWide || afterColon)
                    FlushPending();
            }
            
            _pending.Add(glyph);
        }
    }

    public void Complete() => FlushPending();

    private void FlushPending()
    {
        if (_pending.Count == 0) return;
        EmitToken(_pending, 0, _pending.Count);
        _pending.Clear();
    }

    private void Tokenize(IReadOnlyList<Glyph> glyphs)
    {
        var tokenStart = -1;

        for (int i = 0; i <= glyphs.Count; i++)
        {
            if (i == glyphs.Count)
            {
                EmitToken(glyphs, tokenStart, i); 
                break;
            }
            
            var glyph = glyphs[i];

            if (char.IsWhiteSpace(glyph.ch))
            {
                EmitToken(glyphs, tokenStart, i); 
                tokenStart = -1;
                continue;
            }
            
            if (tokenStart == -1)
            {
                tokenStart = i;
                continue;
            }
            
            var prev = glyphs[i - 1];
            
            var width = Math.Max(glyph.Right - glyph.Left, 1);
            
            if (glyph.Left - prev.Right > 1.5 * width || prev.ch == ':')
            {
                EmitToken(glyphs, tokenStart, i);
                tokenStart = i;
            }
        }
    }

    private void EmitToken(IReadOnlyList<Glyph> glyphs, int start, int end)
    {
        if (start < 0 || end <= start) return;

        var chunks = new List<(int From, int To, bool Leader)>();
        var from = start;

        for (int i = start; i <= end; i++)
        {
            if (i < end && IsLeader(glyphs[i].ch) == IsLeader(glyphs[from].ch)) continue;
            chunks.Add((from, i, IsLeader(glyphs[from].ch)));
            from = i;
        }

        for (int chunk = 0; chunk < chunks.Count; chunk++)
            if (chunks[chunk].Leader && chunks[chunk].To - chunks[chunk].From < 3)
                chunks[chunk] = (chunks[chunk].From, chunks[chunk].To, false);
        
        var merged = new List<(int From, int To, bool Leader)>();

        foreach (var chunk in chunks)
        {
            if (merged.Count > 0 && merged[^1].Leader == chunk.Leader)
                merged[^1] = (merged[^1].From, chunk.To, chunk.Leader);
            else
                merged.Add(chunk);
        }

        foreach (var chunk in merged)
        {
            var text = new string(Enumerable.Range(chunk.From, chunk.To - chunk.From).Select(i => glyphs[i].ch).ToArray());
            if (text is "" or ":") continue;

            var left = double.MaxValue;
            var right = double.MinValue;
            var top = double.MaxValue;
            var bottom = double.MinValue;

            for (var i = chunk.From; i < chunk.To; i++)
            {
                left = Math.Min(left, glyphs[i].Left);
                right = Math.Max(right, glyphs[i].Right);
                top = Math.Min(top, glyphs[i].Top);
                bottom = Math.Max(bottom, glyphs[i].Bottom);
            }
            
            _words.Add(new WordBox(text, left, top, right - left, bottom - top));
        }
    }

    private static bool IsLeader(char character) =>
        character is '_' or '.' or '·' or '…';

    private void CollectSegments(PathRenderInfo path)
    {
        var ctm = path.GetCtm();

        if (ctm is null) return;

        var p = path.GetPath();

        if (p is null) return;

        foreach (var subpath in p.GetSubpaths())
            foreach (var shape in subpath.GetSegments())
            {
                var pts = shape.GetBasePoints();

                for (int i = 1; i < pts.Count; i++)
                {
                    var a = Transform(ctm, pts[i - 1]);
                    var b = Transform(ctm, pts[i]);
                    
                    var dx = Math.Abs(b.GetX() - a.GetX());
                    var dy = Math.Abs(b.GetY() - a.GetY());

                    if (dx < 0.5 && dy >= MinSegmentLength)
                    {
                        var y1 = pageHeight - a.GetY();
                        var y2 = pageHeight - b.GetY();
                        _segments.Add(new RuleSegment(Math.Min(y1, y2), Math.Max(y1, y2), a.GetX(), false));
                    }
                    else if (dy < 0.5 && dx >= MinSegmentLength)
                    {
                        var y = pageHeight - a.GetY();
                        _segments.Add(new RuleSegment(Math.Min(a.GetX(), b.GetX()), Math.Max(a.GetX(), b.GetX()), y, true));
                    }
                }
            }
    }

    private static Point Transform(Matrix m, Point p) => new(
        m.Get(Matrix.I11) * p.GetX() + m.Get(Matrix.I21) * p.GetY() + m.Get(Matrix.I31),
        m.Get(Matrix.I12) * p.GetX() + m.Get(Matrix.I22) * p.GetY() + m.Get(Matrix.I32));
}