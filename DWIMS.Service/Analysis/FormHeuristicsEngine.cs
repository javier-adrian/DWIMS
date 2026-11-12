using System.Text.RegularExpressions;
using DWIMS.Data;
using iText.Layout.Properties;
using MimeKit.Text;

namespace DWIMS.Service.Analysis;

public sealed class FormHeuristicsEngine : IFormHeuristicsEngine
{
    private const double MinBlankWidth = 40;
    private const double BlankPerLabelHeight = 6;
    private const double CellInset = 3;
    private const double MinBelowHeight = 36;
    private const double RightMargin = 18;
    private const double MinSignatureSlotWidth = 120;
    private const double PageWidthPenaltyFraction = 0.8;
    private const double ClusterTolerance = 2;
    private const int LeaderMinRun = 3;
    private const int MaxLabelChars = 60;
    
    private static readonly Regex SignaturePattern = new(
        "signature|signed|approved by|noted by|received by|conforme|certified by", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex RoleTitlePattern = new(
        @"\b(signed|signature|adviser|advisor|president|director|dean|registrar|chairman|chairperson|coordinator|treasurer|secretary|auditor|lead)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex DatePattern = new(
        @"\bdate\b",  
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex NumberPattern = new(
        @"qty|quantity|amount|total|contact no|phone|mobile",  
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly record struct Rect(double X, double Y, double Width, double Height)
    {
        public Rect Inset(double m) => new(X + m, Y + m, Width - 2 * m, Height - 2 * m);
    }

    private sealed record TextLine(double Top, double Height, IReadOnlyList<WordBox> Words);

    private sealed class Proposal(string title, InputTypes inputType, Rect rect, double confidence, string evidence)
    {
        public string Title { get; } = title;
        public InputTypes InputType { get; } = inputType;
        public Rect Rect { get; } = rect;
        public double Confidence { get; set; } = confidence;
        public string Evidence { get; } = evidence;
    }
    
    private sealed record LabelRun(IReadOnlyList<WordBox> LabelWords, bool EndsWithColon, WordBox? Leader, WordBox? NextWord);
    
    public PageProposals Propose(PageContent page)
    {
        var lines = GroupLines(page.Words);
        var grid = TableGrid.Build(page.Segments, page.Words);
        
        var proposals = new List<Proposal>();
        ProposeFromLines(lines, page, proposals);
        ProposeFromCells(grid, proposals);
        ProposeFromUnderlines(lines, page, proposals);
        
        ApplyPenalties(proposals, page);
        proposals = Deduplicate(proposals);
        proposals.Sort((a,b) => a.Rect.Y != b.Rect.Y ?
            a.Rect.Y.CompareTo(b.Rect.Y) :
            a.Rect.X.CompareTo(b.Rect.X));

        var fields = proposals
            .Select(p => new FieldProposal(p.Title, p.InputType,
                p.Rect.X, p.Rect.Y,
                p.Rect.Width, p.Rect.Height,
                p.Confidence, p.Evidence))
            .ToList();

        var steps = new List<StepProposal>();
        var order = 0;

        foreach (var field in fields.Where(f => f.InputType == InputTypes.Signature))
        {
            var (sx, sw) = WidenSlot(field.X, field.Width, page.Width);
            steps.Add(new StepProposal(field.Title, ++order, sx, field.Y, sw, field.Height));
        }

        return new PageProposals(fields, steps);
    }

    private static List<TextLine> GroupLines(IReadOnlyList<WordBox> words)
    {
        if (words.Count == 0) return new();

        var tolerance = Math.Max(4, 0.6 * Median(words.Select(w => w.Height).ToList()));

        var lines = new List<TextLine>();
        var current = new List<WordBox>();
        double sumCenters = 0;

        foreach (var word in words.OrderBy(w => w.Y + w.Height / 2).ThenBy(w => w.X))
        {
            var center = word.Y + word.Height / 2;

            if (current.Count > 0 && Math.Abs(center - sumCenters / current.Count) > tolerance)
            {
                lines.Add(MakeLine(current));
                current = new();
                sumCenters = 0;
            }
            
            current.Add(word);
            sumCenters += center;
        }
        
        if (current.Count > 0) lines.Add(MakeLine(current));
        
        return lines;
    }

    private static TextLine MakeLine(IReadOnlyList<WordBox> words)
    {
        var ordered = words.OrderBy(w => w.X).ToList();
        var top = ordered.Min(w => w.Y);
        var bottom = ordered.Max(w => w.Y + w.Height);

        return new TextLine(top, bottom - top, ordered);
    }

    private static double Median(IReadOnlyList<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        return sorted.Count % 2 == 1 ?
                sorted[sorted.Count / 2] :
                (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;
    }

    private sealed class Cell(int row, int col, Rect rect)
    {
        public int Row { get; } = row;
        public int Col { get; } = col;
        public Rect Rect { get; } = rect;
        public List<WordBox> Words { get; } = new();
    }

    private sealed class TableGrid
    {
        private readonly List<RuleSegment> _verticals = new();
        private readonly List<RuleSegment> _horizontals = new();
        private readonly List<double> _xs = new();
        private readonly List<double> _ys = new();
        private Cell?[,] _cells = new Cell?[0, 0];

        private TableGrid(IReadOnlyList<RuleSegment> segments)
        {
            _verticals = segments.Where(s => !s.Horizontal).ToList();
            _horizontals = segments.Where(s => s.Horizontal).ToList();
        }

        public static TableGrid Build(IReadOnlyList<RuleSegment> segments, IReadOnlyList<WordBox> words)
        {
            var grid = new TableGrid(segments);
            
            grid._xs.AddRange(Cluster(grid._verticals.Select(s => s.Position)));
            grid._ys.AddRange(Cluster(grid._horizontals.Select(s => s.Position)));

            if (grid._xs.Count < 2 || grid._ys.Count < 2) return grid;
            
            grid._cells = new Cell?[grid._ys.Count - 1, grid._xs.Count - 1];

            for (int r = 0; r + 1 < grid._ys.Count; r++)
                for (int c = 0; c + 1 < grid._xs.Count; c++)
                {
                    var yT = grid._ys[r]; var yB = grid._ys[r + 1];
                    var xL = grid._xs[c]; var xR = grid._xs[c + 1];
                    
                    var supported = 
                        Spans(grid._verticals, xL,  yT, yB) && Spans(grid._verticals, xR, yT, yB) &&
                        Spans(grid._horizontals, yT, xL, xR) &&  Spans(grid._horizontals, yB, xL, xR);

                    if (!supported) continue;
                    
                    grid._cells[r, c] = new Cell(r, c, new Rect(xL, yT, xR - xL, yB - yT));
                }

            foreach (var cell in grid.AllCells())
                foreach (var word in words)
                {
                    var cx = word.X + word.Width / 2;
                    var cy = word.Y + word.Height / 2;
                    if (cx >= cell.Rect.X && cx <= cell.Rect.X + cell.Rect.Width &&
                        cy >= cell.Rect.Y && cy <= cell.Rect.Y + cell.Rect.Height)
                        cell.Words.Add(word);
                }

            return grid;
        }

        public IEnumerable<Cell> AllCells()
        {
            for (int r = 0; r < _cells.GetLength(0); r++)
                for (int c = 0; c < _cells.GetLength(1); c++)
                    if (_cells[r, c] is { } cell) yield return cell;
        }

        public IEnumerable<Cell> OccupiedCells => AllCells().Where(c => c.Words.Count > 0);
        
        public Cell? RightNeighbor(Cell cell) => 
            cell.Col + 1 < _cells.GetLength(1) ? _cells[cell.Row, cell.Col + 1] : null;
        
        public Cell? BelowNeighbor(Cell cell) => 
            cell.Row + 1 < _cells.GetLength(0) ? _cells[cell.Row + 1, cell.Col] : null;
        
        private static bool Spans(IEnumerable<RuleSegment> segments, double position, double from, double to) =>
            segments.Any(s => 
                Math.Abs(s.Position - position) <= ClusterTolerance && 
                s.Start <= from + ClusterTolerance &&
                s.End >= to - ClusterTolerance);

        private static List<double> Cluster(IEnumerable<double> positions)
        {
            var clusters = new List<double>();

            foreach (var position in positions.OrderBy(v => v))
            {
                if (clusters.Count > 0 && position - clusters[^1] <= ClusterTolerance)
                    clusters[^1] = (clusters[^1] + position) / 2;
                else
                    clusters.Add(position);
            }
            
            return clusters;
        }
    }

    private static List<LabelRun> FindLabelRuns(IReadOnlyList<WordBox> words)
    {
        var runs = new List<LabelRun>();
        var i = 0;

        while (i < words.Count)
        {
            if (IsLeaderWord(words[i]))
            {
                i++;
                continue;
            }

            var j = i;

            while (j < words.Count && !EndsColon(words[j]) && !IsLeaderWord(words[j])) 
                j++;

            if (j >= words.Count) break;

            if (IsLeaderWord(words[j]))
            {
                runs.Add(new LabelRun(words.Skip(i).Take(j - i).ToList(), false, words[j],
                    j + 1 < words.Count ? words[j + 1] : null));
                i = j + 1;
                continue;
            }
            
            var leader = j + 1 < words.Count && IsLeaderWord(words[j + 1]) ? words[j + 1] : null;
            var next = leader is null ? j + 1 : j + 2;
            
            runs.Add(new LabelRun(words.Skip(i).Take(j + 1 - i).ToList(), true, leader,
                next < words.Count ? words[next] : null));
            
            i = next;
        }
        
        return runs.Where(r => r.LabelWords.Count <= 8).ToList();
    }
    
    private static bool EndsColon(WordBox word) => word.Text.TrimEnd().EndsWith(':');

    private static bool IsLeaderWord(WordBox word)
    {
        var text = word.Text.Trim();
        return text.Length >= LeaderMinRun && text.All(IsLeaderChar);
    }

    private static bool IsLeaderChar(char c) => 
        // c is '_' or '.' or '·' or '…';
        c is '_';

    private void ProposeFromLines(IReadOnlyList<TextLine> lines, PageContent page, List<Proposal> proposals)
    {
        foreach (var line in lines)
        {
            foreach (var run in FindLabelRuns(line.Words))
            {
                var title = CleanTitle(string.Join(" ", run.LabelWords.Select(w => w.Text)));
                if (title.Length is < 1 or > MaxLabelChars) continue;

                Rect blank;

                var confidence = 0.5;
                var evidence = new List<string>();

                if (run.EndsWithColon)
                {
                    confidence += 0.2;
                    evidence.Add("colon");
                }

                if (run.Leader is not null)
                {
                    blank = new Rect(run.Leader.X, line.Top - 1, run.Leader.Width, line.Height + 2);
                    confidence += 0.15;
                    evidence.Add("underscore leader");
                }
                else
                {
                    var last = run.LabelWords[^1];
                    var gapStart = last.X + last.Width;
                    var gapEnd = run.NextWord?.X ?? page.Width - RightMargin;
                    var minBlank = Math.Max(MinBlankWidth, BlankPerLabelHeight * line.Height);
                    
                    if (gapEnd - gapStart < minBlank) continue;
                    
                    blank = new Rect(gapStart, line.Top - 1, gapEnd - gapStart, line.Height + 2);
                    evidence.Add("same-line gap");
                }

                var (type, vocabHit) = GuessType(title);
                if (vocabHit)
                {
                    confidence += 0.1; 
                    evidence.Add("vocabulary");
                }
                
                proposals.Add(new Proposal(title, type, blank, Math.Clamp(confidence, 0, 1), string.Join(", ", evidence)));
            }
        }
    }

    private void ProposeFromCells(TableGrid grid, List<Proposal> proposals)
    {
        foreach (var cell in grid.OccupiedCells)
        {
            var text = string.Join(" ", cell.Words.Select(w => w.Text));
            var title = CleanTitle(text);
            if (title.Length is < 1 or > MaxLabelChars) continue;
            var colon = text.TrimEnd().EndsWith(':');
            var right = grid.RightNeighbor(cell);
            var below = grid.BelowNeighbor(cell);

            Rect blank;

            var confidence = 0.5;
            var evidence = new List<string> {"empty cell"};
            
            if (cell.Words.All(IsLeaderWord)) continue;

            if (right is { Words.Count: 0 })
            {
                blank = right.Rect.Inset(CellInset);
                confidence += 0.1;
            }
            else if (colon && below is { Words.Count: 0 } && below.Rect.Height >= MinBelowHeight)
            {
                blank = below.Rect.Inset(CellInset);
                evidence.Add("cell below");
            }
            else continue;

            if (colon)
            {
                confidence += 0.2; 
                evidence.Add("colon");
            }
            
            var (type, vocabHit) = GuessType(title);
            if (vocabHit)
            {
                confidence += 0.1;
                evidence.Add("vocabulary");
            }
            
            proposals.Add(new Proposal(title, type, blank, Math.Clamp(confidence, 0, 1), string.Join(", ", evidence)));
        }
    }

    private static (InputTypes Type, bool VocabHit) GuessType(string title)
    {
        if (DatePattern.IsMatch(title)) return  (InputTypes.Date, true);
        if (SignaturePattern.IsMatch(title)) return (InputTypes.Signature, true);
        if (NumberPattern.IsMatch(title)) return (InputTypes.Number, true);
        return (InputTypes.Text, false);
    }

    private static string CleanTitle(string raw) =>
        Regex.Replace(raw, @"\s+", " ").Trim().TrimEnd(':').Trim();

    private static void ApplyPenalties(List<Proposal> proposals, PageContent page)
    {
        foreach (var proposal in proposals)
        {
            if (proposal.Rect.Width > PageWidthPenaltyFraction * page.Width)
                proposal.Confidence -= 0.1;

            var rectArea = proposal.Rect.Width * proposal.Rect.Height;

            foreach (var word in page.Words)
            {
                if (IsLeaderWord(word)) continue;

                if (OverlapArea(proposal.Rect, word.X, word.Y, word.Width, word.Height) > 0.2 * rectArea)
                {
                    proposal.Confidence -= 0.2;
                    break;
                }
            }
            
            proposal.Confidence = Math.Clamp(proposal.Confidence, 0, 1);
        }
    }

    private static List<Proposal> Deduplicate(List<Proposal> proposals)
    {
        var removed = new HashSet<Proposal>();

        for (int i = 0; i < proposals.Count; i++)
        {
            if (removed.Contains(proposals[i])) continue;
            
            for (int j = i + 1; j < proposals.Count; j++)
            {
                if (removed.Contains(proposals[j])) continue;
                
                var a = proposals[i].Rect;
                var b = proposals[j].Rect;
                var overlap = OverlapArea(a, b.X, b.Y, b.Width, b.Height);
                var smaller = Math.Min(a.Width * a.Height, b.Width * b.Height);
                
                if (overlap > 0.5 * smaller)
                    removed.Add(
                        proposals[j].Confidence > proposals[i].Confidence ? 
                        proposals[i] : 
                        proposals[j]);
            }
        }
        
        return proposals.Where(p => !removed.Contains(p)).ToList();
    }

    private static double OverlapArea(Rect a, double bx, double by, double bw, double bh)
    {
        var w = Math.Min(a.X + a.Width, bx + bw) - Math.Max(a.X, bx);
        var h = Math.Min(a.Y + a.Height, by + bh) - Math.Max(a.Y, by);
        return w <= 0 || h <= 0 ? 0 : w * h;
    }

    private static (double X, double Width) WidenSlot(double x, double width, double pageWidth)
    {
        if (width >= MinSignatureSlotWidth) return (x, width);

        var nx = Math.Max(0, x + width / 2 - MinSignatureSlotWidth / 2);
        if (nx + MinSignatureSlotWidth > pageWidth) nx = pageWidth - MinSignatureSlotWidth;
        return (nx, MinSignatureSlotWidth);
    }

    private void ProposeFromUnderlines(IReadOnlyList<TextLine> lines, PageContent page, List<Proposal> proposals)
    {
        var rules = page.Segments
            .Where(s => s.Horizontal)
            .Select(s => (Y: s.Position, Start: s.Start, End: s.End))
            .ToList();

        foreach (var line in lines)
            foreach (var group in SplitIntoGroups(line.Words))
            {
                var text = CleanTitle(string.Join(" ", group.Select(w => w.Text)));

                if (text.Length is < 1 or > MaxLabelChars || group.Count > 8) continue;
                
                // if (!RoleTitlePattern.IsMatch(text)) continue;

                var groupLeft = group[0].X;
                var groupRight = group[^1].X + group[^1].Width;
                var center = (groupLeft + groupRight) / 2;

                // var (type, vocabHit) = GuessType(text);
                // if (!vocabHit) continue;

                // var lineLeft = line.Words[0].X;
                // var lineRight = line.Words[^1].X + line.Words[^1].Width;

                double x = 0, width = 0, underlineY = 0;
                var found = false;
                var evidence = "";

                var above = lines.LastOrDefault(l => l.Top < line.Top - line.Height * 0.2);

                if (above is { } && line.Top - (above.Top + above.Height) < line.Height * 0.8)
                {
                    var leaders = above.Words.Where(IsLeaderWord).ToList();

                    if (leaders.Count > 0 && above.Words.Count == leaders.Count)
                    {
                        foreach (var run in SplitIntoRuns(leaders))
                        {
                            var runLeft = run.Min(w => w.X);
                            var runRight = run.Max(w => w.X + w.Width);

                            if (center < runLeft || center > runRight) continue;
                            
                            x = runLeft;
                            width = runRight - runLeft;
                            underlineY = above.Top;
                            found = true;
                            evidence = "leader line above label";
                            break;
                        }
                        // x = leaders.Min(w  => w.X);
                        // width = leaders.Max(w => w.X + w.Width) - x;
                        // underlineY = above.Top;
                        // found = true;
                        // evidence = "leader line above label";
                    }
                }

                if (!found)
                {
                    var rule = rules.Where(u =>
                            u.Y >= line.Top - line.Height * 1.4 &&
                            u.Y <= line.Top - 2 &&
                            // u.End - u.Start >= Math.Max(MinBlankWidth, lineRight - lineLeft) * 0.6 &&
                            u.End - u.Start >= Math.Max(MinBlankWidth, groupRight - groupLeft) * 0.6 &&
                            // u.Start <= lineRight &&
                            u.Start <= groupRight &&
                            // u.End >= lineLeft)
                            u.End >= groupLeft)
                        .Cast<(double Y, double Start, double End)>()
                        .FirstOrDefault();

                    if (rule is { } u)
                    {
                        x = u.Start;
                        width = u.End - u.Start;
                        underlineY = u.Y;
                        found = true;
                        evidence = "drawn rule above label";
                    }
                }
            
                if (!found) continue;
                
                if (width < MinBlankWidth || underlineY <= 0) continue;
            
                var top = Math.Max(0, underlineY - 2.5 * line.Height);
                var rect = new Rect(x, top, width, underlineY - top + 2);
            
                if (page.Words.Any(w => !IsLeaderWord(w) &&
                                        w.X + w.Width / 2 >= x &&
                                        w.X + w.Width / 2 <= x + width &&
                                        w.Y + w.Height / 2 >= top &&
                                        w.Y + w.Height / 2 <= underlineY))
                    continue;
            
                proposals.Add(new Proposal(
                    text,
                    InputTypes.Signature,
                    rect,
                    Math.Clamp(0.5 + 0.1 + 0.1, 0, 1),
                    $"{evidence}, vocabulary"));
            }
    }

    private static List<List<WordBox>> SplitIntoGroups(IReadOnlyList<WordBox> words)
    {
        var groups = new List<List<WordBox>> { new() { words[0] } };

        for (int i = 1; i < words.Count; i++)
        {
            if (words[i].X - (words[i - 1].X + words[i - 1].Width) >= MinBlankWidth)
                groups.Add(new());
            groups[^1].Add(words[i]);
        }

        return groups;
    }

    private static List<List<WordBox>> SplitIntoRuns(IReadOnlyList<WordBox> leaders)
    {
        var runs = new List<List<WordBox>> { new() { leaders[0] } };

        for (int i = 1; i < leaders.Count; i++)
        {
            if (leaders[i].X - (leaders[i - 1].X + leaders[i - 1].Width) >= MinBlankWidth / 2)
                runs.Add(new());
            runs[^1].Add(leaders[i]);
        }

        return runs;
    }
}