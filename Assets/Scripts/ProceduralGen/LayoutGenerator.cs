using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class LayoutGenerator : MonoBehaviour
{
    [Header("Room Prefabs")]
    public List<GameObject> roomPrefabs;

    [Header("Generation settings")]
    public int maxRooms = 10;
    public Transform startingPoint;

    [Header("Max area for house")]
    public GameObject areaLimiter;

    private List<RoomData> generatedRooms = new List<RoomData>();
    private List<RoomDoor> openDoors = new List<RoomDoor>();
    private List<(Vector3, Vector3)> vectors = new List<(Vector3, Vector3)>();

    void Start()
    {
        GenerateLayout();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ClearLayout();
            GenerateLayout();
        }

        for (int i = 0; i < vectors.Count; i++)
        {
            Vector3 cornerA = vectors[i].Item1;
            Vector3 cornerB = vectors[i].Item2;
            Vector3 cornerC = new Vector3(cornerA.x, 0, cornerB.z);
            Vector3 cornerD = new Vector3(cornerB.x, 0, cornerA.z);
            // Debug.DrawLine(cornerA, cornerC, Color.red, 1000, true);
            // Debug.DrawLine(cornerC, cornerB, Color.red, 1000, true);
            // Debug.DrawLine(cornerB, cornerD, Color.red, 1000, true);
            // Debug.DrawLine(cornerD, cornerA, Color.red, 1000, true);
        }
    }

    void GenerateLayout()
    {
        RoomData startRoom = SpawnRoom(roomPrefabs[0], startingPoint.position, roomPrefabs[0].transform.rotation);
        BoxCollider areaCollider = areaLimiter.GetComponent<BoxCollider>();
        Bounds maxAreaBounds = GetWorldBounds(areaCollider, areaLimiter.transform);

        generatedRooms.Add(startRoom);
        openDoors.AddRange(startRoom.Doors);

        for (int i = 1; i < maxRooms; i++)
        {
            if (openDoors.Count == 0) break;

            int resetCounter = 0;
        roomReset:
            GameObject prefab = roomPrefabs[Random.Range(1, roomPrefabs.Count)];
            RoomData newRoom = SpawnRoom(prefab, Vector3.zero, prefab.transform.rotation);
            if (newRoom == null || newRoom.RoomName == roomPrefabs[0].GetComponent<RoomData>().RoomName)
            {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100) break;
                goto roomReset;
            }
            RoomDoor connectingDoor = newRoom.Doors[Random.Range(0, newRoom.Doors.Count)];

        doorReset:
            RoomDoor door = openDoors[Random.Range(0, openDoors.Count)];

            if (door == null) break;
            if (door.isConnected)
            {
                openDoors.Remove(door);
                goto doorReset;
            }

            if (generatedRooms.Count(r => r.RoomName == newRoom.RoomName) >= newRoom.MaxNumberOfAppearances)
            {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100)
                {
                    break;
                }
                goto roomReset;
            }

            ConnectDoors(newRoom, door, connectingDoor);

            if (IsRoomClipping(newRoom))
            {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100)
                {
                    continue;
                }
                goto roomReset;
            }

            BoxCollider newRoomCollider = newRoom.GetComponent<BoxCollider>();
            Bounds newRoomBounds = GetWorldBounds(newRoomCollider, newRoom.transform);
            if (!(maxAreaBounds.min.x < newRoomBounds.min.x &&
                  maxAreaBounds.min.z < newRoomBounds.min.z &&
                  maxAreaBounds.max.x > newRoomBounds.max.x &&
                  maxAreaBounds.max.z > newRoomBounds.max.z))
            {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100)
                {
                    continue;
                }
                goto roomReset;
            }

            if (generatedRooms == null)
            {
                Destroy(newRoom.gameObject);
                if (resetCounter++ > 100)
                {
                    break;
                }
                goto roomReset;
            }

            door.isConnected = true;
            connectingDoor.isConnected = true;

            if (newRoom != null)
            {
                generatedRooms.Add(newRoom);
                openDoors.AddRange(newRoom.Doors);

                vectors.Add((new Vector3(newRoomBounds.min.x, 0, newRoomBounds.min.z),
                             new Vector3(newRoomBounds.max.x, 0, newRoomBounds.max.z)));

                openDoors.Remove(door);
                openDoors.Remove(connectingDoor);
            }
        }
        generatedRooms.RemoveAll(r => r == null);
        if (generatedRooms.Count < 4)
        {
            ClearLayout();
            GenerateLayout();
        }
    }

    RoomData SpawnRoom(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(prefab, pos, rot);
        return go.GetComponent<RoomData>();
    }

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
        foreach (RoomData room in generatedRooms)
        {
            if (room != null)
                Destroy(room.gameObject);
        }

        generatedRooms.Clear();
        openDoors.Clear();
        vectors.Clear();
    }

    void ConnectDoors(RoomData newRoom, RoomDoor oldDoor, RoomDoor newDoor)
    {
        // Aligner la nouvelle porte exactement sur l'ancienne porte (position complète incluant Y)
        Vector3 offset = oldDoor.transform.position - newDoor.transform.position;
        newRoom.transform.position += offset;

        // Aligner les rotations pour que les portes soient face à face
        Vector3 oldDoorForward = oldDoor.transform.forward;
        Vector3 newDoorForward = newDoor.transform.forward;

        // Calculer l'angle de rotation nécessaire pour que les portes soient opposées
        float angleY = Vector3.SignedAngle(newDoorForward, -oldDoorForward, Vector3.up);

        // Appliquer la rotation autour de la position de la nouvelle porte
        newRoom.transform.RotateAround(newDoor.transform.position, Vector3.up, angleY);

        // Recalculer l'offset après rotation pour s'assurer que les portes sont parfaitement alignées
        offset = oldDoor.transform.position - newDoor.transform.position;
        newRoom.transform.position += offset;

        RoomData oldRoom = oldDoor.GetComponentInParent<RoomData>();

        // Essayer différentes rotations (90°, 180°, 270°) pour minimiser le chevauchement
        float minIntersection = GetColliderIntersectionArea(oldRoom, newRoom);
        Vector3 bestPosition = newRoom.transform.position;
        Quaternion bestRotation = newRoom.transform.rotation;

        for (int i = 1; i < 4; i++)
        {
            // Rotation de 90° autour de la porte
            newRoom.transform.RotateAround(newDoor.transform.position, Vector3.up, 90f);

            // Réaligner après rotation
            offset = oldDoor.transform.position - newDoor.transform.position;
            newRoom.transform.position += offset;

            float intersection = GetColliderIntersectionArea(oldRoom, newRoom);

            if (intersection < minIntersection)
            {
                minIntersection = intersection;
                bestPosition = newRoom.transform.position;
                bestRotation = newRoom.transform.rotation;
            }

            if (intersection <= 5f)
            {
                return;
            }
        }

        // Appliquer la meilleure configuration trouvée
        newRoom.transform.position = bestPosition;
        newRoom.transform.rotation = bestRotation;
    }

    float GetColliderIntersectionArea(RoomData roomA, RoomData roomB)
    {
        BoxCollider boxA = roomA.GetComponent<BoxCollider>();
        BoxCollider boxB = roomB.GetComponent<BoxCollider>();

        if (boxA == null || boxB == null)
            return 0f;

        Bounds boundsA = GetWorldBounds(boxA, roomA.transform);
        Bounds boundsB = GetWorldBounds(boxB, roomB.transform);

        if (!boundsA.Intersects(boundsB))
        {
            return 0f;
        }

        float overlapX = Mathf.Min(boundsA.max.x, boundsB.max.x) -
                         Mathf.Max(boundsA.min.x, boundsB.min.x);
        float overlapZ = Mathf.Min(boundsA.max.z, boundsB.max.z) -
                         Mathf.Max(boundsA.min.z, boundsB.min.z);

        if (overlapX <= 0 || overlapZ <= 0)
            return 0f;

        float overlapArea = overlapX * overlapZ;
        float areaA = boundsA.size.x * boundsA.size.z;

        if (areaA < 0.01f)
            return 0f;

        return overlapArea / areaA * 100f;
    }

    Bounds GetWorldBounds(BoxCollider collider, Transform transform)
    {
        Vector3 center = transform.TransformPoint(collider.center);
        Vector3 size = collider.size;
        Vector3[] corners = new Vector3[8];
        corners[0] = transform.TransformPoint(collider.center +
            new Vector3(-size.x, -size.y, -size.z) * 0.5f);
        corners[1] = transform.TransformPoint(collider.center +
            new Vector3(size.x, -size.y, -size.z) * 0.5f);
        corners[2] = transform.TransformPoint(collider.center +
            new Vector3(-size.x, size.y, -size.z) * 0.5f);
        corners[3] = transform.TransformPoint(collider.center +
            new Vector3(size.x, size.y, -size.z) * 0.5f);
        corners[4] = transform.TransformPoint(collider.center +
            new Vector3(-size.x, -size.y, size.z) * 0.5f);
        corners[5] = transform.TransformPoint(collider.center +
            new Vector3(size.x, -size.y, size.z) * 0.5f);
        corners[6] = transform.TransformPoint(collider.center +
            new Vector3(-size.x, size.y, size.z) * 0.5f);
        corners[7] = transform.TransformPoint(collider.center +
            new Vector3(size.x, size.y, size.z) * 0.5f);

        // Create axis-aligned bounding box from corners
        Bounds bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < 8; i++)
        {
            bounds.Encapsulate(corners[i]);
        }

        return bounds;
    }

    bool IsRoomClipping(RoomData newRoom)
    {
        // foreach (RoomData generatedRoom in generatedRooms) {
        //     if (generatedRoom == null) continue;
        //     if (GetColliderIntersectionArea(generatedRoom, newRoom) > 10f) {
        //         return true;
        //     }
        // }
        return false;
    }
}
