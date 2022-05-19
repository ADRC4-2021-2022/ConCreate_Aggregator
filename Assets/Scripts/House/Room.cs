using System.Collections.Generic;
using UnityEngine;
using static System.Math;

public class Room
{
    #region Private variables
    float _area;
    float _volume;
    float _xDim; //Length
    float _yDim; //Height
    float _zDim; //Width
    #endregion

    #region Public variables
    public string Name;

    public GameObject GO;

    public bool Placed;

    //every room has a list of rooms connected to it with a certain distance (vector3)
    public List<(Room, int)> ConnectedRooms = new List<(Room, int)>();

    //everytime we place a room we place the corresponding GO
    public Vector3 RoomPosition
    {
        get
        {
            return GO.transform.position;
        }
        set
        {
            GO.transform.position = value;
        }
    }
    #endregion

    #region Constructor
    //option 1: create room through given dimensions (floats)
    public Room(string name, float length, float width, float height, Vector3 roomPosition)
    {
        Name = name;
        _xDim = length;
        _yDim = height;
        _zDim = width;
        _area = length * width;
        _volume = length * width * height;
        

        //create assigned GO option 1
        GO = new GameObject("RoomGameobject made from Code");
        //Add Components
        GO.AddComponent<Rigidbody>();
        GO.GetComponent<Rigidbody>().isKinematic = true;
        GO.AddComponent<MeshFilter>();
        GO.AddComponent<BoxCollider>();
        GO.AddComponent<MeshRenderer>();
        GO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matTrans");

        RoomPosition = roomPosition;
        Placed = true;
    }

    //option 2: create room through vector3
    public Room(string name, int area, Vector3 roomPosition)
    {
        Name = name;
        _area = area;
        (_xDim, _zDim) = GetFactorsFromArea(area);
        _yDim = 2.7f;
        _volume = area * _zDim;
        

        //create assigned GO option 2: by primitive
        GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GO.transform.position = roomPosition;
        GO.transform.localScale = new Vector3(_xDim, _yDim, _zDim);
        GO.transform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matTrans");
        var roomBounds = GO.GetComponent<Collider>().bounds;

        RoomPosition = roomPosition;
        Placed = true;
    }

    public Room(string name, int area)
    {
        Name = name;
        _area = area;
        (_xDim, _zDim) = GetFactorsFromArea(area);
        _yDim = 2.7f;
        _volume = area * _zDim;

        //create assigned GO option 2: by primitive
        GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GO.name = name;
        GO.transform.localScale = new Vector3(_xDim, _yDim, _zDim);
        GO.transform.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/matTrans");
        var roomBounds = GO.GetComponent<Collider>().bounds;

        Placed = false;
    }
    #endregion

    #region Public methods
    public void AddRoomConnection(Room otherRoom, int distanceBetweenRooms)
    {
        ConnectedRooms.Add((otherRoom, distanceBetweenRooms));
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
