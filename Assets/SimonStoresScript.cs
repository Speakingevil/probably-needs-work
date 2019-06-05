using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class SimonStoresScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;

    public KMSelectable whiteButton;
    public KMSelectable greyButton;
    public KMSelectable blackButton;
    public KMSelectable[] buttons;
    public GameObject screen;
    public Material[] buttonColour;
    public Renderer[] buttonId;

    int D;
    int mode = 0;
    int stage = 1;
    public float flash = 0.4f;
    private int tempPress;
    private int tempHL;
    private IEnumerator[] sequences = new IEnumerator[3];
    private int[] tempNum = new int[3];
    private bool[] alreadyPressed = new bool[6];
    private bool negativeEntry;
    private int[][] step = new int[3][] { new int[6], new int[6], new int[6] };
    private const string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private List<char> uh = new List<char> { 'R', 'G', 'B', 'C', 'M', 'Y' };
    private List<string> flashingColours = new List<string> { };
    private List<string> fc = new List<string> { };
    private char[][] executionOrder = new char[3][] { new char[6] { 'R', 'G', 'B', 'C', 'M', 'Y' }, new char[6] { 'Y', 'B', 'G', 'M', 'C', 'R' }, new char[6] { 'B', 'M', 'R', 'Y', 'G', 'C' } };
    private List<int> selector = new List<int> { 1, 1, 1, 1, 1, 2, 3 };
    private List<int>[] uhh = new List<int>[3] { new List<int> { 0, 1, 2, 3, 4, 5 }, new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 }, new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 } };
    private List<int> subselection0 = new List<int>();
    private List<int> subselection1 = new List<int>();
    private List<int> subselection2 = new List<int>();
    private int[] selection = new int[5];
    private List<char> order = new List<char>();
    private List<string> inputList = new List<string>();
    private char[][] balancedTrits = new char[3][] { new char[6], new char[6], new char[6] };
    private bool[][] lightSeq = new bool[5][] {new bool[6], new bool[6], new bool[6], new bool[6], new bool[6] };
    private string[] finalAnswer = new string[3];

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    //Setting up button interactions
    private void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
        {
            KMSelectable buttonPressed = button;
            button.OnInteract += delegate () { ButtonPress(buttonPressed); return false; };
            KMSelectable buttonHL = button;
            button.OnHighlight += delegate () { HLButton(buttonHL); };
            KMSelectable buttonHLEnd = button;
            button.OnHighlightEnded += delegate () { HLEndButton(buttonHLEnd); };
        }
        whiteButton.OnInteract += delegate () { WButton(); return false; };
        greyButton.OnInteract += delegate () { StartButton(); return false; };
        blackButton.OnInteract += delegate () { BButton(); return false; };
    }

    void Start()
    {
        //Determining initial values
        D = digits.IndexOf(Bomb.GetSerialNumber()[0]) + digits.IndexOf(Bomb.GetSerialNumber()[1]) + digits.IndexOf(Bomb.GetSerialNumber()[2]) + digits.IndexOf(Bomb.GetSerialNumber()[3]) + digits.IndexOf(Bomb.GetSerialNumber()[4]) + digits.IndexOf(Bomb.GetSerialNumber()[5]);
        step[0][0] = Check(digits.IndexOf(Bomb.GetSerialNumber()[2]) * 36 + digits.IndexOf(Bomb.GetSerialNumber()[3]));
        step[1][0] = Check(digits.IndexOf(Bomb.GetSerialNumber()[4]) * 36 + digits.IndexOf(Bomb.GetSerialNumber()[5]));
        step[2][0] = Check(digits.IndexOf(Bomb.GetSerialNumber()[0]) * 36 + digits.IndexOf(Bomb.GetSerialNumber()[1]));
        Debug.LogFormat("[Simon Stores #{1}] D = {0}", D, moduleId);
        Debug.LogFormat("[Simon Stores #{1}] a0 = {0}", step[0][0], moduleId);
        sequences[0] = Sequence0();
        sequences[1] = Sequence1();
        sequences[2] = Sequence2();

        //Randomising button colours
        for (int i = 6; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i);
            order.Add(uh[j]);
            uh.RemoveAt(j);
        }

        //Selecting how many colours flash at each step
        for (int i = 0; i < 5; i++)
        {
            int j = UnityEngine.Random.Range(0, selector.Count);
            selection[i] = selector[j];
            selector.RemoveAt(j);
        }

        //Selecting which colours flash at each step
        for(int i = 0; i < 20; i++)
        {
            if (i < 6)
            {
                int p = UnityEngine.Random.Range(0, uhh[0].Count);
                subselection0.Add(uhh[0][p]);
                uhh[0].RemoveAt(p);
            }
            if(i < 15)
            {
                int q = UnityEngine.Random.Range(0, uhh[1].Count);
                subselection1.Add(uhh[1][q]);
                uhh[1].RemoveAt(q);
            }
            int r = UnityEngine.Random.Range(0, uhh[2].Count);
            subselection2.Add(uhh[2][r]);
            uhh[2].RemoveAt(r);
        }

        //Setting the colours of the buttons
        for (int i = 0; i < 6; i++)
        {
            switch (order[i])
            {
                case 'R':
                    buttonId[i].material = buttonColour[0];
                    break;
                case 'G':
                    buttonId[i].material = buttonColour[1];
                    break;
                case 'B':
                    buttonId[i].material = buttonColour[2];
                    break;
                case 'C':
                    buttonId[i].material = buttonColour[3];
                    break;
                case 'M':
                    buttonId[i].material = buttonColour[4];
                    break;
                case 'Y':
                    buttonId[i].material = buttonColour[5];
                    break;
            }
        }
        //Determining button press order
        for (int i = 0; i < 3; i++)
        {
            //Shifting entries one to the right when yellow is top left
            if (order.IndexOf('Y') == 0)
            {
                char temp = executionOrder[i][5];
                for (int j = 4; j >= 0; j--)
                {
                    executionOrder[i][j + 1] = executionOrder[i][j];
                }
                executionOrder[i][0] = temp;
            }
            //Swapping entries with their complements when red is opposite cyan
            if ((order.IndexOf('R') % 3) == (order.IndexOf('C') % 3))
            {
                for (int j = 0; j < 6; j++)
                {
                    switch (executionOrder[i][j])
                    {
                        case 'R':
                            tempNum[0] = j;
                            break;
                        case 'G':
                            tempNum[1] = j;
                            break;
                        case 'B':
                            tempNum[2] = j;
                            break;
                    }
                }
                for (int j = 0; j < 6; j++)
                {
                    switch (executionOrder[i][j])
                    {
                        case 'C':
                            executionOrder[i][j] = 'R';
                            break;
                        case 'M':
                            executionOrder[i][j] = 'G';
                            break;
                        case 'Y':
                            executionOrder[i][j] = 'B';
                            break;
                    }
                    executionOrder[i][tempNum[0]] = 'C';
                    executionOrder[i][tempNum[1]] = 'M';
                    executionOrder[i][tempNum[2]] = 'Y';
                }
            }
            //Cycling primary entries when green is adjacent to white
            if (order.IndexOf('G') == 0 || order.IndexOf('G') == 5)
            {
                for (int j = 0; j < 6; j++)
                {
                    switch (executionOrder[i][j])
                    {
                        case 'R':
                            tempNum[0] = j;
                            break;
                        case 'G':
                            tempNum[1] = j;
                            break;
                        case 'B':
                            tempNum[2] = j;
                            break;
                    }
                }
                executionOrder[i][tempNum[0]] = 'G';
                executionOrder[i][tempNum[1]] = 'B';
                executionOrder[i][tempNum[2]] = 'R';
            }
            //Cycling secondary entries when magenta is adjacent to black
            if (order.IndexOf('M') == 2 || order.IndexOf('M') == 3)
            {
                for (int j = 0; j < 6; j++)
                {
                    switch (executionOrder[i][j])
                    {
                        case 'C':
                            tempNum[0] = j;
                            break;
                        case 'M':
                            tempNum[1] = j;
                            break;
                        case 'Y':
                            tempNum[2] = j;
                            break;
                    }
                }
                executionOrder[i][tempNum[0]] = 'M';
                executionOrder[i][tempNum[1]] = 'Y';
                executionOrder[i][tempNum[2]] = 'C';
            }
            //Swapping B with its opposite entry when blue and yellow are on the same side
            if ((order.IndexOf('B') < 3 && order.IndexOf('Y') < 3) || (order.IndexOf('B') > 2 && order.IndexOf('Y') > 2))
            {
                for (int j = 0; j < 6; j++)
                {
                    if (executionOrder[i][j] == 'B')
                    {
                        tempNum[0] = j;
                        break;
                    }
                }
                executionOrder[i][tempNum[0]] = executionOrder[i][5 - tempNum[0]];
                executionOrder[i][5 - tempNum[0]] = 'B';
            }
            //Swapping R and Y when red is on the right side
            if (order.IndexOf('R') < 3)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (executionOrder[i][j] == 'R')
                    {
                        tempNum[0] = j;
                    }
                    else if (executionOrder[i][j] == 'Y')
                    {
                        tempNum[1] = j;
                    }
                }
                executionOrder[i][tempNum[1]] = 'R';
                executionOrder[i][tempNum[0]] = 'Y';
            }
            //Swapping G and C when blue is on the left side
            if (order.IndexOf('B') > 2)
            {
                for (int j = 0; j < 6; j++)
                {
                    if (executionOrder[i][j] == 'G')
                    {
                        tempNum[0] = j;
                    }
                    else if (executionOrder[i][j] == 'C')
                    {
                        tempNum[1] = j;
                    }
                }
                executionOrder[i][tempNum[0]] = 'C';
                executionOrder[i][tempNum[1]] = 'G';
            }
        }
        //Determining sequence of operations and setting up button flashes
        for(int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                switch (selection[j])
                {
                    case 1:
                        switch (subselection0[j])
                        {
                            case 0:
                                step[i][j + 1] = Red(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    flashingColours.Add("R");
                                }
                                break;
                            case 1:
                                step[i][j + 1] = Green(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    flashingColours.Add("G");
                                }
                                break;
                            case 2:
                                step[i][j + 1] = Blue(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    flashingColours.Add("B");
                                }
                                break;
                            case 3:
                                step[i][j + 1] = Cyan(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("C");
                                }
                                break;
                            case 4:
                                step[i][j + 1] = Magenta(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("M");
                                }
                                break;
                            case 5:
                                step[i][j + 1] = Yellow(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("Y");
                                }
                                break;
                        }
                        break;
                    case 2:
                        switch (subselection1[j])
                        {
                            case 0:
                                step[i][j + 1] = ReGr(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    flashingColours.Add("RG");
                                }
                                break;
                            case 1:
                                step[i][j + 1] = ReBl(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    flashingColours.Add("RB");
                                }
                                break;
                            case 2:
                                step[i][j + 1] = ReCy(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("RC");
                                }
                                break;
                            case 3:
                                step[i][j + 1] = ReMa(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("RM");
                                }
                                break;
                            case 4:
                                step[i][j + 1] = ReYe(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("RY");
                                }
                                break;
                            case 5:
                                step[i][j + 1] = GrBl(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    flashingColours.Add("GB");
                                }
                                break;
                            case 6:
                                step[i][j + 1] = GrCy(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("GC");
                                }
                                break;
                            case 7:
                                step[i][j + 1] = GrMa(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("GM");
                                }
                                break;
                            case 8:
                                step[i][j + 1] = GrYe(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("GY");
                                }
                                break;
                            case 9:
                                step[i][j + 1] = BlCy(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("BC");
                                }
                                break;
                            case 10:
                                step[i][j + 1] = BlMa(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("BM");
                                }
                                break;
                            case 11:
                                step[i][j + 1] = BlYe(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("BY");
                                }
                                break;
                            case 12:
                                step[i][j + 1] = CyMa(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("CM");
                                }
                                break;
                            case 13:
                                step[i][j + 1] = CyYe(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("CY");
                                }
                                break;
                            case 14:
                                step[i][j + 1] = MaYe(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("MY");
                                }
                                break;
                        }
                        break;
                    case 3:
                        switch (subselection2[j])
                        {
                            case 0:
                                step[i][j + 1] = RGB(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    flashingColours.Add("RGB");
                                }
                                break;
                            case 1:
                                step[i][j + 1] = RGC(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("RGC");
                                }
                                break;
                            case 2:
                                step[i][j + 1] = RGM(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("RGM");
                                }
                                break;
                            case 3:
                                step[i][j + 1] = RGY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("RGY");
                                }
                                break;
                            case 4:
                                step[i][j + 1] = RBC(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("RBC");
                                }
                                break;
                            case 5:
                                step[i][j + 1] = RBM(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("RBM");
                                }
                                break;
                            case 6:
                                step[i][j + 1] = RBY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("RBY");
                                }
                                break;
                            case 7:
                                step[i][j + 1] = RCM(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("RCM");
                                }
                                break;
                            case 8:
                                step[i][j + 1] = RCY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("RCY");
                                }
                                break;
                            case 9:
                                step[i][j + 1] = RMY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('R')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("RMY");
                                }
                                break;
                            case 10:
                                step[i][j + 1] = GBC(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    flashingColours.Add("GBC");
                                }
                                break;
                            case 11:
                                step[i][j + 1] = GBM(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("GBM");
                                }
                                break;
                            case 12:
                                step[i][j + 1] = GBY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("GBY");
                                }
                                break;
                            case 13:
                                step[i][j + 1] = GCM(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("GCM");
                                }
                                break;
                            case 14:
                                step[i][j + 1] = GCY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("GCY");
                                }
                                break;
                            case 15:
                                step[i][j + 1] = GMY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('G')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("GMY");
                                }
                                break;
                            case 16:
                                step[i][j + 1] = BCM(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    flashingColours.Add("BCM");
                                }
                                break;
                            case 17:
                                step[i][j + 1] = BCY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("BCY");
                                }
                                break;
                            case 18:
                                step[i][j + 1] = BMY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('B')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("BMY");
                                }
                                break;
                            case 19:
                                step[i][j + 1] = CMY(step[i][j], i, j + 1);
                                if (i == 2)
                                {
                                    lightSeq[j][order.IndexOf('C')] = true;
                                    lightSeq[j][order.IndexOf('M')] = true;
                                    lightSeq[j][order.IndexOf('Y')] = true;
                                    flashingColours.Add("CMY");
                                }
                                break;
                        }
                        break;
                }
                //Resetting unused (at their current step) values
                step[0][4] = 0;
                step[0][5] = 0;
                step[1][5] = 0;
                if (i == 0)
                {
                    if (0 < j && j < 4)
                    {
                        Debug.LogFormat("[Simon Stores #{0}] a{1} = {2}", moduleId, j, step[0][j]);
                    }
                }
            }
            //Determining final answers
            balancedTrits[i] = BalTer(step[i][i + 3]);
            List<char> answerList = new List<char>();
            for(int j = 0; j < 7; j++)
            {
                if(j < 6)
                {
                    if(balancedTrits[i][5-j] != '0')
                    {
                        answerList.Add(balancedTrits[i][5 - j]);
                        answerList.Add(executionOrder[i][j]);
                    }
                }
                else
                {
                    char[] ansArray = answerList.ToArray();
                    finalAnswer[i] = new string(ansArray);
                }
            }
        }
        for (int i = 0; i < 3; i++)
        {
            fc.Add(flashingColours[i]);
        }
        string[] f = fc.ToArray();
        Debug.LogFormat("[Simon Stores #{0}] The flashing order for stage 1 was {1}", moduleId, String.Join(", ", f));
        Debug.LogFormat("[Simon Stores #{0}] The pressing order for stage 1 was {1}", moduleId, new string(executionOrder[0]));
        Debug.LogFormat("[Simon Stores #{0}] {2} in balanced ternary is {1}", moduleId, new string(balancedTrits[0]), step[0][3]);
        Debug.LogFormat("[Simon Stores #{1}] The correct input for stage 1 was {0}", finalAnswer[stage - 1], moduleId);
        tempNum[2] = 0;
        StartCoroutine(sequences[0]);
    }

    //Pushing the centre button
    void StartButton()
    {
        if (mode == 0)
        {
            mode = 1;
            StopCoroutine(sequences[stage - 1]);
            whiteButton.GetComponentInChildren<Light>().enabled = true;
            greyButton.GetComponentInChildren<Light>().enabled = false;
            greyButton.AddInteractionPunch();
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            for (int i = 0; i < 6; i++)
            {
                buttons[i].GetComponentInChildren<Light>().enabled = false;
            }
        }
        else if (mode == 1)
        {
            mode = 2;
            negativeEntry = false;
            greyButton.AddInteractionPunch();
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            Audio.PlaySoundAtTransform("InputCheck", transform);
            for (int i = 0; i < 6; i++)
            {
                alreadyPressed[i] = false;
                buttons[i].GetComponentInChildren<Light>().enabled = false;
            }
            blackButton.GetComponentInChildren<Light>().enabled = false;
            greyButton.GetComponentInChildren<Light>().enabled = true;
            StartCoroutine(StartButtonMode2());
        }
    }

    //Flashing the sequences
    private IEnumerator Sequence0()
    {
        for (int i = 0; i < 8; i++)
        {
            if (mode == 0)
            {
                if (i % 2 == 1)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        buttons[j].GetComponentInChildren<Light>().enabled = false;
                        greyButton.GetComponentInChildren<Light>().enabled = false;
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        greyButton.GetComponentInChildren<Light>().enabled = true;
                    }
                    else
                    {
                        if (lightSeq[(i / 2) - 1][order.IndexOf('R')] == true)
                        {
                            buttons[order.IndexOf('R')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('G')] == true)
                        {
                            buttons[order.IndexOf('G')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('B')] == true)
                        {
                            buttons[order.IndexOf('B')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('C')] == true)
                        {
                            buttons[order.IndexOf('C')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('M')] == true)
                        {
                            buttons[order.IndexOf('M')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('Y')] == true)
                        {
                            buttons[order.IndexOf('Y')].GetComponentInChildren<Light>().enabled = true;
                        }
                    }
                }
                yield return new WaitForSeconds(flash);
            }
            if (i == 7)
            {
                i = -1;
            }
        }
    }

    private IEnumerator Sequence1()
    {
        for (int i = 0; i < 10; i++)
        {
            if (mode == 0)
            {
                if (i % 2 == 1)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        buttons[j].GetComponentInChildren<Light>().enabled = false;
                        greyButton.GetComponentInChildren<Light>().enabled = false;
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        greyButton.GetComponentInChildren<Light>().enabled = true;
                    }
                    else
                    {
                        if (lightSeq[(i / 2) - 1][order.IndexOf('R')] == true)
                        {
                            buttons[order.IndexOf('R')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('G')] == true)
                        {
                            buttons[order.IndexOf('G')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('B')] == true)
                        {
                            buttons[order.IndexOf('B')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('C')] == true)
                        {
                            buttons[order.IndexOf('C')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('M')] == true)
                        {
                            buttons[order.IndexOf('M')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('Y')] == true)
                        {
                            buttons[order.IndexOf('Y')].GetComponentInChildren<Light>().enabled = true;
                        }
                    }
                }
                yield return new WaitForSeconds(flash);
            }
            if (i == 9)
            {
                i = -1;
            }
        }
    }

    private IEnumerator Sequence2()
    {
        for (int i = 0; i < 12; i++)
        {
            if (mode == 0)
            {
                if (i % 2 == 1)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        buttons[j].GetComponentInChildren<Light>().enabled = false;
                        greyButton.GetComponentInChildren<Light>().enabled = false;
                    }
                }
                else
                {
                    if (i == 0)
                    {
                        greyButton.GetComponentInChildren<Light>().enabled = true;
                    }
                    else
                    {
                        if (lightSeq[(i / 2) - 1][order.IndexOf('R')] == true)
                        {
                            buttons[order.IndexOf('R')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('G')] == true)
                        {
                            buttons[order.IndexOf('G')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('B')] == true)
                        {
                            buttons[order.IndexOf('B')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('C')] == true)
                        {
                            buttons[order.IndexOf('C')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('M')] == true)
                        {
                            buttons[order.IndexOf('M')].GetComponentInChildren<Light>().enabled = true;
                        }
                        if (lightSeq[(i / 2) - 1][order.IndexOf('Y')] == true)
                        {
                            buttons[order.IndexOf('Y')].GetComponentInChildren<Light>().enabled = true;
                        }
                    }
                }
                yield return new WaitForSeconds(flash);
            }
            if (i == 11)
            {
                i = -1;
            }
        }
    }

    //Submission 'animation'
    private IEnumerator StartButtonMode2()
    {
        tempNum[2] = 0;
        for (int i = 0; i < stage * 8 + 1; i++)
        {
            switch (i % 8)
            {
                case 0:
                    buttons[5].GetComponentInChildren<Light>().enabled = false;
                    whiteButton.GetComponentInChildren<Light>().enabled = true;
                    break;
                case 1:
                    whiteButton.GetComponentInChildren<Light>().enabled = false;
                    buttons[0].GetComponentInChildren<Light>().enabled = true;
                    break;
                case 2:
                    buttons[0].GetComponentInChildren<Light>().enabled = false;
                    buttons[1].GetComponentInChildren<Light>().enabled = true;
                    break;
                case 3:
                    buttons[1].GetComponentInChildren<Light>().enabled = false;
                    buttons[2].GetComponentInChildren<Light>().enabled = true;
                    break;
                case 4:
                    buttons[2].GetComponentInChildren<Light>().enabled = false;
                    blackButton.GetComponentInChildren<Light>().enabled = true;
                    break;
                case 5:
                    blackButton.GetComponentInChildren<Light>().enabled = false;
                    buttons[3].GetComponentInChildren<Light>().enabled = true;
                    break;
                case 6:
                    buttons[3].GetComponentInChildren<Light>().enabled = false;
                    buttons[4].GetComponentInChildren<Light>().enabled = true;
                    break;
                case 7:
                    buttons[4].GetComponentInChildren<Light>().enabled = false;
                    buttons[5].GetComponentInChildren<Light>().enabled = true;
                    break;
            }
            yield return new WaitForSeconds(flash / (2 * stage));
        }
        greyButton.GetComponentInChildren<Light>().enabled = false;
        whiteButton.GetComponentInChildren<Light>().enabled = false;

        //Checking input sequence against final answer
        string[] inputArray = inputList.ToArray();
        string finalInput = String.Join(String.Empty, inputArray);
        if(finalInput.Equals(finalAnswer[stage - 1]))
        {
            Audio.PlaySoundAtTransform("InputCorrect", transform);
            if(stage < 3)
            {
                stage++;
                if (stage == 2)
                {
                    Debug.LogFormat("[Simon Stores #{1}] b0 = {0}", step[1][0], moduleId);
                    Debug.LogFormat("[Simon Stores #{0}] The flashing order has added {1}", moduleId, flashingColours[3]);
                    Debug.LogFormat("[Simon Stores #{1}] The button order for stage 2 was {0}", new string(executionOrder[1]), moduleId);
                    for (int i = 1; i < 5; i++)
                    {
                        Debug.LogFormat("[Simon Stores #{1}] b{2} = {0}", step[1][i], moduleId, i);
                    }
                    Debug.LogFormat("[Simon Stores #{1}] {2} in balanced ternary is {0}", new string(balancedTrits[1]), moduleId, step[1][4]);
                    Debug.LogFormat("[Simon Stores #{1}] The correct input for stage 2 was {0}", finalAnswer[1], moduleId);
                }
                else
                {
                    Debug.LogFormat("[Simon Stores #{1}] c0 = {0}", step[2][0], moduleId);
                    Debug.LogFormat("[Simon Stores #{0}] The flashing order has added {1}", moduleId, flashingColours[4]);
                    Debug.LogFormat("[Simon Stores #{1}] The button order for stage 3 was {0}", new string(executionOrder[2]), moduleId);
                    for (int i = 1; i < 6; i++)
                    {
                        Debug.LogFormat("[Simon Stores #{1}] c{2} = {0}", step[2][i], moduleId, i);
                    }
                    Debug.LogFormat("[Simon Stores #{1}] {2} in balanced ternary is {0}", new string(balancedTrits[2]), moduleId, step[2][5]);
                    Debug.LogFormat("[Simon Stores #{1}] The correct input for stage 3 was {0}", finalAnswer[2], moduleId);
                }
            }
            else
            {
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
            }
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Simon Stores #{2}] The input you submitted for stage {0} was {1}" , stage, finalInput, moduleId);
        }

        //Resetting after submission
        if (moduleSolved == false)
        {
            mode = 0;
            StartCoroutine(sequences[stage - 1]);
        }
        inputList.Clear();
    }

    //Pressing white button
    void WButton()
    {
        if (mode == 1)
        {
            if (negativeEntry == true)
            {
                negativeEntry = false;
                whiteButton.GetComponentInChildren<Light>().enabled = true;
                blackButton.GetComponentInChildren<Light>().enabled = false;
                whiteButton.AddInteractionPunch(.5f);
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
            }
        }
    }

    //Pressing black button
    void BButton()
    {
        if (mode == 1)
        {
            if (negativeEntry == false)
            {
                negativeEntry = true;
                whiteButton.GetComponentInChildren<Light>().enabled = false;
                blackButton.GetComponentInChildren<Light>().enabled = true;
                blackButton.AddInteractionPunch(.5f);
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
            }
        }
    }

    //Pressing coloured buttons
    void ButtonPress(KMSelectable button)
    {
        if (mode == 1)
        {
            for (int i = 0; i < 6; i++)
            {
                if (buttons[i] == button)
                {
                    tempPress = i;
                    break;
                }
            }
            if (alreadyPressed[tempPress] == false)
            {
                if (negativeEntry == false)
                {
                    inputList.Add("+" + new string(order[tempPress], 1));
                }
                else
                {
                    inputList.Add("-" + new string(order[tempPress], 1));
                }
                alreadyPressed[tempPress] = true;
                buttons[tempPress].GetComponentInChildren<Light>().enabled = true;
                buttons[tempPress].AddInteractionPunch(.5f);
                tempNum[2]++;
                switch (tempNum[2])
                {
                    case 1:
                        Audio.PlaySoundAtTransform("ButtonPress1", transform);
                        break;
                    case 2:
                        Audio.PlaySoundAtTransform("ButtonPress2", transform);
                        break;
                    case 3:
                        Audio.PlaySoundAtTransform("ButtonPress3", transform);
                        break;
                    case 4:
                        Audio.PlaySoundAtTransform("ButtonPress4", transform);
                        break;
                    case 5:
                        Audio.PlaySoundAtTransform("ButtonPress5", transform);
                        break;
                    case 6:
                        Audio.PlaySoundAtTransform("ButtonPress6", transform);
                        break;
                }
            }
        }
    }

    //Colourblind accessibility
    void HLButton(KMSelectable button)
    {
        if (mode != 2)
        {
            for (int i = 0; i < 6; i++)
            {
                if (buttons[i] == button)
                {
                    tempHL = i;
                    break;
                }
            }
            screen.GetComponentInChildren<TextMesh>().text = new string(order[tempHL], 1);
        }
    }

    void HLEndButton(KMSelectable button)
    {
        screen.GetComponentInChildren<TextMesh>().text = String.Empty;
    }

    //Operations used in evaluatating sequence steps
    int Check(int val)
    {
        while (val > 364)
        {
            val -= 365;
        }
        while (val < -364)
        {
            val += 365;
        }
        return val;
    }

    int Red(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x += D;
                break;
            case 1:
                x += step[0][j - 1] + (int)Math.Pow(j,2);
                break;
            case 2:
                x += step[1][j - 1] - step[0][j - 1];
                break;
        }
        x = Check(x);
        return x;
    }

    int Green(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x -= D;
                break;
            case 1:
                x = (2 * x) - step[0][j - 1];
                break;
            case 2:
                x -= 2 * step[1][j - 1];
                break;
        }
        x = Check(x);
        return x;
    }

    int Blue(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = (2 * x) - D;
                break;
            case 1:
                x = (2 * x) - step[0][0] - (4 * (int)Math.Pow(j, 2));
                break;
            case 2:
                x += step[1][0] -  step[0][3];
                break;
        }
        x = Check(x);
        return x;
    }

    int Cyan(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = D - (8 * j) - x;
                break;
            case 1:
                x += step[0][1];
                break;
            case 2:
                x += step[0][j - 1] - step[1][j - 1] ;
                break;
        }
        x = Check(x);
        return x;
    }

    int Magenta(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = (3 * (int)Math.Pow(j, 3)) - (2 * x);
                break;
            case 1:
                x += step[0][2] - D;
                break;
            case 2:
                x -= 2 * step[0][j - 1];
                break;
        }
        x = Check(x);
        return x;
    }

    int Yellow(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x += D - (6 * j);
                break;
            case 1:
                x += step[0][3] - step[0][j - 1];
                break;
            case 2:
                x += step[1][4] - step[0][0];
                break;
        }
        x = Check(x);
        return x;
    }

    int ReGr(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Math.Max(Red(x, 0, j), Green(x, 0, j));
                break;
            case 1:
                x = Math.Abs(Red(x, 1, j) - Green(x, 1, j));
                break;
            case 2:
                x = Blue(x, 2, j) + Blue(step[1][j - 1], 2, j) + Blue(step[0][j - 1], 2, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int ReBl(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Math.Max(Red(x, 0, j), Blue(x, 0, j));
                break;
            case 1:
                x = Math.Abs(Red(x, 1, j) - Blue(x, 1, j));
                break;
            case 2:
                x = Green(x, 2, j) + Green(step[1][j - 1], 2, j) + Green(step[0][j - 1], 2, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int ReCy(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Red(x, 0, j) + Cyan(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Red(x, 1, j) - Cyan(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Red(x, 2, j), Cyan(x, 2, j), -Math.Abs(Red(x, 2, j) - Cyan(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int ReMa(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Red(x, 0, j) + Magenta(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Red(x, 1, j) - Magenta(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Red(x, 2, j), Magenta(x, 2, j), -Math.Abs(Red(x, 2, j) - Magenta(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int ReYe(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Red(x, 0, j) + Yellow(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Red(x, 1, j) - Yellow(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Red(x, 2, j), Yellow(x, 2, j), -Math.Abs(Red(x, 2, j) - Yellow(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int GrBl(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Math.Max(Green(x, 0, j), Blue(x, 0, j));
                break;
            case 1:
                x = Math.Abs(Green(x, 1, j) - Blue(x, 1, j));
                break;
            case 2:
                x = Red(x, 2, j) + Red(step[1][j - 1], 2, j) + Red(step[0][j - 1], 2, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GrCy(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Green(x, 0, j) + Cyan(x, 0, j) - 2 * D;
                break;
            case 1:
                x = 4 * D - Math.Abs(Green(x, 1, j) - Cyan(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Green(x, 2, j), Cyan(x, 2, j), -Math.Abs(Green(x, 2, j) - Cyan(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int GrMa(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Green(x, 0, j) + Magenta(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Green(x, 1, j) - Magenta(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Green(x, 2, j), Magenta(x, 2, j), -Math.Abs(Green(x, 2, j) - Magenta(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int GrYe(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Green(x, 0, j) + Yellow(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Green(x, 1, j) - Yellow(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Green(x, 2, j), Yellow(x, 2, j), -Math.Abs(Green(x, 2, j) - Yellow(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int BlCy(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Blue(x, 0, j) + Cyan(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Blue(x, 1, j) - Cyan(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Blue(x, 2, j), Cyan(x, 2, j), -Math.Abs(Green(x, 2, j) - Cyan(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int BlMa(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Blue(x, 0, j) + Magenta(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Blue(x, 1, j) - Magenta(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Blue(x, 2, j), Magenta(x, 2, j), -Math.Abs(Blue(x, 2, j) - Magenta(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int BlYe(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Blue(x, 0, j) + Yellow(x, 0, j) - (2 * D);
                break;
            case 1:
                x = (4 * D) - Math.Abs(Blue(x, 1, j) - Yellow(x, 1, j));
                break;
            case 2:
                x = Mathf.Min(Blue(x, 2, j), Yellow(x, 2, j), -Math.Abs(Blue(x, 2, j) - Yellow(x, 2, j)));
                break;
        }
        x = Check(x);
        return x;
    }

    int CyMa(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Math.Min(Cyan(x, 0, j), Magenta(x, 0, j));
                break;
            case 1:
                x = Math.Max(Yellow(x, 1, j) , Yellow(step[0][j - 1], 1, j));
                break;
            case 2:
                x = Yellow(x, 2, j) - Magenta(x, 2, j) - Cyan(x, 2, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int CyYe(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Math.Min(Cyan(x, 0, j), Yellow(x, 0, j));
                break;
            case 1:
                x = Math.Max(Magenta(x, 1, j), Magenta(step[0][j - 1], 1, j));
                break;
            case 2:
                x = Magenta(x, 2, j) - Cyan(x, 2, j) - Yellow(x, 2, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int MaYe(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Math.Min(Magenta(x, 0, j), Yellow(x, 0, j));
                break;
            case 1:
                x = Math.Max(Cyan(x, 1, j), Cyan(step[0][j - 1], 1, j));
                break;
            case 2:
                x = Cyan(x, 2, j) - Magenta(x, 2, j) - Yellow(x, 2, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RGB(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x += step[0][0];
                break;
            case 1:
                x += (((x + 400) % 4) * step[1][0]) - step[0][3];
                break;
            case 2:
                x += (((x + 600) % 3) * step[2][0]) - (((step[1][j - 1] + 600) % 3) * step[1][0]) + (((step[0][j - 1] + 600) % 3) * step[0][0]);
                break;
        }
        x = Check(x);
        return x;
    }

    int RGC(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Red(x,i,j),Green(x,i,j),Cyan(x,i,j));
                break;
            case 1:
                x += Red(x,i,j) + Green(x,i,j) - Cyan(step[0][j - 1],i,j);
                break;
            case 2:
                x = Red(x,i,j) + Green(x,i,j) - Cyan(step[1][j - 1],i,j) - Cyan(step[0][j - 1],i,j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RGM(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Red(x, i, j), Green(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Red(x, i, j) + Green(x, i, j) - Magenta(step[0][j - 1], i, j);
                break;
            case 2:
                x = Red(x, i, j) + Green(x, i, j) - Magenta(step[1][j - 1], i, j) - Magenta(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RGY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Red(x, i, j), Green(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Red(x, i, j) + Green(x, i, j) - Yellow(step[0][j - 1], i, j);
                break;
            case 2:
                x = Red(x, i, j) + Green(x, i, j) - Yellow(step[1][j - 1], i, j) - Yellow(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RBC(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Red(x, i, j), Blue(x, i, j), Cyan(x, i, j));
                break;
            case 1:
                x += Red(x, i, j) + Blue(x, i, j) - Cyan(step[0][j - 1], i, j);
                break;
            case 2:
                x = Red(x, i, j) + Blue(x, i, j) - Cyan(step[1][j - 1], i, j) - Cyan(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RBM(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Red(x, i, j), Blue(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Red(x, i, j) + Blue(x, i, j) - Magenta(step[0][j - 1], i, j);
                break;
            case 2:
                x = Red(x, i, j) + Blue(x, i, j) - Magenta(step[1][j - 1], i, j) - Magenta(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RBY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Red(x, i, j), Blue(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Red(x, i, j) + Blue(x, i, j) - Yellow(step[0][j - 1], i, j);
                break;
            case 2:
                x = Red(x, i, j) + Blue(x, i, j) - Yellow(step[1][j - 1], i, j) - Yellow(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RCM(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Red(x, i, j), Cyan(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Cyan(step[0][j - 1], i, j) + Magenta(step[0][j - 1], i, j) - Red(x, i, j);
                break;
            case 2:
                x = Cyan(x, i, j) + Magenta(x, i, j) - Red(step[1][j - 1], i, j) - Red(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RCY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Red(x, i, j), Cyan(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Cyan(step[0][j - 1], i, j) + Yellow(step[0][j - 1], i, j) - Red(x, i, j);
                break;
            case 2:
                x = Cyan(x, i, j) + Yellow(x, i, j) - Red(step[1][j - 1], i, j) - Red(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int RMY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Red(x, i, j), Yellow(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Yellow(step[0][j - 1], i, j) + Magenta(step[0][j - 1], i, j) - Red(x, i, j);
                break;
            case 2:
                x = Yellow(x, i, j) + Magenta(x, i, j) - Red(step[1][j - 1], i, j) - Red(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GBC(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Green(x, i, j), Blue(x, i, j), Cyan(x, i, j));
                break;
            case 1:
                x += Green(x, i, j) + Blue(x, i, j) - Cyan(step[0][j - 1], i, j);
                break;
            case 2:
                x = Green(x, i, j) + Blue(x, i, j) - Cyan(step[1][j - 1], i, j) - Cyan(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GBM(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Green(x, i, j), Blue(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Green(x, i, j) + Blue(x, i, j) - Magenta(step[0][j - 1], i, j);
                break;
            case 2:
                x = Green(x, i, j) + Blue(x, i, j) - Magenta(step[1][j - 1], i, j) - Magenta(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GBY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Max(Green(x, i, j), Blue(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Green(x, i, j) + Blue(x, i, j) - Yellow(step[0][j - 1], i, j);
                break;
            case 2:
                x = Green(x, i, j) + Blue(x, i, j) - Yellow(step[1][j - 1], i, j) - Yellow(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GCM(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Green(x, i, j), Cyan(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Cyan(step[0][j - 1], i, j) + Magenta(step[0][j - 1], i, j) - Green(x, i, j);
                break;
            case 2:
                x = Cyan(x, i, j) + Magenta(x, i, j) - Green(step[1][j - 1], i, j) - Green(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GCY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Green(x, i, j), Cyan(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Cyan(step[0][j - 1], i, j) + Yellow(step[0][j - 1], i, j) - Green(x, i, j);
                break;
            case 2:
                x = Cyan(x, i, j) + Yellow(x, i, j) - Green(step[1][j - 1], i, j) - Green(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int GMY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Green(x, i, j), Magenta(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Magenta(step[0][j - 1], i, j) + Yellow(step[0][j - 1], i, j) - Green(x, i, j);
                break;
            case 2:
                x = Magenta(x, i, j) + Yellow(x, i, j) - Green(step[1][j - 1], i, j) - Green(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int BCM(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Blue(x, i, j), Cyan(x, i, j), Magenta(x, i, j));
                break;
            case 1:
                x += Cyan(step[0][j - 1], i, j) + Magenta(step[0][j - 1], i, j) - Blue(x, i, j);
                break;
            case 2:
                x = Cyan(x, i, j) + Magenta(x, i, j) - Blue(step[1][j - 1], i, j) - Blue(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int BCY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Blue(x, i, j), Cyan(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Cyan(step[0][j - 1], i, j) + Yellow(step[0][j - 1], i, j) - Blue(x, i, j);
                break;
            case 2:
                x = Cyan(x, i, j) + Yellow(x, i, j) - Blue(step[1][j - 1], i, j) - Blue(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int BMY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x = Mathf.Min(Blue(x, i, j), Magenta(x, i, j), Yellow(x, i, j));
                break;
            case 1:
                x += Magenta(step[0][j - 1], i, j) + Yellow(step[0][j - 1], i, j) - Blue(x, i, j);
                break;
            case 2:
                x = Magenta(x, i, j) + Yellow(x, i, j) - Blue(step[1][j - 1], i, j) - Blue(step[0][j - 1], i, j);
                break;
        }
        x = Check(x);
        return x;
    }

    int CMY(int x, int i, int j)
    {
        switch (i)
        {
            case 0:
                x -= step[0][0];
                break;
            case 1:
                x += (((step[1][0] + 400) % 4) * x) - step[0][3];
                break;
            case 2:
                x += (((step[2][0] + 600) % 3) * x) - (((step[1][0] + 600) % 3) * step[1][j - 1]) + (((step[0][0] + 600) % 3) * step[0][j - 1]);
                break;
        }
        x = Check(x);
        return x;
    }

    char[] BalTer(int x)
    {
        char[] Y = new char[6];
        for (int i = 6; i > 0; i--)
        {
            if (Math.Abs(x) < ((int)Math.Pow(3, i - 1) + 1) / 2)
            {
                Y[6 - i] = '0';
            }
            else
            {
                if (x > 0)
                {
                    Y[6 - i] = '+';
                    x -= (int)Math.Pow(3, i - 1);
                }
                else
                {
                    Y[6 - i] = '-';
                    x += (int)Math.Pow(3, i - 1);
                }
            }
        }
        return Y;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} AWRKMCA [A = gray, K = black] | !{0} cycle [shows colors of buttons clockwise from white]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            for (int i = 0; i < 6; i++)
            {
                buttons[i].OnHighlight();
                yield return new WaitForSeconds(1.2f);
                buttons[i].OnHighlightEnded();
                yield return new WaitForSeconds(.1f);
            }
            yield break;
        }

        var m = Regex.Match(command, @"^\s*([AWKRMGBCY, ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;

        yield return null;
        yield return m.Groups[1].Value
            .Select(ch =>
            {
                if (ch == 'A' || ch == 'a')
                    return greyButton;
                if (ch == 'W' || ch == 'w')
                    return whiteButton;
                if (ch == 'K' || ch == 'k')
                    return blackButton;
                var pos = order.IndexOf(char.ToUpperInvariant(ch));
                return pos == -1 ? null : buttons[pos];
            })
            .Where(btn => btn != null)
            .ToArray();
    }
}

