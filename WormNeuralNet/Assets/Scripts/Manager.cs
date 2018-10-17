using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using Mono.Data.SqliteClient;

public class Manager : MonoBehaviour
{

    public GameObject boomerPrefab;
    public GameObject hex;
    public GameObject stacy; //our beautiful second hexagon cause she begone

    private String dbName = "URI=file:data.db";
    private SqliteConnection sqliteConnection;
    private SqliteCommand sqliteCommand;
    private SqliteDataReader sqliteDataReader;
    //public GameObject canvas;
    private bool isTraning = false;
    private bool rightMouseDown = false;
    private bool spaceDown = false;

    private int populationSize = 100; //ADJUSTABLE: (don't go above 400 with normal time)
    private int generationNumber = 0;

    private int[] layers = new int[] { 3, 10, 10, 1 }; //3 inputs and 1 output
    private List<NeuralNetwork> nets;
    private List<Boomerang> boomerangList = null;
    public List<float> distances = null;

    public float overallFitness = 1;
    public int m;
    public float median;
    public float medianSum;
    public double normalizedOverallFitness;
    public float averageMedian;

    private float firstQuartileAverage;
    private float secondQuartileAverage;
    private float thirdQuartileAverage;
    private float fourthQuartileAverage;
    private float fourthQuartileSum;
    private float thirdQuartileSum;
    private float secondQuartileSum;
    private float firstQuartileSum;

    public int textFileNumber = 1;
    public int generationCounter;

    

    public void Start()
    { 
        CreateDataBase();
    }

    public void CreateDataBase()
    {
        string sqltext = "CREATE TABLE IF NOT EXISTS data ('id' INTEGER PRIMARY KEY AUTOINCREMENT, 'generation' INTEGER NOT NULL, 'normalizedOverallFitness' REAL NOT NULL, 'overallFitness' REAL NOT NULL, 'averageMedian' REAL NOT NULL)";
        sqliteConnection = new SqliteConnection(dbName);
        sqliteConnection.Open();
        sqliteCommand = new SqliteCommand(sqltext, sqliteConnection);
        sqliteCommand.ExecuteNonQuery();
        sqliteConnection.Close();
    }


    public void Timer() //sets training status to false once timer is reached
    {
        isTraning = false;
    }

    public void OnGUI()
    {
        //generation
        GUI.Label(new Rect(0, 0, 400, 300), "Generation:");
        GUI.Label(new Rect(0, 15, 400, 300), generationNumber.ToString());
        //fitness
        GUI.Label(new Rect(0, 30, 400, 300), "Fitness:");
        GUI.Label(new Rect(0, 45, 400, 300), (overallFitness / m).ToString());
        //normalized fitness
        GUI.Label(new Rect(0, 60, 400, 300), "Normalized Fitness:");
        GUI.Label(new Rect(0, 75, 400, 300), normalizedOverallFitness.ToString());
        //amount of worms
        GUI.Label(new Rect(0, 90, 400, 300), "Amount of worms (worse half gets sorted out each generation):");
        GUI.Label(new Rect(0, 105, 400, 300), populationSize.ToString());
        //median distance
        GUI.Label(new Rect(0, 120, 400, 300), "Median distance of last Generation");
        GUI.Label(new Rect(0, 135, 400, 300), median.ToString());
        //noise lvl
        GUI.Label(new Rect(0, 150, 400, 300), "Current Noise Level");
        GUI.Label(new Rect(0, 165, 400, 300), MENU.noise.ToString());
        //generation at which simulation stops
        GUI.Label(new Rect(0, 180, 400, 300), "Amount of Generations until inevitable Death:");
        GUI.Label(new Rect(0, 195, 400, 300), MENU.maxGenerationNumber.ToString());
    }

