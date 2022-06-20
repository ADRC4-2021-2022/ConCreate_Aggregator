using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TilePattern
{

    #region public fields
    public List<TileConnection> ConnectionTypes;
    public List<TileConnection>[] Connections; // array of faces of each tile (6 faces in a cube), each entry is a list of connections for that face
    public int Index;
    #endregion

    #region private fields
    readonly GameObject _goTilePrefab;

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

    #region public functions
    public bool HasFaceWithConnectionType(int faceIndex, ConnectionType connectionType)
    {
        var c = Connections[faceIndex].Find(c => c.Type == connectionType);
        return c != null;
    }
    #endregion

    #region private functions
    private void GetConnections()
    {
        Connections = new List<TileConnection>[6] { new List<TileConnection>(), new List<TileConnection>(), new List<TileConnection>(), new List<TileConnection>(), new List<TileConnection>(), new List<TileConnection>() };

        List<GameObject> goConnections = Util.GetChildObjectByLayer(_goTilePrefab.transform, LayerMask.NameToLayer("Connections"));

        foreach (var goConnection in goConnections)
        {
            var connection = ConnectionTypes.First(c => goConnection.CompareTag(c.Name));
            connection.AddTilePatternToConnection(this);
            Vector3 rotation = goConnection.transform.rotation.eulerAngles;
            Vector3 connectionPosition = goConnection.transform.localPosition;

            // negative x axis
            if (connectionPosition.x == -2 && rotation == new Vector3(0, 270, 0))
                Connections[0].Add(connection);

            // positive x axis
            else if (connectionPosition.x == 2 && rotation == new Vector3(0, 90, 0))
                Connections[1].Add(connection);

            // negative z axis
            else if (connectionPosition.z == -2 && rotation == new Vector3(0, 180, 0))
                Connections[4].Add(connection);

            // positive z axis
            else if (connectionPosition.z == 2 && rotation == Vector3.zero)
                Connections[5].Add(connection);

            // negative y axis
            else if (connectionPosition.y == -1.5f)
                Connections[2].Add(connection);

            // positive y axis
            else if (connectionPosition.y == 1.5f)
                Connections[3].Add(connection);
        }
    }
    #endregion
}
