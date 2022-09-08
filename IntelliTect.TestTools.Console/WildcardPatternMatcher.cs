// This code was originally sourced from https://github.com/PowerShell/PowerShell/blob/main/src/System.Management.Automation/engine/regex.cs
// and then modified to remove of PowerShell specific elements.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace IntelliTect.TestTools.Console;

internal class WildcardPatternMatcher
{
    private readonly PatternElement[] _patternElements;
    private readonly CharacterNormalizer _characterNormalizer;

    internal WildcardPatternMatcher(WildcardPattern wildcardPattern)
    {
        _characterNormalizer = new CharacterNormalizer(wildcardPattern.Options);
        _patternElements = MyWildcardPatternParser.Parse(
                        wildcardPattern,
                        _characterNormalizer);
    }

    internal bool IsMatch(string str)
    {
        // - each state of NFA is represented by (patternPosition, stringPosition) tuple
        //     - state transitions are documented in
        //       ProcessStringCharacter and ProcessEndOfString methods
        // - the algorithm below tries to see if there is a path
        //   from (0, 0) to (lengthOfPattern, lengthOfString)
        //    - this is a regular graph traversal
        //    - there are O(1) edges per node (at most 2 edges)
        //      so the whole graph traversal takes O(number of nodes in the graph) =
        //      = O(lengthOfPattern * lengthOfString) time
        //    - for efficient remembering which states have already been visited,
        //      the traversal goes methodically from beginning to end of the string
        //      therefore requiring only O(lengthOfPattern) memory for remembering
        //      which states have been already visited
        //  - Wikipedia calls this algorithm the "NFA" algorithm at
        //    http://en.wikipedia.org/wiki/Regular_expression#Implementations_and_running_times

        var patternPositionsForCurrentStringPosition =
                new PatternPositionsVisitor(_patternElements.Length);
        patternPositionsForCurrentStringPosition.Add(0);

        var patternPositionsForNextStringPosition =
                new PatternPositionsVisitor(_patternElements.Length);

        for (int currentStringPosition = 0;
             currentStringPosition < str.Length;
             currentStringPosition++)
        {
            char currentStringCharacter = _characterNormalizer.Normalize(str[currentStringPosition]);
            patternPositionsForCurrentStringPosition.StringPosition = currentStringPosition;
            patternPositionsForNextStringPosition.StringPosition = currentStringPosition + 1;

            while (patternPositionsForCurrentStringPosition.MoveNext(out int patternPosition))
            {
                _patternElements[patternPosition].ProcessStringCharacter(
                    currentStringCharacter,
                    patternPosition,
                    patternPositionsForCurrentStringPosition,
                    patternPositionsForNextStringPosition);
            }

            // swap patternPositionsForCurrentStringPosition
            // with patternPositionsForNextStringPosition
            var tmp = patternPositionsForCurrentStringPosition;
            patternPositionsForCurrentStringPosition = patternPositionsForNextStringPosition;
            patternPositionsForNextStringPosition = tmp;
        }

        while (patternPositionsForCurrentStringPosition.MoveNext(out int patternPosition2))
        {
            _patternElements[patternPosition2].ProcessEndOfString(
                patternPosition2,
                patternPositionsForCurrentStringPosition);
        }

        return patternPositionsForCurrentStringPosition.ReachedEndOfPattern;
    }

    private class PatternPositionsVisitor
    {
        private readonly int _lengthOfPattern;

        private readonly int[] _isPatternPositionVisitedMarker;

        private readonly int[] _patternPositionsForFurtherProcessing;
        private int _patternPositionsForFurtherProcessingCount;

        public PatternPositionsVisitor(int lengthOfPattern)
        {
            if (lengthOfPattern < 0)
            {
                throw new ArgumentException(
                            $"Caller should verify {nameof(lengthOfPattern)} >= 0", nameof(lengthOfPattern));
            }

            _lengthOfPattern = lengthOfPattern;

            _isPatternPositionVisitedMarker = new int[lengthOfPattern + 1];
            for (int i = 0; i < _isPatternPositionVisitedMarker.Length; i++)
            {
                _isPatternPositionVisitedMarker[i] = -1;
            }

            _patternPositionsForFurtherProcessing = new int[lengthOfPattern];
            _patternPositionsForFurtherProcessingCount = 0;
        }

