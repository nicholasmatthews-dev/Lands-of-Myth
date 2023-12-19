using Godot;
using System;
using System.Collections.Generic;

namespace LOM.Files;

public abstract class BaseFile : IFileRead, IFileWrite {
    public enum ReadWriteMode {
        Read = 0,
        Write = 1,
        ReadWrite = 2,
        WriteRead = 3
    }

    protected static Dictionary<ReadWriteMode, FileAccess.ModeFlags> godotFileAccess = new(){
        {ReadWriteMode.Read, FileAccess.ModeFlags.Read},
        {ReadWriteMode.Write, FileAccess.ModeFlags.Write},
        {ReadWriteMode.ReadWrite, FileAccess.ModeFlags.ReadWrite},
        {ReadWriteMode.WriteRead, FileAccess.ModeFlags.WriteRead}
    };

    protected FileAccess file;
    protected string path;
    protected ReadWriteMode readWriteMode;

    public bool IsOpened(){
        return file is not null;
    }

    private bool CanRead(){
        return readWriteMode == ReadWriteMode.Read 
        || readWriteMode == ReadWriteMode.ReadWrite 
        || readWriteMode == ReadWriteMode.WriteRead;
    }

    private bool CanWrite(){
        return readWriteMode == ReadWriteMode.Write
        || readWriteMode == ReadWriteMode.ReadWrite
        || readWriteMode == ReadWriteMode.WriteRead; 
    }

    public ulong ReadInt64()
    {
        if (CanRead()){
            return file.Get64();
        }
        else {
            throw new Exception("");
        }
    }

    public byte[] ReadBuffer(int bufferLength)
    {
        return file.GetBuffer(bufferLength);
    }

    public void StoreInt64(ulong toStore)
    {
        
    }

    public void StoreBuffer(byte[] buffer)
    {
        throw new System.NotImplementedException();
    }

    public void Close()
    {
        file.Close();
    }
}