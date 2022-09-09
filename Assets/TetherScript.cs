using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;
using KModkit;

public class TetherScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public TextMesh[] LettersRepresentation;
    public AudioClip[] SFX;
    public TextAsset FiveLetterWords;


    bool focused, Interactable = true;

    string[] Qwerty = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Z", "X", "C", "V", "B", "N", "M" };
    private KeyCode[] TypableKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P,KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M
    };
    private KeyCode[] OtherKeys =
    {
        KeyCode.Return, KeyCode.Backspace
    };

    string[] Selection = new string[] { };
    string[] SelectedWords = new string[6] { "", "", "", "", "", "" };
    int Filled = 0;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;


    void Awake()
    {
        moduleId = moduleIdCounter++;
        GetComponent<KMSelectable>().OnFocus += delegate () { focused = true; };
        GetComponent<KMSelectable>().OnDefocus += delegate () { focused = false; };
        if (Application.isEditor)
            focused = true;
    }

    void Update()
    {
        if (focused && Interactable)
        {
            for (int i = 0; i < TypableKeys.Count(); i++)
            {
                if (Input.GetKeyDown(TypableKeys[i]))
                {
                    Type(i);
                }
            }
            if (Input.GetKeyDown(OtherKeys[1]))
                BackSpace();
            if (Input.GetKeyDown(OtherKeys[0]))
                EnterBypass();
        }
    }

    void Type(int Key)
    {
        if (Filled != 20)
        {
            for (int x = 0; x < LettersRepresentation.Length; x++)
            {
                LettersRepresentation[x].color = ((x / 5 == 0) || (x / 5 == 5)) ? Color.black : new Color(63f / 255f, 63f / 255f, 63f / 255f);
            }

            LettersRepresentation[5 + Filled].text = Qwerty[Key];
            Filled++;
        }
    }

    void EnterBypass()
    {
        StartCoroutine(Enter());
    }

    IEnumerator Enter()
    {
        if (Filled == 20)
        {
            Interactable = false;
            for (int x = 0; x < LettersRepresentation.Length; x++)
            {
                LettersRepresentation[x].color = ((x / 5 == 0) || (x / 5 == 5)) ? Color.black : new Color(63f / 255f, 63f / 255f, 63f / 255f);
            }

            string WordChain = "";
            for (int x = 0; x < 20; x++)
            {
                WordChain = WordChain + LettersRepresentation[5 + x].text + ((x % 5 == 4 && x < 15) ? ", " : "");
            }
            Debug.LogFormat("[Tether #{0}] The word chain you sent: {1}", moduleId, WordChain);

            for (int a = 1; a < SelectedWords.Length - 1; a++)
            {
                string CompareOne = "", CompareTwo = "", CompareThree = "";
                for (int x = 0; x < 5; x++)
                {
                    LettersRepresentation[(a * 5) + x].color = Color.red;
                    CompareOne += LettersRepresentation[((a - 1) * 5) + x].text;
                    CompareTwo += LettersRepresentation[(a * 5) + x].text;
                    CompareThree += LettersRepresentation[((a + 1) * 5) + x].text;
                    Audio.PlaySoundAtTransform(SFX[1].name, transform);
                    yield return new WaitForSecondsRealtime(0.25f);
                }

                if (!CompareTwo.EqualsAny(Selection) || CompareOne.Remove(0, 2) != CompareTwo.Remove(3) || CompareTwo.Remove(0, 2) != CompareThree.Remove(3))
                {
                    Debug.LogFormat("[Tether #{0}] The word {1} did not bind. The module strikes.", moduleId, CompareTwo);
                    Module.HandleStrike();
                    Interactable = true;
                    yield break;
                }

                else
                {
                    for (int x = 0; x < 5; x++)
                    {
                        LettersRepresentation[(a * 5) + x].color = new Color(24f / 255f, 128f / 255f, 0f);
                    }
                    Audio.PlaySoundAtTransform(SFX[0].name, transform);
                }

            }
            Debug.LogFormat("[Tether #{0}] All the words you sent are valid. The module is solved.", moduleId);
            Module.HandlePass();
            ModuleSolved = true;
        }
    }

    void BackSpace()
    {
        if (Filled > 0)
        {
            for (int x = 0; x < LettersRepresentation.Length; x++)
            {
                LettersRepresentation[x].color = ((x / 5 == 0) || (x / 5 == 5)) ? Color.black : new Color(63f / 255f, 63f / 255f, 63f / 255f);
            }

            Filled--;
            LettersRepresentation[5 + Filled].text = "";
        }
    }

    void Start()
    {
        bool Done = false;
        Selection = JsonConvert.DeserializeObject<string[]>(FiveLetterWords.text);
        do
        {
            SelectedWords = new string[6] { "", "", "", "", "", "" };
            List<string> Comparer = new List<string>();
            SelectedWords[0] = Selection[UnityEngine.Random.Range(0, Selection.Length)];
            Comparer.Add(SelectedWords[0]);
            for (int a = 1; a < SelectedWords.Length; a++)
            {
                string LookForABasis = SelectedWords[a - 1].Remove(0, 2);
                List<string> RandomSelection = new List<string>();
                bool TerminateLoop = false;
                for (int x = 0; x < Selection.Length; x++)
                {
                    if (Selection[x].Remove(3) != LookForABasis)
                    {
                        if (TerminateLoop)
                            break;
                        else
                            continue;
                    }
                    TerminateLoop = true;
                    RandomSelection.Add(Selection[x]);
                }
                if (RandomSelection.Count() == 0)
                    break;
                SelectedWords[a] = RandomSelection[UnityEngine.Random.Range(0, RandomSelection.Count())];
                if (SelectedWords[a].EqualsAny(Comparer))
                    break;
                Comparer.Add(SelectedWords[a]);
                if (a == SelectedWords.Length - 1)
                    Done = true;
            }
        }
        while (!Done);
        for (int x = 0; x < LettersRepresentation.Length; x++)
        {
            LettersRepresentation[x].text = ((x / 5 == 0) || (x / 5 == 5)) ? SelectedWords[x / 5][x % 5].ToString() : "";
            LettersRepresentation[x].color = ((x / 5 == 0) || (x / 5 == 5)) ? Color.black : new Color(63f / 255f, 63f / 255f, 63f / 255f);
        }
        Debug.LogFormat("[Tether #{0}] The starting word is {1} / The ending word is {2}", moduleId, SelectedWords[0], SelectedWords[5]);
        Debug.LogFormat("[Tether #{0}] A guranteed word chain is:  {1}, {2}, {3}, {4}", moduleId, SelectedWords[1], SelectedWords[2], SelectedWords[3], SelectedWords[4]);
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To submit your answer on the module, use !{0} submit [4 5-letter words]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            string AllLetters = "QWERTYUIOPASDFGHJKLZXCVBNM";
            if (!Interactable)
            {
                yield return "sendtochaterror You can not interact with the module right now. Command ignored.";
                yield break;
            }

            if (parameters.Length != 5)
            {
                yield return "sendtochaterror Invalid parameter length. Command ignored.";
                yield break;
            }

            for (int x = 1; x < 5; x++)
            {
                if (!parameters[x].ToUpper().ToCharArray().All(c => AllLetters.Contains(c)))
                {
                    yield return "sendtochaterror A word in the command contains an invalid letter. Command ignored.";
                    yield break;
                }

                if (parameters[x].Length != 5)
                {
                    yield return "sendtochaterror A word in the command is not 5 letters long. Command ignored.";
                    yield break;
                }
            }

            while (Filled != 0)
                BackSpace();

            for (int x = 1; x < 5; x++)
            {
                for (int y = 0; y < parameters[x].Length; y++)
                {
                    Type(Array.IndexOf(Qwerty, parameters[x][y].ToString().ToUpper()));
                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            yield return "strike";
            yield return "solve";
            EnterBypass();
        }
    }
}
