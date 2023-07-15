using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Area52.Infrastructure.Clef;

[System.Diagnostics.DebuggerDisplay("BufferBuilder: length {length}")]
internal struct BufferBuilder : IDisposable
{
    private byte[] buffer;
    private int length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferBuilder(int initSize)
    {
        this.buffer = ArrayPool<byte>.Shared.Rent(initSize);
        this.length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        this.length = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ref ReadOnlySequence<byte> seq)
    {
        int seqLen = Convert.ToInt32(seq.Length);
        this.EnshureSize(seqLen);
        seq.CopyTo(this.buffer.AsSpan(this.length));
        this.length += seqLen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan()
    {
        return this.buffer.AsSpan(0, this.length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty()
    {
        return (this.length == 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnshureLength(int size)
    {
        if (this.buffer.Length < size)
        {
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(size);
            this.buffer.AsSpan(0, this.length).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(this.buffer, false);
            this.buffer = newBuffer;
        }
    }

    private void EnshureSize(int appendSize)
    {
        if (this.buffer.Length < this.length + appendSize)
        {
            byte[] newBuffer = ArrayPool<byte>.Shared.Rent(this.length + appendSize + 256); // TODO pozriet ako je to v area 52
            this.buffer.AsSpan(0, this.length).CopyTo(newBuffer);
            ArrayPool<byte>.Shared.Return(this.buffer, false);
            this.buffer = newBuffer;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(this.buffer, false);
    }
}
