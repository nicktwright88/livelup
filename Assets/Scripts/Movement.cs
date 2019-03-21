using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class Movement : MonoBehaviour {

    //Unity objects
    public Rigidbody2D rb2;
    public GameObject groundPrefab;
    GameObject groundClone;

    //Unity texts
    public Text latitudeText;
    public Text longitudeText;
    public Text squareLatText;
    public Text squareLongText;
    public Text homeSquareLatText;
    public Text homeSquareLongText;
    public Text tileCountText;
    public Text visitCountText;
    public Text secondCountText;

    //lat&long
    LocationInfo li;

    //lat/long measurements
    public float latitude = -91f;
    public float longitude = -181f;
    int newSquareLat = -91;
    int newSquareLong = -181;
    int currSquareLat = -91;
    int currSquareLong = -181;
    int homeSquareLat = -91;
    int homeSquareLong = -181;
    int tileCount = 0;
    int visitCount = 0;
    int secondCount = 0;

    //game vars
    bool homeSet = false;
    Vector3 newPosition;
    bool noSavedFile = false;
    bool saveFileSynced = false;

    Dictionary<string, TileData> dict = new Dictionary<string,TileData>();

    //used to approximate GPS to game board. Higher = more precision, less stability
    int reducer = 10000; //default 10000

    //used for setting up Service (for running in background)
    AndroidJavaClass unityClass;
    AndroidJavaObject unityActivity;
    AndroidJavaClass customClass;

    // Use this for initialization
    IEnumerator Start ()
    {
        LoadData();

        //Initialize GPS Service
        Debug.Log("Initializing...");

        //Start locaiton services
        //If user has not given permission, this triggers permission request
        Input.location.Start();

        //While user has not given location permission, wait for them to do it
        //TODO: Add fail-out
        while (!Input.location.isEnabledByUser)
        {
            Debug.Log("Waiting for location service...");
            yield return new WaitForSeconds(1);
        }

        //Starts location services if user has JUST given permission
        //TODO: Determine if needed or if it can be rolled up into above
        Input.location.Start();
        yield return new WaitForSeconds(1);
        Debug.Log(Input.location.status);

        if (!Input.location.isEnabledByUser || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Could not determine device location");
            yield break;
        }

        Debug.Log(Input.location.status);

        //start getting GPS location every second
        Debug.Log("Grabbing GPS Location every second");
        InvokeRepeating("GetGPSLoc", 0.0f, 1f);

        //start saving the game every 10 second
        Debug.Log("Saving the game every 10 seconds");
        InvokeRepeating("SaveData", 0.0f, 10f);

        while (newSquareLong == 0 && newSquareLat == 0)
        {
            Debug.Log("Waiting for current GPS Location...");
            yield return new WaitForSeconds(1);
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        //TODO: Update this to edit the actual "GPS data" in li (?)
        // 0/w = up, 1/a = left, 2/s = down, 3/d = right
        if (!homeSet)
        {
            if (Input.GetKeyDown("w")) {
                rb2.MovePosition(new Vector3(rb2.position.x, rb2.position.y + 1));
                UpdateTile(rb2.position.x, rb2.position.y);
            }
            else if (Input.GetKeyDown("a")) {
                rb2.MovePosition(new Vector3(rb2.position.x - 1, rb2.position.y));
                UpdateTile(rb2.position.x, rb2.position.y);
            }
            else if (Input.GetKeyDown("s")) {
                rb2.MovePosition(new Vector3(rb2.position.x, rb2.position.y - 1));
                UpdateTile(rb2.position.x, rb2.position.y);
            }
            else if (Input.GetKeyDown("d")) {
                rb2.MovePosition(new Vector3(rb2.position.x + 1, rb2.position.y));
                UpdateTile(rb2.position.x, rb2.position.y);
            }

        }
    }

    private void OnApplicationQuit()
    {
        //Nothing
        SaveData();
    }

    private void OnApplicationPause(bool pause)
    {
        //Nothing
        SaveData();
    }

    void GetGPSLoc()
    {
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Could not determine device location");
        }
        else if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.Log("Location Services Status not Running. Status: ");
            Debug.Log(Input.location.status);
        }
        else
        {
            li = Input.location.lastData;

            latitude = li.latitude;
            longitude = li.longitude;

            latitudeText.text = latitude.ToString("N6");
            longitudeText.text = longitude.ToString("N6");

            newSquareLat = Mathf.RoundToInt(latitude * reducer);
            squareLatText.text = newSquareLat.ToString();

            newSquareLong = Mathf.RoundToInt(longitude * reducer);
            squareLongText.text = newSquareLong.ToString();

            if (noSavedFile && !homeSet)
            {
                SetHomeGPS(newSquareLat, newSquareLong);
            }

            if (Input.touchCount == 3)
            {
                newSquareLat++;
                newSquareLong++;
            }

            if (newSquareLat != currSquareLat || newSquareLong != currSquareLong)
            {
                //Debug.Log("PositionUpdated/ing");
                UpdatePosition(newSquareLong, newSquareLat);
            }
        }


        IncrementSeconds();

        Debug.Log("ADDING SECOND");
        int zeroNewSquareLong = newSquareLong - homeSquareLong;
        int zeroNewSquareLat = newSquareLat - homeSquareLat;
        string currentPosition = zeroNewSquareLong + "," + zeroNewSquareLat;
        dict[currentPosition].AddSecond();
    }

    void SetHomeGPS(int setSquareLat = -91, int setSquareLong = -181)
    {
        Debug.Log("SETTING HOME VALUES");
        
        homeSquareLat = setSquareLat;
        homeSquareLong = setSquareLong;
        homeSquareLongText.text = homeSquareLong.ToString();
        homeSquareLatText.text = homeSquareLat.ToString();
        homeSet = true;
    }

    void UpdatePosition(int newSquareLong, int newSquareLat)
    {

        int zeroNewSquareLong = newSquareLong - homeSquareLong;
        int zeroNewSquareLat = newSquareLat - homeSquareLat;

        newPosition = new Vector3(zeroNewSquareLong, zeroNewSquareLat);
        rb2.position = newPosition;

        currSquareLat = newSquareLat;
        currSquareLong = newSquareLong;
        
        UpdateTile(zeroNewSquareLong, zeroNewSquareLat);
    }

    void UpdateTile(float rbx, float rby)
    {
        Debug.Log("Adding Tile");
        string currentPosition = rbx + "," + rby;
        if (!dict.ContainsKey(currentPosition))
        {
            TileData tile = new TileData();
            dict.Add(currentPosition, tile);
            if (rbx != 0 || rby != 0)
            {
                groundClone = Instantiate(groundPrefab,
                            new Vector3(rbx, rby), Quaternion.identity) as GameObject;
            }
            IncrementTiles();
            Debug.Log(currentPosition);
        }
        Debug.Log("INCREMENT VISITS BY 1");
        IncrementVisits();
        dict[currentPosition].AddVisit();
    }

    void IncrementTiles()
    {
        tileCount++;
        tileCountText.text = "Tiles: " + tileCount.ToString();
    }

    void IncrementVisits(int visits = 1)
    {
        visitCount += visits;
        visitCountText.text = "Visits: " + visitCount.ToString();
    }

    void IncrementSeconds(int seconds = 1)
    {
        secondCount += seconds;
        secondCountText.text = "Seconds: " + secondCount.ToString();
    }

    void SaveData()
    {
        if (noSavedFile || saveFileSynced)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(Application.persistentDataPath + "/map.dat");

            MapMap data = new MapMap();
            data.saved = "YES";
            data.dict = dict;
            data.homeSquareLat = homeSquareLat;
            data.homeSquareLong = homeSquareLong;

            bf.Serialize(file, data);
            file.Close();
            
            saveFileSynced = true;
            noSavedFile = false;
        }
    }

    void LoadData()
    {
        String saveFilename = Application.persistentDataPath + "/map.dat";

        if (!File.Exists(saveFilename)) {
            Debug.Log("File doesn't exist.");
            noSavedFile = true;
        }
        else if (new FileInfo(saveFilename).Length == 0)
        {
            Debug.Log("File length is zero.");
            noSavedFile = true;
        }
        else
        {
            Debug.Log("Loading File");
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(saveFilename, FileMode.Open);
            MapMap data = (MapMap)bf.Deserialize(file);
            file.Close();
            
            dict = data.dict;

            foreach (KeyValuePair<String,TileData> k in dict)
            {
                IncrementTiles();
                string[] xy = k.Key.Split(',');
                Debug.Log(xy);
                Debug.Log("INCREMENT VISITS BY "+k.Value.visitCount);
                IncrementVisits(k.Value.visitCount);
                IncrementSeconds(k.Value.secondCount);
                groundClone = Instantiate(groundPrefab,
                                 new Vector3(Int32.Parse(xy[0]), 
                                            Int32.Parse(xy[1])), 
                                            Quaternion.identity) as GameObject;
            }

            noSavedFile = false;
            saveFileSynced = true;

            SetHomeGPS(data.homeSquareLat, data.homeSquareLong);
        }
    }
}

[Serializable] 
class MapMap
{
    public string saved = "NO";
    public Dictionary<string, TileData> dict;
    public int homeSquareLat = -91;
    public int homeSquareLong = -181;

    public int getDictVals()
    {
        return dict.Count;
    }
}

[Serializable]
class TileData
{
    public int visitCount;
    public int secondCount;

    public TileData()
    {
        visitCount = 0;
        secondCount = 0;
    }

    public void AddSecond()
    {
        secondCount++;
    }

    public void AddVisit()
    {
        visitCount++;
    }
}