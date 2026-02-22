using MSCLoader;
using UnityEngine;
using I386API;

namespace computer386
{
    public class computer386 : Mod
    {
        public override string ID => "KIRJOITA";
        public override string Name => "KIRJOITA";
        public override string Author => "lxzy";
        public override string Version => "1.0";
        public override string Description => "KIRJOITA on the computer to play.";
        public override Game SupportedGames => Game.MyWinterCar;

        static readonly string[] easySentences = new string[]
        {
            "the sun came up and the dog went outside",
            "she put the book down and went to bed",
            "he drove to the store to get some milk",
            "the cat sat on the mat by the door",
            "it was a cold day and the wind was strong",
            "she made some tea and sat by the window",
            "the old man walked down the road very slowly",
            "he fixed the car and it ran much better",
            "the kids played in the yard all afternoon long",
            "she wrote a letter and put it in the box",
            "the bus was late so he walked to work",
            "he ate his food and drank a cold glass of water",
            "the fire was warm and the room felt nice",
            "she found her keys under the pile of old papers",
            "he cut the wood and stacked it by the shed",
            "the dog barked at the door until she opened it",
            "it was quiet in the house that cold winter night",
            "she packed her bag and left before the sun rose",
            "he turned off the light and went straight to sleep",
            "the snow fell all night and covered the whole yard",
        };

        static readonly string[] mediumSentences = new string[]
        {
            "the mechanic checked the engine, but couldn't find the broken gasket",
            "she carried the groceries up three flights of stairs, then collapsed on the sofa",
            "the forecast said heavy snow, so he didn't bother starting the car",
            "he replaced the spark plugs, and the engine finally turned over",
            "the old farmhouse hadn't been heated in years, and the pipes had frozen solid",
            "she borrowed money from her brother, but forgot to pay the electric bill",
            "the road was covered in black ice, and it wasn't safe to drive",
            "he spent the entire weekend on the roof, but it still wasn't done",
            "the nearest gas station was forty kilometers away, and the tank was empty",
            "she found a cracked radiator hose, but didn't have the right size to replace it",
            "the tow truck arrived two hours late, right in the middle of a blizzard",
            "he checked the oil and found it was nearly bone dry, which explained everything",
            "the battery died overnight, because he'd left the headlights on again",
            "she had to shovel the driveway first, otherwise she couldn't get the car out",
            "the transmission was slipping badly, especially on every single uphill stretch",
            "he drained the old coolant, flushed the system, and refilled it with fresh mix",
            "the neighbor helped push the car out of the ditch, but it wasn't easy",
            "she changed the winter tires herself, using a borrowed floor jack and a lot of patience",
            "the fuel pump failed without warning, leaving him stranded on the forest road",
            "he wrapped the frozen pipes with tape and rags, then hoped for the best",
        };

        static readonly string[] hardSentences = new string[]
        {
            "the differential bearing had been grinding for weeks, but nobody'd bothered to check it",
            "she diagnosed the intermittent fault herself, tracing every circuit until she'd found it",
            "the carburetor's internals had corroded badly, so it'd need a full rebuild before spring",
            "he torqued the cylinder head bolts in sequence, careful not to warp the mating surface",
            "the crank sensor threw a fault code that he'd never seen before, and it took days to trace",
            "she'd bypassed the immobilizer by splicing directly into the ignition module's wiring harness",
            "the clutch system's hydraulic line had been weeping slowly, hidden behind the firewall",
            "he pressure-tested the cooling system and found a hairline crack he wouldn't have spotted otherwise",
            "the alternator's output had dropped to twelve volts under load, which wasn't anywhere near enough",
            "she'd memorized every relay's location in the fuse panel, so the fault didn't take long to find",
            "the suspension geometry was shot after he'd clipped the concrete barrier at highway speed",
            "he rebuilt the master cylinder using a repair kit he'd sourced from a specialist supplier",
            "the timing chain had stretched, retarding ignition by nearly four degrees and killing fuel economy",
            "she rewired the instrument cluster from scratch, undoing what the previous owner had badly botched",
            "the exhaust manifold had cracked between cylinders two and three, causing a loud cold-start tick",
            "he pulled the gearbox, only to find the second gear's synchro ring worn completely smooth",
            "the wheel bearing had developed enough play that it'd shake the steering wheel above eighty",
            "she'd fabricated a custom bracket to relocate the battery, improving the car's weight distribution",
            "the lambda sensor's readings were erratic, pointing to an exhaust leak upstream of the sensor",
            "he'd mapped out the entire vacuum system on paper before he dared replace a single hose",
        };

