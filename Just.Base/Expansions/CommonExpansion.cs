using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Just
{
    public static class CommonExpansion
    {

        /// <summary>
        ///  获取成员元数据的Description特性描述信息
        /// </summary>
        /// <param name="member">成员元数据对象</param>
        /// <param name="inherit">是否搜索成员的继承链以查找描述特性</param>
        /// <returns>返回Description特性描述信息，如不存在则返回成员的名称</returns>
        public static string ToDescription(this MemberInfo member, bool inherit = false)
        {
            DescriptionAttribute attr = member.GetCustomAttribute<DescriptionAttribute>(inherit);
            return attr?.Description;
        }

        /// <summary>
        ///  获取成员元数据的DisplayName特性描述信息
        /// </summary>
        /// <param name="member">成员元数据对象</param>
        /// <param name="inherit">是否搜索成员的继承链以查找描述特性</param>
        /// <returns>返回DisplayName特性描述信息，如不存在则返回成员的名称</returns>
        public static string ToDisplayName(this MemberInfo member, bool inherit = false)
        {
            DisplayNameAttribute attr = member.GetCustomAttribute<DisplayNameAttribute>(inherit);
            return attr?.DisplayName;
        }

        /// <summary>
        /// 获取枚举上标注的<see cref="DescriptionAttribute"/>特性的文字描述
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToDescription(this Enum value)
        {
            Type type = value.GetType();
            MemberInfo member = type.GetMember(value.ToString()).FirstOrDefault();
            return member != null ? member.ToDescription() : value.ToString();
        }


        public static void DispatcherInvoke(this Window win, Action action)
        {
            win.Dispatcher.Invoke(action);
        }
        public static TResult DispatcherInvoke<TResult>(this Window win, Func<TResult> func)
        {
            return win.Dispatcher.Invoke(func);
        }

        public static string[] Split(this string str, params string[] separator)
        {
            return str?.Split(separator, StringSplitOptions.None);
        }

        private const string RegChars = "\\.+*?^$|(){}[]{}";
        public static string ToRegPattern(this string text)
        {
            foreach (var chr in RegChars)
            {
                text = text?.Replace(chr.ToString(), @"\" + chr);
            }
            return text;
        }
        public static string OneLine(this string text)
        {
            return text?.Replace("\n", " ").Replace("\r", " ");
        }
        public static bool Contains(this string text, string value, StringComparison comparisonType)
        {
            return text.IndexOf(value, comparisonType) != -1;
        }

        public static Dictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            dict = dict ?? new Dictionary<TKey, TValue>();
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
            return dict;
        }
        public static Dictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> keyValues)
        {
            foreach (var kv in keyValues)
            {
                dict = dict.AddOrUpdate(kv.Key, kv.Value);
            }
            return dict;
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> list)
        {
            if (list == null)
                return Enumerable.Empty<T>();
            return list;
        }


    }
}