        public int StringPosition { private get; set; }

        public void Add(int patternPosition)
        {
            if (patternPosition < 0)
            {
                throw new ArgumentException(
                        "There should never be more elements in the queue than the length of the pattern", nameof(patternPosition));
            }
            if (patternPosition > _lengthOfPattern)
            {
                throw new ArgumentException(
                        $"Caller should verify {nameof(patternPosition)} <= this.{nameof(_lengthOfPattern)}");
            }

            // is patternPosition already visited?);
            if (_isPatternPositionVisitedMarker[patternPosition] == this.StringPosition)
            {
                return;
            }

            // mark patternPosition as visited
            _isPatternPositionVisitedMarker[patternPosition] = this.StringPosition;

            // add patternPosition to the queue for further processing
            if (patternPosition < _lengthOfPattern)
            {
                _patternPositionsForFurtherProcessing[_patternPositionsForFurtherProcessingCount] = patternPosition;
                _patternPositionsForFurtherProcessingCount++;
                if (_patternPositionsForFurtherProcessingCount > _lengthOfPattern)
                {
                    throw new InvalidOperationException(
                            "There should never be more elements in the queue than the length of the pattern");
                }
            }
        }

        public bool ReachedEndOfPattern
        {
            get
            {
                return _isPatternPositionVisitedMarker[_lengthOfPattern] >= this.StringPosition;
            }
        }

        // non-virtual MoveNext is more performant
        // than implementing IEnumerable / virtual MoveNext
        public bool MoveNext(out int patternPosition)
        {
            if (_patternPositionsForFurtherProcessingCount < 0)
            {
                throw new InvalidOperationException(
                        "There should never be more elements in the queue than the length of the pattern");
            }
            if (_patternPositionsForFurtherProcessingCount == 0)
            {
                patternPosition = -1;
                return false;
            }

            _patternPositionsForFurtherProcessingCount--;
            patternPosition = _patternPositionsForFurtherProcessing[_patternPositionsForFurtherProcessingCount];
            return true;
        }
    }

    private abstract class PatternElement
    {
        public abstract void ProcessStringCharacter(
                        char currentStringCharacter,
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForCurrentStringPosition,
                        PatternPositionsVisitor patternPositionsForNextStringPosition);

        public abstract void ProcessEndOfString(
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForEndOfStringPosition);
    }

    private class QuestionMarkElement : PatternElement
    {
        public override void ProcessStringCharacter(
                        char currentStringCharacter,
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForCurrentStringPosition,
                        PatternPositionsVisitor patternPositionsForNextStringPosition)
        {
            // '?' : (patternPosition, stringPosition) => (patternPosition + 1, stringPosition + 1)
            patternPositionsForNextStringPosition.Add(currentPatternPosition + 1);
        }

        public override void ProcessEndOfString(
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForEndOfStringPosition)
        {
            // '?' : (patternPosition, endOfString) => <no transitions out of this state - cannot move beyond end of string>
        }
    }

    private class LiteralCharacterElement : QuestionMarkElement
    {
        private readonly char _literalCharacter;

        public LiteralCharacterElement(char literalCharacter)
        {
            _literalCharacter = literalCharacter;
        }

        public override void ProcessStringCharacter(
                        char currentStringCharacter,
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForCurrentStringPosition,
                        PatternPositionsVisitor patternPositionsForNextStringPosition)
        {
            if (_literalCharacter == currentStringCharacter)
            {
                base.ProcessStringCharacter(
                        currentStringCharacter,
                        currentPatternPosition,
                        patternPositionsForCurrentStringPosition,
                        patternPositionsForNextStringPosition);
            }
        }
    }

    private class BracketExpressionElement : QuestionMarkElement
    {
        private readonly Regex _Regex;

        public BracketExpressionElement(Regex regex)
        {
            _Regex = regex ?? throw new ArgumentNullException(nameof(regex));
        }

