using System.Collections.Generic;

//Add all connection types here
public enum ConnectionType { con0 = 0, conYellow = 1, conBlue = 2, conGreen = 3, conTopBottom = 4 }
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
