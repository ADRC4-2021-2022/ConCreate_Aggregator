using System.Collections.Generic;
using System.Linq;
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

    public MeshCollider Collider
    {
        get
        {
            if (_collider == null)
            {
                //Debug.Log($"Searching Collider found for {Name}");
                var goCollider = Util.GetChildObject(GOPart.transform, "PartCollider").First();
                if (goCollider != null)
                    _collider = goCollider.GetComponentInChildren<MeshCollider>();
                else
                    Debug.Log($"No Collider found for {Name}");
            }
            return _collider;
        }
    }

    public string Name
    {
        get
        {
            return GOPart.name;
        }
        set
        {
            GOPart.name = value;
        }
    }
    #endregion

    #region private fields
    private GameObject _prefab;
    private MeshCollider _collider;
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

        //GameObject.Destroy(GOPart);
        if (GOPart == null)
        {
            GOPart = GameObject.Instantiate(_prefab, Vector3.zero, Quaternion.identity);
            connectedGOPart = GOPart.AddComponent<PartTrigger>();
            connectedGOPart.ConnectedPart = this;
        }
        GOPart.transform.position = Vector3.zero;
        GOPart.transform.rotation = Quaternion.identity;

        GOPart.SetActive(false);



    }
    #endregion

    #region public functions
    public void PlaceFirstPart(Vector3 position, Quaternion rotation)
    {
        //Create the part gameobject in the scene
        GOPart.SetActive(true);

        GOPart.transform.SetPositionAndRotation(position, rotation);

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
        GOPart.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material.color = Util.RandomColor;
        if (availableConnection != null && connectionToPlace != null)
        {
            Util.RotatePositionFromToUsingParent(connectionToPlace, availableConnection);

            //set the part status as 'placed and being checked'
            Status = PartStatus.Checking;
        }
    }

    public void PlacePart(Connection connectionToPlace)
    {
        Debug.Log($"Part {Name} placed");
        Status = PartStatus.Placed;

        //Set all connections available (except for the one used)
        foreach (var connection in Connections)
        {
            if (connection == connectionToPlace)
            {
                connection.Available = false;
            }
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
        List<GameObject> wallConnections = Util.GetChildObject(GOPart.transform, "onlyWallConn");
        List<GameObject> floorConnections = Util.GetChildObject(GOPart.transform, "onlyFloorConn");
        List<GameObject> wallAndFloorConnections = Util.GetChildObject(GOPart.transform, "bothWallFloorConn");
        //List<GameObject> surfaceConnections = Util.GetChildObject(GOPart.transform, "surfaceConn");
        //List<GameObject> stackingConnectionPrefabs = Util.GetChildObject(GOPart.transform, "StackingConnectionNormal");

        //Create a connection object for each connectionPrefab using its transform position, rotation and x scale as length
        foreach (var connectionGO in wallConnections) Connections.Add(new Connection(connectionGO, this));
        foreach (var connectionGO in floorConnections) Connections.Add(new Connection(connectionGO, this));
        foreach (var connectionGO in wallAndFloorConnections) Connections.Add(new Connection(connectionGO, this));
        //foreach (var connectionGO in surfaceConnections) Connections.Add(new Connection(connectionGO, this));
        /*foreach (var connectionGO in stackingConnectionPrefabs)
        {
            Connections.Add(new Connection(connectionGO, this));
        }*/
    }
    #endregion
}
