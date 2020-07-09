using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.IO;

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
                    return new string[] { "build-index" };
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
                    return new string[] { "handle permission to use the webcam or microphone on some platform" };
                case "time":
                    return new string[] { "handle Unity times" };
                case "cursor":
                    return new string[] { "handle cursor visibility and lock state" };
                case "tier":
                    return new string[] { "log the current graphic tier" };
            }
            return new string[0];
        }
        public string GetCompletion(string label, string args, out List<string> options)
        {
            if (label == "lds")
            {
                int sceneCount = SceneManager.sceneCountInBuildSettings;
                string[] scenes = new string[sceneCount];
                for (int i = 0; i < sceneCount; i++)
                    scenes[i] = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));

                return Utils.GetCompletion(args, out options, scenes.GetEnumerator());
            }

            options = new List<string>();
            return args;
        }

        public bool Execute(string label, string[] args)
        {
            switch (label)
            {
                case "ping":
                    return executePing(args);
                case "quit":
                    return executeQuit(args);
                case "ppref":
                    return executePPREF(args);
                case "lds":
                    return executeLDS(args);
                case "screen":
                    return executeScreen(args);
                case "screenshot":
                    return executeScreenshot(args);
                case "fps":
                    return executeFPS(args);
                case "debug":
                    return executeDebug(args);
                case "volume":
                    return executeVolume(args);
                case "beep":
                    return executeBeep(args);
                case "path":
                    return executePath(args);
                case "auth":
                    return executeAuth(args);
                case "time":
                    return executeTime(args);
                case "mic":
                    return executeMic(args);
                case "cursor":
                    return executeCursor(args);
                case "webcam":
                    return executeWebcam(args);
                case "tier":
                    return executeTier(args);
            }

            return true;
        }

        private bool executePing(string[] args)
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
                return false;
#endif
            return true;
        }
        private bool executeQuit(string[] args)
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
                return false;
            return true;
        }
        private bool executeLDS(string[] args)
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
                return false;
            return true;
        }
        private bool executeScreenshot(string[] args)
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
                return false;
            return true;
        }
        private bool executeDebug(string[] args)
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
                        return true;
                    case "-e":
                        Debug.LogError(message, this);
                        return true;
                    case "-x":
                        Debug.LogException(new Exception(message), this);
                        return true;
                    case "-l":
                        Debug.Log(message, this);
                        return true;
                    case "-w":
                        Debug.LogWarning(message, this);
                        return true;
                    case "-B":
                        Debug.Break();
                        return true;
                    case "-V":
                        Debug.Log(Debug.developerConsoleVisible);
                        return true;
                    case "-C":
                        Debug.ClearDeveloperConsole();
                        return true;
                    case "-D":
                        Debug.Log(Debug.isDebugBuild);
                        return true;
                    case "-H":
                        if (args.Length == 1)
                            Debug.Log(_hideDevConsole);
                        else if (args.Length == 2)
                        {
                            bool hideDevConsole = Utils.BoolParse(args[1]);
                            _hideDevConsole = hideDevConsole;
                        }
                        else
                            return false;
                        return true;
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
                            return false;
                        return true;
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
                            return false;
                        return true;
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
                        return true;
                    default:
                        Debug.Log(string.Join(" ", args, 0, args.Length));
                        return true;
                }
            }
            return true;
        }
        private bool executeVolume(string[] args)
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
                return false;
            return true;
        }
        private bool executeBeep(string[] args)
        {
            if (_beepSource == null)
            {
                Debug.LogError("the audio source is not set!");
                return true;
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
                return false;
            return true;
        }
        private bool executePPREF(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "-D")
                    PlayerPrefs.DeleteAll();
                else if (args[0] == "-S")
                    PlayerPrefs.Save();
                else
                    return false;
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
                        return false;
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
                        return false;
                }
            }
            else
                return false;
            return true;
        }
        private bool executeFPS(string[] args)
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
                return false;
            return true;
        }
        private bool executeScreen(string[] args)
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
            return true;
        }
        private bool executePath(string[] args)
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
            return true;
        }
        private bool executeAuth(string[] args)
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
                        return false;
                }
            }
            else
                return false;
            return true;
        }
        private bool executeTime(string[] args)
        {
            if (args.Length == 0)
            {
                Debug.Log(
                    "fixed delta time               " + Time.fixedDeltaTime + "\n" +
                    "maximum delta time             " + Time.maximumDeltaTime + "\n" +
                    "time scale                     " + Time.timeScale + "\n" +
                    "maximum particle delta time    " + Time.maximumParticleDeltaTime + "\n"
                );
            }
            else if (args.Length == 2)
            {
                switch (args[0])
                {
                    case "-s":
                        float timescale = Utils.FloatParse(args[1]);
                        Time.timeScale = timescale;
                        break;
                    default:
                        return false;
                }
            }
            else
                return false;
            return true;
        }
        private bool executeMic(string[] args)
        {
#if !UNITY_WEBGL
            if (Microphone.devices.Length <= 0)
                return true;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                string name = Microphone.devices[i];
                Microphone.GetDeviceCaps(name, out int minFreq, out int maxFreq);
                strBuilder.Append(i).Append(") ").Append(name).Append("\n   - frequency : ").Append(minFreq).Append("Hz to ").Append(maxFreq).Append("Hz\n   - is recording : ").Append(Microphone.IsRecording(name)).Append("\n   - position : ").Append(Microphone.GetPosition(name)).Append("\n");
            }

            Debug.Log(strBuilder.ToString());
#endif
            return true;
        }
        private bool executeCursor(string[] args)
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
                        return false;
                }
            }
            else
                return false;
            return true;
        }
        private bool executeWebcam(string[] args)
        {
            string log = "";
            var devices = WebCamTexture.devices;

            for (int i = 0; i < devices.Length; i++)
                log += devices[i].name + "\n";

            Debug.Log(log);
            return true;
        }
        private bool executeTier(string[] args)
        {
            Debug.Log(Graphics.activeTier);
            return true;
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
