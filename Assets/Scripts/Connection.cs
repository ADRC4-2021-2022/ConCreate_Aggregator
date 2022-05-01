using UnityEngine;

public class Connection
{
    #region public fields

    public GameObject GOConnection;
    public string Name { get; private set; }

    //Relative rotation and position to the library part
    public Vector3 Position
    {
        get
        {
            return GOConnection.transform.position;
        }
    }

    public Vector3 Normal
    {
        get
        {
            Debug.Log(GOConnection.transform.localRotation.eulerAngles);
            return GOConnection.transform.localToWorldMatrix * Vector3.forward;
        }
    }

    public Quaternion NormalAsQuaternion
    {
        get
        {
            return GOConnection.transform.rotation;
        }
    }

    public bool Visible
    {
        set
        {
            GOConnection.SetActive(value);
        }
    }

    public float Length;
    public Part ThisPart;

    //Available becomes false if the connection has been used or if no part can be added to the connection
    public bool Available
    {
        get
        {
            return _available;
        }
        set
        {
            _available = value;
            //Debug.Log($"Connection available is {value}");
        }
    }

    /// <summary>
    /// Is this connection in the library of parts that are not place yet
    /// </summary>
    public bool LibraryConnection
    {
        get
        {
            return ThisPart.Status == PartStatus.Available;
        }
    }
    #endregion

    #region private fields
    bool _available = false;
    #endregion
    #region constructors
    public Connection(GameObject goConnection, Part thisPart)
    {
        GOConnection = goConnection;
        Length = GOConnection.transform.localScale.z;
        ThisPart = thisPart;
    }
    #endregion
    #region public functions
    public void NameGameObject(string name)
    {
        GOConnection.name = name;
        Name = name;
    }
    #endregion
    #region private functions

    #endregion
}
