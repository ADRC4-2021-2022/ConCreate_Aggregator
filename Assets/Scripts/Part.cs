using System.Collections.Generic;
using UnityEngine;

public enum PartStatus { Available, Checking, Placed }
public class Part
{
    #region public fields
    public List<Connection> Connections = new List<Connection>();
    public GameObject GOPart;
    //For communication with object in unity
    public PartTrigger connectedGOPart;

    public bool Intersecting = false;
    public PartStatus Status;

    #endregion

    #region private fields
    private GameObject _prefab;
    #endregion

    #region constructors
    public Part(GameObject partPrefab)
    {
        _prefab = partPrefab;
        InitializeGO();
        LoadPartConnections();
        Status = PartStatus.Available;
    }

    public void InitializeGO()
    {
        //GOPart = _prefab;
        GameObject.Destroy(GOPart);
        GOPart = GameObject.Instantiate(_prefab, Vector3.zero, Quaternion.identity);
        GOPart.SetActive(false);

        connectedGOPart = GOPart.AddComponent<PartTrigger>();
        connectedGOPart.ConnectedPart = this;

    }
    #endregion

    #region public functions
    public void PlaceFirstPart(Vector3 position, Quaternion rotation)
    {
        //Create the part gameobject in the scene
        GOPart.name = "FirstPart";
        GOPart.SetActive(true);

        //Set all connections available
        foreach (var connection in Connections)
        {
            connection.Available = true;
        }

        //set the part as placed
        Status = PartStatus.Placed;

    }

    public void PositionPart(Connection availableConnection, Connection connectionToPlace)
    {
        //Enable the part gameobject in the scene
        //anchorConnection.NameGameObject("anchor");
        GOPart.SetActive(true);
        if (availableConnection != null && connectionToPlace != null)
        {
            Util.RotatePositionFromToUsingParent(connectionToPlace, availableConnection);

            //Set all connections available (except for the one used)
            foreach (var connection in Connections)
            {
                if (connection != connectionToPlace)
                {
                    connection.Available = true;
                }
                else
                {
                    connection.Available = false;
                }
            }

            //set the part as placed
            Status = PartStatus.Checking;
        }
    }

    public void ResetPart()
    {
        GOPart.SetActive(false);
        InitializeGO();
        Status = PartStatus.Available;
    }
    #endregion

    #region private functions
    //after having a list of connections thanks to the tag, we loop through all the connections of the individual
    //part (prefab) and we add them into the list. each connection is characterized by position, rotation, x lenght
    private void LoadPartConnections()
    {
        //Find all instances of ConnectionNormal prefab in the children of your GOPartPrefab using the tags
        List<GameObject> connectionPrefabs = GetChildObject(GOPart.transform, "ConnectionNormal");

        //Create a connection object for each connectionPrefab using its transform position, rotation and x scale as length
        foreach (var connectionGO in connectionPrefabs)
        {
            Connections.Add(new Connection(connectionGO, this));
        }
    }

    public List<GameObject> GetChildObject(Transform parent, string tag)
    {
        List<GameObject> taggedChildren = new List<GameObject>();

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.tag == tag)
            {
                taggedChildren.Add(child.gameObject);
            }
            if (child.childCount > 0)
            {
                GetChildObject(child, tag);
            }
        }

        return taggedChildren;
    }


    #endregion
}
