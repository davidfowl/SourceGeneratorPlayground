using System.Text;

namespace SourceGenerator
{
    class CodeWriter
    {
        private readonly StringBuilder _codeBuilder = new();
        private int _indent;

        public CodeWriter(StringBuilder stringBuilder)
        {
            _codeBuilder = stringBuilder;
        }

        public void StartBlock()
        {
            WriteLine("{");
            Indent();
        }

        public void EndBlock()
        {
            Unindent();
            WriteLine("}");
        }

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            _indent--;
        }

        public void WriteLineNoIndent(string value)
        {
            _codeBuilder.AppendLine(value);
        }

        public void WriteNoIndent(string value)
        {
            _codeBuilder.Append(value);
        }

        public void Write(string value)
        {
            if (_indent > 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.Append(value);
        }

        public void WriteLine(string value)
        {
            if (_indent > 0)
            {
                _codeBuilder.Append(new string(' ', _indent * 4));
            }
            _codeBuilder.AppendLine(value);
        }

        public void WriteCommentedLine(string value)
        {
            _codeBuilder.Append("// ");
            WriteLine(value);
        }

        public override string ToString()
        {
            return _codeBuilder.ToString();
        }
    }
}
