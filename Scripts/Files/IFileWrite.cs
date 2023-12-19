namespace LOM.Files;

public interface IFileWrite : IFile {
    /// <summary>
    /// Stores a given unsigned long to the file.
    /// </summary>
    /// <param name="toStore">The ulong to store.</param>
    public void StoreInt64(ulong toStore);
    /// <summary>
    /// Stores a given buffer of bytes to the file.
    /// </summary>
    /// <param name="buffer">The buffer to store.</param>
    public void StoreBuffer(byte[] buffer);
}