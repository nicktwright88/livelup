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

    //lat&long HOLY INFO
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

    //game vars
    bool homeSet = false;
    Vector3 newPosition;
    //static Activity myActivity;

    Dictionary<string, int> dict = new Dictionary<string,int>();

    //used to approximate GPS to game board. Higher = more precision, less stability
    int reducer = 10000; //default 10000

    //used for setting up Service (for running in background)
    AndroidJavaClass unityClass;
    AndroidJavaObject unityActivity;
    AndroidJavaClass customClass;

    // Use this for initialization
    IEnumerator Start ()
    {
        Debug.Log("WHAT'S UP DOC?");
        Debug.Log(Input.location.status);
        /*groundClone = Instantiate(groundPrefab,
                                    new Vector3(rb2.position.x, rb2.position.y), Quaternion.identity) as GameObject;*/

        Input.location.Start();

        // First, check if user has location service enabled
        while (!Input.location.isEnabledByUser)
        {
            Debug.Log("HUH?");
            yield return new WaitForSeconds(1);
        }

        Input.location.Start();

        Debug.Log(Input.location.status);

        //start getting GPS location every second
        InvokeRepeating("getGPSLoc", 0.0f, 1f);

        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("NOT ENABLED");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.Log("Unable to determine device location");
        }

        Debug.Log("Should be working");
        Debug.Log(Input.location.status);

        loadData();
    }
	
	// Update is called once per frame
	void Update ()
    {

        //
        latitude = li.latitude;
        longitude = li.longitude;

        latitudeText.text = latitude.ToString("N6");
        longitudeText.text = longitude.ToString("N6");

        newSquareLat = Mathf.RoundToInt(latitude * reducer);
        squareLatText.text = newSquareLat.ToString();

        newSquareLong = Mathf.RoundToInt(longitude * reducer);
        squareLongText.text = newSquareLong.ToString();


        if (Input.touchCount == 3)
        {
            newSquareLat++;
            newSquareLong++;
        }

        //if latitude/longitude are not both 0, we have GPS lock and can set our home position.
        //if home position is already set, don't set it again.
        if (homeSquareLat == -91 && li.latitude != 0 && homeSquareLong == -181 && li.longitude != 0)
        {
            Invoke("setHomeGPS", 1);
        }
        
        if (!homeSet)
        {
            if (Input.GetKeyDown("w")) {
                rb2.MovePosition(new Vector3(rb2.position.x, rb2.position.y + 1));
                loadPosition(rb2.position.x, rb2.position.y);
            }
            else if (Input.GetKeyDown("a")) {
                rb2.MovePosition(new Vector3(rb2.position.x - 1, rb2.position.y));
                loadPosition(rb2.position.x, rb2.position.y);
            }
            else if (Input.GetKeyDown("s")) {
                rb2.MovePosition(new Vector3(rb2.position.x, rb2.position.y - 1));
                loadPosition(rb2.position.x, rb2.position.y);
            }
            else if (Input.GetKeyDown("d")) {
                rb2.MovePosition(new Vector3(rb2.position.x + 1, rb2.position.y));
                loadPosition(rb2.position.x, rb2.position.y);
            }

        } else if (newSquareLat != currSquareLat || newSquareLong != currSquareLong)
        {
            int zeroNewSquareLong = newSquareLong - homeSquareLong;
            int zeroNewSquareLat = newSquareLat - homeSquareLat;

            updatePosition(zeroNewSquareLong, zeroNewSquareLat);

            if (zeroNewSquareLong != 0 || zeroNewSquareLat != 0)
            {
                loadPosition(zeroNewSquareLong, zeroNewSquareLat);
                //groundClone = Instantiate(groundPrefab,
                  //              new Vector3(zeroNewSquareLong, zeroNewSquareLat), Quaternion.identity) as GameObject;
            }
        }
    }

    private void OnApplicationQuit()
    {
        saveData();
    }

    private void OnApplicationPause(bool pause)
    {
        if (homeSet)
        {
            saveData();
        }
    }

    void getGPSLoc()
    {
        if (Input.location.status != LocationServiceStatus.Running)
        {
            /*
            Debug.Log("WHY DOES THIS NOT WORK");
            print("WHY DOES THIS NOT WORK");

            Debug.Log(Input.location.status);
            print(Input.location.status);*/

        } else
        {
            li = Input.location.lastData;
        }
    }

    void setHomeGPS()
    {
        homeSquareLat = newSquareLat;
        homeSquareLong = newSquareLong;
        homeSquareLongText.text = homeSquareLong.ToString();
        homeSquareLatText.text = homeSquareLat.ToString();
        homeSet = true;
    }

    // 0/w = up, 1/a = left, 2/s = down, 3/d = right
    void updatePosition(int zeroNewSquareLong, int zeroNewSquareLat)
    {
        newPosition = new Vector3(zeroNewSquareLong, zeroNewSquareLat);
        rb2.MovePosition(newPosition);

        currSquareLat = newSquareLat;
        currSquareLong = newSquareLong;
    }

    void loadPosition(float rbx, float rby)
    {
        string currentPosition = rbx + "," + rby;
        if (!dict.ContainsKey(currentPosition))
        {
            dict.Add(currentPosition, 1);
            groundClone = Instantiate(groundPrefab,
                            new Vector3(rbx, rby), Quaternion.identity) as GameObject;
        } else
        {
            dict[currentPosition]++;
        }
    }

    void saveData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/map.dat");

        mapMap data = new mapMap();
        data.saved = "YES";
        data.dict = dict;

        bf.Serialize(file, data);
        file.Close();
    }

    void loadData()
    {
        Debug.Log("Map file Length: " + new FileInfo(Application.persistentDataPath + "/map.dat").Length);

        if(File.Exists(Application.persistentDataPath + "/map.dat") && new FileInfo(Application.persistentDataPath + "/map.dat").Length != 0)
        {
            
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/map.dat", FileMode.Open);
            mapMap data = (mapMap)bf.Deserialize(file);
            file.Close();
            
            dict = data.dict;

            //List<string> dictlist = new List<string>(data.dict.Keys);

            foreach (KeyValuePair<String,int> k in dict)
            {
                string[] xy = k.Key.Split(',');
                groundClone = Instantiate(groundPrefab,
                                 new Vector3(Int32.Parse(xy[0]), 
                                            Int32.Parse(xy[1])), 
                                            Quaternion.identity) as GameObject;
            }
        }
    }
}

[Serializable] 
class mapMap
{
    public string saved = "NO";
    public Dictionary<string, int> dict;

    public int getDictVals()
    {
        return dict.Count;
    }
}