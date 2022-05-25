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

    //public bool CheckInsideBoundingBox(Transform boundingBox, out float distance, out Vector3 direction)
    //{
    //    var boxCollider = boundingBox.GetComponent<BoxCollider>();
    //    bool intersects = Physics.ComputePenetration(
    //        Collider,
    //        GOPart.transform.position,
    //        GOPart.transform.rotation,
    //        boxCollider,
    //        boundingBox.position,
    //        boundingBox.rotation,
    //        out direction,
    //        out distance);
    //    if (intersects) return true;

    //    return false;
    //}

    public bool CheckInsideBoundingMeshes(List<Transform> boxes)
    {
        List<Collider> intersecting = new List<Collider>();
        foreach (var box in boxes)
        {
            var boxCollider = box.GetComponent<Collider>();
            bool intersects = Physics.ComputePenetration(
                Collider,
                GOPart.transform.position,
                GOPart.transform.rotation,
                boxCollider,
                box.position,
                box.rotation,
                out Vector3 direction,
                out float distance);
            if (intersects) intersecting.Add(boxCollider);
        }

        if (intersecting.Count == 0) return false;

        var partMesh = Collider.sharedMesh;
        var vertices = partMesh.vertices;
        foreach (var vertex in vertices)
        {
            var transVertex = GOPart.transform.TransformPoint(vertex);
            //var vertGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //vertGo.transform.localPosition = transVertex;
            //vertGo.transform.localScale = Vector3.one * 0.05f;
            bool vertexInside = false;
            int counter = intersecting.Count - 1;
            while (!vertexInside && counter >= 0)
            {
                var mesh = intersecting[counter];
                vertexInside = Util.PointInsideCollider(transVertex, mesh);
                counter--;
            }
            if (!vertexInside) return false;
        }

        return true;
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
