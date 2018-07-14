//#define ASYNC
//#define ASYNC_IO

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace StringsAreEvil.Internal
{
#if NETCOREAPP2_1
    // Simple and incomplete implementation of a pipe reader over a file
    internal class FilePipeReader2 : PipeReader
    {
        private readonly FileStream _stream;
        private int _unconsumedBytes;

        private readonly byte[] _buffer;
        private ReadOnlySequence<byte> _currentSequence;

        public FilePipeReader2(string path, int bufferSize = 4096)
        {
            Console.WriteLine($"Buffersize: {bufferSize}");
#if ASYNC_IO
            const bool useAsync = true;
#else
            const bool useAsync = false;
#endif
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, useAsync);
            _buffer = new byte[bufferSize];
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            ReadOnlySequence<byte> unconsumedBuffer = _currentSequence.Slice(consumed);
            ReadOnlySequence<byte> examinedBuffer = _currentSequence.Slice(examined);

            if (examinedBuffer.Length == 0)
            {
                // If we didn't consume everything, copy to the front of the buffer
                if (unconsumedBuffer.Length > 0)
                {
                    _unconsumedBytes = (int)unconsumedBuffer.Length;

                    unconsumedBuffer.CopyTo(_buffer);
                }
            }
            else
            {
                // We didn't examine everything so don't yield the awaiter
                _currentSequence = unconsumedBuffer;
            }
        }

        public override void CancelPendingRead()
        {
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            _stream.Dispose();
        }

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
#if ASYNC
            ValueTask<int> readTask = _stream.ReadAsync(_buffer.AsMemory(_unconsumedBytes, _buffer.Length - _unconsumedBytes));

            return readTask.IsCompleted
                ? new ValueTask<ReadResult>(CreateReadResult(readTask.Result))
                : Async(readTask);

            ReadResult CreateReadResult(int read)
            {
                _currentSequence = new ReadOnlySequence<byte>(_buffer, 0, _unconsumedBytes + read);
                return new ReadResult(_currentSequence, isCanceled: false, isCompleted: read == 0);
            }

            async ValueTask<ReadResult> Async(ValueTask<int> task) => CreateReadResult(await task);
#else
            // Blocking reads, because we're synchronous
            int read = _stream.Read(_buffer, _unconsumedBytes, _buffer.Length - _unconsumedBytes);

            _currentSequence = new ReadOnlySequence<byte>(_buffer, 0, _unconsumedBytes + read);

            var result = new ReadResult(_currentSequence, isCanceled: false, isCompleted: read == 0);

            return new ValueTask<ReadResult>(result);
#endif
        }

        public override bool TryRead(out ReadResult result)
        {
            throw new NotImplementedException();
        }
    }
#endif
}
