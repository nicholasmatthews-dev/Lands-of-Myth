using System;
using System.Collections.Generic;
using Godot;

namespace LOM.Files;

public class CompressedFile : BaseFile {

    public static FileAccess.CompressionMode compressionMode = FileAccess.CompressionMode.GZip;

    public CompressedFile(string path, ReadWriteMode mode){
        this.path = path;
        readWriteMode = mode;
        file = FileAccess.OpenCompressed(path, godotFileAccess[readWriteMode], compressionMode);
    }
}