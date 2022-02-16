using System.Collections.Generic;
using UnityEngine;

public class Part
{
    #region public fields
    public List<Connection> Connections = new List<Connection>();
    public GameObject GOPart;

    public bool Placed = false;

    #endregion

    #region private fields
    //For communication with object in unity
    PartTrigger _connectedGOPart;

    #endregion

    #region constructors
    public Part(GameObject partPrefab)
    {
        GOPart = partPrefab;
        GOPart = GameObject.Instantiate(partPrefab, Vector3.zero, Quaternion.identity);
        GOPart.SetActive(false);

        _connectedGOPart = GOPart.AddComponent<PartTrigger>();
        _connectedGOPart.ConnectedPart = this;

        LoadPartConnections();
    }
    #endregion

    #region public functions


    public void PlaceFirstPart(Vector3 position, Quaternion rotation)
    {
        //Create the part gameobject in the scene
        GOPart.name = "FirstPart";
        GOPart.SetActive(true);

        //Set all connections available (except for the one used)
        foreach (var connection in Connections)
        {
            connection.Available = true;
        }

        //set the part as placed
        Placed = true;

    }

    public void PlacePart(Connection targetConnection, Connection anchorConnection)
    {
        //Enable the part gameobject in the scene
        anchorConnection.NameGameObject("anchor");
        GOPart.SetActive(true);

        
        Util.RotatePositionFromToUsingParent(anchorConnection, targetConnection);

        //Set all connections available (except for the one used)
        foreach (var connection in Connections)
        {
            if (connection != anchorConnection)
            {
                connection.Available = true;
            }
        }

        //set the part as placed
        Placed = true;
    }

    /*public void MoveStartToPosition(Vector3 target)
    {
        //Move start point to target
        _connectedGOPart.transform.position = target;
    }*/

    

    /*
    public bool PlacePart(Connection connection)
    {
        //Position your part on a certain connection
        PlacePart(connection.Position, connection.Normal, connection);

        //Check if part intersects with other parts
        //option 1: Turn all the part colliders in your building into triggers
        //Check when instantiation the new part prefab if any of the parts has been triggered
        //Try this first since it is easy

        //using GOPartPrefab. blablabla getComponent blablabla
        //GOOGLE HOW TO CHECK IF GO COLLIDERS ARE COLLIDING
        //IF statement, if managed to place the part--> return true, otherwise return false

        //Remove the part if it can't be placed

        //Return if the part can is placed or not
        return false;
    }*/

    /*IEnumerator Coroutine()
    {
        SetNextPart();

        yield return new WaitForSeconds(0.001f);
    }*/

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

    /*private void OnTriggerEnter(Collider collider)
    {
        List<Part> _buildingParts = new List<Part>();
        _buildingParts
        GOPart.GetComponent<Collider>();
        if ()
        {

        }
    }*/

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