    public void Update()
    {
        //setTime();
        if (isTraning == false) //initialized nets in the first (0th) generation
        {
            if (generationNumber == 0)
            {
                InitBoomerangNeuralNetworks();
                generationNumber++;
            }
            else //adjust m < amount of repetitions per neural net, 1
            {
                if (m < 10)
                {
                    Train();
                }
                else
                {
                    nets.Sort();
                    //FitnessExport();
                    InsertData(generationNumber, normalizedOverallFitness, (overallFitness / m), averageMedian);
                    median = 0;
                    //populationAdjust();
                    for (int i = 0; i < populationSize / 2; i++) //runs through half of the population
                    {
                        nets[i] = new NeuralNetwork(nets[i + (populationSize / 2)]);
                        nets[i].Mutate();

                        nets[i + (populationSize / 2)] = new NeuralNetwork(nets[i + (populationSize / 2)]); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
                    }
                    for (int i = 0; i < populationSize; i++) //fitness value is reset for each net
                    {
                        nets[i].SetFitness(0f);
                    }
                    generationNumber++;
                    m = 0;
                    overallFitness = 0;
                    medianSum = 0;
                    median = 0;
                    if(generationNumber == MENU.maxGenerationNumber)
                    {
                        Time.timeScale = 0;
                    }
                }



            }
        }
        if (Input.GetMouseButtonDown(1)) //responsible for teleporting the hexagon (scent) to the mouse cursor when clicked
        {
            rightMouseDown = true;
        }
        if(Input.GetKeyDown("space"))
        {
            spaceDown = true;
        }
        if (Input.GetMouseButtonUp(1)) //same
        {
            rightMouseDown = false;
        }
        if(Input.GetKeyUp("space"))
        {
            spaceDown = false;
        }

        if (rightMouseDown == true) //same
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            hex.transform.position = mousePosition;
        }
        if(spaceDown == true)
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            stacy.transform.position = mousePosition;
        }
    }

    public void Train() //spawns hexagon (scent) at random place, introduced training sequence and prints out the amount of repetitions so far
    {
        isTraning = true; //sets training status to true
        if (m > 0)
        {
            if (boomerangList != null)
            {
                for (int i = 0; i < boomerangList.Count; i++)
                {
                    distances.Add(boomerangList[i].normalizedDistance);
                }
                distances.Sort();
                median = distances[populationSize / 2];
                float firstQuartile = distances[0];
                float secondQuartile = distances[populationSize / 4];
                float thirdQuartile = distances[3*populationSize / 4];
                float fourthQuartile = distances[populationSize-1];
                for (int i = 0; i < populationSize; i++)
                {
                    overallFitness += (boomerangList[i].actualDistance) / populationSize;
                }
                normalizedOverallFitness = (1 - (Math.Exp(((double)overallFitness) / (m * 20)) - Math.Exp(-((double)overallFitness) / (m * 20))) / (Math.Exp(((double)overallFitness) / (m * 20)) + Math.Exp(-((double)overallFitness) / (m * 20))));
                //MonoBehaviour.print(overallFitness);
                medianSum += median;
                averageMedian = medianSum / m;

                firstQuartileSum += firstQuartile;
                firstQuartileAverage = firstQuartileSum / m;
                secondQuartileSum += secondQuartile;
                secondQuartileAverage = secondQuartileSum / m;
                thirdQuartileSum += thirdQuartile;
                thirdQuartileAverage = thirdQuartileSum / m;
                fourthQuartileSum += fourthQuartile;
                fourthQuartileAverage = fourthQuartileSum / m;
            }
        }
        distances.Clear();
        hex.transform.position = new Vector3(UnityEngine.Random.Range(30f, 30f), UnityEngine.Random.Range(30f, 30f), 0);
        stacy.transform.position = new Vector3(UnityEngine.Random.Range(-30f, -30f), UnityEngine.Random.Range(-30f, -30f), 0);
        Invoke("Timer", 20f); //adjust to change how long each run takes (give time in seconds)
        CreateBoomerangBodies(); //creates worms (duh)
        m += 1;
    }


    public void CreateBoomerangBodies() //creates boomerang bodies (duh again)
    {
        //destroys all boomerangs
        if (boomerangList != null)
        {
            for (int i = 0; i < boomerangList.Count; i++)
                GameObject.Destroy(boomerangList[i].gameObject);
        }

        boomerangList = new List<Boomerang>();
        //creates new boomerangs
        for (int i = 0; i < populationSize; i++)
        {
            Boomerang boomer = (Instantiate(boomerPrefab, new Vector3(UnityEngine.Random.Range(-30f, 30f), UnityEngine.Random.Range(-30f, 30f), 0), boomerPrefab.transform.rotation)).GetComponent<Boomerang>();
            boomer.Init(nets[i], hex.transform, stacy.transform);
            boomerangList.Add(boomer);
        }

    }

    void InitBoomerangNeuralNetworks() //obvious enough, initialized neural net
    {

        nets = new List<NeuralNetwork>();


        for (int i = 0; i < populationSize; i++) //mutates networks
        {
            NeuralNetwork net = new NeuralNetwork(layers);
            net.Mutate();
            nets.Add(net);
        }
        Time.timeScale = 1;
    }
/*
    public void FitnessExport() //creates a .txt file and adds data to it
    {

        if ((generationNumber-generationCounter) > 200)
        {
            textFileNumber += 1;
            generationCounter = generationNumber;
        }
        // Create an instance of StreamWriter to write text to a file.
        // The using statement also closes the StreamWriter.
        using (StreamWriter sw = new StreamWriter("TestFile" + textFileNumber.ToString() + ".txt", true))
        {
            // Add some text to the file.
            sw.WriteLine(normalizedOverallFitness.ToString() +" : "+ (overallFitness / m).ToString() +" : "+ generationNumber.ToString() + " : " + firstQuartileAverage.ToString() + " : " + secondQuartileAverage + averageMedian.ToString() +" : "+ thirdQuartileAverage +" : "+ fourthQuartileAverage );
            sw.Close();
        }
        
        
    }
*/
    public void InsertData(int generation, double normalizedOverallFitness, float fitness, float averageMedian)
    {
        sqliteConnection = new SqliteConnection(dbName);
        sqliteConnection.Open();
        sqliteCommand = new SqliteCommand("INSERT INTO data (generation, normalizedOverallFitness, overallFitness, averageMedian) VALUES (@generation, @normalizedOverallFitness, @overallFitness, @averageMedian);", sqliteConnection);
        sqliteCommand.Parameters.Add("@generation", generation);
        sqliteCommand.Parameters.Add("@normalizedOverallFitness", normalizedOverallFitness);
        sqliteCommand.Parameters.Add("@overallFitness", fitness);
        sqliteCommand.Parameters.Add("@averageMedian", averageMedian);
        sqliteCommand.ExecuteNonQuery();
        sqliteConnection.Close();
    }
}

