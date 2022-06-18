using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TilePattern
{

    #region public fields
    public List<TileConnection> ConnectionTypes;
    public TileConnection[] Connections;
    public int Index;
    #endregion

    #region private fields
    GameObject _goTilePrefab;

    #endregion

    #region constructors
    public TilePattern(int index, GameObject goTilePrefab, List<TileConnection> connectionTypes)
    {
        Index = index;
        _goTilePrefab = goTilePrefab;
        ConnectionTypes = connectionTypes;
        GetConnections();
    }
    #endregion


    #region private functions
    public void GetConnections()
    {
        Connections = new TileConnection[6];

        List<GameObject> goConnections = Util.GetChildObjectByLayer(_goTilePrefab.transform, LayerMask.NameToLayer("Connections"));

        foreach (var goConnection in goConnections)
        {
            var connection = ConnectionTypes.First(c => goConnection.CompareTag(c.Name));
            connection.AddTilePatternToConnection(this);
            Vector3 rotation = goConnection.transform.rotation.eulerAngles;
            Vector3 connectionPosition = goConnection.transform.localPosition;

            // negative x axis
            if (connectionPosition.x == -2 && rotation == new Vector3(0, 270, 0))
                Connections[0] = connection;

            // positive x axis
            if (connectionPosition.x == 2 && rotation == new Vector3(0, 90, 0))
                Connections[1] = connection;

            // negative z axis
            if (connectionPosition.z == -2 && rotation == new Vector3(0, 180, 0))
                Connections[4] = connection;

            // positive z axis
            if (connectionPosition.z == 2 && rotation == Vector3.zero)
                Connections[5] = connection;

            // negative y axis
            if (connectionPosition.y == -1.5f)
                Connections[2] = connection;

            // positive y axis
            if (connectionPosition.y == 1.5f)
                Connections[3] = connection;
        }
    }
    #endregion
}
