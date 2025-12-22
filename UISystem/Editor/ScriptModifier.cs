using System;
using System.Text;

namespace FFramework.Editor
{
    internal static class ScriptModifier
    {
        // 将字段插入到#region {regionName} ... #endregion 中；若不存在则创建该区域并插入到类内首行之后
        public static string InsertFieldIntoRegion(string code, string fieldLine, string regionName = "字段")
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(fieldLine)) return code;

            int regionStart = code.IndexOf("#region " + regionName, StringComparison.Ordinal);
            int regionEnd = regionStart >= 0 ? code.IndexOf("#endregion", regionStart, StringComparison.Ordinal) : -1;

            if (regionStart >= 0 && regionEnd > regionStart)
            {
                // 插入到 #region 与 #endregion 内，靠近开头（避免跑到底部）
                int insertPos = regionStart + ("#region " + regionName).Length;
                // 找到该行行尾
                insertPos = code.IndexOf('\n', insertPos);
                if (insertPos < 0) insertPos = regionEnd;
                var sb = new StringBuilder(code.Length + fieldLine.Length + 4);
                sb.Append(code, 0, insertPos + 1);
                sb.AppendLine(fieldLine);
                sb.Append(code, insertPos + 1, code.Length - (insertPos + 1));
                return sb.ToString();
            }

            // 找到类体开始位置，在第一个 '{' 之后创建区域
            int classStartBrace = code.IndexOf('{');
            if (classStartBrace >= 0)
            {
                int insert = classStartBrace + 1;
                var sb = new StringBuilder(code.Length + fieldLine.Length + 64);
                sb.Append(code, 0, insert);
                sb.AppendLine();
                sb.AppendLine($"        #region {regionName}");
                sb.AppendLine(fieldLine);
                sb.AppendLine("        #endregion");
                sb.Append(code, insert, code.Length - insert);
                return sb.ToString();
            }

            // 兜底：追加到文件末尾
            return code + Environment.NewLine + fieldLine + Environment.NewLine;
        }

        // 确保存在 using 指令
        public static string EnsureUsing(string code, string ns)
        {
            if (string.IsNullOrEmpty(ns)) return code;
            string usingLine = $"using {ns};";
            if (code.IndexOf(usingLine, StringComparison.Ordinal) >= 0) return code;

            // 插入到现有using列表末尾或文件开头
            int firstNamespaceIdx = code.IndexOf("namespace ", StringComparison.Ordinal);
            int insertPos = 0;
            if (firstNamespaceIdx > 0)
            {
                // 在第一个namespace之前插入
                insertPos = firstNamespaceIdx;
            }
            else
            {
                insertPos = 0;
            }

            var sb = new StringBuilder(code.Length + usingLine.Length + 2);
            sb.Append(code, 0, insertPos);
            sb.AppendLine(usingLine);
            sb.Append(code, insertPos, code.Length - insertPos);
            return sb.ToString();
        }
    }
}