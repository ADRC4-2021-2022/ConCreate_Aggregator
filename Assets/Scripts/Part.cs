using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;

public class Part
{
    #region public fields
    public List<Connection> Connections = new List<Connection>();
    public GameObject GOPart;

    public bool Placed = false;
    //public bool Fitting;

    #endregion

    #region private fields
    //For communication with object in unity
    PartTrigger _connectedGOPart;

    #endregion

    #region constructors
    public Part(GameObject partPrefab)
    {
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

    public void PlacePart(Vector3 position, Quaternion rotation, Connection anchorConnection)
    {
        anchorConnection.NameGameObject("anchor");
        GOPart.SetActive(true);
          
        //Move the part so the anchor point is on the part pivot point
        Vector3 movementAnchor =  GOPart.transform.position - anchorConnection.Position;
        Debug.Log(movementAnchor.magnitude);        

        //Movement part to used connection
        Vector3 movement = position - GOPart.transform.position;
        var test = GameObject.CreatePrimitive(PrimitiveType.Cube);
        test.transform.position = GOPart.transform.position;
        Debug.Log(movement.magnitude);
        GOPart.transform.Translate(movementAnchor + movement);

        var pivot = new GameObject("pivot");
        pivot.transform.position = position;
        GOPart.transform.parent = pivot.transform;

        //float angle;
        //Vector3 orientation;

        //pivot.transform.Rotate(new Vector3(0, angle, 0));
        //pivot.transform.LookAt();

        // remove the parent from the part
        GOPart.transform.parent = null;
        GameObject.Destroy(pivot);
        //Rotation
        //var normalTarget = anchorConnection.GOConnection.transform.rotation * Vector3.up;
        //Vector3 upAxis = (anchorConnection.GOConnection.transform.rotation * Vector3.up).normalized;
        //float angle = anchorConnection.GOConnection.transform.rotation.eulerAngles.y - rotation.eulerAngles.y;
        
        //Vector3 origin = GOPart.transform.rotation * Vector3.up;
        //Vector3 target = anchorConnection.GOConnection.transform.rotation * Vector3.up;
        //Quaternion finalRotation = Util.RotateFromTo(origin, target);
        //_connectedGOPart.transform.localRotation = rotation;

        /*Rotation doesn't work yet. this is a certain direction.
        //Rotate the part according to the anchorConnection rotation and the rotation
        Vector3 upAxis = (anchorConnection.GOConnection.transform.rotation * Vector3.up).normalized;
        float angle = anchorConnection.GOConnection.transform.rotation.eulerAngles.y - rotation.eulerAngles.y;
        
        GOPart.transform.RotateAround(anchorConnection.Position,upAxis, angle);
        */

        //Create the part gameobject in the scene


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

    /// <summary>
    /// Get the quaternion for rotation between two vectors. (this only take one axis into account)
    /// </summary>
    /// <param name="origin">Original orientation vector</param>
    /// <param name="target">Target orientation vector</param>
    public Quaternion RotateFromTo(Vector3 origin, Vector3 target)
    {
        origin.Normalize();
        target.Normalize();

        float dot = Vector3.Dot(origin, target);
        float s = Mathf.Sqrt((1 + dot) * 2);
        float invs = 1 / s;

        Vector3 c = Vector3.Cross(origin, target);

        Quaternion rotation = new Quaternion();

        rotation.x = c.x * invs;
        rotation.y = c.y * invs;
        rotation.z = c.z * invs;
        rotation.w = s * 0.5f;

        rotation.Normalize();

        return rotation;

        //source: https://stackoverflow.com/questions/21828801/how-to-find-correct-rotation-from-one-vector-to-another
    }

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

        //option 2: Your building is voxelised
        //Voxelise your new part
        //Check how many voxels overlap with the allready used voxels
        // This option gives you more controll over how much overlap you want to allow

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