        public override void ProcessStringCharacter(
                        char currentStringCharacter,
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForCurrentStringPosition,
                        PatternPositionsVisitor patternPositionsForNextStringPosition)
        {
            if (_Regex.IsMatch(new string(currentStringCharacter, 1)))
            {
                base.ProcessStringCharacter(currentStringCharacter, currentPatternPosition,
                                            patternPositionsForCurrentStringPosition,
                                            patternPositionsForNextStringPosition);
            }
        }
    }

    private class AsterixElement : PatternElement
    {
        public override void ProcessStringCharacter(
                        char currentStringCharacter,
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForCurrentStringPosition,
                        PatternPositionsVisitor patternPositionsForNextStringPosition)
        {
            // '*' : (patternPosition, stringPosition) => (patternPosition + 1, stringPosition)
            patternPositionsForCurrentStringPosition.Add(currentPatternPosition + 1);

            // '*' : (patternPosition, stringPosition) => (patternPosition, stringPosition + 1)
            patternPositionsForNextStringPosition.Add(currentPatternPosition);
        }

        public override void ProcessEndOfString(
                        int currentPatternPosition,
                        PatternPositionsVisitor patternPositionsForEndOfStringPosition)
        {
            // '*' : (patternPosition, endOfString) => (patternPosition + 1, endOfString)
            patternPositionsForEndOfStringPosition.Add(currentPatternPosition + 1);
        }
    }

    private class MyWildcardPatternParser : WildcardPatternParser
    {
        private readonly List<PatternElement> _patternElements = new List<PatternElement>();
        private CharacterNormalizer _characterNormalizer;
        private RegexOptions _regexOptions;
        private StringBuilder _bracketExpressionBuilder;

        public static PatternElement[] Parse(
                        WildcardPattern pattern,
                        CharacterNormalizer characterNormalizer)
        {
            var parser = new MyWildcardPatternParser
            {
                _characterNormalizer = characterNormalizer,
                _regexOptions = WildcardPatternToRegexParser.TranslateWildcardOptionsIntoRegexOptions(pattern.Options),
            };
            WildcardPatternParser.Parse(pattern, parser);
            return parser._patternElements.ToArray();
        }

        protected override void AppendLiteralCharacter(char c)
        {
            c = _characterNormalizer.Normalize(c);
            _patternElements.Add(new LiteralCharacterElement(c));
        }

        protected override void AppendAsterix()
        {
            _patternElements.Add(new AsterixElement());
        }

        protected override void AppendQuestionMark()
        {
            _patternElements.Add(new QuestionMarkElement());
        }

        protected override void BeginBracketExpression()
        {
            _bracketExpressionBuilder = new StringBuilder();
            _bracketExpressionBuilder.Append('[');
        }

        protected override void AppendLiteralCharacterToBracketExpression(char c)
        {
            WildcardPatternToRegexParser.AppendLiteralCharacterToBracketExpression(
                _bracketExpressionBuilder,
                c);
        }

        protected override void AppendCharacterRangeToBracketExpression(
                        char startOfCharacterRange,
                        char endOfCharacterRange)
        {
            WildcardPatternToRegexParser.AppendCharacterRangeToBracketExpression(
                _bracketExpressionBuilder,
                startOfCharacterRange,
                endOfCharacterRange);
        }

        protected override void EndBracketExpression()
        {
            _bracketExpressionBuilder.Append(']');
            Regex regex = new Regex(_bracketExpressionBuilder.ToString(), _regexOptions);
            _patternElements.Add(new BracketExpressionElement(regex));
        }
    }

    private struct CharacterNormalizer
    {
        private readonly CultureInfo _cultureInfo;
        private readonly bool _caseInsensitive;

        public CharacterNormalizer(WildcardOptions options)
        {
            _caseInsensitive = 0 != (options & WildcardOptions.IgnoreCase);
            if (_caseInsensitive)
            {
                _cultureInfo = 0 != (options & WildcardOptions.CultureInvariant)
                    ? CultureInfo.InvariantCulture
                    : CultureInfo.CurrentCulture;
            }
            else
            {
                // Don't bother saving the culture if we won't use it
                _cultureInfo = null;
            }
        }

        public char Normalize(char x)
        {
            if (_caseInsensitive)
            {
                return _cultureInfo.TextInfo.ToLower(x);
            }

            return x;
        }
    }
}

