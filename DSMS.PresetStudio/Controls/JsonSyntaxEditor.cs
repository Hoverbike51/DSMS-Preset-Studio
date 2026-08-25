using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace DSMS.PresetStudio.Controls;

/// <summary>
/// Lightweight, dependency-free JSON editor with theme-aware syntax highlighting.
/// </summary>
public sealed class JsonSyntaxEditor : RichTextBox
{
    private readonly DispatcherTimer _highlightTimer;
    private bool _isHighlighting;
    private bool _isSettingText;

    public JsonSyntaxEditor()
    {
        _highlightTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(140)
        };
        _highlightTimer.Tick += (_, _) =>
        {
            _highlightTimer.Stop();
            ApplyHighlighting();
        };

        Document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            PageWidth = 5000
        };
        Document.Blocks.Add(new Paragraph { Margin = new Thickness(0) });
    }

    public bool IsInternalUpdate => _isHighlighting || _isSettingText;

    public string Text
    {
        get => ReadDocumentText();
        set
        {
            var normalized = value ?? string.Empty;
            if (ReadDocumentText() == normalized) return;

            _highlightTimer.Stop();
            _isSettingText = true;
            try
            {
                var range = new TextRange(Document.ContentStart, Document.ContentEnd);
                range.Text = normalized;
                CaretPosition = Document.ContentStart;
            }
            finally
            {
                _isSettingText = false;
            }
            ApplyHighlighting();
        }
    }

    public void RefreshHighlighting()
    {
        _highlightTimer.Stop();
        ApplyHighlighting();
    }

    protected override void OnTextChanged(TextChangedEventArgs e)
    {
        base.OnTextChanged(e);
        if (_isHighlighting || _isSettingText) return;
        _highlightTimer.Stop();
        _highlightTimer.Start();
    }

    private string ReadDocumentText()
    {
        var value = new TextRange(Document.ContentStart, Document.ContentEnd).Text;
        return value.EndsWith("\r\n", StringComparison.Ordinal) ? value[..^2] : value;
    }

    private void ApplyHighlighting()
    {
        if (_isHighlighting) return;

        var source = ReadDocumentText();
        var caretOffset = new TextRange(Document.ContentStart, CaretPosition).Text.Length;
        var selectionStartOffset = new TextRange(Document.ContentStart, Selection.Start).Text.Length;
        var selectionEndOffset = new TextRange(Document.ContentStart, Selection.End).Text.Length;
        var verticalOffset = VerticalOffset;
        var horizontalOffset = HorizontalOffset;

        _isHighlighting = true;
        try
        {
            var paragraph = new Paragraph
            {
                Margin = new Thickness(0),
                FontFamily = FontFamily,
                FontSize = FontSize
            };

            foreach (var token in Tokenize(source))
                paragraph.Inlines.Add(new Run(token.Text) { Foreground = BrushFor(token.Kind) });

            Document.Blocks.Clear();
            Document.Blocks.Add(paragraph);

            var selectionStart = PositionAtCharacterOffset(Math.Min(selectionStartOffset, source.Length));
            var selectionEnd = PositionAtCharacterOffset(Math.Min(selectionEndOffset, source.Length));
            Selection.Select(selectionStart, selectionEnd);
            CaretPosition = PositionAtCharacterOffset(Math.Min(caretOffset, source.Length));
            ScrollToVerticalOffset(verticalOffset);
            ScrollToHorizontalOffset(horizontalOffset);
        }
        finally
        {
            _isHighlighting = false;
        }
    }

    private Brush BrushFor(JsonTokenKind kind)
    {
        var resourceKey = kind switch
        {
            JsonTokenKind.Property => "JsonPropertyBrush",
            JsonTokenKind.String => "JsonStringBrush",
            JsonTokenKind.Number => "JsonNumberBrush",
            JsonTokenKind.Keyword => "JsonKeywordBrush",
            _ => "JsonDefaultBrush"
        };
        return (Brush?)Application.Current.TryFindResource(resourceKey) ?? Foreground;
    }

    private TextPointer PositionAtCharacterOffset(int targetOffset)
    {
        var position = Document.ContentStart;
        var traversed = 0;

        while (position is not null && position.CompareTo(Document.ContentEnd) < 0)
        {
            if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
            {
                var runText = position.GetTextInRun(LogicalDirection.Forward);
                if (traversed + runText.Length >= targetOffset)
                    return position.GetPositionAtOffset(targetOffset - traversed, LogicalDirection.Forward) ?? position;
                traversed += runText.Length;
                position = position.GetPositionAtOffset(runText.Length, LogicalDirection.Forward);
            }
            else
            {
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
        }

        return Document.ContentEnd;
    }

    private static IEnumerable<JsonToken> Tokenize(string source)
    {
        var index = 0;
        while (index < source.Length)
        {
            var start = index;
            var current = source[index];

            if (current == '"')
            {
                index++;
                var escaped = false;
                while (index < source.Length)
                {
                    var character = source[index++];
                    if (escaped) { escaped = false; continue; }
                    if (character == '\\') { escaped = true; continue; }
                    if (character == '"') break;
                }

                var lookAhead = index;
                while (lookAhead < source.Length && char.IsWhiteSpace(source[lookAhead])) lookAhead++;
                var kind = lookAhead < source.Length && source[lookAhead] == ':'
                    ? JsonTokenKind.Property
                    : JsonTokenKind.String;
                yield return new JsonToken(source[start..index], kind);
                continue;
            }

            if (current == '-' || char.IsDigit(current))
            {
                index++;
                while (index < source.Length && (char.IsDigit(source[index]) || source[index] is '.' or 'e' or 'E' or '+' or '-')) index++;
                yield return new JsonToken(source[start..index], JsonTokenKind.Number);
                continue;
            }

            if (char.IsLetter(current))
            {
                index++;
                while (index < source.Length && char.IsLetter(source[index])) index++;
                var word = source[start..index];
                yield return new JsonToken(word, word is "true" or "false" or "null" ? JsonTokenKind.Keyword : JsonTokenKind.Default);
                continue;
            }

            index++;
            while (index < source.Length && source[index] != '"' && source[index] != '-' && !char.IsDigit(source[index]) && !char.IsLetter(source[index])) index++;
            yield return new JsonToken(source[start..index], JsonTokenKind.Default);
        }
    }

    private readonly record struct JsonToken(string Text, JsonTokenKind Kind);

    private enum JsonTokenKind
    {
        Default,
        Property,
        String,
        Number,
        Keyword
    }
}
