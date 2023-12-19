namespace LOM.Files;

public interface IFile {
    /// <summary>
    /// Closes the file and disposes of its associated resources.
    /// </summary>
    public void Close();
}