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
    public Text playerLevelText;
    public Text playerPointCountText;
    public Text currentTileLevelText;
    public Text currentTileSecondCountText;
    public Text currentTileVisitCountText;

    //Unity UI Elements
    public Slider playerLevelBar;
    public Slider tileLevelBar;

    //lat&long
    LocationInfo li;

    //Measurements
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
    int playerLevel = 0;
    int playerPointCount = 0;

    //game vars
    bool homeSet = false;
    Vector3 newPosition;
    bool noSavedFile = false;
    bool saveFileSynced = false;
    String saveFilename;
    String backupFilename;
    int[] levelPoints = new int[101];

    Dictionary<string, TileData> dict = new Dictionary<string,TileData>();

    //used to approximate GPS to game board. Higher = more precision, less stability
    int reducer = 10000; //default 10000

    //used for setting up Service (for running in background) (not working right now)
    AndroidJavaClass unityClass;
    AndroidJavaObject unityActivity;
    AndroidJavaClass customClass;

    // Use this for initialization
    IEnumerator Start ()
    {
        //loading data (if any)
        saveFilename = Application.persistentDataPath + "/map.dat";
        backupFilename = Application.persistentDataPath + "/mapB.dat";

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

        //log current "successful" location status
        Debug.Log(Input.location.status);

        //start getting GPS location every second
        Debug.Log("Grabbing GPS Location every second");
        InvokeRepeating("GetGPSLoc", 0.0f, 1f);

        //start saving the game every 10 second
        Debug.Log("Saving the game every 10 seconds");
        InvokeRepeating("SaveData", 0.0f, 10f);

        while (newSquareLat == 0 && newSquareLong == 0)
        {
            Debug.Log("Waiting for current GPS Location...");
            yield return new WaitForSeconds(1);
        }

        //Set up array of total point values for each level
        for (int i = 0; i <= 100; i++)
        {
            levelPoints[i] = (int)Math.Round((double)((4 * Math.Pow(i,3)) / 5));
            if (i > 0)
            {
                levelPoints[i] += levelPoints[i - 1];
            }
        }

        playerLevelBar.value = CalculatePlayerLevelBar();
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
            Debug.Log("Location Services Status not Running. Status: " + Input.location.status);
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
                UpdatePosition(newSquareLong, newSquareLat);
            } else
            {
                string currentPosition = GetCurrentPositionText();
                IncrementSeconds(currentPosition);
            }
        }
    }

    void SetHomeGPS(int setSquareLat = -91, int setSquareLong = -181)
    {
        Debug.Log("SETTING HOME VALUES");
        
        homeSquareLat = setSquareLat;
        homeSquareLong = setSquareLong;
        homeSquareLatText.text = homeSquareLat.ToString();
        homeSquareLongText.text = homeSquareLong.ToString();
        homeSet = true;
    }

    void UpdatePosition(int newSquareLong, int newSquareLat)
    {
        int zeroNewSquareLat = newSquareLat - homeSquareLat;
        int zeroNewSquareLong = newSquareLong - homeSquareLong;

        newPosition = new Vector3(zeroNewSquareLong, zeroNewSquareLat);
        rb2.position = newPosition;

        currSquareLat = newSquareLat;
        currSquareLong = newSquareLong;
        
        UpdateTile(zeroNewSquareLong, zeroNewSquareLat);

        string currentPosition = zeroNewSquareLong + "," + zeroNewSquareLat;

        DisplayCurrentTileInfo(currentPosition);
    }

    void UpdateTile(float rbx, float rby)
    {
        
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
        }
        IncrementSeconds(currentPosition);
        IncrementVisits(currentPosition);
    }

    void IncrementTiles()
    {
        tileCount++;
        tileCountText.text = "Total Tiles: " + tileCount.ToString();
    }

    void IncrementVisits(string currentPosition)
    {
        //Add a visit to the tile
        dict[currentPosition].AddVisit();
        visitCount++;
        visitCountText.text = "Total Visits: " + visitCount.ToString();

        //checks the total points to determine the correct level
        int newLevel = CheckNextLevel(dict[currentPosition].secondCount, dict[currentPosition].tileLevel);

        //Add the tile level to the player points
        AddPointsToPlayer(dict[currentPosition].tileLevel);
    }

    //seconds act as points for each tile
    void IncrementSeconds(String currentPosition)
    {
        dict[currentPosition].AddSecond();
        secondCount++;
        secondCountText.text = "Total Seconds: " + secondCount.ToString();

        if (currentPosition.Equals(GetCurrentPositionText()))
        {
            currentTileSecondCountText.text = "Current Tile Seconds: " + dict[currentPosition].secondCount.ToString();
        }

        //checks the total points to determine the correct level
        int newLevel = CheckNextLevel(dict[currentPosition].secondCount, dict[currentPosition].tileLevel);

        if (newLevel > dict[currentPosition].tileLevel)
        {
            SetTileLevel(currentPosition, newLevel);
        }

        tileLevelBar.value = CalculateTileLevelBar();
    }

    void SetTileLevel(String currentPosition, int newLevel)
    {
        dict[currentPosition].tileLevel = newLevel;

        if (currentPosition.Equals(GetCurrentPositionText()))
        {
            currentTileLevelText.text = "Current Tile Level: " + dict[currentPosition].tileLevel.ToString();
        }

        tileLevelBar.value = CalculateTileLevelBar();
    }

    public void AddPointsToPlayer(int tileLevel)
    {
        playerPointCount += tileLevel;
        int newLevel = CheckNextLevel(playerPointCount, playerLevel);

        Debug.Log("INITIAL!");

        Debug.Log(newLevel);

        if (newLevel > playerLevel)
        {
            SetPlayerLevel(newLevel);
        }
        playerPointCountText.text = "Player Points: " + playerPointCount.ToString();
        playerLevelBar.value = CalculatePlayerLevelBar();
    }

    public void SetPlayerLevel(int newLevel)
    {
        playerLevel = newLevel;
        playerLevelText.text = "Player Level: " + newLevel.ToString();
        playerLevelBar.value = CalculatePlayerLevelBar();
    }

    public String GetCurrentPositionText()
    {
        int zeroNewSquareLat = newSquareLat - homeSquareLat;
        int zeroNewSquareLong = newSquareLong - homeSquareLong;

        return zeroNewSquareLong + "," + zeroNewSquareLat;
    }

    public float CalculatePlayerLevelBar()
    {
        float pointsSoFarThisLevel = (float)playerPointCount - (float)levelPoints[playerLevel];
        float fullPointsThisLevel = (float)levelPoints[playerLevel + 1] - (float)levelPoints[playerLevel];
        return pointsSoFarThisLevel / fullPointsThisLevel;
    }

    public float CalculateTileLevelBar()
    {
        String currentPosition = GetCurrentPositionText();


        float pointsSoFarThisLevel = (float)dict[currentPosition].secondCount - (float)levelPoints[dict[currentPosition].tileLevel];
        float fullPointsThisLevel = (float)levelPoints[dict[currentPosition].tileLevel + 1] - (float)levelPoints[dict[currentPosition].tileLevel];
        return pointsSoFarThisLevel / fullPointsThisLevel;
    }

    //for use during every new tile visit
    public void DisplayCurrentTileInfo(String currentPosition)
    {
        currentTileLevelText.text = "Current Tile Level: " + dict[currentPosition].tileLevel.ToString();
        currentTileSecondCountText.text = "Current Tile Seconds: " + dict[currentPosition].secondCount.ToString();
        currentTileVisitCountText.text = "Current Tile Visits: " + dict[currentPosition].visitCount.ToString();
        tileLevelBar.value = CalculateTileLevelBar();
    }

    //for use during initialization only
    void AddTotalVisits(int visits)
    {
        visitCount += visits;
        visitCountText.text = "Total Visits: " + visitCount.ToString();
    }
    
    //for use during initialization only
    void AddTotalSeconds(int seconds)
    {
        secondCount += seconds;
        secondCountText.text = "Total Seconds: " + secondCount.ToString();
        tileLevelBar.value = CalculateTileLevelBar();
    }

    //for use during Initialization only
    void SetTotalPlayerPoints(int points)
    {
        playerPointCount += points;
        playerPointCountText.text = "Player Points: " + playerPointCount.ToString();
        playerLevelBar.value = CalculatePlayerLevelBar();
    }

    public int CheckNextLevel(int points, int level)
    {
        
        int newLevel = -1;
        if (levelPoints[level] > points)
        {
            //TODO?: Return CORRECT level if somehow the level is too high. This should never happen, though.
            newLevel = -1;
        } else {
            for (int i = level + 1; i <= 100; i++)
            {
                if (newLevel == -1 && levelPoints[i] > points)
                {
                    newLevel = i-1;
                }
            }
            //newLevel = 100;
        }
        return newLevel;
    }

    void SaveData()
    {
        String saveFilename = Application.persistentDataPath + "/map.dat";
        if (noSavedFile || saveFileSynced)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(saveFilename);
            FileStream backupFile = File.Create(backupFilename);

            MapMap data = new MapMap();
            data.saved = "YES";
            data.dict = dict;
            data.playerLevel = playerLevel;
            data.playerPointCount = playerPointCount;
            data.homeSquareLat = homeSquareLat;
            data.homeSquareLong = homeSquareLong;

            if (!noSavedFile && saveFileSynced)
            {
                bf.Serialize(backupFile, data);
                long backupLength = new FileInfo(backupFilename).Length;
                long saveLength = new FileInfo(saveFilename).Length;
                if (backupLength > saveLength)
                {
                    System.IO.File.Copy(backupFilename, saveFilename, true);
                }
            } else if (saveFileSynced)
            {
                bf.Serialize(file, data);
            }

            file.Close();
            backupFile.Close();
            
            saveFileSynced = true;
            noSavedFile = false;
        }
    }

    void LoadData()
    {
        if (!File.Exists(saveFilename)) {
            Debug.Log("File doesn't exist.");
            noSavedFile = true;
        }
        else if (new FileInfo(saveFilename).Length == 0 &&
                    new FileInfo(backupFilename).Length == 0)
        {
            Debug.Log("File length is zero.");
            noSavedFile = true;
        }
        else
        {
            Debug.Log("Loading File");
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file;

            if (new FileInfo(backupFilename).Length < new FileInfo(saveFilename).Length)
            {
                file = File.Open(saveFilename, FileMode.Open);
            } else {
                file = File.Open(backupFilename, FileMode.Open);
            }

            MapMap data = (MapMap)bf.Deserialize(file);
            file.Close();
            
            dict = data.dict;

            foreach (KeyValuePair<String,TileData> k in dict)
            {
                IncrementTiles();
                string[] xy = k.Key.Split(',');

                AddTotalVisits(k.Value.visitCount);
                AddTotalSeconds(k.Value.secondCount);
                groundClone = Instantiate(groundPrefab,
                                 new Vector3(Int32.Parse(xy[0]), 
                                            Int32.Parse(xy[1])), 
                                            Quaternion.identity) as GameObject;
            }

            noSavedFile = false;
            saveFileSynced = true;

            SetHomeGPS(data.homeSquareLat, data.homeSquareLong);

            SetPlayerLevel(data.playerLevel);

            SetTotalPlayerPoints(data.playerPointCount);
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
    public int playerLevel = 0;
    public int playerPointCount = 0;

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
    public int tileLevel;

    public TileData()
    {
        visitCount = 0;
        secondCount = 0;
        tileLevel = 0;
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