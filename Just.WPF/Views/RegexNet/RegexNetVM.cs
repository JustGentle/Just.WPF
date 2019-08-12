using PropertyChanged;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Just.WPF.Views.RegexNet
{
    [AddINotifyPropertyChangedInterface]
    public class RegexNetVM
    {
        protected int _matchStartAt = 0;
        protected Regex Reg { get; set; }

        #region 属性
        public RegexOptions Options { get; set; } = RegexOptions.None;
        private string _pattern;
        public string Pattern
        {
            get
            {
                return _pattern;
            }
            set
            {
                _pattern = value;
                _matchStartAt = 0;
            }
        }
        private string _input;
        public string Input
        {
            get
            {
                return _input;
            }
            set
            {
                _input = value;
                _matchStartAt = 0;
            }
        }
        public string Repalcement { get; set; }
        public string Result { get; set; }
        public ObservableCollection<MatchItem> MatchData { get; set; } = new ObservableCollection<MatchItem>();
        #endregion

        #region 方法
        public void Reset()
        {
            Result = string.Empty;
            MatchData.Clear();
            _matchStartAt = 0;
        }
        public Regex Init(RegexOptions options)
        {
            try
            {
                Options = options;
                Result = string.Empty;
                MatchData.Clear();
                Reg = new Regex(Pattern ?? string.Empty, Options);
            }
            catch (Exception ex)
            {
                Logger.Error("正则初始化失败", ex);
                NotifyWin.Warn("初始化失败：" + ex.Message);
            }
            return Reg;
        }
        public bool IsMatch()
        {
            try
            {
                var result = Reg.IsMatch(Input ?? string.Empty);
                Result = result.ToString();
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("正则执行失败", ex);
                NotifyWin.Warn("执行失败：" + ex.Message);
                return false;
            }
        }
        public Match Match()
        {
            try
            {
                var match = Reg.Match(Input ?? string.Empty, _matchStartAt);
                if (match.Success)
                {
                    _matchStartAt = match.Index + match.Length;

                    AddMatchData(match);

                    Result = match.Value;
                }
                else
                    _matchStartAt = 0;

                return match;
            }
            catch (Exception ex)
            {
                Logger.Error("正则执行失败", ex);
                NotifyWin.Warn("执行失败：" + ex.Message);
                return null;
            }
        }

        public MatchCollection Matches()
        {
            try
            {
                var matches = Reg.Matches(Input ?? string.Empty);

                for (int n = 0; n < matches.Count; n++)
                {
                    var match = matches[n];
                    AddMatchData(match);
                }

                Result = string.Join(Environment.NewLine, matches.Cast<Match>().Select(m => m.Value));

                return matches;
            }
            catch (Exception ex)
            {
                Logger.Error("正则执行失败", ex);
                NotifyWin.Warn("执行失败：" + ex.Message);
                return null;
            }
        }

        public string Replace()
        {
            try
            {
                Matches();
                Result = Reg.Replace(Input ?? string.Empty, Repalcement ?? string.Empty);
                return Result;
            }
            catch (Exception ex)
            {
                Logger.Error("正则执行失败", ex);
                NotifyWin.Warn("执行失败：" + ex.Message);
                return null;
            }
        }

        public string[] Split()
        {
            try
            {
                Matches();
                var result = Reg.Split(Input ?? string.Empty);
                Result = string.Join(Environment.NewLine, result);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("正则执行失败", ex);
                NotifyWin.Warn("执行失败：" + ex.Message);
                return null;
            }
        }

        private void AddMatchData(Match match)
        {
            var matchItem = new MatchItem
            {
                Index = MatchData.Count,
                Value = match.Value
            };

            for (int i = 0; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                var groupName = Reg.GroupNameFromNumber(i);
                matchItem.Groups.Add(new GroupItem
                {
                    Index = i,
                    Position = group.Index,
                    Value = group.Value,
                    GroupName = groupName == i.ToString() ? string.Empty : groupName
                });
            }

            MatchData.Add(matchItem);
        }
        #endregion
    }

    public class MatchItem
    {
        public int Index { get; set; }
        public string Value { get; set; }
        public ObservableCollection<GroupItem> Groups { get; set; } = new ObservableCollection<GroupItem>();
    }
    public class GroupItem
    {
        public int Index { get; set; }
        public int Position { get; set; }
        public string GroupName { get; set; }
        public string Value { get; set; }
    }
}
