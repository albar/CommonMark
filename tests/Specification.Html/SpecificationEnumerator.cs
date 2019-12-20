using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bunnypro.CommonMark.Specification.Html.Test
{
    public class SpecificationEnumerator : IEnumerator<string>, IDisposable
    {
        private static readonly string _filename = @"spec/spec.txt";
        private StreamReader _reader;

        private StringBuilder _markdown;
        private StringBuilder _html;
        private readonly string[] _section = new string[]
        {
            null, // h1
            null, // h2
            null, // h3
            null, // h4
            null, // h5
            null, // h6
        };

        public string Current { get; private set; }
        object IEnumerator.Current => Current;

        public string CurrentLine => Current;
        public int CurrentLineNumber { get; private set; }
        public string[] CurrentSection => _section.ToArray();
        public int CurrentSpecificationNumber { get; private set; }
        public int CurrentSpecificationStartLineNumber { get; private set; }
        public LineState CurrentLineState { get; private set; }
        public bool IsEndOfFile { get; private set; }

        public bool MoveNextSpecification()
        {
            EndCurrentSpecification();

            while (CurrentLineState == LineState.OutsideSpecification && MoveNext())
                continue;

            return !IsEndOfFile;
        }

        public bool TryGetCurrentSpecification(out Specification specification)
        {
            try
            {
                specification = GetCurrentSpecification();
                return true;
            }
            catch
            {
                specification = null;
                return false;
            }
        }

        public Specification GetCurrentSpecification()
        {
            while (MoveNext() && CurrentLineState == LineState.InsideMarkdown)
                continue;

            if (CurrentLineState != LineState.SpecificationSeparator)
                throw new Exception();

            while (MoveNext() && CurrentLineState == LineState.InsideHtml)
                continue;

            if (CurrentLineState != LineState.SpecificationClosing)
                throw new Exception();

            var specification = new Specification
            {
                Section = CurrentSection,
                SpecificationNumber = CurrentSpecificationNumber,
                Markdown = _markdown.ToString(),
                Html = _html.ToString(),
                StartLine = CurrentSpecificationStartLineNumber,
                EndLine = CurrentLineNumber,
            };

            EndCurrentSpecification();

            return specification;
        }

        private void EndCurrentSpecification()
        {
            while (CurrentLineState != LineState.OutsideSpecification &&
                CurrentLineState != LineState.SpecificationClosing && MoveNext())
                continue;

            if (CurrentLineState == LineState.SpecificationClosing)
                MoveNext();
        }

        public bool MoveNextSection()
        {
            var curretSection = CurrentSection;

            while (MoveNext() && curretSection.SequenceEqual(_section))
                continue;

            return !IsEndOfFile;
        }

        public bool MoveNextSection(int level)
        {
            var currentSection = _section.Take(level).ToArray();

            while (MoveNext() && currentSection.SequenceEqual(_section.Take(level).ToArray()))
                continue;

            return !IsEndOfFile;
        }

        public bool MoveNext()
        {
            var line = _reader.ReadLine();

            if (line == null)
            {
                IsEndOfFile = true;
                return false;
            }

            Current = line;
            CurrentLineNumber++;

            if (CurrentLineState == LineState.SpecificationClosing)
                CurrentLineState = LineState.OutsideSpecification;

            if (CurrentLineState == LineState.OutsideSpecification &&
                Regex.Match(CurrentLine, @"(^`{32} example$)|^(#+) +(.+)$") is Match outsideMatch &&
                outsideMatch.Groups.Count > 1)
            {
                if (outsideMatch.Groups[1].Length > 0)
                {
                    CurrentLineState = LineState.SpecificationOpening;
                    CurrentSpecificationNumber++;
                    CurrentSpecificationStartLineNumber = CurrentLineNumber;
                    _markdown = new StringBuilder();
                    _html = new StringBuilder();
                }
                else
                {
                    var level = outsideMatch.Groups[2].Length;
                    _section[level] = outsideMatch.Groups[3].Value;
                    for (var i = level + 1; i < _section.Length; i++)
                    {
                        _section[i] = null;
                    }
                }
            }
            else if (Regex.Match(CurrentLine, @"(^.$)|(^`{32}$)") is Match insideMatch && insideMatch.Groups.Count > 1)
            {
                if (insideMatch.Groups[1].Length > 0)
                    CurrentLineState = LineState.SpecificationSeparator;
                else
                    CurrentLineState = LineState.SpecificationClosing;
            }
            else if (CurrentLineState == LineState.SpecificationOpening || CurrentLineState == LineState.InsideMarkdown)
            {
                _markdown.AppendLine(CurrentLine.Replace('→', '\t'));
                CurrentLineState = LineState.InsideMarkdown;
            }
            else if (CurrentLineState == LineState.SpecificationSeparator || CurrentLineState == LineState.InsideHtml)
            {
                _html.AppendLine(CurrentLine);
                CurrentLineState = LineState.InsideHtml;
            }

            return true;
        }

        public void Reset()
        {
            _reader = new StreamReader(_filename, Encoding.UTF8);
            CurrentLineState = LineState.OutsideSpecification;
            CurrentLineNumber = 0;
            CurrentSpecificationNumber = 0;
        }

        #region Disposable
        public void Dispose()
        {
            if (_reader == null)
                return;

            try
            {
                _reader.Close();
            }
            finally
            {
                _reader.Dispose();
            }
        }
        #endregion

        public enum LineState
        {
            OutsideSpecification,
            SpecificationOpening, // /^`{32} example$/
            InsideMarkdown,
            SpecificationSeparator, // /^.$/
            InsideHtml,
            SpecificationClosing,
        }
    }
}