        enum GameState { Boot, PickDiff, PickTime, Countdown, Typing, Results }

        static GameState state;
        static int selectedDiff;
        static int selectedTime;

        static readonly string[] diffLabels = new string[] { "EASY", "MEDIUM", "HARD" };
        static readonly string[] timeLabels = new string[] { "15s", "30s", "60s" };
        static readonly float[] timeLimits = new float[] { 15f, 30f, 60f };

        static float timeLimit;
        static float timeLeft;
        static float bootTimer;
        static float lastTimerSecond;

        static string currentSentence;
        static string inputBuffer;
        static int correctWords;
        static int totalTyped;

        static int countdownTick;
        static float countdownTimer;

        static int bootLine;
        static float bootLineTimer;

        static bool needsRedraw;

        static readonly System.Collections.Generic.List<int> usedIndices
            = new System.Collections.Generic.List<int>();

        public override void ModSetup()
        {
            SetupFunction(Setup.PreLoad, Mod_OnPreLoad);
        }

        private void Mod_OnPreLoad()
        {
            Command.Create("kirjoita", OnEnter, OnUpdate);

            Diskette disk = Diskette.Create("kirjoita", new Vector3(-1287.209f, 0.9939466f, 1081.927f), new Vector3(272.8291f, 134.1103f, 308.2567f));

            Texture2D label = new Texture2D(128, 128);
            label.LoadImage(I386API.Properties.Resources.FLOPPY_BLANK);
            DrawTextScaled(label, "KIRJOITA", 8, 54, Color.black, 2);
            label.Apply();
            disk.SetTexture(label);
        }

        static void DrawTextScaled(Texture2D tex, string text, int startX, int startY, Color col, int scale)
        {
            foreach (char c in text)
            {
                int[,] glyph = GetGlyph(c);
                if (glyph != null)
                {
                    int rows = glyph.GetLength(0);
                    int cols = glyph.GetLength(1);
                    for (int row = 0; row < rows; row++)
                        for (int col2 = 0; col2 < cols; col2++)
                            if (glyph[row, col2] == 1)
                                for (int sy = 0; sy < scale; sy++)
                                    for (int sx = 0; sx < scale; sx++)
                                        tex.SetPixel(startX + col2 * scale + sx, startY + (rows - 1 - row) * scale + sy, col);
                    startX += (cols + 1) * scale;
                }
                else { startX += 4 * scale; }
                if (startX > 118) break;
            }
        }

        static void DrawText(Texture2D tex, string text, int startX, int startY, Color col)
        {
            foreach (char c in text)
            {
                int[,] glyph = GetGlyph(c);
                if (glyph != null)
                {
                    for (int row = 0; row < glyph.GetLength(0); row++)
                        for (int col2 = 0; col2 < glyph.GetLength(1); col2++)
                            if (glyph[row, col2] == 1)
                                tex.SetPixel(startX + col2, startY + (glyph.GetLength(0) - 1 - row), col);
                    startX += glyph.GetLength(1) + 1;
                }
                else { startX += 4; }
                if (startX > 118) break;
            }
        }

