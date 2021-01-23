using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace UShell.Commands
{
    public class CommandUnity : MonoBehaviour, ICommand
    {
        #region FIELDS
        private static CommandUnity _instance;

        [SerializeField]
        private AudioSource _beepSource;
        [SerializeField]
        private float _pingTimeout = 5f;
        [SerializeField]
        private bool _hideDevConsole;

#if !UNITY_WEBGL
        private Ping _ping;
        private float _time;
        private bool _isPinging;
#endif

        private bool _planRestart;
        private DateTime _restartDate;
        private DateTime _lastTime;

        private AudioClip _generatedClip;
        #endregion

        #region MESSAGES
        void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            if (this.transform.parent != null)
                this.transform.SetParent(null);
            DontDestroyOnLoad(this.gameObject);

            Shell.Main.RegisterCmd("quit", this);
            Shell.Main.RegisterCmd("debug", this); //OP Only
            Shell.Main.RegisterCmd("path", this);
            Shell.Main.RegisterCmd("ppref", this); //OP Only
            Shell.Main.RegisterCmd("beep", this);
            Shell.Main.RegisterCmd("volume", this);
            Shell.Main.RegisterCmd("lds", this); //OP Only
            Shell.Main.RegisterCmd("fps", this);
            Shell.Main.RegisterCmd("screen", this);
            Shell.Main.RegisterCmd("ping", this);
            Shell.Main.RegisterCmd("screenshot", this);
            Shell.Main.RegisterCmd("auth", this);
            Shell.Main.RegisterCmd("time", this); //OP Only
            Shell.Main.RegisterCmd("mic", this);
            Shell.Main.RegisterCmd("cursor", this); //OP Only
            Shell.Main.RegisterCmd("webcam", this);
            Shell.Main.RegisterCmd("tier", this);
            Shell.Main.RegisterCmd("restart", this);
        }
        void Update()
        {
            if (Debug.isDebugBuild && _hideDevConsole && Debug.developerConsoleVisible)
                Debug.ClearDeveloperConsole();

#if !UNITY_WEBGL
            if (_isPinging)
            {
                if (_time > _pingTimeout)
                {
                    Debug.Log("ping timeout (" + _ping.ip + ")");
                    _ping = null;
                    _isPinging = false;
                }
                else if (_ping.isDone)
                {
                    Debug.Log("pong! (" + _ping.ip + ": " + _ping.time + "ms)");
                    _ping = null;
                    _isPinging = false;
                }
                
                _time += Time.deltaTime;
            }
#endif

            if (_planRestart && _lastTime.TimeOfDay < _restartDate.TimeOfDay && DateTime.Now.TimeOfDay >= _restartDate.TimeOfDay)
                restartProcess();

            _lastTime = DateTime.Now;
        }
        #endregion

        #region METHODS
        public string[] GetSyntaxes(string label)
        {
            switch (label)
            {
                case "ping":
                    return new string[] { "address" };
                case "ppref":
                    return new string[]
                    {
                        "[-DS] [-rk key] [-fis key [value]]",
                        "-D",
                        "-S",
                        "-r key",
                        "-k key",
                        "-f key [value]",
                        "-i key [value]",
                        "-s key [value]"
                    };
                case "lds":
                    return new string[] { "build-index|scene-name" };
                case "screenshot":
                    return new string[] {
                        "[filename [supersize]]",
                        "",
                        "filename",
                        "filename supersize"
                    };
                case "fps":
                    return new string[] { "[rate]" };
                case "debug":
                    return new string[]
                    {
                        "[-aexlw [message]] [-BVCDG] [-H [true|false]] [-L start end] [-R start direction]",
                        "",
                        "-a [message]",
                        "-e [message]",
                        "-x [message]",
                        "-l [message]",
                        "-w [message]",
                        "-B",
                        "-V",
                        "-C",
                        "-D",
                        "-G",
                        "-H [true|false]",
                        "-L start end",
                        "-R start direction"
                    };
                case "volume":
                    return new string[] { "[value]" };
                case "beep":
                    return new string[] { "[frequency]" };
                case "quit":
                    return new string[] { "[exitcode]" };
                case "auth":
                    return new string[] {
                        "[-mw]",
                        "",
                        "-m",
                        "-w"
                    };
                case "time":
                    return new string[] {
                        "[-f fixed-delta-time] [-m maximum-delta-time] [-s time-scale] [-p maximum-particle-delta-time]",
                        "",
                        "-f fixedDeltaTime",
                        "-m maximumDeltaTime",
                        "-s timeScale",
                        "-p maximumParticleDeltaTime",
                    };
                case "cursor":
                    return new string[] {
                        "[-hslcn]",
                        "",
                        "-h",
                        "-s",
                        "-l",
                        "-c",
                        "-n"
                    };
                case "restart":
                    return new string[]
                    {
                        "[-p [plan-restart]] [-t [restart-time]]",
                        "",
                        "-p [true|false]",
                        "-t [restart-time]",
                    };
            }
            return new string[0];
        }
        public string[] GetInfos(string label)
        {
            switch (label)
            {
                case "ping":
                    return new string[] { "ping the ip address" };
                case "quit":
                    return new string[] { "exit the game (has no effect in the Unity Editor)" };
                case "ppref":
                    return new string[]
                    {
                        "manage player preferences",
                        "delete all the player preferences entries",
                        "write all modified preferences to disk",
                        "remove an entry specified by a key",
                        "return true if an entry exists with a given key",
                        "log/modify the value of a key as a float",
                        "log/modify the value of a key as an integer",
                        "log/modify the value of a key as a string"
                    };
                case "lds":
                    return new string[] { "load a scene in single mode" };
                case "screen":
                    return new string[] { "log screen informations" };
                case "screenshot":
                    return new string[]
                    {
                        "take a screenshot",
                        "take a screenshot and save it to the game data folder",
                        "take a screenshot and save it to the game data folder with a specified name",
                        "take a screenshot to the indicated path with a specified supersize"
                    };
                case "fps":
                    return new string[] { "cap the framerate (-1: no limit)" };
                case "debug":
                    return new string[]
                    {
                        "debugging tools",
                        "same as using the -l option",
                        "assert",
                        "error",
                        "exception",
                        "log",
                        "warning",
                        "pauses the editor",
                        "is the developer console visible?",
                        "clear the developer console",
                        "is it a development build?",
                        "draw a grid as gizmos (only visible in the editor)",
                        "is the developer console always hidden?",
                        "draw a line between start and end points",
                        "draw a ray from a start point with a given direction"
                    };
                case "beep":
                    return new string[] { "play a short sound" };
                case "volume":
                    return new string[] { "log and modify the general sound volume of Unity" };
                case "path":
                    return new string[] { "log important paths" };
                case "mic":
                    return new string[] { "log informations about the user microphones" };
                case "webcam":
                    return new string[] { "log informations about the user webcams" };
                case "auth":
                    return new string[]
                    {
                        "handle webcam and microphone permissions on some platforms",
                        "log permissions",
                        "ask the user the permission to use the microphone",
                        "ask the user the permission to use the webcam"
                    };
                case "time":
                    return new string[] {
                        "handle Unity times",
                        "log Unity times",
                    };
                case "cursor":
                    return new string[] {
                        "handle cursor visibility and lock state",
                        "log cursor states",
                        "hide the cursor",
                        "show the cursor",
                        "lock the cursor to the center of the screen",
                        "confine the cursor inside the game window",
                        "free the cursor"
                    };
                case "tier":
                    return new string[] { "log the current graphic tier" };
                case "restart":
                    return new string[]
                    {
                        "manage restart of the process",
                        "restart the current process, with the same arguments",
                        "plan the restart of the process",
                        "the process will be restarted at this time",
                    };
            }
            return new string[0];
        }
        public string GetCompletion(string label, string[] args, out List<string> options)
        {
            if (label == "lds")
            {
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                string[] scenes = new string[sceneCount];
                for (int i = 0; i < sceneCount; i++)
                    scenes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));

                return Utils.GetCompletion(args[0], args.Length > 1, out options, scenes);
            }

            options = new List<string>();
            return "";
        }

        public void Execute(string label, string[] args)
        {
            switch (label)
            {
                case "ping":
                    executePing(args);
                    break;
                case "quit":
                    executeQuit(args);
                    break;
                case "ppref":
                    executePPREF(args);
                    break;
                case "lds":
                    executeLDS(args);
                    break;
                case "screen":
                    executeScreen(args);
                    break;
                case "screenshot":
                    executeScreenshot(args);
                    break;
                case "fps":
                    executeFPS(args);
                    break;
                case "debug":
                    executeDebug(args);
                    break;
                case "volume":
                    executeVolume(args);
                    break;
                case "beep":
                    executeBeep(args);
                    break;
                case "path":
                    executePath(args);
                    break;
                case "auth":
                    executeAuth(args);
                    break;
                case "time":
                    executeTime(args);
                    break;
                case "mic":
                    executeMic(args);
                    break;
                case "cursor":
                    executeCursor(args);
                    break;
                case "webcam":
                    executeWebcam(args);
                    break;
                case "tier":
                    executeTier(args);
                    break;
                case "restart":
                    executeRestart(args);
                    break;
            }
        }

        private void executePing(string[] args)
        {
#if !UNITY_WEBGL
            if (args.Length == 1)
            {
                _ping = new Ping(args[0]);
                Debug.Log("trying to ping " + args[0] + "...");
                _time = 0f;
                _isPinging = true;
            }
            else
                throw new ArgumentException();
#endif
        }
        private void executeQuit(string[] args)
        {
            if (args.Length == 0)
            {
                Application.Quit();
            }
            else if (args.Length == 1)
            {
                int exitCode = Utils.IntParse(args[0]);
                Application.Quit(exitCode);
            }
            else
                throw new ArgumentException();
        }
        private void executeLDS(string[] args)
        {
            if (args.Length == 1)
            {
                string sceneName;
                if (int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sceneId))
                    sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(sceneId));
                else
                    sceneName = args[0];

                Debug.Log("loading scene " + sceneName + "...");

                //Hack to keep consistent logs 
                if (string.IsNullOrEmpty(sceneName))
                    sceneName = " ";
                
                SceneManager.LoadScene(sceneName);
            }
            else
                throw new ArgumentException();
        }
        private void executeScreenshot(string[] args)
        {
            if (args.Length == 0)
            {
                string path =
                    Application.dataPath +
                    "/screenshot" + "_" +
                    DateTime.Now.Year + "_" +
                    DateTime.Now.Month + "_" +
                    DateTime.Now.Day + "_" +
                    DateTime.Now.Hour + "_" +
                    DateTime.Now.Minute + "_" +
                    DateTime.Now.Second +
                    ".png";
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log(path);
            }
            else if (args.Length == 1)
            {
                ScreenCapture.CaptureScreenshot(Application.dataPath + "/" + args[0]);
                Debug.Log(Application.dataPath + "/" + args[0]);
            }
            else if (args.Length == 2)
            {
                int supersize = Utils.IntParse(args[1]);
                ScreenCapture.CaptureScreenshot(Application.dataPath + "/" + args[0], supersize);
                Debug.Log(Application.dataPath + "/" + args[0]);
            }
            else
                throw new ArgumentException();
        }
        private void executeDebug(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log("");
            }
            else
            {
                string message = string.Join(" ", args, 1, args.Length - 1);
                switch (args[0])
                {
                    case "-a":
                        Debug.LogAssertion(message, this);
                        break;
                    case "-e":
                        Debug.LogError(message, this);
                        break;
                    case "-x":
                        Debug.LogException(new Exception(message), this);
                        break;
                    case "-l":
                        Debug.Log(message, this);
                        break;
                    case "-w":
                        Debug.LogWarning(message, this);
                        break;
                    case "-B":
                        Debug.Break();
                        break;
                    case "-V":
                        Debug.Log(Debug.developerConsoleVisible);
                        break;
                    case "-C":
                        Debug.ClearDeveloperConsole();
                        break;
                    case "-D":
                        Debug.Log(Debug.isDebugBuild);
                        break;
                    case "-H":
                        if (args.Length == 1)
                            Debug.Log(_hideDevConsole);
                        else if (args.Length == 2)
                        {
                            bool hideDevConsole = Utils.BoolParse(args[1]);
                            _hideDevConsole = hideDevConsole;
                        }
                        else
                            throw new ArgumentException();
                        break;
                    case "-L":
                        if (args.Length == 3)
                        {
                            if (Utils.TryParseVector3(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out Vector3 start) &&
                                Utils.TryParseVector3(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out Vector3 end)
                            )
                            {
                                Debug.DrawLine(start, end, Color.black, 10f, false);
                            }
                            else
                                Debug.LogError("cannot parse \'" + args[1] + "\' to Vector3");
                        }
                        else
                            throw new ArgumentException();
                        break;
                    case "-R":
                        if (args.Length == 3)
                        {
                            if (Utils.TryParseVector3(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out Vector3 start) &&
                                Utils.TryParseVector3(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out Vector3 dir)
                            )
                            {
                                Debug.DrawRay(start, dir, Color.black, 10f, false);
                            }
                            else
                                Debug.LogError("cannot parse \'" + args[1] + "\' to Vector3");
                        }
                        else
                            throw new ArgumentException();
                        break;
                    case "-G":
                        int size = 64;
                        Color color = Color.grey;
                        float duration = 10f;
                        bool depthTest = false;
                        for (int i = 1 - size; i < size; i++)
                        {
                            Debug.DrawLine(new Vector3(-size, 0f, i), new Vector3(size, 0f, i), color, duration, depthTest);
                            Debug.DrawLine(new Vector3(i, 0f, -size), new Vector3(i, 0f, size), color, duration, depthTest);
                        }
                        break;
                    default:
                        Debug.Log(string.Join(" ", args, 0, args.Length));
                        break;
                }
            }
        }
        private void executeVolume(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log(AudioListener.volume);
            }
            else if (args.Length == 1)
            {
                float volume = Utils.FloatParse(args[0]);
                volume = Mathf.Clamp01(volume); //REALLY IMPORTANT!
                AudioListener.volume = volume;
            }
            else
                throw new ArgumentException();
        }
        private void executeBeep(string[] args)
        {
            if (_beepSource == null)
            {
                Debug.LogError("the audio source is not set!");
                return;
            }

            if (args.Length == 0)
            {
                AudioClip.Destroy(_generatedClip);
                _generatedClip = generateAudioClip("beep", 44100, 44100, 440f);
                _beepSource.clip = _generatedClip;

                _beepSource.Stop();
                _beepSource.Play();
            }
            else if (args.Length == 1)
            {
                float soundFrequency = Utils.FloatParse(args[0]);
                soundFrequency = Mathf.Clamp(soundFrequency , -1000f, 1000f); //REALLY IMPORTANT!

                AudioClip.Destroy(_generatedClip);
                _generatedClip = generateAudioClip("beep", 44100, 44100, soundFrequency);
                _beepSource.clip = _generatedClip;

                _beepSource.Stop();
                _beepSource.Play();
            }
            else
                throw new ArgumentException();
        }
        private void executePPREF(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "-D")
                    PlayerPrefs.DeleteAll();
                else if (args[0] == "-S")
                    PlayerPrefs.Save();
                else
                    throw new ArgumentException();
            }
            else if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "-r":
                        PlayerPrefs.DeleteKey(args[1]);
                        break;
                    case "-k":
                        Debug.Log(PlayerPrefs.HasKey(args[1]));
                        break;
                    case "-f":
                        Debug.Log(PlayerPrefs.GetFloat(args[1]));
                        break;
                    case "-i":
                        Debug.Log(PlayerPrefs.GetInt(args[1]));
                        break;
                    case "-s":
                        Debug.Log(PlayerPrefs.GetString(args[1]));
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else if (args.Length == 3)
            {
                switch (args[0])
                {
                    case "-f":
                        float floatValue = Utils.FloatParse(args[2]);
                        PlayerPrefs.SetFloat(args[1], floatValue);
                        break;
                    case "-i":
                        int intValue = Utils.IntParse(args[2]);
                        PlayerPrefs.SetInt(args[1], intValue);
                        break;
                    case "-s":
                        try {
                            PlayerPrefs.SetString(args[1], args[2]);
                        } catch (PlayerPrefsException e) {
                            Debug.LogError(e.Message);
                        }
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
                throw new ArgumentException();
        }
        private void executeFPS(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log(Application.targetFrameRate);
            }
            else if (args.Length == 1)
            {
                int target = Utils.IntParse(args[0]);
                Application.targetFrameRate = target;
            }
            else
                throw new ArgumentException();
        }
        private void executeScreen(string[] args)
        {
            string log;

            Resolution currentResolution = Screen.currentResolution;
            log = "Screen resolution: " + currentResolution.width + "x" + currentResolution.height + " (" + currentResolution.refreshRate + "Hz)\n";
            log += "Window size: " + Screen.width + "x" + Screen.height + "\n";
            log += "Fullscreen: " + Screen.fullScreen + "\n";
            log += "Fullscreen mode: " + (FullScreenMode)Screen.fullScreenMode + "\n";
            log += "DPI: " + Screen.dpi + "\n";

            Resolution[] resolutions = Screen.resolutions;
            log += "Available screen resolutions: " + resolutions.Length + "\n";
            for (int i = 0; i < resolutions.Length; i++)
                log += "\t" + i + "- " + resolutions[i].width + "x" + resolutions[i].height + " (" + resolutions[i].refreshRate + "Hz)\n";

            Debug.Log(log);
        }
        private void executePath(string[] args)
        {
            Debug.Log(
                "absolute url       " + Application.absoluteURL +
                "\ndata               " + Application.dataPath +
                "\npersistent data    " + Application.persistentDataPath +
                "\nstreaming assets   " + Application.streamingAssetsPath +
                "\nconsole log        " + Application.consoleLogPath +
                "\ncache              " + Application.temporaryCachePath +
                "\nworking directory  " + Environment.CurrentDirectory +
                "\nassembly           " + Assembly.GetExecutingAssembly().Location
            );
        }
        private void executeAuth(string[] args)
        {
            if (args.Length == 0)
            {
                bool mic = Application.HasUserAuthorization(UserAuthorization.Microphone);
                bool webCam = Application.HasUserAuthorization(UserAuthorization.WebCam);
                Debug.Log(
                    "mic       "   + mic +
                    "\nwebcam    " + webCam
                );
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-m":
                        Application.RequestUserAuthorization(UserAuthorization.Microphone);
                        break;
                    case "-w":
                        Application.RequestUserAuthorization(UserAuthorization.WebCam);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
                throw new ArgumentException();
        }
        private void executeTime(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log(
                    "current time                   " + DateTime.Now + "\n" +
                    "launch time                    " + DateTime.Now.AddSeconds(-Time.realtimeSinceStartup) + "\n" +
                    "fixed delta time               " + Time.fixedDeltaTime + "\n" +
                    "maximum delta time             " + Time.maximumDeltaTime + "\n" +
                    "time scale                     " + Time.timeScale + "\n" +
                    "maximum particle delta time    " + Time.maximumParticleDeltaTime
                );
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-f":
                        Debug.Log(Time.fixedDeltaTime);
                        break;
                    case "-m":
                        Debug.Log(Time.maximumDeltaTime);
                        break;
                    case "-s":
                        Debug.Log(Time.timeScale);
                        break;
                    case "-p":
                        Debug.Log(Time.maximumParticleDeltaTime);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "-f":
                        float fixedDeltaTime = Utils.FloatParse(args[1]);
                        Time.fixedDeltaTime = fixedDeltaTime;
                        break;
                    case "-m":
                        float maximumDeltaTime = Utils.FloatParse(args[1]);
                        Time.maximumDeltaTime = maximumDeltaTime;
                        break;
                    case "-s":
                        float timeScale = Utils.FloatParse(args[1]);
                        Time.timeScale = timeScale;
                        break;
                    case "-p":
                        float maximumParticleDeltaTime = Utils.FloatParse(args[1]);
                        Time.maximumParticleDeltaTime = maximumParticleDeltaTime;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
                throw new ArgumentException();
        }
        private void executeMic(string[] args)
        {
#if !UNITY_WEBGL && !UNITY_STANDALONE_OSX
            if (Microphone.devices.Length <= 0)
                return;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                string name = Microphone.devices[i];
                Microphone.GetDeviceCaps(name, out int minFreq, out int maxFreq);
                strBuilder.Append(i).Append(") ").Append(name).Append("\n   - frequency : ").Append(minFreq).Append("Hz to ").Append(maxFreq).Append("Hz\n   - is recording : ").Append(Microphone.IsRecording(name)).Append("\n   - position : ").Append(Microphone.GetPosition(name)).Append("\n");
            }

            Debug.Log(strBuilder.ToString());
#endif
        }
        private void executeCursor(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log(
                    "visible       "   + Cursor.visible +
                    "\nlock state    " + Cursor.lockState
                );
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-h":
                        Cursor.visible = false;
                        break;
                    case "-s":
                        Cursor.visible = true;
                        break;
                    case "-l":
                        Cursor.lockState = CursorLockMode.Locked;
                        break;
                    case "-c":
                        Cursor.lockState = CursorLockMode.Confined;
                        break;
                    case "-n":
                        Cursor.lockState = CursorLockMode.None;
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
                throw new ArgumentException();
        }
        private void executeWebcam(string[] args)
        {
#if !UNITY_STANDALONE_OSX
            string log = "";
            var devices = WebCamTexture.devices;

            for (int i = 0; i < devices.Length; i++)
                log += devices[i].name + "\n";

            Debug.Log(log);
#endif
        }
        private void executeTier(string[] args)
        {
            Debug.Log(Graphics.activeTier);
        }
        private void executeRestart(string[] args)
        {
            if (args.Length == 0)
            {
                restartProcess();
            }
            else if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-t":
                        Debug.Log(_restartDate);
                        break;
                    case "-p":
                        Debug.Log(_planRestart);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "-t":
                        _restartDate = DateTime.Parse(args[1]);
                        break;
                    case "-p":
                        _planRestart = Utils.BoolParse(args[1]);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
                throw new ArgumentException();
        }

        private static void restartProcess()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = Environment.GetCommandLineArgs()[0];
            psi.Arguments = Environment.CommandLine;
            psi.UseShellExecute = true;
            psi.CreateNoWindow = false;
            psi.WindowStyle = ProcessWindowStyle.Normal;
            Process.Start(psi);
            Application.Quit();
        }

        private static AudioClip generateAudioClip(string name, int lengthSamples, int sampleFrequency, float soundFrequency)
        {
            soundFrequency = Mathf.Clamp(soundFrequency, -1000f, 1000f); //REALLY IMPORTANT!

            float[] data = new float[lengthSamples];
            for (int i = 0; i < data.Length; i++)
                data[i] = .25f * (float)Math.Sin((2f * Math.PI * soundFrequency) / sampleFrequency * i);

            AudioClip audioClip = AudioClip.Create(name, lengthSamples, 1, sampleFrequency, false);
            audioClip.SetData(data, 0);

            return audioClip;
        }
        #endregion
    }
}
