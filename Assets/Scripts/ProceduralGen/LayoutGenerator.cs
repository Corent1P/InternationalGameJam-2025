using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class LayoutGenerator : MonoBehaviour
{
    [Header("Room Prefabs")]
    public List<GameObject> roomPrefabs;

    [Header("Generation settings")]
    public int maxRooms = 10;

    private List<RoomData> generatedRooms = new List<RoomData>();
    private List<RoomDoor> openDoors = new List<RoomDoor>();
    private List<(Vector3, Vector3)> vectors = new List<(Vector3, Vector3)>();

    void Start()
    {
        GenerateLayout();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) {
            ClearLayout();
            GenerateLayout();
        }
    }

    void GenerateLayout()
    {
        RoomData StartRoom = SpawnRoom(roomPrefabs[0], Vector3.zero, roomPrefabs[0].transform.rotation);

        generatedRooms.Add(StartRoom);
        openDoors.AddRange(StartRoom.Doors);

        for (int i = 1; i < maxRooms; i++) {
            if (openDoors.Count == 0) break;

            int resetCounter = 0;
roomReset:
            GameObject prefab = roomPrefabs[Random.Range(1, roomPrefabs.Count)];
            RoomData newRoom = SpawnRoom(prefab, Vector3.zero, prefab.transform.rotation);
            if (newRoom == null || newRoom.RoomName == roomPrefabs[0].GetComponent<RoomData>().RoomName) {
                Destroy(newRoom.gameObject);
                // Debug.Log("Destroyed room due to null or same as start room.");
                if (resetCounter++ > 100) break;
                goto roomReset;
            }
            RoomDoor connectingDoor = newRoom.Doors[Random.Range(0, newRoom.Doors.Count)];

doorReset:
            RoomDoor door = openDoors[Random.Range(0, openDoors.Count)];

            if (door == null) break;
            if (door.isConnected) {
                openDoors.Remove(door);
                goto doorReset;
            }

            if (generatedRooms.Count(r => r.RoomName == newRoom.RoomName) >= newRoom.MaxNumberOfAppearances) {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100) {
                    Debug.Log("Max reset attempts reached. Stopping generation. 1");
                    break;
                }
                goto roomReset;
            }

            ConnectDoors(newRoom, door, connectingDoor);

            if (IsRoomClipping(newRoom)) {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100) {
                    Debug.Log("Max reset attempts reached. Stopping generation. 2");
                    continue;
                }
                goto roomReset;
            }

            if (generatedRooms == null) {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100) {
                    Debug.Log("Max reset attempts reached. Stopping generation. 3");
                    break;
                }
                goto roomReset;
            }

            door.isConnected = true;
            connectingDoor.isConnected = true;

            generatedRooms.Add(newRoom);
            Debug.Log("Room added: " + newRoom.RoomName);
            openDoors.AddRange(newRoom.Doors);

            openDoors.Remove(door);
            openDoors.Remove(connectingDoor);
        }
        generatedRooms.RemoveAll(r => r == null);
        if (generatedRooms.Count < 10) {
            ClearLayout();
            GenerateLayout();
        }
    }

    RoomData SpawnRoom(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(prefab, pos, rot);
        return go.GetComponent<RoomData>();
    }

    // Vector3 GetRoomWorldSize(RoomData room)
    // {
    //     Collider[] colliders = room.GetComponentsInChildren<Collider>();
    //     if (colliders.Length == 0)
    //         return Vector3.zero;

    //     Bounds bounds = new Bounds(colliders[0].bounds.center, Vector3.zero);
    //     foreach (Collider col in colliders)
    //         bounds.Encapsulate(col.bounds);

    //     return bounds.size;
    // }

    Vector3 GetRoomWorldSize(RoomData room)
    {
        Collider collider = room.GetComponent<Collider>();
        if (collider == null)
            return Vector3.zero;

        Bounds bounds = new Bounds(collider.bounds.center, Vector3.zero);

        return bounds.size;
    }

    void ClearLayout()
    {
        foreach (RoomData room in generatedRooms) {
            if (room != null)
                Destroy(room.gameObject);
        }

        generatedRooms.Clear();
        openDoors.Clear();
        vectors.Clear();
    }

    void ConnectDoors(RoomData newRoom, RoomDoor oldDoor, RoomDoor newDoor)
    {
        newRoom.transform.position +=
            oldDoor.transform.position - newDoor.transform.position;

        //! A décommenter
        // RoomData oldRoom = oldDoor.GetComponentInParent<RoomData>();

        for (int i = 0; i < 4; i++)
        {
            // float intersection = GetColliderIntersectionArea(oldRoom, newRoom);

            // Debug.Log("Intersection between " + newRoom.RoomName + " and " + oldRoom.RoomName + ": " + intersection);
            // if (intersection < 0.01f)
            // {
            //     Debug.Log("Connected " + newRoom.RoomName + " to " + oldRoom.RoomName);
            //     return;
            // }
            newRoom.transform.RotateAround(newDoor.transform.position, Vector3.up, 90f);
        }
    }

    // float GetColliderIntersectionArea(RoomData roomA, RoomData roomB)
    // {
    //     Collider[] collidersA = roomA.GetComponentsInChildren<Collider>();
    //     Collider[] collidersB = roomB.GetComponentsInChildren<Collider>();

    //     Bounds boundsA = collidersA[0].bounds;
    //     Bounds boundsB = collidersB[0].bounds;

    //     Vector3 boundsAMin = boundsA.min + roomA.transform.position;
    //     Vector3 boundsAMax = boundsA.max + roomA.transform.position;
    //     Vector3 boundsBMin = boundsB.min + roomB.transform.position;
    //     Vector3 boundsBMax = boundsB.max + roomB.transform.position;

    //     float overlapX = Mathf.Max(boundsAMin.x, boundsBMin.x) - Mathf.Min(boundsAMax.x, boundsBMax.x);
    //     float overlapY = Mathf.Max(boundsAMin.z, boundsBMin.z) - Mathf.Min(boundsAMax.z, boundsBMax.z);

    //     float overlapArea = overlapX * overlapY;
    //     float areaA = boundsA.size.x * boundsA.size.z;

    //     if (areaA < 0.01f)
    //         return 0f;

    //     return overlapArea / areaA * 100f;
    // }
    
        float GetColliderIntersectionArea(RoomData roomA, RoomData roomB)
    {
        Collider collidersA = roomA.GetComponent<Collider>();
        Collider collidersB = roomB.GetComponent<Collider>();

        Bounds boundsA = collidersA.bounds;
        Bounds boundsB = collidersB.bounds;

        Vector3 boundsAMin = boundsA.min + roomA.transform.position;
        Vector3 boundsAMax = boundsA.max + roomA.transform.position;
        Vector3 boundsBMin = boundsB.min + roomB.transform.position;
        Vector3 boundsBMax = boundsB.max + roomB.transform.position;

        float overlapX = Mathf.Max(boundsAMin.x, boundsBMin.x) - Mathf.Min(boundsAMax.x, boundsBMax.x);
        float overlapY = Mathf.Max(boundsAMin.z, boundsBMin.z) - Mathf.Min(boundsAMax.z, boundsBMax.z);

        float overlapArea = overlapX * overlapY;
        float areaA = boundsA.size.x * boundsA.size.z;

        if (areaA < 0.01f)
            return 0f;

        return overlapArea / areaA * 100f;
    }

    bool IsRoomClipping(RoomData newRoom)
    {
        //! A décommenter
        // foreach (RoomData generatedRoom in generatedRooms) {
        //     if (generatedRoom == null) continue;
        //     // float temp = GetColliderIntersectionArea(generatedRoom, newRoom);
        //     if (GetColliderIntersectionArea(generatedRoom, newRoom) > 1f) {
        //         return true;
        //     }
        // }
        return false;
    }
}
