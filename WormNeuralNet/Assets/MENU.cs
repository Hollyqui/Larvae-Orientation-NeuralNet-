using UnityEngine;

public class MENU : MonoBehaviour
{
    public static float noise;
    public static float maxGenerationNumber;
    // Use this for initialization
    public void slider_TimeAdjust(float newValue)
    {
        Time.timeScale = newValue;
    }
    public void noiseinput(float newValue)
    {
        noise = newValue / 1000;
    }
    public void stopSimulation(float newValue)
    {
        maxGenerationNumber = newValue;
    }

    /*public void Slider_WormAmountAdjust(float amountWorms)
    {
        amount = amountWorms;
        amountTest = (int)amount;
        MonoBehaviour.print("amount of worms: " + amountWorms);
        MonoBehaviour.print("amountTest: " + amountTest);
    }
    */
    // Update is called once per frame
    void Update () {
		
	}
}
