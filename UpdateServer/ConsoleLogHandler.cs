using System.Text;

namespace UpdateServer;

public class ConsoleLogHandler : IDisposable {
    private readonly TextWriter _mOldOut;
    private FileStream? _mFileStream;
    private StreamWriter? _mFileWriter;

    public ConsoleLogHandler(string path) {
        TextWriter mDoubleWriter;
        _mOldOut = Console.Out;

        try {
            _mFileStream = File.Create(path);

            _mFileWriter = new StreamWriter(_mFileStream) {
                AutoFlush = true
            };

            mDoubleWriter = new DoubleWriter(_mFileWriter, _mOldOut);
        } catch (Exception e) {
            Console.WriteLine("Cannot open file for writing");
            Console.WriteLine(e.Message);
            return;
        }

        Console.SetOut(mDoubleWriter);
    }

    public void Dispose() {
        Console.SetOut(_mOldOut);

        if (_mFileWriter != null) {
            _mFileWriter.Flush();
            _mFileWriter.Close();
            _mFileWriter = null;
        }

        if (_mFileStream != null) {
            _mFileStream.Close();
            _mFileStream = null;
        }
    }

    private class DoubleWriter : TextWriter {
        private readonly TextWriter? _mOne;
        private readonly TextWriter _mTwo;

        public DoubleWriter(TextWriter? one, TextWriter two) {
            _mOne = one;
            _mTwo = two;
        }

        public override Encoding Encoding => _mOne?.Encoding ?? Encoding.UTF8;

        public override void Flush() {
            _mOne?.Flush();
            _mTwo.Flush();
        }

        public override void Write(char value) {
            _mOne?.Write(value);
            _mTwo.Write(value);
        }
    }
}