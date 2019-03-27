using System;
using System.Text;
using System.IO;

namespace DacpacTool
{
    public class NavigableReader : IDisposable
    {
        private StreamReader _sr;
        private Stream _s;
        private int _line = 1;
        private int _column = 1;
        private char _lastRead;

        public NavigableReader(Stream s)
        {
            _s = s;
            _s.Position = 0;
            _sr = new StreamReader(_s);
        }

        public void Dispose()
        {
            _sr.Dispose();
        }

        public char Read()
        {
            var resInt = _sr.Read();
            if (resInt < 0) throw new ApplicationException("EOF Reached");

            var result = (char)resInt;
            if ((result == '\n') || (result == '\r' && _sr.Peek() != '\n'))
            {
                _line++;
                _column = 1;
            }
            else
            {
                _column++;
            }
            _lastRead = result;
            return result;
        }

        public string ReadLine()
        {
            var sb = new StringBuilder();
            var newLine = _line + 1;
            while (_line != newLine)
            {
                sb.Append(Read());
            }
            return sb.ToString();
        }

        public void NavigateTo(int line, int column)
        {
            if (line <= 0 || column <= 0) throw new ApplicationException("Line and Column parameters must be positive.");
            if ((line < _line) || ((line == _line) && (column < _column)))
            {
                _s.Position = 0;
                _sr.Dispose();
                _sr = new StreamReader(_s);
                _line = 1;
                _column = 1;
            }
            while (_line < line) ReadLine();
            while (_column < column) Read();
            if ((_line != line) || (_column != column)) throw new ApplicationException(string.Format("Cannot navigate to ({0},{1})", line, column));
        }

        public string ReadUntil(int line, int column)
        {
            var sb = new StringBuilder();
            if ((line < _line) || ((line == _line) && (column < _column))) throw new ApplicationException("Location entered must be past current location.");
            sb.Append(_lastRead);
            while (_line < line) sb.Append(ReadLine());
            while (_column < column) sb.Append(Read());
            if ((_line != line) || (_column != column)) throw new ApplicationException(string.Format("Cannot navigate to ({0},{1})", line, column));
            return sb.ToString();
        }
    }
}
