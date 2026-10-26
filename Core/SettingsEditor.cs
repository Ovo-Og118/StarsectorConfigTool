using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StarsectorConfigTool.Core
{
    /// <summary>
    /// 编辑 starsector-core\data\config\settings.json。
    ///
    /// 这个文件里混有 # 注释、尾随注释和制表符缩进，不能当纯 JSON 解析再序列化，
    /// 否则会把注释和格式全部丢掉。这里只在原文本上精确替换「值」的那一段，
    /// 其他字节（缩进、注释、换行风格、编码）一律原样保留。
    /// </summary>
    public class SettingsEditor
    {
        private readonly string _path;
        private string _text;

        public string Path_ { get { return _path; } }
        public string Text { get { return _text; } }

        public SettingsEditor(string path)
        {
            _path = path;
            _text = File.ReadAllText(path, Encoding.UTF8);
        }

        /// <summary>
        /// 定位 "key": 123 里的数字部分。
        /// 结尾用 [^\r\n]* 吃掉尾随注释（逗号、# 注释），保证替换后这些内容都还在。
        ///
        /// 注意：这里不能写 $ 锚点。文件是 CRLF 换行，而 .NET 的 Multiline 模式下
        /// $ 只匹配 \n 之前，停在 \r 上就会匹配失败。
        /// </summary>
        private static Regex BuildRegex(string key)
        {
            string pattern =
                "^(?<pre>[ \\t]*\"" + Regex.Escape(key) + "\"[ \\t]*:[ \\t]*)" +
                "(?<val>-?[0-9]+(?:\\.[0-9]+)?)" +
                "(?<post>[^\\r\\n]*)";
            return new Regex(pattern, RegexOptions.Multiline);
        }

        /// <summary>读取某个键的数值。键不存在返回 false。</summary>
        public bool TryGet(string key, out double value)
        {
            value = 0;
            Match m = BuildRegex(key).Match(_text);
            if (!m.Success) return false;
            return double.TryParse(
                m.Groups["val"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        /// <summary>
        /// 写入某个键的数值。键不存在、或该键在文件里出现多次时返回 false，
        /// 避免误改到不该改的地方。
        /// </summary>
        public bool TrySet(string key, double value, bool isInt)
        {
            Regex rx = BuildRegex(key);
            MatchCollection all = rx.Matches(_text);
            if (all.Count != 1) return false;

            Match m = all[0];
            Group valGroup = m.Groups["val"];

            string formatted;
            if (isInt)
            {
                long l = (long)Math.Round(value);
                formatted = l.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                // 去掉多余的 0，但保留必要的小数位
                formatted = value.ToString("0.####", CultureInfo.InvariantCulture);
            }

            _text = _text.Substring(0, valGroup.Index)
                  + formatted
                  + _text.Substring(valGroup.Index + valGroup.Length);
            return true;
        }

        /// <summary>把当前文本写回磁盘。保持 UTF-8 无 BOM 与原换行风格。</summary>
        public void Save()
        {
            File.WriteAllText(_path, _text, new UTF8Encoding(false));
        }
    }
}
