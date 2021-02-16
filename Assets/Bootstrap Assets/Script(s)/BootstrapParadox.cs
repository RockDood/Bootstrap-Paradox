using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class BootstrapParadox : MonoBehaviour {

    //Variables

    //Unity Variables
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMBombModule Module;

    //Selectables
    public KMSelectable[] letterMod;
    public KMSelectable[] DialMod;
    public KMSelectable Transmit;
    public KMSelectable Enter;

    //Game Objects

    //These are the Transforms for the dials
    public Transform[] DialModels;

    //Holds the text meshes for each respective dial
    public TextMesh[] Units;
    public TextMesh[] Tens;
    public TextMesh[] Minutes;
    public TextMesh[] M_Tens;
    public TextMesh[] M_Hundreds;

    //Self-explanatory.
    public TextMesh DisplayText;
    public TextMesh InputText;
    private const float _interactionPunchIntensity = .5f;

    //Script Variables

    //reference for Letter Cycling.
    private static string[] Alphabet = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

    //Reference for dial cycling
    private static int[] Numbers = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    //Holds current digit inputs for what the dials are showing. Would recommend maybe also using this for time submission.
    private int[] DialDigits = new[] { 0, 0, 0, 0, 0 };

    //Holds the Numeric position of what letter is displayed/should be entered into the message screen.
    private int LetterPosition = 0;

    //Determines if any dial is rotating. Used to prevent spamming of dial buttons to keep things from getting out of hand and keeping numbers from incrementing numbers when the module isn't ready.
    bool rotating;

    //Module ID stuff
    static int _moduleIdCounter = 1;
    int _moduleId;

    //Methods Begin Here

    void Awake ()
    {
        //Just Module ID Stuff
        _moduleId = _moduleIdCounter++;

        //Handles what dial arrow got pressed. Nothing special here.
        for (int i = 0; i < DialMod.Length; i++)
        {
            var j = i;
            DialMod[i].OnInteract += delegate { DialIncrementer(j); return false; };
        }

        for (int i = 0; i < letterMod.Length; i++)
        {
            var j = i;
            letterMod[i].OnInteract += delegate { CycleLetters(j); return false; };
        }

        Enter.OnInteract += delegate { EnterLetter(); return false; };
        Transmit.OnInteract += delegate { TestSolve(); return false; };
    }

    void Start () 
    {
		
	}

    //This starts the process of rotating the dials. It increments the necessary dial and wraps around as necessary. Dial "1" is the tens seconds so it only counts to 50-ish before wrapping. This also toggles "bool rotating" 
    void DialIncrementer(int dial)
    {
        if (rotating)
            return;
        DialMod[dial].AddInteractionPunch(_interactionPunchIntensity);
        DialDigits[dial]++;

        if (dial != 1)
        {
            if (DialDigits[dial] > 9)
            {
                DialDigits[dial] = 0;
            }
        }
        else
        {
            if (DialDigits[dial] > 5)
            {
                DialDigits[dial] = 0;
            }
        }
        rotating = true;
        StartCoroutine(RotateDial(dial));
    }

    //This (as you'd guess) actually rotates the necessary dials.
    private IEnumerator RotateDial(int dial)
    {
        var originalRotation = DialModels[dial].localRotation;
        const float duration = .25f;
        float currentTime = 0;
        var direction = 30f;
        while (currentTime <= duration)
        {
            yield return null;
            currentTime += Time.deltaTime;
            DialModels[dial].localRotation = Quaternion.Slerp(originalRotation, Quaternion.Euler((direction + 30) * currentTime / duration - 30, -180, 90), currentTime / duration);
        }
        
        DialModels[dial].localEulerAngles = new Vector3(0, -180, 90);
        updateDials(dial);
    }

    //This updates the Text Meshes on the dials to line up with what ever the DialDigit for that dial is supposed to be along with the other 4 text meshes on the dial that rotated to help make it look like a fluid rotation. The last thing this does is change "bool rotating" to false to allow dials to rotate again.
    void updateDials(int dial)
    {
        var dialToTurn = Units;
        switch (dial)
        {
            case 0:
                dialToTurn = Units;
                break;
            case 1:
                dialToTurn = Tens;
                break;
            case 2:
                dialToTurn = Minutes;
                break;
            case 3:
                dialToTurn = M_Tens;
                break;
            case 4:
                dialToTurn = M_Hundreds;
                break;
        }

        if (dial != 1)
        {
            //Top Hidden
            dialToTurn[0].text = Numbers[(DialDigits[dial] + 8) % 10].ToString();
            //Top Visible
            dialToTurn[1].text = Numbers[(DialDigits[dial] + 9) % 10].ToString();
            //Center
            dialToTurn[2].text = Numbers[DialDigits[dial] % 10].ToString();
            //Bottom Visible
            dialToTurn[3].text = Numbers[(DialDigits[dial] + 1) % 10].ToString();
            //Bottom Hidden
            dialToTurn[4].text = Numbers[(DialDigits[dial] + 2) % 10].ToString();
        }
        else
        {
            //Top Hidden
            dialToTurn[0].text = Numbers[(DialDigits[dial] + 4) % 6].ToString();
            //Top Visible
            dialToTurn[1].text = Numbers[(DialDigits[dial] + 5) % 6].ToString();
            //Center
            dialToTurn[2].text = Numbers[DialDigits[dial] % 6].ToString();
            //Bottom Visible
            dialToTurn[3].text = Numbers[(DialDigits[dial] + 1) % 6].ToString();
            //Bottom Hidden
            dialToTurn[4].text = Numbers[(DialDigits[dial] + 2) % 6].ToString();
        }
            rotating = false;
    }

    //Cycles Input Display, Nothing special, No animation was need imo.
    void CycleLetters(int direction)
    {
        letterMod[direction].AddInteractionPunch(_interactionPunchIntensity);
        if (direction == 1)
        {
            LetterPosition++;
            if (LetterPosition > 25)
                LetterPosition = 0;
        }
        else
        {
            LetterPosition--;
            if (LetterPosition < 0)
                LetterPosition = 25;
        }
        InputText.text = Alphabet[(LetterPosition)].ToString();
    }

    void EnterLetter()
    {
        Enter.AddInteractionPunch(_interactionPunchIntensity);
        Debug.Log("Entering Letter");
    }

    void TestSolve()
    {
        Transmit.AddInteractionPunch(_interactionPunchIntensity);
        Debug.Log("Solving...");
    }
}