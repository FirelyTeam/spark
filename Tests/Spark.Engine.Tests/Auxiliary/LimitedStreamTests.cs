/* 
 * Copyright (c) 2016-2018, Firely <info@fire.ly>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using System;
using Xunit;
using Task = System.Threading.Tasks.Task;
using System.IO;
using Spark.Engine.Auxiliary;

namespace Spark.Engine.Tests.Auxiliary;

public class LimitedStreamTests
{
    [Fact]
    public void TestWriteWithinLimit()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 10);

        sut.Write([1, 2, 3, 4, 5], 0, 5);

        byte[] actual = new byte[5];
        innerStream.Seek(0, SeekOrigin.Begin);
        innerStream.Read(actual, 0, 5);

        Assert.Equal(1, actual[0]);
        Assert.Equal(5, actual[4]);
    }

    [Fact]
    public void TestWriteAboveLimit()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Write([1, 2, 3, 4, 5], 0, 5));
    }

    [Fact]
    public void TestWriteWithinThenAboveLimit()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 10);

        sut.Write([1, 2, 3, 4, 5], 0, 5);

        byte[] actual5 = new byte[5];
        innerStream.Seek(0, SeekOrigin.Begin);
        innerStream.Read(actual5, 0, 5);

        Assert.Equal(1, actual5[0]);
        Assert.Equal(5, actual5[4]);

        sut.Write([6, 7, 8, 9, 10], 0, 5);

        byte[] actual10 = new byte[10];
        innerStream.Seek(0, SeekOrigin.Begin);
        innerStream.Read(actual10, 0, 10);

        Assert.Equal(1, actual10[0]);
        Assert.Equal(10, actual10[9]);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Write([11], 0, 1));
    }

    [Fact]
    public void TestWriteWithinLimitWithOffset()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 3);

        sut.Write([1, 2, 3, 4, 5], 2, 3);

        byte[] actual3 = new byte[3];
        innerStream.Seek(0, SeekOrigin.Begin);
        innerStream.Read(actual3, 0, 3);

        Assert.Equal(3, actual3[0]);
        Assert.Equal(5, actual3[2]);
    }

    [Fact]
    public void TestWriteAboveLimitWithByteLengthShorterThanCount()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 3);

        Assert.Throws<ArgumentOutOfRangeException>(() => sut.Write([1, 2, 3, 4, 5], 1, 13));
    }

    [Fact]
    public void TestCopyToWithinLimit()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 5);

        MemoryStream sourceStream = new MemoryStream([1, 2, 3, 4, 5]);

        sourceStream.CopyTo(sut);

        byte[] actual = new byte[5];
        innerStream.Seek(0, SeekOrigin.Begin);
        innerStream.Read(actual, 0, 5);

        Assert.Equal(1, actual[0]);
        Assert.Equal(5, actual[4]);
    }

    [Fact]
    public void TestCopyToAboveLimit()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 3);

        MemoryStream sourceStream = new MemoryStream([1, 2, 3, 4, 5]);

        Assert.Throws<ArgumentOutOfRangeException>(() => sourceStream.CopyTo(sut));
    }

    [Fact]
    public async Task TestCopyToAsyncAboveLimit()
    {
        MemoryStream innerStream = new MemoryStream();
        LimitedStream sut = new LimitedStream(innerStream, 3);

        MemoryStream sourceStream = new MemoryStream([1, 2, 3, 4, 5]);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sourceStream.CopyToAsync(sut, TestContext.Current.CancellationToken));
    }
}