        static int[,] GetGlyph(char c)
        {
            switch (char.ToUpper(c))
            {
                case 'A': return new int[,] { { 0, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 1, 1, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 } };
                case 'B': return new int[,] { { 1, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 1, 1, 0 } };
                case 'C': return new int[,] { { 0, 1, 1, 1 }, { 1, 0, 0, 0 }, { 1, 0, 0, 0 }, { 1, 0, 0, 0 }, { 0, 1, 1, 1 } };
                case 'D': return new int[,] { { 1, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 1, 1, 0 } };
                case 'E': return new int[,] { { 1, 1, 1, 1 }, { 1, 0, 0, 0 }, { 1, 1, 1, 0 }, { 1, 0, 0, 0 }, { 1, 1, 1, 1 } };
                case 'F': return new int[,] { { 1, 1, 1, 1 }, { 1, 0, 0, 0 }, { 1, 1, 1, 0 }, { 1, 0, 0, 0 }, { 1, 0, 0, 0 } };
                case 'G': return new int[,] { { 0, 1, 1, 1 }, { 1, 0, 0, 0 }, { 1, 0, 1, 1 }, { 1, 0, 0, 1 }, { 0, 1, 1, 1 } };
                case 'H': return new int[,] { { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 1, 1, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 } };
                case 'I': return new int[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 1, 1, 1 } };
                case 'J': return new int[,] { { 0, 0, 1 }, { 0, 0, 1 }, { 0, 0, 1 }, { 1, 0, 1 }, { 0, 1, 1 } };
                case 'K': return new int[,] { { 1, 0, 0, 1 }, { 1, 0, 1, 0 }, { 1, 1, 0, 0 }, { 1, 0, 1, 0 }, { 1, 0, 0, 1 } };
                case 'L': return new int[,] { { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 1, 1 } };
                case 'M': return new int[,] { { 1, 0, 0, 0, 1 }, { 1, 1, 0, 1, 1 }, { 1, 0, 1, 0, 1 }, { 1, 0, 0, 0, 1 }, { 1, 0, 0, 0, 1 } };
                case 'N': return new int[,] { { 1, 0, 0, 1 }, { 1, 1, 0, 1 }, { 1, 0, 1, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 } };
                case 'O': return new int[,] { { 0, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 0, 1, 1, 0 } };
                case 'P': return new int[,] { { 1, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 1, 1, 0 }, { 1, 0, 0, 0 }, { 1, 0, 0, 0 } };
                case 'Q': return new int[,] { { 0, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 1, 0 }, { 0, 1, 0, 1 } };
                case 'R': return new int[,] { { 1, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 1, 1, 0 }, { 1, 0, 1, 0 }, { 1, 0, 0, 1 } };
                case 'S': return new int[,] { { 0, 1, 1, 1 }, { 1, 0, 0, 0 }, { 0, 1, 1, 0 }, { 0, 0, 0, 1 }, { 1, 1, 1, 0 } };
                case 'T': return new int[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } };
                case 'U': return new int[,] { { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 0, 1, 1, 0 } };
                case 'V': return new int[,] { { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 0, 1, 1, 0 }, { 0, 1, 1, 0 } };
                case 'W': return new int[,] { { 1, 0, 0, 0, 1 }, { 1, 0, 0, 0, 1 }, { 1, 0, 1, 0, 1 }, { 1, 1, 0, 1, 1 }, { 1, 0, 0, 0, 1 } };
                case 'X': return new int[,] { { 1, 0, 0, 1 }, { 0, 1, 1, 0 }, { 0, 1, 1, 0 }, { 0, 1, 1, 0 }, { 1, 0, 0, 1 } };
                case 'Y': return new int[,] { { 1, 0, 0, 1 }, { 0, 1, 1, 0 }, { 0, 1, 0, 0 }, { 0, 1, 0, 0 }, { 0, 1, 0, 0 } };
                case 'Z': return new int[,] { { 1, 1, 1, 1 }, { 0, 0, 1, 0 }, { 0, 1, 0, 0 }, { 1, 0, 0, 0 }, { 1, 1, 1, 1 } };
                case '0': return new int[,] { { 0, 1, 1, 0 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 1, 0, 0, 1 }, { 0, 1, 1, 0 } };
                case '1': return new int[,] { { 0, 1, 0 }, { 1, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 1, 1, 1 } };
                case '.': return new int[,] { { 0 }, { 0 }, { 0 }, { 0 }, { 1 } };
                case ' ': return new int[,] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
                default: return null;
            }
        }

        static readonly string[] bootLines = new string[]
        {
            "Nokia Data BIOS v1.03  (C) 1991 Nokia Data",
            "",
            "CPU  : Intel 80386DX @ 33MHz",
            "FPU  : Intel 80387",
            "",
            "Fixed Disk 0: WD 40MB  [OK]",
            "Drive A: 3.5in 1.44MB  [OK]",
            "",
            "MS-DOS Version 5.00",
            "",
            "C:\\>KIRJOITA.EXE",
            "",
            "  Press any key to continue_",
        };

        static bool OnEnter()
        {
            state = GameState.Boot;
            bootLine = 0;
            bootTimer = 0f;
            bootLineTimer = 0f;
            needsRedraw = true;
            I386.POS_ClearScreen();
            return false;
        }

        static bool CheckAltQ()
        {
            return I386.GetKey(KeyCode.LeftAlt) && I386.GetKeyDown(KeyCode.Q);
        }

        static bool OnUpdate()
        {
            if (CheckAltQ()) return true;

            switch (state)
            {
                case GameState.Boot: return UpdateBoot();
                case GameState.PickDiff: return UpdatePickDiff();
                case GameState.PickTime: return UpdatePickTime();
                case GameState.Countdown: return UpdateCountdown();
                case GameState.Typing: return UpdateTyping();
                case GameState.Results: return UpdateResults();
            }
            return false;
        }

        static bool UpdateBoot()
        {
            bootLineTimer += Time.deltaTime;
            if (bootLine < bootLines.Length && bootLineTimer >= 0.18f)
            {
                bootLineTimer -= 0.18f;
                I386.POS_WriteNewLine(bootLines[bootLine]);
                bootLine++;
                return false;
            }

            if (bootLine >= bootLines.Length)
            {
                DrainCharBuffer();
                if (I386.GetKeyDown(KeyCode.Return) || I386.GetKeyDown(KeyCode.Space))
                {
                    state = GameState.PickDiff;
                    selectedDiff = 0;
                    selectedTime = 1;
                    needsRedraw = true;
                }
            }

            return false;
        }

        static bool UpdatePickDiff()
        {
            if (needsRedraw)
            {
                I386.POS_ClearScreen();
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  *** KIRJOITA v1.0 ***");
                I386.POS_WriteNewLine("  Typing Speed Test");
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  Step 1 of 2 - Select difficulty:");
                I386.POS_NewLine();
                for (int i = 0; i < diffLabels.Length; i++)
                {
                    string prefix = (i == selectedDiff) ? "  > " : "    ";
                    I386.POS_WriteNewLine(prefix + diffLabels[i]);
                }
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  UP/DOWN   ENTER confirm   ALT+Q quit");
                needsRedraw = false;
            }

            if (I386.GetKeyDown(KeyCode.UpArrow))
            {
                selectedDiff = (selectedDiff + diffLabels.Length - 1) % diffLabels.Length;
                needsRedraw = true;
            }
            else if (I386.GetKeyDown(KeyCode.DownArrow))
            {
                selectedDiff = (selectedDiff + 1) % diffLabels.Length;
                needsRedraw = true;
            }
            else if (I386.GetKeyDown(KeyCode.Return) || I386.GetKeyDown(KeyCode.KeypadEnter))
            {
                state = GameState.PickTime;
                needsRedraw = true;
            }
            return false;
        }

        static bool UpdatePickTime()
        {
            if (needsRedraw)
            {
                I386.POS_ClearScreen();
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  *** KIRJOITA v1.0 ***");
                I386.POS_WriteNewLine("  Difficulty: " + diffLabels[selectedDiff] + " [locked]");
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  Step 2 of 2 - Select time:");
                I386.POS_NewLine();
                for (int i = 0; i < timeLabels.Length; i++)
                {
                    string prefix = (i == selectedTime) ? "  > " : "    ";
                    I386.POS_WriteNewLine(prefix + timeLabels[i]);
                }
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  UP/DOWN   ENTER start   ALT+Q quit");
                needsRedraw = false;
            }

            if (I386.GetKeyDown(KeyCode.UpArrow))
            {
                selectedTime = (selectedTime + timeLabels.Length - 1) % timeLabels.Length;
                needsRedraw = true;
            }
            else if (I386.GetKeyDown(KeyCode.DownArrow))
            {
                selectedTime = (selectedTime + 1) % timeLabels.Length;
                needsRedraw = true;
            }
            else if (I386.GetKeyDown(KeyCode.Return) || I386.GetKeyDown(KeyCode.KeypadEnter))
            {
                StartCountdown();
            }
            return false;
        }

        static void StartCountdown()
        {
            state = GameState.Countdown;
            timeLimit = timeLimits[selectedTime];
            countdownTick = 3;
            countdownTimer = 0f;
            needsRedraw = true;
        }

        static bool UpdateCountdown()
        {
            countdownTimer += Time.deltaTime;
            if (countdownTimer >= 1f)
            {
                countdownTimer -= 1f;
                countdownTick--;
                if (countdownTick <= 0)
                {
                    StartTyping();
                    return false;
                }
                needsRedraw = true;
            }

            if (needsRedraw)
            {
                I386.POS_ClearScreen();
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  " + diffLabels[selectedDiff] + " - " + timeLabels[selectedTime]);
                I386.POS_NewLine();
                I386.POS_WriteNewLine("  Get ready...");
                I386.POS_NewLine();
                I386.POS_WriteNewLine("         " + countdownTick.ToString());
                needsRedraw = false;
            }

            return false;
        }

        static string[] GetPool()
        {
            if (selectedDiff == 0) return easySentences;
            if (selectedDiff == 1) return mediumSentences;
            return hardSentences;
        }

        static string PickSentence()
        {
            string[] pool = GetPool();
            if (usedIndices.Count >= pool.Length)
                usedIndices.Clear();
            int idx = Random.Range(0, pool.Length);
            int attempts = 0;
            while (usedIndices.Contains(idx) && attempts < pool.Length * 2)
            {
                idx = Random.Range(0, pool.Length);
                attempts++;
            }
            usedIndices.Add(idx);
            return pool[idx];
        }

        static void StartTyping()
        {
            state = GameState.Typing;
            timeLeft = timeLimit;
            correctWords = 0;
            totalTyped = 0;
            lastTimerSecond = timeLeft;
            usedIndices.Clear();
            currentSentence = PickSentence();
            inputBuffer = "";
            needsRedraw = true;
        }

        static void DrainCharBuffer()
        {
            int lim = 64;
            while (lim-- > 0 && I386.POS_GetChar() != '\0') { }
        }

        static bool UpdateTyping()
        {
            DrainCharBuffer();
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                if (inputBuffer.Length > 0)
                    ScoreWords(inputBuffer, currentSentence);
                ShowResults();
                return false;
            }

            bool inputChanged = false;
            bool shift = I386.GetKey(KeyCode.LeftShift) || I386.GetKey(KeyCode.RightShift);

            if (I386.GetKeyDown(KeyCode.Backspace))
            {
                if (inputBuffer.Length > 0)
                {
                    inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
                    inputChanged = true;
                }
            }
            else if (I386.GetKeyDown(KeyCode.Space))
            {
                if (inputBuffer.Length > 0 && inputBuffer[inputBuffer.Length - 1] != ' ')
                {
                    inputBuffer += ' ';
                    inputChanged = true;
                    if (inputBuffer.TrimEnd() == currentSentence)
                    {
                        ScoreWords(inputBuffer, currentSentence);
                        currentSentence = PickSentence();
                        inputBuffer = "";
                        inputChanged = true;
                    }
                }
            }
            else
            {
                char typed = GetTypedChar(shift);
                if (typed != '\0' && inputBuffer.Length < currentSentence.Length + 20)
                {
                    inputBuffer += typed;
                    inputChanged = true;
                    if (inputBuffer == currentSentence)
                    {
                        ScoreWords(inputBuffer, currentSentence);
                        currentSentence = PickSentence();
                        inputBuffer = "";
                        inputChanged = true;
                    }
                }
            }

            int timerNow = Mathf.CeilToInt(timeLeft);
            int timerLast = Mathf.CeilToInt(lastTimerSecond);
            bool timerTicked = timerNow != timerLast;
            lastTimerSecond = timeLeft;

            if (needsRedraw || inputChanged || timerTicked)
            {
                DrawTypingScreen();
                needsRedraw = false;
            }

            return false;
        }

        static char GetTypedChar(bool shift)
        {
            if (I386.GetKeyDown(KeyCode.A)) return shift ? 'A' : 'a';
            if (I386.GetKeyDown(KeyCode.B)) return shift ? 'B' : 'b';
            if (I386.GetKeyDown(KeyCode.C)) return shift ? 'C' : 'c';
            if (I386.GetKeyDown(KeyCode.D)) return shift ? 'D' : 'd';
            if (I386.GetKeyDown(KeyCode.E)) return shift ? 'E' : 'e';
            if (I386.GetKeyDown(KeyCode.F)) return shift ? 'F' : 'f';
            if (I386.GetKeyDown(KeyCode.G)) return shift ? 'G' : 'g';
            if (I386.GetKeyDown(KeyCode.H)) return shift ? 'H' : 'h';
            if (I386.GetKeyDown(KeyCode.I)) return shift ? 'I' : 'i';
            if (I386.GetKeyDown(KeyCode.J)) return shift ? 'J' : 'j';
            if (I386.GetKeyDown(KeyCode.K)) return shift ? 'K' : 'k';
            if (I386.GetKeyDown(KeyCode.L)) return shift ? 'L' : 'l';
            if (I386.GetKeyDown(KeyCode.M)) return shift ? 'M' : 'm';
            if (I386.GetKeyDown(KeyCode.N)) return shift ? 'N' : 'n';
            if (I386.GetKeyDown(KeyCode.O)) return shift ? 'O' : 'o';
            if (I386.GetKeyDown(KeyCode.P)) return shift ? 'P' : 'p';
            if (I386.GetKeyDown(KeyCode.Q)) return shift ? 'Q' : 'q';
            if (I386.GetKeyDown(KeyCode.R)) return shift ? 'R' : 'r';
            if (I386.GetKeyDown(KeyCode.S)) return shift ? 'S' : 's';
            if (I386.GetKeyDown(KeyCode.T)) return shift ? 'T' : 't';
            if (I386.GetKeyDown(KeyCode.U)) return shift ? 'U' : 'u';
            if (I386.GetKeyDown(KeyCode.V)) return shift ? 'V' : 'v';
            if (I386.GetKeyDown(KeyCode.W)) return shift ? 'W' : 'w';
            if (I386.GetKeyDown(KeyCode.X)) return shift ? 'X' : 'x';
            if (I386.GetKeyDown(KeyCode.Y)) return shift ? 'Y' : 'y';
            if (I386.GetKeyDown(KeyCode.Z)) return shift ? 'Z' : 'z';
            if (I386.GetKeyDown(KeyCode.Alpha0)) return shift ? ')' : '0';
            if (I386.GetKeyDown(KeyCode.Alpha1)) return shift ? '!' : '1';
            if (I386.GetKeyDown(KeyCode.Alpha2)) return shift ? '@' : '2';
            if (I386.GetKeyDown(KeyCode.Alpha3)) return shift ? '#' : '3';
            if (I386.GetKeyDown(KeyCode.Alpha4)) return shift ? '$' : '4';
            if (I386.GetKeyDown(KeyCode.Alpha5)) return shift ? '%' : '5';
            if (I386.GetKeyDown(KeyCode.Alpha6)) return shift ? '^' : '6';
            if (I386.GetKeyDown(KeyCode.Alpha7)) return shift ? '&' : '7';
            if (I386.GetKeyDown(KeyCode.Alpha8)) return shift ? '*' : '8';
            if (I386.GetKeyDown(KeyCode.Alpha9)) return shift ? '(' : '9';
            if (I386.GetKeyDown(KeyCode.Minus)) return shift ? '_' : '-';
            if (I386.GetKeyDown(KeyCode.Equals)) return shift ? '+' : '=';
            if (I386.GetKeyDown(KeyCode.Comma)) return shift ? '<' : ',';
            if (I386.GetKeyDown(KeyCode.Period)) return shift ? '>' : '.';
            if (I386.GetKeyDown(KeyCode.Slash)) return shift ? '?' : '/';
            if (I386.GetKeyDown(KeyCode.Semicolon)) return shift ? ':' : ';';
            if (I386.GetKeyDown(KeyCode.Quote)) return shift ? '"' : '\'';
            return '\0';
        }

        static void ScoreWords(string typed, string expected)
        {
            string[] tw = typed.TrimEnd().Split(' ');
            string[] ew = expected.Split(' ');
            for (int i = 0; i < tw.Length; i++)
            {
                if (tw[i].Length == 0) continue;
                totalTyped++;
                if (i < ew.Length && tw[i] == ew[i]) correctWords++;
            }
        }

        static void DrawTypingScreen()
        {
            if (currentSentence == null) return;

            I386.POS_ClearScreen();
            I386.POS_NewLine();
            I386.POS_WriteNewLine("  Time: " + Mathf.CeilToInt(timeLeft).ToString() + "s   Words: " + correctWords.ToString());
            I386.POS_NewLine();

            const int W = 54;
            int sentenceLen = currentSentence.Length;
            int inputLen = inputBuffer.Length;

            int pos = 0;
            while (pos < sentenceLen)
            {
                int end = Mathf.Min(pos + W, sentenceLen);
                if (end < sentenceLen && currentSentence[end] != ' ')
                {
                    int lastSpace = currentSentence.LastIndexOf(' ', end - 1, end - pos);
                    if (lastSpace > pos) end = lastSpace + 1;
                }
                I386.POS_WriteNewLine("  " + currentSentence.Substring(pos, end - pos));
                pos = end;
            }
            I386.POS_NewLine();


            pos = 0;
            int typedPos = 0;
            while (pos < sentenceLen)
            {
                int end = Mathf.Min(pos + W, sentenceLen);
                if (end < sentenceLen && currentSentence[end] != ' ')
                {
                    int lastSpace = currentSentence.LastIndexOf(' ', end - 1, end - pos);
                    if (lastSpace > pos) end = lastSpace + 1;
                }
                int lineLen = end - pos;

                string typedLine = "  ";
                string markerLine = "  ";
                bool hasMarkers = false;

                for (int i = 0; i < lineLen; i++)
                {
                    int absIdx = pos + i;
                    if (typedPos < inputLen)
                    {
                        char t = inputBuffer[typedPos];
                        char e = absIdx < sentenceLen ? currentSentence[absIdx] : '\0';
                        typedLine += t.ToString();
                        if (t == e) { markerLine += ' '; }
                        else { markerLine += '^'; hasMarkers = true; }
                        typedPos++;
                    }
                    else if (absIdx == inputLen)
                    {
                        typedLine += "_";
                        markerLine += ' ';
                        break;
                    }
                    else break;
                }

                if (typedPos == inputLen && pos + lineLen >= inputLen && !typedLine.Contains("_"))
                    typedLine += "_";

                if (typedLine.Length > 2)
                    I386.POS_WriteNewLine(typedLine);
                if (hasMarkers)
                    I386.POS_WriteNewLine(markerLine);

                pos = end;
                if (typedPos >= inputLen) break;
            }

            if (inputLen == 0)
                I386.POS_WriteNewLine("  _");
        }

        static void ShowResults()
        {
            state = GameState.Results;
            float minutes = timeLimit / 60f;
            int wpm = Mathf.RoundToInt(correctWords / minutes);
            int accuracy = totalTyped > 0 ? Mathf.RoundToInt((float)correctWords / totalTyped * 100f) : 0;

            I386.POS_ClearScreen();
            I386.POS_NewLine();
            I386.POS_WriteNewLine("  *** TULOS ***");
            I386.POS_NewLine();
            I386.POS_WriteNewLine("  Difficulty: " + diffLabels[selectedDiff]);
            I386.POS_WriteNewLine("  Time:       " + ((int)timeLimit).ToString() + "s");
            I386.POS_WriteNewLine("  Words:      " + correctWords.ToString() + " / " + totalTyped.ToString());
            I386.POS_WriteNewLine("  WPM:        " + wpm.ToString());
            I386.POS_WriteNewLine("  Accuracy:   " + accuracy.ToString() + "%");
            I386.POS_NewLine();

            string rating;
            if (wpm >= 80) rating = "  Hemmetin nopea! Oletko ihminen?";
            else if (wpm >= 60) rating = "  Mahtavaa! Tosi nopea.";
            else if (wpm >= 40) rating = "  Ihan ok. Harjoittele lisaa.";
            else if (wpm >= 20) rating = "  Hitaanpuoleinen. Yrita uudelleen.";
            else rating = "  Pakkanen hidastaa sormia...";

            I386.POS_WriteNewLine(rating);
            I386.POS_NewLine();
            I386.POS_WriteNewLine("  ENTER = play again   ALT+Q = quit");
            needsRedraw = false;
        }

        static bool UpdateResults()
        {
            if (I386.GetKeyDown(KeyCode.Return) || I386.GetKeyDown(KeyCode.KeypadEnter))
            {
                state = GameState.PickDiff;
                needsRedraw = true;
                return false;
            }
            return false;
        }
    }
}