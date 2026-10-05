using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;

namespace TodoSynchronizer.Core.Helpers
{
    public class HtmlHelper
    {
        // Static data tables
        protected static Dictionary<string, string> _tags;
        protected static HashSet<string> _ignoreTags;
        // Placeholder that survives tag stripping and entity decoding, with the
        // equation's final text held aside until the very end of Convert.
        private const char EquationPlaceholderStart = '\uE000';
        private const char EquationPlaceholderEnd = '\uE001';
        // Inline math delimiter, written via escape so the source stays unambiguous.
        private const string InlineMathDelimiter = "\u0024";
        // Unicode block tables from the BCL: a character is "Chinese" when it falls in
        // the CJK Unified Ideographs block or its Extension A block.
        private static readonly Regex ChineseCharacterRegex = new Regex(
            @"[\p{IsCJKUnifiedIdeographs}\p{IsCJKUnifiedIdeographsExtensionA}]",
            RegexOptions.Compiled);
        // Instance variables
        protected TextBuilder _text;
        // Set while walking the first child of <pre>, whose leading whitespace is dropped.
        private bool _skipPreLeadingWhitespace;
        // Static constructor (one time only)
        static HtmlHelper()
        {
            _tags = new Dictionary<string, string>();
            _tags.Add("address", "\n");
            _tags.Add("blockquote", "\n");
            _tags.Add("div", "\n");
            _tags.Add("dl", "\n");
            _tags.Add("fieldset", "\n");
            _tags.Add("form", "\n");
            _tags.Add("h1", "\n");
            _tags.Add("/h1", "\n");
            _tags.Add("h2", "\n");
            _tags.Add("/h2", "\n");
            _tags.Add("h3", "\n");
            _tags.Add("/h3", "\n");
            _tags.Add("h4", "\n");
            _tags.Add("/h4", "\n");
            _tags.Add("h5", "\n");
            _tags.Add("/h5", "\n");
            _tags.Add("h6", "\n");
            _tags.Add("/h6", "\n");
            _tags.Add("p", "\n");
            _tags.Add("/p", "\n");
            _tags.Add("table", "\n");
            _tags.Add("/table", "\n");
            _tags.Add("ul", "\n");
            _tags.Add("/ul", "\n");
            _tags.Add("ol", "\n");
            _tags.Add("/ol", "\n");
            _tags.Add("/li", "\n");
            _tags.Add("br", "\n");
            _tags.Add("/td", "\t");
            _tags.Add("/tr", "\n");
            _tags.Add("/pre", "\n");
            _ignoreTags = new HashSet<string>();
            _ignoreTags.Add("script");
            _ignoreTags.Add("noscript");
            _ignoreTags.Add("style");
            _ignoreTags.Add("object");
        }
        /// <summary>
        /// Converts the given HTML to plain text and returns the result.
        /// </summary>
        /// <param name="html">HTML to be converted</param>
        /// <returns>Resulting plain text</returns>
        public string Convert(string html)
        {
            if (html == null)
                return "";
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var equationTexts = new List<string>();
            _text = new TextBuilder();
            // Content outside <body> is dropped, as it would be when rendering the page.
            var body = doc.DocumentNode.SelectSingleNode("//body");
            Walk(body ?? doc.DocumentNode, equationTexts);
            var plainText = HttpUtility.HtmlDecode(_text.ToString());
            return ExpandEquationPlaceholders(plainText, equationTexts);
        }
        // Writes the plain text of a node's children in document order. Comments and any
        // other non-text, non-element nodes are dropped.
        private void Walk(HtmlNode parent, List<string> equationTexts)
        {
            foreach (var node in parent.ChildNodes)
            {
                if (node.NodeType == HtmlNodeType.Text)
                {
                    var text = ((HtmlTextNode)node).Text ?? "";
                    if (_skipPreLeadingWhitespace)
                    {
                        text = SkipPreLeadingWhitespace(text);
                        _skipPreLeadingWhitespace = false;
                    }
                    // Outside <pre>, whitespace only separates words.
                    _text.Write(_text.Preformatted ? text : CollapseWhitespace(text));
                }
                else
                {
                    // Only text can be the first thing inside <pre>.
                    _skipPreLeadingWhitespace = false;
                    if (node.NodeType == HtmlNodeType.Element)
                        WalkElement(node, equationTexts);
                }
            }
        }
        // Writes one element: the text of its own tag, of its children and of its closing
        // tag. Elements in _ignoreTags contribute only the text of their tags.
        private void WalkElement(HtmlNode element, List<string> equationTexts)
        {
            if (IsEquationImage(element))
            {
                var latex = GetEquationLatex(element);
                if (!string.IsNullOrEmpty(latex))
                {
                    equationTexts.Add(InlineMathDelimiter + latex + InlineMathDelimiter);
                    _text.Write(EquationPlaceholderStart + (equationTexts.Count - 1).ToString() + EquationPlaceholderEnd);
                    return;
                }
            }
            var name = element.Name.ToLowerInvariant();
            string value;
            if (_tags.TryGetValue(name, out value))
                _text.Write(value);
            if (_ignoreTags.Contains(name))
                return;
            var href = name == "a" ? element.GetAttributeValue("href", null) : null;
            if (href != null)
            {
                // Links are written as [text](url).
                _text.Write("[");
                Walk(element, equationTexts);
                _text.Write("](" + href + ")");
            }
            else
            {
                var preformatted = name == "pre";
                if (preformatted)
                {
                    _text.Preformatted = true;
                    _skipPreLeadingWhitespace = true;
                }
                Walk(element, equationTexts);
                if (preformatted)
                {
                    _skipPreLeadingWhitespace = false;
                    _text.Preformatted = false;
                }
            }
            if (_tags.TryGetValue("/" + name, out value))
                _text.Write(value);
        }
        // Replaces every whitespace character with a space, so that line breaks in the
        // source do not become line breaks in the text outside of <pre>.
        private static string CollapseWhitespace(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (var c in text)
                result.Append(char.IsWhiteSpace(c) ? ' ' : c);
            return result.ToString();
        }
        // Drops the whitespace that follows <pre>, up to and including the first line
        // break, the way a browser ignores the line break after the opening tag.
        private static string SkipPreLeadingWhitespace(string text)
        {
            var pos = 0;
            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
            {
                var c = text[pos++];
                if (c == '\n')
                    break;
            }
            return text.Substring(pos);
        }
        // Restores the LaTeX text captured before tag stripping. The replacement is
        // done last so the LaTeX is never decoded or whitespace-collapsed as HTML.
        // A space is added only where the equation touches a Chinese character, which
        // keeps "推广到$n$个事件" readable without padding full-width punctuation.
        private static string ExpandEquationPlaceholders(string text, List<string> equationTexts)
        {
            if (equationTexts.Count == 0)
                return text;
            var pattern = EquationPlaceholderStart + @"(\d+)" + EquationPlaceholderEnd;
            return Regex.Replace(text, pattern, match =>
            {
                var equation = equationTexts[int.Parse(match.Groups[1].Value)];
                var precedingChar = match.Index > 0 ? text[match.Index - 1] : '\0';
                var followingIndex = match.Index + match.Length;
                var followingChar = followingIndex < text.Length ? text[followingIndex] : '\0';
                return (IsChineseCharacter(precedingChar) ? " " : "")
                    + equation
                    + (IsChineseCharacter(followingChar) ? " " : "");
            });
        }
        private static bool IsChineseCharacter(char c)
        {
            return ChineseCharacterRegex.IsMatch(c.ToString());
        }
        // True when the element is an image holding a Canvas math equation.
        private static bool IsEquationImage(HtmlNode node)
        {
            if (node.Name != "img")
                return false;
            var className = node.GetAttributeValue("class", null);
            return className != null
                && className.Split(new[] { ' ', '\t', '\r', '\n', '\f' }, StringSplitOptions.RemoveEmptyEntries)
                    .Contains("equation_image");
        }
        // Canvas stores the raw LaTeX in data-equation-content.
        private static string GetEquationLatex(HtmlNode node)
        {
            var value = HttpUtility.HtmlDecode(node.GetAttributeValue("data-equation-content", null));
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        /// <summary>
        /// A StringBuilder class that helps eliminate excess whitespace.
        /// </summary>
        protected class TextBuilder
        {
            private StringBuilder _text;
            private StringBuilder _currLine;
            private int _emptyLines;
            private bool _preformatted;
            // Construction
            public TextBuilder()
            {
                _text = new StringBuilder();
                _currLine = new StringBuilder();
                _emptyLines = 0;
                _preformatted = false;
            }
            /// <summary>
            /// Normally, extra whitespace characters are discarded.
            /// If this property is set to true, they are passed
            /// through unchanged.
            /// </summary>
            public bool Preformatted
            {
                get
                {
                    return _preformatted;
                }
                set
                {
                    if (value)
                    {
                        // Clear line buffer if changing to
                        // preformatted mode
                        if (_currLine.Length > 0)
                            FlushCurrLine();
                        _emptyLines = 0;
                    }
                    _preformatted = value;
                }
            }
            /// <summary>
            /// Clears all current text.
            /// </summary>
            public void Clear()
            {
                _text.Length = 0;
                _currLine.Length = 0;
                _emptyLines = 0;
            }
            /// <summary>
            /// Writes the given string to the output buffer.
            /// </summary>
            /// <param name="s"></param>
            public void Write(string s)
            {
                foreach (char c in s)
                    Write(c);
            }
            /// <summary>
            /// Writes the given character to the output buffer.
            /// </summary>
            /// <param name="c">Character to write</param>
            public void Write(char c)
            {
                if (_preformatted)
                {
                    // Write preformatted character
                    _text.Append(c);
                }
                else
                {
                    if (c == '\r')
                    {
                        // Ignore carriage returns. We'll process
                        // '\n' if it comes next
                    }
                    else if (c == '\n')
                    {
                        // Flush current line
                        FlushCurrLine();
                    }
                    else if (Char.IsWhiteSpace(c))
                    {
                        // Write single space character
                        int len = _currLine.Length;
                        if (len == 0 || !Char.IsWhiteSpace(_currLine[len - 1]))
                            _currLine.Append(' ');
                    }
                    else
                    {
                        // Add character to current line
                        _currLine.Append(c);
                    }
                }
            }
            // Appends the current line to output buffer
            protected void FlushCurrLine()
            {
                // Get current line
                string line = _currLine.ToString().Trim();
                // Determine if line contains non-space characters
                string tmp = line.Replace(" ", String.Empty);
                if (tmp.Length == 0)
                {
                    // An empty line
                    _emptyLines++;
                    if (_emptyLines < 2 && _text.Length > 0)
                        _text.AppendLine(line);
                }
                else
                {
                    // A non-empty line
                    _emptyLines = 0;
                    _text.AppendLine(line);
                }
                // Reset current line
                _currLine.Length = 0;
            }
            /// <summary>
            /// Returns the current output as a string.
            /// </summary>
            public override string ToString()
            {
                if (_currLine.Length > 0)
                    FlushCurrLine();
                return _text.ToString();
            }
        }
    }

}
