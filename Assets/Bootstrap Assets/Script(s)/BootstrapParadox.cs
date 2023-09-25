using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class BootstrapParadox : MonoBehaviour
{

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

    //The words that determine the timeline you are in.
    private static readonly string[][] Timelines = {
        new string[] { "Time", "Door", "Gang", "Nerf", "Ship", "Lips", "Cook", "Book", "Pops", "Dead", "Card", "Hats", "Punk", "Bomb", "Play", "Boot", "Dude" },
        new string[] { "Line", "Lock", "Hood", "Dart", "Yard", "Chap", "Heat", "Read", "Soda", "Beat", "Deck", "Caps", "Rock", "Bang", "Game", "Shoe", "Rock" }
    };

    //Holds current digit inputs for what the dials are showing. Would recommend maybe also using this for time submission.
    private readonly int[] DialDigits = new[] { 0, 0, 0, 0, 0 };

    //Holds the Numeric position of what letter is displayed/should be entered into the message screen.
    private int LetterPosition = 0;

    //Determines if any dial is rotating. Used to prevent spamming of dial buttons to keep things from getting out of hand and keeping numbers from incrementing numbers when the module isn't ready.
    private bool rotating;

    //Used to tell whether the module has activated yet.
    private bool activated;

    //Used to determine if the message coroutine is currently active.
    private bool showingMessage;

    //Used to tell whether the message has been sent.
    private bool messageShown;

    //Used to determine if a strike just happened.
    private bool strikeHappened;

    //Contains whether the module is solved.
    private bool moduleSolved;

    //The last time the module gave a warning sound is stored here.
    private int storedTime = -1;

    //Received time for the message.
    private int receivedTime;

    //Correct time to submit the message.
    private int submitTime;

    //Stores the index of the timeline used.
    private int usedTimeline;

    //Contains the message shown on the module.
    private string message;

    //Contains the encrypted destination timeline.
    private string newMessage;

    //Used for quickly enabling additional logging
    private bool testing = false;

    private bool challengeMode = true;

    private string[] hints = new[] {
        "Glfimrjfvg", //Tourniquet
        "Yvztmzw", //Bandage
        "N.E, NW.S, W.NW, SW.SE, W.SE, NW.N, SW.NW, NW.N, NW.NE, NW.N, SW.NW, W.NW, SW.SE", //Constitution
        "tzbariearg", //Government
        "K**e**y**bo**ar**d**s mak**e f**or** a**n ex**c**el**l**en**t** w**ay** of com**m**un**ic**a**t**ing w**ith** ot**he**r**s**", //Legislation
        "jevbu", //Glove
        "Many **p**eople **a**re **a**lle**r**gic to t**h**ings su**c**h **as** p**e**a**nu**t b**utt**er and **w**as**ps**", //Baseball Hat
        "fgnqvhz", //Stadium
        "N.E, W.SW, SW.S, W.E, N.S, N.S, W.E, NW.SW, N.SE, S.NE", //Hard Drive
        "Remo**t**e**s** h**elp** c**o**ntro**l** th**i**ngs **a**t a dis**ta**nce", //Blue Ray
        "kdegalybhu", //Flashdrive
        "Rleeavpu", //Calliope
        "ivizvk", //Reaper
        "N.E, SW.E, NW.NE, W.SE, SW.NW, NW.S, SW.NW, SW.S, SW.SE", //Musician
        "Yrnqre", //Leader
        "Tfrwv", //Guide
        "lhhaht", //Assist
		
		//Flat Hints
        "caty uzrueeuft pudhahtufru, tyu ryleeufjudh iult tyu mahhavf caty jdult ukkvdt lfg kodavoh gubvtavf. yuld mu hln tylt nvo ldu gvafj cuee. soht l eatteu iat kodtyud, lfg xuup lt at! ak nvo cvdx yldg, tyu woleatn vk nvod lfhcudh caee hyvc kvd at!",
        "X = Z",
        "Construction of the number, Composition of the number, and Construction of the a word",
        "Y = N",
        "The key you get is the key you need",
        "I've tried to ensure that everyone is playing fairly.",
        "U = O",
        "C = R",
        "M = M",
        "P = P",
        "@YAGPDB.xyz challenge <plain text>",
        "6 ciphers will be useful during the challenge",
        "Only 1 of them will be useful after the challenge",
        "T = T"
    };

    //Module ID stuff
    static int _moduleIdCounter = 1;
    int _moduleId;

    //Methods Begin Here

    void Awake()
    {
        //Just Module ID Stuff
        _moduleId = _moduleIdCounter++;

        //Handles what dial arrow got pressed. Nothing special here.
        for (int i = 0; i < DialMod.Length; i++)
        {
            var j = i;
            DialMod[i].OnInteract += delegate { DialIncrementer(j, true); return false; };
        }

        for (int i = 0; i < letterMod.Length; i++)
        {
            var j = i;
            letterMod[i].OnInteract += delegate { CycleLetters(j); return false; };
        }

        Enter.OnInteract += delegate { EnterLetter(); return false; };
        Transmit.OnInteract += delegate { TestSolve(); return false; };

        //Handles when the lights turn on
        Module.OnActivate += LightsOn;
    }

    void Start()
    {
        //Empty displays
        InputText.text = "";
        DisplayText.text = "";
        DisplayText.color = Color.black;

        //Calculate message receive time
        int startingTime = (int)BombInfo.GetTime();
        if (startingTime <= 180)
        {
            receivedTime = startingTime;
            Debug.LogFormat("[The Bootstrap Paradox #{0}] Bomb starting time is less than 3 minutes, the received time will be the starting time: ({1}:{2}).", _moduleId, (receivedTime / 60).ToString("000"), (receivedTime % 60).ToString("00"));
        }
        else
        {
            receivedTime = Rnd.Range(startingTime / 10 * 6, startingTime / 10 * 9);
            receivedTime = startingTime - 3; //Uncomment to test in TestHarness easier
            Debug.LogFormat("[The Bootstrap Paradox #{0}] Message due for reception at {1}:{2}.", _moduleId, (receivedTime / 60).ToString("000"), (receivedTime % 60).ToString("00"));
        }
    }

    //Handles calculations for the answer to transmit.
    void CalculateStuff(bool log)
    {
        //Log received time in seconds
        if (testing)
            Debug.LogFormat("[The Bootstrap Paradox #{0}] The received time in seconds is {1}.", _moduleId, receivedTime);

        //Generate the message
        usedTimeline = Rnd.Range(0, Timelines[0].Length);
        usedTimeline = 2;
        message = Rot13Cipher(Timelines[0][usedTimeline]);

        //Calculate submit time
        var time = receivedTime;
        var timeline = Timelines[0][usedTimeline];
        time /= 8;
        if (testing)
            Debug.LogFormat("[The Bootstrap Paradox #{0}] R after divide by 8 is {1}.", _moduleId, time);
        time *= 5;
        if (testing)
            Debug.LogFormat("[The Bootstrap Paradox #{0}] R after multiplying by 5 is {1}.", _moduleId, time);

        if (challengeMode)
            Debug.LogFormat("[The Bootstrap Paradox #{0}] Standard rules have been applied.", _moduleId, time);


        switch (Timelines[0][usedTimeline])
        {
            case "Time":
            case "Lips":
            case "Card":
            case "Punk":
            case "Dude":
                if (timeline != "Punk" || timeline != "Dude")
                {
                    time /= 5;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies", _moduleId);
                }
                if (receivedTime % 2 == 0)
                {
                    time *= 3;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies", _moduleId);
                }
                if (new[] { 2, 3, 5, 7 }.Contains(DigitalRoot(receivedTime)))
                {
                    time /= 2;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies", _moduleId);
                }
                break;

            case "Door":
            case "Cook":
            case "Hats":
            case "Boot":
                if (receivedTime.ToString().Contains("4"))
                {
                    time /= 4;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies", _moduleId);
                }
                if (DigitalRoot(receivedTime) % 2 == 0)
                {
                    time *= 3;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies", _moduleId);
                }
                if (receivedTime % 2 == 1)
                {
                    time /= 2;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies", _moduleId);
                }
                break;

            case "Gang":
            case "Book":
            case "Pops":
            case "Play":
                //string numStr = receivedTime.ToString();
                //string reversedStr = new string(numStr.Reverse().ToArray());
                //Debug.LogFormat("First string to evaluate: {0}. Second String to evaluate is {1}. {2}", numStr, reversedStr, numStr == reversedStr ? "The strings match." : "The strings do not match.");
                //var VarIsPalindrome = numStr == reversedStr;

                var timeStr = receivedTime.ToString();
                if (timeStr.Distinct().Count() < timeStr.Length)
                {
                    time /= 3;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies", _moduleId);
                }

                if (IsPalindrome(receivedTime))
                {
                    time *= 2;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies", _moduleId);
                }
                if (receivedTime.ToString().Contains("3"))
                {
                    time /= 3;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies", _moduleId);
                }
                break;

            //Nerf
            //Ship
            //Dead
            //Bomb
            default:
                if (timeline == "Bang")
                {
                    time /= 2;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 1 applies", _moduleId, time);
                }
                if (new[] {0, 1, 4, 6, 8, 9 }.Contains(DigitalRoot(receivedTime)))
                {
                    time /= 3;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 2 applies", _moduleId, time);
                }
                if (receivedTime.ToString().Contains("8"))
                {
                    time *= 4;
                    if (testing)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies, which makes R: {1}", _moduleId, time);

                    if (challengeMode)
                        Debug.LogFormat("[The Bootstrap Paradox #{0}] Appendix rule 3 applies", _moduleId, time);
                }
                break;
        }
        submitTime = time;
        if (testing)
            Debug.LogFormat("[The Bootstrap Paradox #{0}] (C) at the end of modifications is {1}.", _moduleId, submitTime);

        //Encrypt the destination timeline
        newMessage = Rot13Cipher(Timelines[1][usedTimeline]);

        if (log)
        {
            if (testing)
                Debug.LogFormat("[The Bootstrap Paradox #{0}] You timeline according to the decrypted message is {1}, which makes the destination timeline {2}.", _moduleId, Timelines[0][usedTimeline], Timelines[1][usedTimeline]);

            Debug.LogFormat("[The Bootstrap Paradox #{0}] A new message has been received at {2}:{3}, it is {1}.", _moduleId, message, (receivedTime / 60).ToString("000"), (receivedTime % 60).ToString("00"));
            Debug.LogFormat("[The Bootstrap Paradox #{0}] The dials must be set to {1}:{2}.", _moduleId, (submitTime / 60).ToString("000"), (submitTime % 60).ToString("00"));
            Debug.LogFormat("[The Bootstrap Paradox #{0}] The encrypted message to transmit is {1}.", _moduleId, newMessage);
        }
    }

    void LightsOn()
    {
        activated = true;
    }

    void Update()
    {
        int bombTime = (int)BombInfo.GetTime();
        if (bombTime == receivedTime && !messageShown && activated)
        {
            CalculateStuff(true);
            messageShown = true;
            StartCoroutine(DisplayMode(15f));
            return;
        }
        if ((bombTime == (receivedTime + 60) || bombTime == (receivedTime + 30) || bombTime == (receivedTime + 10) || bombTime == (receivedTime + 3) || bombTime == (receivedTime + 2) || bombTime == (receivedTime + 1)) && storedTime != bombTime && activated)
        {
            storedTime = bombTime;
            Audio.PlaySoundAtTransform("beep", transform);
        }
    }

    //Takes a string and returns the string after applying Rot13 in all caps.
    string Rot13Cipher(string input)
    {
        string result = "";
        input = input.ToUpperInvariant();
        for (int i = 0; i < input.Length; i++)
            result += (char)('A' + (input[i] - 'A' + 13) % 26);
        return result;
    }

    bool IsPalindrome(int num)
    {
        string numStr = num.ToString();
        string reversedStr = new string(numStr.Reverse().ToArray());
        //Debug.LogFormat("First string to evaluate: {0}. Second String to evaluate is {1}. {2}", numStr, reversedStr, numStr == reversedStr ? "The strings match." : "The strings do not match.");
        return numStr == reversedStr;
    }


    //Takes a number and returns the digital root
    int DigitalRoot(int number)
    {
        string combo = number.ToString();
        while (combo.Length > 1)
        {
            int total = 0;
            for (int i = 0; i < combo.Length; i++)
                total += int.Parse(combo.Substring(i, 1));
            combo = total.ToString();
        }
        return int.Parse(combo);
    }

    //This starts the process of rotating the dials. It increments the necessary dial and wraps around as necessary. Dial "1" is the tens seconds so it only counts to 50-ish before wrapping. This also toggles "private bool rotating".
    void DialIncrementer(int dial, bool interact)
    {

        Audio.PlaySoundAtTransform("click", DialMod[dial].transform);
        if (interact)
        {
            DialMod[dial].AddInteractionPunch(_interactionPunchIntensity);
            if (rotating || showingMessage)
                return;
        }
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
        const float duration = .1f;
        float currentTime = 0;
        var direction = 30f;
        while (currentTime <= duration)
        {
            yield return null;
            currentTime += Time.deltaTime;
            DialModels[dial].localRotation = Quaternion.Slerp(originalRotation, Quaternion.Euler((direction + 30) * currentTime / duration - 30, -180, 90), currentTime / duration);
        }

        DialModels[dial].localEulerAngles = new Vector3(0, -180, 90);
        UpdateDials(dial);
    }

    //This updates the Text Meshes on the dials to line up with what ever the DialDigit for that dial is supposed to be along with the other 4 text meshes on the dial that rotated to help make it look like a fluid rotation. The last thing this does is change "bool rotating" to false to allow dials to rotate again.
    void UpdateDials(int dial)
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
            dialToTurn[0].text = ((DialDigits[dial] + 8) % 10).ToString();
            //Top Visible
            dialToTurn[1].text = ((DialDigits[dial] + 9) % 10).ToString();
            //Center
            dialToTurn[2].text = (DialDigits[dial] % 10).ToString();
            //Bottom Visible
            dialToTurn[3].text = ((DialDigits[dial] + 1) % 10).ToString();
            //Bottom Hidden
            dialToTurn[4].text = ((DialDigits[dial] + 2) % 10).ToString();
        }
        else
        {
            //Top Hidden
            dialToTurn[0].text = ((DialDigits[dial] + 4) % 6).ToString();
            //Top Visible
            dialToTurn[1].text = ((DialDigits[dial] + 5) % 6).ToString();
            //Center
            dialToTurn[2].text = (DialDigits[dial] % 6).ToString();
            //Bottom Visible
            dialToTurn[3].text = ((DialDigits[dial] + 1) % 6).ToString();
            //Bottom Hidden
            dialToTurn[4].text = ((DialDigits[dial] + 2) % 6).ToString();
        }
        rotating = false;
    }

    //Cycles Input Display, Nothing special, No animation was need imo.
    void CycleLetters(int direction)
    {

        letterMod[direction].AddInteractionPunch(_interactionPunchIntensity);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, letterMod[direction].transform);
        if (showingMessage || !messageShown || moduleSolved)
            return;
        if (direction == 1)
            LetterPosition = (LetterPosition + 1) % 26;
        else
            LetterPosition = (LetterPosition + 25) % 26;
        InputText.text = ((char)('A' + LetterPosition)).ToString();
    }

    void EnterLetter()
    {
        Enter.AddInteractionPunch(_interactionPunchIntensity);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Enter.transform);
        if (showingMessage || !messageShown || moduleSolved)
            return;
        if (strikeHappened)
        {
            strikeHappened = false;
            DisplayText.text = "";
        }
        if (DisplayText.text.Length > 3)
            DisplayText.text = DisplayText.text.Substring(DisplayText.text.Length - 3, 3);
        DisplayText.text += ((char)('A' + LetterPosition)).ToString();
    }

    void TestSolve()
    {
        Transmit.AddInteractionPunch(_interactionPunchIntensity);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Transmit.transform);
        if (showingMessage || !messageShown || moduleSolved)
            return;
        int bombTime = (int)BombInfo.GetTime();
        Debug.LogFormat("[The Bootstrap Paradox #{0}] The dials read {1}.", _moduleId, DialDigits.Join("").Reverse().Join("").Insert(3, ":"));
        if (DisplayText.text != newMessage || /*(bombTime != submitTime && !noDelay) || */((submitTime / 60).ToString("000") + ":" + (submitTime % 60).ToString("00")) != DialDigits.Join("").Reverse().Join("").Insert(3, ":"))
        {
            strikeHappened = true;
            Debug.LogFormat("[The Bootstrap Paradox #{0}] Transmission rejected, strike! Receiving new message...", _moduleId);
            Module.HandleStrike();

            if (challengeMode)
            {
                Debug.LogFormat("[The Bootstrap Paradox #{0}] Hint: {1}", _moduleId, hints[(int)Math.Floor((double)Rnd.Range(0, hints.Length))]);
            };

            //Generate a new solution
            receivedTime = bombTime;
            CalculateStuff(true);
            DisplayText.color = Color.black;
            InputText.text = "";
            StartCoroutine(DisplayMode(10f));
        }
        else
        {
            moduleSolved = true;
            Debug.LogFormat("[The Bootstrap Paradox #{0}] Transmission accepted, module solved.", _moduleId);
            if (challengeMode)
            {
                Debug.LogFormat("[The Bootstrap Paradox #{0}] Hint: {1}", _moduleId, hints[(int)Math.Floor((double)Rnd.Range(0, hints.Length))]);
            };
            Module.HandlePass();
            Audio.PlaySoundAtTransform("rewarp", transform);
            InputText.text = "";
            StartCoroutine(RewarpText());
        }
    }

    //Handles the 30 second period where the message is shown initially.
    IEnumerator DisplayMode(float time)
    {
        showingMessage = true;
        Audio.PlaySoundAtTransform("warp", transform);
        StartCoroutine(GlitchWarp());
        StartCoroutine(FadeColors(Color.red));
        string formattedTime = (receivedTime / 60).ToString("000") + (receivedTime % 60).ToString("00");
        for (int i = 4; i >= 0; i--)
        {
            while (DialDigits[4 - i] != int.Parse(formattedTime[i].ToString()))
            {
                DialIncrementer(4 - i, false);
                while (rotating) yield return null;
            }
        }
        float t = 0f;
        while (t < time)
        {
            yield return null;
            t += Time.deltaTime;
        }
        DisplayText.text = "";
        formattedTime = "00000";
        for (int i = 4; i >= 0; i--)
        {
            while (DialDigits[4 - i] != int.Parse(formattedTime[i].ToString()))
            {
                DialIncrementer(4 - i, false);
                while (rotating) yield return null;
            }
        }
        InputText.text = ((char)('A' + LetterPosition)).ToString();
        showingMessage = false;
    }

    //Fades the color on the display.
    IEnumerator FadeColors(Color target)
    {
        Color initCol = DisplayText.color;
        float t = 0f;
        while (t < 3f)
        {
            yield return null;
            DisplayText.color = Color.Lerp(initCol, target, t);
            t += Time.deltaTime;
        }
        DisplayText.color = target;
    }

    //Handles glitching the text for a few seconds prior to the 15 second period.
    IEnumerator GlitchWarp()
    {
        string text;
        float t = 0f;
        while (t < 3f)
        {
            yield return null;
            text = "";
            for (int i = 0; i < 4; i++)
                text += (char)('A' + Rnd.Range(0, 26));
            DisplayText.text = text;
            t += Time.deltaTime;
        }
        DisplayText.text = message;
    }

    //Handles glitching the text for once the module solved.
    IEnumerator RewarpText()
    {
        StartCoroutine(FadeColors(Color.black));
        string text;
        float t = 0f;
        while (t < 3f)
        {
            yield return null;
            text = "";
            for (int i = 0; i < 4; i++)
                text += (char)('A' + Rnd.Range(0, 26));
            DisplayText.text = text;
            t += Time.deltaTime;
        }
        string formattedTime = "00000";
        for (int i = 4; i >= 0; i--)
        {
            while (DialDigits[4 - i] != int.Parse(formattedTime[i].ToString()))
            {
                DialIncrementer(4 - i, false);
                while (rotating) yield return null;
            }
        }
        DisplayText.text = "";

    }

    //Twitch Plays Support

    //The help message that contains how to use all commands.
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} enter <l1> (l2)... [Selects the specified letter and then presses the enter button (optionally include multiple letters)] | !{0} dials <#####> [Sets the dials to the specified digits] | !{0} transmit (##:##) [Presses the transmit button (optionally when the bomb timer is '##:##')]";
#pragma warning restore 414

    //Handles the commands sent to the bot.
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*(?:transmit|submit)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
            {
                if (showingMessage || !messageShown)
                {
                    yield return "sendtochaterror A transmission cannot be sent yet!";
                    yield break;
                }
                yield return null;
                Transmit.OnInteract();
            }
            else
            {
                if (!parameters[1].Contains(":"))
                {
                    yield return "sendtochaterror!f The specified time '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                string[] split = parameters[1].Split(':');
                if (split[0].Length == 0 || split[1].Length != 2)
                {
                    yield return "sendtochaterror!f The specified time '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                int temp;
                int temp2 = -1;
                if (!int.TryParse(split[0], out temp) && !int.TryParse(split[1], out temp2))
                {
                    yield return "sendtochaterror!f The specified time '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (int.Parse(split[0]) < 0 || int.Parse(split[1]) < 0)
                {
                    yield return "sendtochaterror The specified time '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (showingMessage || !messageShown)
                {
                    yield return "sendtochaterror A transmission cannot be sent yet!";
                    yield break;
                }
                yield return null;
                while (BombInfo.GetFormattedTime() != parameters[1]) yield return "trycancel";
                Transmit.OnInteract();
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*(?:enter|input)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify at least one letter to select!";
            else
            {
                string letters = "";
                for (int i = 1; i < parameters.Length; i++)
                {
                    var piece = parameters[i].ToUpperInvariant();
                    for (int j = 0; j < piece.Length; j++)
                    {
                        if (piece[j] < 'A' || piece[j] > 'Z')
                        {
                            yield return "sendtochaterror The specified letter '" + parameters[i][j] + "' is invalid!";
                            yield break;
                        }
                    }
                    letters += piece;
                }
                if (showingMessage || !messageShown)
                {
                    yield return "sendtochaterror Letters cannot be selected yet!";
                    yield break;
                }
                yield return null;
                for (int i = 0; i < letters.Length; i++)
                {
                    int ct1 = 0, ct2 = 0, index = LetterPosition;
                    while (index != letters[i] - 'A')
                    {
                        ct1++;
                        index = (index + 1) % 26;
                    }
                    index = LetterPosition;
                    while (index != letters[i] - 'A')
                    {
                        ct2++;
                        index = (index + 25) % 26;
                    }
                    if (ct1 < ct2)
                    {
                        for (int k = 0; k < ct1; k++)
                        {
                            letterMod[1].OnInteract();
                            yield return new WaitForSeconds(.1f);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < ct2; k++)
                        {
                            letterMod[0].OnInteract();
                            yield return new WaitForSeconds(.1f);
                        }
                    }
                    Enter.OnInteract();
                    yield return new WaitForSeconds(.1f);
                }
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*dials\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify the digits to set the dials too!";
            else
            {
                int parsed;
                if (!int.TryParse(parameters[1], out parsed))
                {
                    yield return "sendtochaterror!f The specified set of digits '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (parsed < 0)
                {
                    yield return "sendtochaterror The specified set of digits '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (parameters[1].Length != 5)
                {
                    yield return "sendtochaterror The specified set of digits '" + parameters[1] + "' must be 5 digits long!";
                    yield break;
                }
                if (parameters[1][3] >= '6')
                {
                    yield return "sendtochaterror The 4th digit '" + parameters[1][3] + "' must be in range 0-5!";
                    yield break;
                }
                if (showingMessage || !messageShown)
                {
                    yield return "sendtochaterror The dials cannot be set yet!";
                    yield break;
                }
                yield return null;
                for (int i = 4; i >= 0; i--)
                {
                    while (DialDigits[4 - i] != parameters[1][i] - '0')
                    {
                        while (rotating) yield return "trycancel";
                        DialMod[4 - i].OnInteract();
                    }
                }
            }
        }
    }

    //Handles if the module is autosolved
    IEnumerator TwitchHandleForcedSolve()
    {
        while (showingMessage || !messageShown) yield return true;
        if (DisplayText.text.Length != 0 && !strikeHappened)
        {
            for (int i = 0; i < DisplayText.text.Length; i++)
            {
                if (DisplayText.text[i] != newMessage[i])
                {
                    moduleSolved = true;
                    Module.HandlePass();
                    Audio.PlaySoundAtTransform("rewarp", transform);
                    StartCoroutine(RewarpText());
                    yield break;
                }
            }
        }
        int start = strikeHappened ? 0 : DisplayText.text.Length;
        for (int i = start; i < 4; i++)
            yield return ProcessTwitchCommand("enter " + newMessage[i]);
        string submitDigits = (submitTime / 60).ToString("000") + (submitTime % 60).ToString("00");
        for (int i = 4; i >= 0; i--)
        {
            while (DialDigits[4 - i] != int.Parse(submitDigits[i].ToString()))
            {
                while (rotating) yield return true;
                DialMod[4 - i].OnInteract();
            }
        }
        /*if (!noDelay)
            while ((int)BombInfo.GetTime() != submitTime) yield return true;*/
        Transmit.OnInteract();
    }
}