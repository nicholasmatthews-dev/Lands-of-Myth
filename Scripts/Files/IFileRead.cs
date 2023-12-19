namespace LOM.Files;

public interface IFileRead : IFile {
    /// <summary>
    /// Reads the next 64 bits as an unsigned long.
    /// </summary>
    /// <returns>An unsigned long.</returns>
    public ulong ReadInt64();
    /// <summary>
    /// Reads the next <c>bufferLength</c> bytes as an array.
    /// </summary>
    /// <param name="bufferLength">The length of the buffer (in bytes)</param>
    /// <returns>A byte array of the specified length.</returns>
    public byte[] ReadBuffer(int bufferLength);
}