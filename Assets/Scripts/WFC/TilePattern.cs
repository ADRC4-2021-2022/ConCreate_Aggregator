using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum PatternType { mat_ConPink, mat_ConYellow, mat_ConBlue, mat_ConOrange, mat_ConCyan, mat_ConGreen }
public class TilePattern
{
    #region public fields
    public List<TileConnection> ConnectionTypes;
    public TileConnection[] Connections;
    public int Index;
    public GameObject GOTilePrefab;

    /*public Vector3Int[] Axes = new Vector3Int[6]
    {
        new Vector3Int(-1,0,0), //-x  
        new Vector3Int(1,0,0),  //x   
        new Vector3Int(0,1,0),  //y      
        new Vector3Int(0,-1,0), //-y      
        new Vector3Int(0,0,-1), //-z       
        new Vector3Int(0,0,1),  //z     
    };

     Dictionary<string, ConnectionType> ConnectionTypes = new Dictionary<string, ConnectionType>
     {
         {"conPink",ConnectionType.conPink },
         {"conYellow",ConnectionType.conYellow }
     };*/
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
    public List<GameObject> GetChildObjectByTag(Transform parent, string tag)
    {
        List<GameObject> taggedChildren = new List<GameObject>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.CompareTag(tag))
            {
                taggedChildren.Add(child.gameObject);
            }
            if (child.childCount > 0)
            {
                GetChildObjectByTag(child, tag);
            }
        }
        return taggedChildren;
    }

    public List<GameObject> GetChildObjectByLayer(Transform parent, int layer)
    {
        List<GameObject> layerChildren = new List<GameObject>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.gameObject.layer == layer)
            {
                layerChildren.Add(child.gameObject);
            }
            if (child.childCount > 0)
            {
                GetChildObjectByLayer(child, layer);
            }
        }
        return layerChildren;
    }
    #endregion

    #region private functions
    private void GetConnections()
    {
        Connections = new TileConnection[6];

        List<GameObject> goConnections = GetChildObjectByLayer(GOTilePrefab.transform, LayerMask.NameToLayer("Connections"));

        foreach (var goConnection in goConnections)
        {
            var connection = ConnectionTypes.First(c => goConnection.CompareTag(c.Name));
            connection.AddTilePatternToConnection(this);
            Vector3 rotation = goConnection.transform.rotation.eulerAngles;
            if (rotation.x != 0)
            {
                //we know it is a vertical connection
                if (rotation.x == 90)
                {
                    Connections[3] = connection;
                }
                else
                {
                    Connections[2] = connection;
                }
            }
            //else
            //{
            //    Connections[(int)rotation.y % 90] = connection;
            else if (rotation.y == 90)                              
            //we know it is a connection in the positive x axis  
            {
                Connections[1] = connection;                       
            }
            else if (rotation.y == 180)                          
            //we know it is a connection in the negative z axis   
            {
                Connections[4] = connection;                        
            }
            else if (rotation.y == 270) //ADDED
            //we know it is a connection in the negative x axis     
            {
                Connections[0] = connection;                       
            }
            else                                                   
            //we know it is a connection in the positive z axis    
            {
                Connections[5] = connection;                      
            }
        }
    }
    #endregion
}
