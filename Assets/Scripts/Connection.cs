using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Connection
{
    #region public fields
    //Relative normal direction and position to the library part
    Vector3 Position;
    Vector3 Normal;
    float Length;

    //Available becomes false if the connection has been used or if no part can be added to the connection
    bool Available;

    #endregion

    #region private fields

    #endregion
    #region constructors
    public Connection(Vector3 position, Vector3 normal, float length)
    {
        Position = position;
        Normal = normal;
        Length = length;

    }

    #endregion
    #region public functions

    #endregion
    #region private functions

    #endregion
}
