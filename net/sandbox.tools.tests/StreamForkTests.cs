using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using sandbox.common;

namespace sandbox.tools.tests
{
    public class StreamForkTests
    {
        Random _rand = new Random();

        [Theory]
        [InlineData(512)]
        [InlineData(1024 - 1)]
        [InlineData(1024)]
        [InlineData(1024 + 1)]
        [InlineData(1024 * 1024)]
        [InlineData(1024 * 1024 + 1)]
        [InlineData(16 * 1024 * 1024)]
        public async Task ForkMemStream_UnlimitBuffer(int cbyte)
        {
            var buff = new byte[cbyte];

            _rand.NextBytes(buff);

            using (var stream = new MemoryStream(buff, writable: false))
            {
                Stream[] feeds = stream.Fork(2);

                var tasks = new Task[] { };

                await Task.WhenAll(VerifyStreamDataAsync(feeds[0], buff), VerifyStreamDataAsync(feeds[1], buff));
            }
        }

        [Theory]
        [InlineData(1024 * 1024)]
        [InlineData(16 * 1024 * 1024)]
        [InlineData(100 * 1024 * 1024)]
        public async Task ForkMemStream_LimitBuffer(int cbyte)
        {
            var buff = new byte[cbyte];

            _rand.NextBytes(buff);

            using (var stream = new MemoryStream(buff, writable: false))
            {
                Stream[] feeds = stream.Fork(2, 1024 * 1024);

                var tasks = new Task[] { };

                await Task.WhenAll(VerifyLargeStreamDataAsync(feeds[0], buff), VerifyLargeStreamDataAsync(feeds[1], buff));
            }
        }

        public async Task VerifyLargeStreamDataAsync(Stream stream, byte[] data)
        {
            var buff = new byte[1024];
            var bytesRead = 0;
            var index = 0;

            while((bytesRead = await stream.ReadAsync(buff, 0, buff.Length)) != 0)
            {
                for(int i = 0; i < buff.Length && index < data.Length; i++)
                {
                    Assert.Equal(data[index], buff[i]);

                    index++;
                }
            }
        }

        public async Task VerifyStreamDataAsync(Stream stream, byte[] data)
        {
            var buff = new byte[data.Length];

            var bytesRead = await stream.ReadAsync(buff, 0, data.Length);

            Assert.Equal(data.Length, bytesRead);

            for(int i = 0; i< data.Length; i++)
            {
                Assert.Equal(data[i], buff[i]);
            }

            //verify there is no more data in the stream
            Assert.Equal(0, await stream.ReadAsync(buff, 0, data.Length));
        }
    }
}
