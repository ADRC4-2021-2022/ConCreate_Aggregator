using static System.Math;
using System.Collections.Generic;
using UnityEngine;

public class Room
{
    #region Private variables
    float _area;
    float _volume;

    string _name;

    float _xDim; //Length
    float _yDim; //Height
    float _zDim; //Width

    Vector3 _roomPosition;


    //every room has a list of rooms connected to it with a certain distance (vector3)
    List<(Room, Vector3)> _roomConnections = new List<(Room, Vector3)>();
    #endregion

    #region Public variables
    public GameObject gameObject;

    bool placed;
    #endregion

    #region Constructor
    //option 1: create room through given dimensions (floats)
    public Room(string name, float length, float width, float height, Vector3 roomPosition)
    {
        _name = name;
        _xDim = length;
        _yDim = height;
        _zDim = width;
        _area = length * width;
        _volume = length * width * height;
        _roomPosition = roomPosition;

        //create assigned GO option 1
        gameObject = new GameObject("RoomGameobject made from Code");
        //Add Components
        gameObject.AddComponent<Rigidbody>();
        gameObject.GetComponent<Rigidbody>().isKinematic = true;
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<BoxCollider>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matTrans");
    }

    //option 2: create room through vector3
    public Room(string name, int area, Vector3 roomPosition)
    {
        _name = name;
        _area = area;
        (_xDim, _zDim) = GetFactorsFromArea(area);
        _yDim = 2.7f;
        _volume = area * _zDim;
        _roomPosition = roomPosition;

        //create assigned GO option 2: by primitive
        gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.transform.position = roomPosition;
        gameObject.transform.localScale = new Vector3(_xDim, _yDim, _zDim);
        gameObject.transform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matTrans");
        var roomBounds = gameObject.GetComponent<Collider>().bounds;
    }
    #endregion

    #region Public methods
    public void AddRoomConnection(Room otherRoom, Vector3 distanceBetweenRooms)
    {
        _roomConnections.Add((otherRoom, distanceBetweenRooms));
    }
    #endregion

    #region Private methods
    //create box given the area, by finding factors which could be potential dimensions
    private (int, int) GetFactorsFromArea(int area)
    {
        var randomShape = Random.Range(0, 2);
        //excluding area = 3, whose factors have to be 1 and 3 no other option
        if (area == 3)
        {
            if (randomShape == 0)
            {
                return (1, 3);
            }
            else
            {
                return (3, 1);
            }
        }

        var factors = new List<(int, int)>();
        for (int i = 2; i * i <= area; i++)
            if (area % i == 0)
            {
                if (randomShape == 0)
                {
                    Debug.Log(i + "*" + area / i + "\n");
                    factors.Add((i, area / i));
                }
                else
                {
                    Debug.Log(area / i + "*" + i + "\n");
                    factors.Add((area / i, i));
                }
            }
        return FindMinimumDifference(factors);
    }

    //how to find the most similar factors for the room dimensions (i.e 3 and 4 instead of 2 and 6 (12))
    private (int, int) FindMinimumDifference(List<(int, int)> tuples)
    {
        int diff = int.MaxValue;
        (int, int) minPair = (int.MaxValue, int.MaxValue);

        foreach (var tuple in tuples)
        {
            if (Abs(tuple.Item1 - tuple.Item2) < diff) 
            {
                diff = Abs(tuple.Item1 - tuple.Item2);
                minPair = tuple;
            }
        }
        return minPair;
    }
    #endregion
}
