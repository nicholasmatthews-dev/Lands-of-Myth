using Godot;
using System;

public class World {
    private static string rootFolder = "user://Worlds/";
    public readonly string Name = "New World";

    public World(string worldName){
        Name = worldName;
    }

    /// <summary>
    /// Returns the full path to this World's save directory.
    /// <para>
    /// NOTE: The file path is returned without a trailing "/" character at the end of the
    /// path.
    /// </para>
    /// </summary>
    /// <returns>A string representing the path as described above.</returns>
    public string GetSavePath(){
        return rootFolder + Name;
    }
}