using System.Security.Cryptography;

namespace TorrentCs.Data;

public class PieceCalculator : IPieceCalculator
{
    public void ComputePieces(List<ContainedFile> files, int pieceSize, IFileHandler fileHandler, List<Piece> pieces)
    {
        long totalSize = files.Sum(f => f.Size);
        long position = 0;
        int index = 0;

        while (position < totalSize)
        {
            int currentPieceSize = (int)Math.Min(pieceSize, totalSize - position);
            var data = ReadBlockData(files, fileHandler, position, currentPieceSize);
            var hash = new Sha1Hash(SHA1.HashData(data));
            pieces.Add(new Piece(index, currentPieceSize, hash));
            position += currentPieceSize;
            index++;
        }
    }

    private static byte[] ReadBlockData(List<ContainedFile> files, IFileHandler fileHandler, long offset, int length)
    {
        var result = new byte[length];
        long position = 0;
        int written = 0;

        foreach (var file in files)
        {
            long fileEnd = position + file.Size;

            if (written < length && offset < fileEnd && offset + length > position)
            {
                long fileOffset = Math.Max(0, offset - position);
                int resultOffset = (int)Math.Max(0, position - offset);
                int count = (int)Math.Min(file.Size - fileOffset, length - resultOffset);

                var stream = fileHandler.GetFileStream(file.Name);
                stream.Seek(fileOffset, SeekOrigin.Begin);
                ReadExactly(stream, result, resultOffset, count);
                written += count;
            }

            position += file.Size;
            if (written >= length) break;
        }

        return result;
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        while (count > 0)
        {
            int read = stream.Read(buffer, offset, count);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
            count -= read;
        }
    }
}
