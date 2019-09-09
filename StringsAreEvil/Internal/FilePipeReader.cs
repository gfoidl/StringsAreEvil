﻿using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace StringsAreEvil.Internal
{
    // Simple and incomplete implementation of a pipe reader over a file
    internal class FilePipeReader : PipeReader
    {
        private readonly FileStream _stream;
        private int _unconsumedBytes;

        private readonly byte[] _buffer;
        private ReadOnlySequence<byte> _currentSequence;

        public FilePipeReader(string path, int bufferSize = 4096)
        {
            _stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize);
            _buffer = new byte[bufferSize];
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            var unconsumedBuffer = _currentSequence.Slice(consumed);
            var examinedBuffer = _currentSequence.Slice(examined);

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

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            // Blocking reads, because we're synchronous
            var read = _stream.Read(_buffer, _unconsumedBytes, _buffer.Length - _unconsumedBytes);

            _currentSequence = new ReadOnlySequence<byte>(_buffer, 0, _unconsumedBytes + read);

            var result = new ReadResult(_currentSequence, isCanceled: false, isCompleted: read == 0);

            return new ValueTask<ReadResult>(result);
        }

        public override bool TryRead(out ReadResult result)
        {
            throw new NotImplementedException();
        }
    }
}
