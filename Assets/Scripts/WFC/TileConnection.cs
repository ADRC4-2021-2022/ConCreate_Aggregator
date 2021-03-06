using System.Collections.Generic;

//Add all connection types here
public enum ConnectionType { WFC_connOrange = 0, WFC_connYellow = 1, WFC_connApple = 2, WFC_connBlue = 3, WFC_connPink = 4, WFC_connBlack = 5 }
public class TileConnection
{
    #region public fields
    public ConnectionType Type;
    public string Name;
    public List<TilePattern> ConnectingTiles;
    #endregion

    #region constructors
    public TileConnection(ConnectionType type, string name)
    {
        Type = type;
        Name = name;
    }
    #endregion

    #region public functions
    public void AddTilePatternToConnection(TilePattern pattern)
    {
        if (ConnectingTiles == null) ConnectingTiles = new List<TilePattern>();
        ConnectingTiles.Add(pattern);
    }
    #endregion
}
