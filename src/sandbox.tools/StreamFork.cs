using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sandbox.tools
{
    public static class StreamFork
    {
        private const int BUFF_SIZE = 8 * 1024 * 1024;

        public static Stream[] Fork(this Stream stream, int count)
        {
            Stream[] streams = new Stream[count];

            var buff = ForkedStreamBuffer.FromStream(stream, count);

            for(int i = 0; i < count; i++)
            {
                streams[i] = new ForkStream(buff);
            }

            return streams;
        }

        private class ForkedStreamBuffer
        {
            private const int DEFAULT_PAGE_SIZE = 16 * 1024;

            internal Stream _source;
            private byte[] _pageData;
            private long _basePos;
            private int _bytesRead;
            private int _feedCount;
            private int _readCount;
            private object _navLock;
            private SemaphoreSlim _pageReady;
            private ForkedStreamBuffer _nextPage;
            private ForkedStreamBuffer _prevPage;
            
            public static ForkedStreamBuffer FromStream(Stream source, int feedCount, int pageSize = DEFAULT_PAGE_SIZE)
            {
                return new ForkedStreamBuffer(source, feedCount, pageSize, 0);
            }

            public async Task<ForkedStreamBuffer> ReadAsync(ForkStream feed, byte[] buff, int buffOffset, int count)
            {
                //if the count is zero return
                if (count == 0)
                {
                    return this;
                }

                //otherwise make sure the page is fully read from the source
                await _pageReady.WaitAsync();

                //release the semaphore imediately 
                _pageReady.Release();

                //if the source index isn't in the page throw an exception
                if(feed.Position < _basePos || feed.Position > EndOfPage)
                {
                    throw new ArgumentOutOfRangeException("feed.Position");
                }

                //if the buffer offset is outside the bounds of buff throw
                if(buffOffset < 0 || buffOffset > buff.Length - 1)
                {
                    throw new ArgumentOutOfRangeException("buffOffset");
                }

                //if the count is less than zero or larger than the remaining buff
                if(count < 0 || count > buff.Length - buffOffset)
                {
                    throw new ArgumentOutOfRangeException("count");
                }
                var startIndex = Convert.ToInt32(feed.Position - _basePos);

                var cBytes = Math.Min(count, _bytesRead - startIndex);

                //read to the end of the requested section, or the end of the page if the section goes past the page
                Array.ConstrainedCopy(_pageData, startIndex, buff, buffOffset, cBytes);

                //adjust the position of the feed
                feed._position += cBytes;

                //adjust the buffer offset
                buffOffset += cBytes;

                //adjust the count
                count -= cBytes;

                if (feed.Position > EndOfPage)
                {
                    var retPage = this.GetNextPage();

                    if (retPage != null)
                    {
                        retPage = await retPage.ReadAsync(feed, buff, buffOffset, count);
                    }

                    return retPage;
                }

                return this;
            }

            private ForkedStreamBuffer(ForkedStreamBuffer prevPage) :
                this(prevPage._source, prevPage._feedCount, prevPage.PageSize, prevPage._basePos + prevPage.PageSize)
            {
                _prevPage = prevPage;
            }

            private ForkedStreamBuffer(Stream source, int feedCount, int pageSize, long basePos)
            {
                _source = source;

                _pageData = new byte[pageSize];

                _basePos = basePos;

                _bytesRead = 0;
                
                _feedCount = feedCount;

                _readCount = 0;

                _navLock = new object();

                _pageReady = new SemaphoreSlim(0, 1);

                _nextPage = null;

                _prevPage = null;

                Task t = ReadPageDataAsync();
            }

            private async Task ReadPageDataAsync()
            {
                _bytesRead = await _source.ReadAsync(_pageData, 0, _pageData.Length);
                
                _pageReady.Release();
            }

            private void MarkPageRead()
            {
                var currReadCount = Interlocked.Increment(ref _readCount);

                if(currReadCount == _feedCount)
                {
                    var next = GetNextPage();

                    next._prevPage = null;
                }
            }

            private ForkedStreamBuffer GetNextPage()
            {
                if (this.IsLastPage)
                {
                    return null;
                }

                lock (_navLock)
                {
                    if (_nextPage == null)
                    {
                        _nextPage = new ForkedStreamBuffer(this);
                    }
                }

                return _nextPage;
            }

            private bool IsLastPage
            {
                get
                {
                    return _bytesRead < PageSize;
                }
            }

            private int PageSize
            {
                get
                {
                    return _pageData.Length;
                }
            }

            /// <summary>
            /// The logical position at the last byte of the current buffer page
            /// </summary>
            private long EndOfPage
            {
                get
                {
                    return _basePos + _bytesRead - 1;
                }
            }
        }

        private class ForkStream : Stream
        {
            private ForkedStreamBuffer _buffer;
            internal long _position;

            public ForkStream(ForkedStreamBuffer buffer)
            {
                _buffer = buffer;
                _position = 0;
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }

            public override long Length
            {
                get
                {
                    return _buffer._source.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return _position;
                }

                set
                {
                    throw new InvalidOperationException("The current stream cannot seek");
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
            }

            public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default(CancellationToken))
            {
                if (_buffer == null)
                {
                    return 0;
                }

                var startPos = _position;

                _buffer = await _buffer.ReadAsync(this, buffer, offset, count);

                return Convert.ToInt32(_position - startPos);
            }

            public override void Flush()
            {
                throw new InvalidOperationException("The current stream is read only");
            }
            
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException("The current stream cannot seek");
            }

            public override void SetLength(long value)
            {
                throw new InvalidOperationException("The current stream is read only");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException("The current stream is read only");
            }
        }
    }
}
