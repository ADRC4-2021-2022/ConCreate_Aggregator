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
    public readonly GameObject GOTilePrefab;
    #endregion

    #region constructors
    public TilePattern(int index, GameObject goTilePrefab, List<TileConnection> connectionTypes)
    {
        Index = index;
        GOTilePrefab = goTilePrefab;
        ConnectionTypes = connectionTypes;
        GetConnections();
    }
    #endregion

    #region public functions
    /// <summary>
    /// Check if the provided faceIndex's connection list has the provided connectionType in it
    /// </summary>
    /// <param name="faceIndex">Face of the TilePattern to check for the connection</param>
    /// <param name="connectionType">ConnectionType to check for</param>
    /// <returns></returns>
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

        List<GameObject> goConnections = Util.GetChildObjectsByLayer(GOTilePrefab.transform, LayerMask.NameToLayer("Connections"));

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
            else if (connectionPosition.y == -1.55f)
                Connections[2].Add(connection);

            // positive y axis
            else if (connectionPosition.y == 1.55f)
                Connections[3].Add(connection);
        }
    }
    #endregion
}
