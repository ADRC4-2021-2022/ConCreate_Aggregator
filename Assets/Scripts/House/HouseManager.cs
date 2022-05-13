using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HouseManager : MonoBehaviour
{
    #region Private variables
    private Dictionary<string, Vector3> _directions = new Dictionary<string, Vector3>()
    {
        { "North", new Vector3(0,0,1) },
        { "South", new Vector3(0,0,-1) },
        { "East", new Vector3(1,0,0) },
        { "West", new Vector3(-1,0,0) },
        { "NorthEast", new Vector3(1,0,1) },
        { "NorthWest", new Vector3(-1,0,1) },
        { "SouthEast", new Vector3(1,0,-1) },
        { "SouthWest", new Vector3(-1,0,-1) }
    };

    List<Room> _rooms = new List<Room>();

    float _collisionCheckRadius = 50f;
    Collider[] _collisionNeighbours = new Collider[25];
    #endregion
    // Start is called before the first frame update
    void Start()
    {
        CreateRoomsWithEquation();
    }

    // Update is called once per frame
    void Update()
    {

    }

    //create rooms given their positions (coordinates)
    public void CreateRooms()
    {
        if (_rooms.Count > 0)
        {
            foreach (Room room in _rooms)
            {
                GameObject.Destroy(room.GO);
            }
        }
        _rooms = new List<Room>()
        {
            new Room("Hall", 3, new Vector3(0, 0, 0)),
            new Room("LivingRoom", 24, new Vector3(-3.5f, 0,  3.5f)),
            new Room("Kitchen", 16, new Vector3(-9.5f, 0, 5.5f)),
            new Room("Bedroom1", 16, new Vector3(-2.5f, 0, -3.5f)),
            new Room("EnsuiteBathroom", 6, new Vector3(-2, 0, -7.5f)),
            new Room("Bedroom2", 12, new Vector3(-7.5f, 0, -2)),
            new Room("Storage", 3, new Vector3(2, 0, -2)),
            new Room("Bathroom", 4, new Vector3(3.5f, 0, 0.5f))
        };
    }

    //create rooms with unknown coordinates, by using the equation
    public void CreateRoomsWithEquation()
    {
        if (_rooms.Count > 0)
        {
            foreach (Room room in _rooms)
            {
                GameObject.Destroy(room.GO);
            }
        }
        _rooms = new List<Room>()
        {
            new Room("Hall", 3),
            new Room("LivingRoom", 24),
            new Room("Kitchen", 16),
            new Room("Bedroom1", 16),
            new Room("EnsuiteBathroom", 6),
            new Room("Bedroom2", 12),
            new Room("Storage", 3),
            new Room("Bathroom", 4)
        };
        _rooms[0].AddRoomConnection(GetRoomByName("Bathroom"), 20);
        _rooms[0].AddRoomConnection(GetRoomByName("Storage"), 20);
        _rooms[0].AddRoomConnection(GetRoomByName("LivingRoom"), 20);
        _rooms[0].AddRoomConnection(GetRoomByName("Bedroom1"), 20);
        _rooms[0].AddRoomConnection(GetRoomByName("Bedroom2"), 20);

        _rooms[1].AddRoomConnection(GetRoomByName("Kitchen"), 20);
        
        _rooms[5].AddRoomConnection(GetRoomByName("EnsuiteBathroom"), 20);

        AttemptToPlaceRooms();
    }

    private Room GetRoomByName(string name)
    {
        return _rooms.Find(room => room.Name == name);
    }

    public void AttemptToPlaceRooms()
    {
        foreach (Room room in _rooms)
        {
            foreach ((Room, int) connectedRoom in room.ConnectedRooms)
            {
                // WORKING, BUT JUST PREDETERMINED DIRECTIONS
                var originalPosition = connectedRoom.Item1.RoomPosition;
                foreach (var direction in _directions.Values)
                {
                    if (CheckCollisionWithPhysicsPenetration(connectedRoom.Item1))
                    {
                        connectedRoom.Item1.RoomPosition = originalPosition + (connectedRoom.Item2 * direction);
                    }
                    else break;
                }

                // NOT WORKING, TRYING THE EQUATION
                /*foreach (var direction in _directions.Values)
                {
                    var roomBounds = room.GO.GetComponent<Collider>().bounds;
                    var connectedRoomBounds = connectedRoom.Item1.GO.GetComponent<Collider>().bounds;

                    if (CheckCollisionWithPhysicsPenetration(connectedRoom.Item1))
                    {
                        var abX = connectedRoom.Item1.RoomPosition.x - room.RoomPosition.x;
                        var halfSizeAX = roomBounds.extents.x;
                        var halfSizeBX = connectedRoomBounds.extents.x;
                        var dX = abX - halfSizeAX - halfSizeBX;
                        //connectedRoom.Item1.GO.transform.position = room.GO.transform.position + (new Vector3(dX * direction.x, 0, 0));
                        connectedRoom.Item1.GO.transform.position = room.GO.transform.position + (new Vector3(abX, 0, 0) + (new Vector3(1 * direction.x, 0, 0)));

                        var abZ = connectedRoom.Item1.RoomPosition.z - room.RoomPosition.z;
                        var halfSizeAZ = roomBounds.extents.z;
                        var halfSizeBZ = connectedRoomBounds.extents.z;
                        var dZ = abZ - halfSizeAZ - halfSizeBZ;
                        //connectedRoom.Item1.GO.transform.position = room.GO.transform.position + (new Vector3(0, 0, dZ * direction.z));
                        connectedRoom.Item1.GO.transform.position = room.GO.transform.position + (new Vector3(0, 0, abZ) + (new Vector3(0, 0, 1 * direction.z)));
                    }
                    else break;
                }*/
            }
        }
    }

    private bool CheckCollisionWithPhysicsPenetration(Room currentRoom)
    {
        var thisCollider = currentRoom.GO.GetComponent<Collider>();
        if (!thisCollider) return true; // nothing to do without a Collider attached

        int count = Physics.OverlapSphereNonAlloc(thisCollider.gameObject.transform.position, _collisionCheckRadius, _collisionNeighbours);

        for (int i = 0; i < count; ++i)
        {
            var otherCollider = _collisionNeighbours[i];

            if (otherCollider == thisCollider)
                continue;

            Vector3 otherPosition = otherCollider.gameObject.transform.position;
            Quaternion otherRotation = otherCollider.gameObject.transform.rotation;
            bool isOverlapping = Physics.ComputePenetration(
            thisCollider, thisCollider.gameObject.transform.position, thisCollider.gameObject.transform.rotation,
            otherCollider, otherPosition, otherRotation,
            out _, out _);

            // draw a line showing the depenetration direction if overlapped
            if (isOverlapping) return true;
        }
        return false;
    }

    public void OffsetBounds()
    {
        foreach (Room room in _rooms.Where(r => r.Placed = true))
        {
            room.GO.GetComponent<Collider>().bounds.Expand(3f);
        }
    }
}
