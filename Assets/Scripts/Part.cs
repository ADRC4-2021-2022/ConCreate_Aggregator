using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Part
{
    #region public fields
    List<Connection> Connections = new List<Connection>();
    GameObject GOPartPrefab;

    #endregion

    #region private fields
    //For communication with object in unity
    PartTrigger _connectedGOPart;

    #endregion

    #region constructors

    #endregion
    #region public functions
    public bool PlacePart(Connection connection)
    {
        //Position your part on a certain connection

        //Check if part intersects with other parts
        //option 1: Turn all the part colliders in your building into triggers
        //Check when instantiation the new part prefab if any of the parts has been triggered
        //Try this first since it is easy

        //option 2: Your building is voxelised
        //Voxelise your new part
        //Check how many voxels overlap with the allready used voxels
        // This option gives you more controll over how much overlap you want to allow
        
        //Remove the part if it can't be placed

        //Return if the part can is placed or not
        return false;
    }

    #endregion
    #region private functions
    private void LoadPart()
    {
        //Find all instances of ConnectionNormal prefab in the children of your GOPartPrefab using the tags
        List<GameObject> connectionPrefabs = GetChildObject(GOPartPrefab.transform, "ConnectionNormal");

        //Create a connection object for each connectionPrefab using its transform position, rotation and x scale as length

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
