using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Add all connection types here
public class TileConnection
{
    #region public fields
    public string Name;
    public List<TilePattern> ConnectingTiles;
    #endregion

    #region constructors
    public TileConnection(string name)
    {
        Name = name;
    }
    #endregion

    #region public functions
    /// <summary>
    /// Add a pattern to the possible connecting tile list
    /// </summary>
    /// <param name="pattern">The pattern</param>
    public void AddTilePatternToConnection(TilePattern pattern)
    {
        if (ConnectingTiles == null) ConnectingTiles = new List<TilePattern>();
        ConnectingTiles.Add(pattern);
    }
    #endregion
}
