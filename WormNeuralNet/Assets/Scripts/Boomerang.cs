using UnityEngine;
using System;

public class Boomerang : MonoBehaviour //called boomerang, but is the worm (took some of the code from another project, didn't want to mess up references, that's why it's still boomerang)
{
    private bool initilized = false;
    private Transform hex;
    private Transform stacy;
    private NeuralNetwork net;
    private Rigidbody2D rBody;
    private Material[] mats;

    public float normalizedDistanceHexagon; //normalized distance
    public float normalizedDistanceStacy;
    public float normalizedDistance;
    public float noiseboom;
    public float normalizedDistancePlusNoise; //normalized distance with noise added
    public float distanceOneSecondAgo; //normalized...
    public float distanceTwoSecondsAgo; //normalized...
    public float changeInDistance1sec; //the change in distance from now to one second ago (actually 0.1 seconds ago, but to stay consistant with variable naming)
    public float changeInDistance2sec; //the change in distance from one second ago (actually 0.1 seconds ago) to two seconds ago (actually 0.2 seconds ago)

    public float timer; //probably not needed, but I'm stupid: just used to reset the counter
    public float counter; //continuously counts up to 0.3 seconds and gets reset

    public float actualDistanceHexagon;
    public float actualDistanceStacy;
    public float actualDistance;
    public float colourDistance;

    //not needed yet, for future development

    //public float distanceBackup; 
    //public float individualFitness;



    void Start() //initialized worm body
    {
        rBody = GetComponent<Rigidbody2D>();
        mats = new Material[transform.childCount];
        for (int i = 0; i < mats.Length; i++)
            mats[i] = transform.GetChild(i).GetComponent<Renderer>().material;
    }

    void FixedUpdate()  //don't rename method
    {
        //distanceBackup = normalizedDistance; //ignore, for future stuff

        UpdateDistance(); //updates all distance variables...

        if (initilized == true) //does the continuous calculation of movement etc. whilst simulation is running
        {
            DistanceDelay(); //check distanceDelay Method for description

            ColorDistance(); //fancy coloring

            float[] inputs = { changeInDistance1sec, changeInDistance2sec, normalizedDistancePlusNoise / 100 }; //inputs those three parameters into the neural net (distance/100 because trigger too strong otherwise, can be adjusted)
            float[] output = net.FeedForward(inputs);  //'gives' the inputs to the neural net

            rBody.velocity = 20f * transform.up; //velocity, can be adjusted through variable
            rBody.angularVelocity = 60000f * output[0]; //angular velocity = constant (can be adjusted) * output of neural net

            net.AddFitness(normalizedDistance); //fitnessfunction based on the distance. Could be changes to something else (like change in distance or so)


            //MonoBehaviour.print("fitnessadded: " +distance)
            //MonoBehaviour.print("fitnessadded");  (control print, ignore)
        }
    }

    public float UpdateDistance() //just normalized distance to 1-tanh (close to hexagon [scent] --> distance = 1, far away --> distance = 0) but tanh wasn't available so I had to use trigidentities via exp
    {
        noiseboom = MENU.noise;
        actualDistanceHexagon = Vector2.Distance(transform.position, hex.position); //actual distance between worm and scent
        double odourHexagon = 1 - (Math.Exp(actualDistanceHexagon / 50) - Math.Exp(-actualDistanceHexagon / 50)) / (Math.Exp(actualDistanceHexagon / 50) + Math.Exp(-actualDistanceHexagon / 50));
        normalizedDistanceHexagon = (float)odourHexagon; //1-tanh(actual distance)


        actualDistanceStacy = Vector2.Distance(transform.position, stacy.position); //actual distance between worm and scent
        double odourStacy = 1 - (Math.Exp(actualDistanceStacy / 50) - Math.Exp(-actualDistanceStacy / 50)) / (Math.Exp(actualDistanceStacy / 50) + Math.Exp(-actualDistanceStacy / 50));
        normalizedDistanceStacy = (float)odourStacy; //1-tanh(actual distance)

        normalizedDistance = (normalizedDistanceHexagon + normalizedDistanceStacy);
        normalizedDistancePlusNoise = normalizedDistance + UnityEngine.Random.Range(-noiseboom*normalizedDistance, normalizedDistance *noiseboom);
        actualDistance = (actualDistanceHexagon + actualDistanceStacy);

        return normalizedDistance;
    }
    
    public void DistanceDelay() //'saves' distance 0.1 and 0.2 secs ago, serves as 'memory' of worm
    {
        counter = Time.fixedTime - timer;
        if (counter > 0.3)
        {
            //MonoBehaviour.print("distance: " + distanceBackup + "distance1: " + distanceOneSecondAgo + "distance2: " + distanceTwoSecondsAgo); <-- ignore, bug printout
            timer = Time.fixedTime;

        }

        else if (counter > 0.2)
        {
            UpdateDistance();
        }
        else if (counter > 0.1)
        {
            UpdateDistance();
            distanceOneSecondAgo = normalizedDistancePlusNoise;
        }
        else if (counter > 0)
        {
            UpdateDistance();
            distanceTwoSecondsAgo = normalizedDistancePlusNoise;
        }
        changeInDistance1sec = (normalizedDistance - distanceOneSecondAgo) * 10; //calculates change in distance (duh)
        changeInDistance2sec = (normalizedDistance - distanceTwoSecondsAgo) * 10;

    }

    public void Init(NeuralNetwork net, Transform hex, Transform stacy) //initialized hex and net
    {
        this.stacy = stacy;
        this.hex = hex;
        this.net = net;
        initilized = true;
    }

    public void ColorDistance()
    {
        colourDistance = actualDistance;
        if (colourDistance > 20f)
            colourDistance = 20f;
        for (int i = 0; i < mats.Length; i++)
            mats[i].color = new Color(colourDistance / 20f, (1f - (colourDistance / 20f)), (1f - (colourDistance / 20f)));
    }
}
