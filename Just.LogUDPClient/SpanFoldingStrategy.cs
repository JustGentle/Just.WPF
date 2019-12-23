using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Just.LogUDPClient
{
    public class SpanFoldingStrategy
    {
        public HighlightingRuleSet RuleSet { get; set; }
        public SpanFoldingStrategy(HighlightingRuleSet ruleSet)
        {
            RuleSet = ruleSet;
        }
        public void UpdateFoldings(FoldingManager manager, ITextSource document)
        {
            IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document);
            manager.UpdateFoldings(newFoldings, -1);
        }
        public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();
            foreach (var span in RuleSet.Spans)
            {
                var spanStack = new Stack<Match>();
                var end = 0;
                var mStart = span.StartExpression.Match(document.Text, end);
                if (!mStart.Success) continue;
                spanStack.Push(mStart);
                end = mStart.Index + mStart.Length;

                while (end < document.TextLength)
                {
                    var mEnd = span.EndExpression.Match(document.Text, end);
                    if (!mEnd.Success) break;

                    mStart = span.StartExpression.Match(document.Text, end);
                    if (mStart.Success && mStart.Index < mEnd.Index)
                    {
                        spanStack.Push(mStart);
                    }
                    if (spanStack.Count == 0)
                    {
                        break;
                    }
                    var start = spanStack.Pop();
                    end = mEnd.Index + mEnd.Length;
                    if (document.Text.IndexOf('\n', start.Index, end - start.Index) >= 0)
                    {
                        newFoldings.Add(new NewFolding(start.Index, end) { Name = start.Value });
                    }
                }

            }
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}
