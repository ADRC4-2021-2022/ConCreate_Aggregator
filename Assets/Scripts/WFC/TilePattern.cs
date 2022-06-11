using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PatternType { mat_ConPink, mat_ConYellow, mat_ConBlue, mat_ConOrange, mat_ConCyan, mat_ConGreen }
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
            if (connectionPosition.y != 0)
            {
                //we know it is a topBottom connection
                if (connectionPosition.y == 1.5f)
                {
                    Connections[2] = connection; //positive y axis (debug once working to ensure that this is correct) 
                }
                else
                {
                    Connections[3] = connection; //negative y axis (debug once working to ensure that this is correct)
                }
            }

            //Connections[(int)rotation.y % 90] = connection;
            else if (rotation.y == 90)
            {
                Connections[1] = connection; //positive x axis   
            }
            else if (rotation.y == 180)
            {
                Connections[4] = connection; //negative z axis 
            }
            else if (rotation.y == 270)
            {
                Connections[0] = connection; //negative x axis  
            }
            else
            {
                Connections[5] = connection; //positive z axis  
            }
        }
    }
    #endregion

}
