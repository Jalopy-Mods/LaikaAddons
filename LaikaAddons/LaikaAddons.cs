using JaLoader;
using System.Collections;
using System.Reflection.Emit;
using UnityEngine;
using static iTween;

namespace LaikaAddons
{
    public class LaikaAddons : Mod
    {
        public override string ModID => "LaikaAddons"; // The mod's ID. Try making it as unique as possible, to avoid conflitcting IDs.
        public override string ModName => "Laika Addons"; // The mod's name. This is shown in the mods list. Does not need to be unique.
        public override string ModAuthor => "Leaxx"; // The mod's author (you). Also shown in the mods list.
        public override string ModDescription => "Adds various features to the Laika for more immersive gameplay!"; // The mod's description. This is also shown in the mods list, upon clicking on "More Info".
        public override string ModVersion => "1.0.2"; // The mod's version. Also shown in the mods list. If your mod is open-source on GitHub, make sure that you're using the same format as your release tags (for example, 1.0.0)
        public override string GitHubLink => "https://github.com/Jalopy-Mods/LaikaAddons"; // If your mod is open-source on GitHub, you can link it here to allow for automatic update-checking in-game. It compares the current ModVersion with the tag of the latest release (ex. 1.0.0 compared with 1.0.1)
        public override WhenToInit WhenToInit => WhenToInit.InGame; // When should the mod's OnEnable/Awake/Start/Update functions be called?

        public override bool UseAssets => true; // Does your mod use custom assetbundles?

        private Transform car;

        private KeyCode leftTurnSignalKey;
        private KeyCode rightTurnSignalKey;
        private KeyCode hazardsKey;
        private KeyCode lightsKey;
        private KeyCode hornKey;
        private KeyCode radioKey;
        private KeyCode wipersKey;
        private KeyCode wipersWaterKey;

        public bool leftTurnSignalOn;
        public bool rightTurnSignalOn;
        public bool hazardsOn;

        private GameObject cigLighterObject;
        private GameObject turnSignalLeverObject;
        private GameObject lightKnobObject;
        private GameObject hazardsButton;

        private Material commonMaterial;
        private Material commonDefaultMaterial;
        private Material turnSignalGlowMaterial;

        private CarLogicC carLogic;

        private readonly GameObject[] signals = new GameObject[4];

        private GameObject player;
        private AudioSource source;

        private AudioClip buttonSound;
        private AudioClip onSound;
        private AudioClip offSound;

        private HazardsButton hazardsScript;
        private TurnSignalLever turnSignalScript;
        private LightsKnob lightsKnobScript;

        float currentTime = 0f;

        private bool EnoughBattery
        {
            get
            {
                if (carLogic.GetComponent<CarPerformanceC>().installedBattery != null && carLogic.GetComponent<CarPerformanceC>().installedBattery.GetComponent<EngineComponentC>().charge > 0.0)
                {
                    return true;
                }

                return false;
            }
        }

        public override void SettingsDeclaration() // Declare all of your per-user settings here
        {
            base.SettingsDeclaration();

            InstantiateSettings();

            AddHeader("Fixes");
            AddToggle("CigLighterFix", "Fix cigarette lighter texture", true);

            AddHeader("Addons");
            AddToggle("AddTurnSignals", "Add turn signals", true);

            AddHeader("Keybinds");
            AddKeybind("TurnSignalLeftKey", "Left turn signal", KeyCode.Z);
            AddKeybind("TurnSignalRightKey", "Right turn signal", KeyCode.C);
            AddKeybind("HazardsKey", "Hazards", KeyCode.X);
            AddKeybind("LightsKey", "Lights", KeyCode.L);
            AddKeybind("HornKey", "Horn", KeyCode.H);
            AddKeybind("RadioKey", "Radio", KeyCode.R);
            AddKeybind("WipersKey", "Wipers", KeyCode.P);
            AddKeybind("WipersWaterKey", "Wipers Spray", KeyCode.LeftBracket);
        }

        public override void Start() // Default Unity Start() function
        {
            base.Start();

            commonMaterial = new Material(Shader.Find("Legacy Shaders/Diffuse"))
            {
                color = new Color32(150, 150, 150, 255),
                mainTexture = PNGToTexture("TextureMap"),
                name = "LaikaAddons_TextureMap"
            };

            var glowMaterial = ModHelper.Instance.GetGlowMaterial(commonMaterial);

            turnSignalGlowMaterial = new Material(Shader.Find("Legacy Shaders/Self-Illumin/Diffuse"))
            {
                color = new Color32(255, 136, 0, 255),
                name = "LaikaAddons_GlowMaterial"
            };
            
            car = ModHelper.Instance.laika.transform;
            player = ModHelper.Instance.player;

            leftTurnSignalKey = GetPrimaryKeybind("TurnSignalLeftKey");
            rightTurnSignalKey = GetPrimaryKeybind("TurnSignalRightKey");
            hazardsKey = GetPrimaryKeybind("HazardsKey");
            lightsKey = GetPrimaryKeybind("LightsKey");
            hornKey = GetPrimaryKeybind("HornKey");
            radioKey = GetPrimaryKeybind("RadioKey");
            wipersKey = GetPrimaryKeybind("WipersKey");
            wipersWaterKey = GetPrimaryKeybind("WipersWaterKey");

            car = car.transform.Find("TweenHolder/Frame");
            turnSignalLeverObject = car.Find("Indicators").gameObject;
            cigLighterObject = car.Find("CigLighter").gameObject;
            lightKnobObject = car.Find("gimballLock/WindowWipers").gameObject;

            var cigLighterObj = Instantiate(LoadAsset<GameObject>("ciglighter", "cigLighter", "", ".prefab"));
            var indicatorObj = Instantiate(LoadAsset<GameObject>("indicatorlever", "indicatorLever", "", ".prefab"));
            var lightsKnob = Instantiate(LoadAsset<GameObject>("lightknob", "lightKnob", "", ".prefab"));

            if (GetToggleValue("CigLighterFix") == true)
            {
                Mesh cigMesh = cigLighterObj.GetComponent<MeshFilter>().sharedMesh;
                cigLighterObject.GetComponent<MeshFilter>().mesh = cigMesh;
                cigLighterObject.GetComponent<MeshRenderer>().material = commonMaterial;
                cigLighterObject.transform.GetChild(0).gameObject.GetComponent<CigLighterC>().startMaterial = commonMaterial;
                cigLighterObject.transform.GetChild(0).gameObject.GetComponent<CigLighterC>().glowMaterial = glowMaterial;
            }

            if (GetToggleValue("AddTurnSignals") == true)
            {
                Mesh lightsKnobMesh = lightsKnob.GetComponent<MeshFilter>().sharedMesh;
                lightKnobObject.GetComponent<MeshFilter>().mesh = lightsKnobMesh;
                lightKnobObject.GetComponent<MeshRenderer>().material = commonMaterial;
                lightKnobObject.AddComponent<AudioSource>();
                lightKnobObject.AddComponent<BoxCollider>();
                lightKnobObject.tag = "CarInteractor";

                lightsKnobScript = lightKnobObject.AddComponent<LightsKnob>();
                lightsKnobScript.enabled = true;
                lightsKnobScript.glowMaterial = glowMaterial;

                Mesh indicatorMesh = indicatorObj.GetComponent<MeshFilter>().sharedMesh;
                turnSignalLeverObject.GetComponent<MeshFilter>().mesh = indicatorMesh;
                turnSignalScript = turnSignalLeverObject.transform.GetChild(0).gameObject.AddComponent<TurnSignalLever>();
                var orgTurnSignalScript = turnSignalLeverObject.transform.GetChild(0).gameObject.GetComponent<HeadlightLogicC>();
                turnSignalScript.enabled = false;

                hazardsButton = car.Find("HazardButton").GetChild(0).gameObject;
                var hazardsLogic = hazardsButton.GetComponent<HazardsLogicC>();
                hazardsScript = hazardsButton.AddComponent<HazardsButton>();
                hazardsScript.enabled = false;

                commonDefaultMaterial = hazardsLogic.startMaterial;
                buttonSound = hazardsLogic.audioSample;

                hazardsScript.positionRotation = hazardsLogic.positionRotation;
                hazardsScript.audioSample = turnSignalScript.audioSample = lightsKnobScript.audioSample = buttonSound;
                hazardsScript.startMaterial = commonDefaultMaterial;
                hazardsScript.glowMaterial = hazardsLogic.glowMaterial;

                lightsKnobScript.errorAudio = hazardsLogic.errorAudio;

                turnSignalScript.startMaterial = commonDefaultMaterial;
                turnSignalScript.glowMaterial = hazardsLogic.glowMaterial;

                Vector3[] turnSignalLeverPositions = new Vector3[3];

                turnSignalLeverPositions[0] = orgTurnSignalScript.position[0];
                turnSignalLeverPositions[1] = orgTurnSignalScript.position[1];
                turnSignalLeverPositions[2] = new Vector3(81.1f, -15, -90);

                turnSignalScript.position = turnSignalLeverPositions;

                Destroy(orgTurnSignalScript);
                turnSignalScript.enabled = true;

                Destroy(hazardsLogic);
                hazardsScript.enabled = true;

                carLogic = FindObjectOfType<CarLogicC>();
                lightsKnobScript.carLogic = carLogic.gameObject;

                source = carLogic.gameObject.GetComponent<AudioSource>();
                onSound = carLogic.flickAudioOn;
                offSound = carLogic.flickAudioOff;

                signals[0] = car.Find("R_RearLight").Find("R_RearLight_002").gameObject;
                signals[1] = car.Find("FR_Indicator").gameObject;
                signals[2] = car.Find("L_RearLight").Find("L_RearLight_002").gameObject;
                signals[3] = car.Find("FL_Indicator").gameObject;

                foreach (var signal in signals)
                {
                    var light = signal.AddComponent<Light>();
                    light.type = LightType.Spot;
                    light.range = 25;
                    light.spotAngle = 80;
                    light.color = new Color(1f, 0.5f, 0);
                    light.intensity = 1.5f;
                    light.enabled = false;
                }

                signals[1].GetComponent<Light>().range = signals[3].GetComponent<Light>().range = 50;
            }

            /*string[] joystickNames = Input.GetJoystickNames();
            for (int i = 0; i < joystickNames.Length; i++)
            {
                Console.Instance.Log("Joystick " + i + ": " + joystickNames[i]);
            }*/
        }

        public override void Update() // Default Unity Update() function
        {
            base.Update();

            // keybinds
            if(player.transform.parent != null) 
            {
                if (Input.GetKeyDown(hazardsKey))
                {
                    if (GetToggleValue("AddTurnSignals") == true)
                    {
                        leftTurnSignalOn = false;
                        rightTurnSignalOn = false;
                        hazardsOn = !hazardsOn;
                        carLogic.hazardLightsOn = hazardsOn;

                        if (hazardsOn)
                        {
                            hazardsScript.currentPos = 0;
                            hazardsScript.StartCoroutine(hazardsScript.Trigger());
                        }
                        else
                        {
                            hazardsScript.currentPos = 1;
                            hazardsScript.StartCoroutine(hazardsScript.Trigger());
                        }

                        turnSignalScript.currentPos = 3;
                        turnSignalScript.StartCoroutine(turnSignalScript.Trigger());

                        StopAllFlashes();
                    }
                    else
                    {
                        FindObjectOfType<HazardsLogicC>().StartCoroutine(FindObjectOfType<HazardsLogicC>().Trigger());
                    }
                }

                if (Input.GetKeyDown(hornKey))
                {
                    FindObjectOfType<CarHornC>().Trigger();
                }
                if(Input.GetKeyUp(hornKey))
                {
                    FindObjectOfType<CarHornC>().StopAction();
                }

                if (Input.GetKeyDown(radioKey))
                {
                    FindObjectOfType<RadioFreqLogicC>().StartCoroutine(FindObjectOfType<RadioFreqLogicC>().Trigger());
                }

                if (Input.GetKeyDown(lightsKey))
                {
                    if (GetToggleValue("AddTurnSignals") == true)
                    {
                        FindObjectOfType<LightsKnob>().StartCoroutine(FindObjectOfType<LightsKnob>().Trigger());
                    }
                    else
                    {
                        FindObjectOfType<HeadlightLogicC>().StartCoroutine(FindObjectOfType<HeadlightLogicC>().Trigger());
                    }
                }

                if (Input.GetKeyDown(wipersKey))
                {
                    FindObjectOfType<WindowWipersLogicC>().StartCoroutine(FindObjectOfType<WindowWipersLogicC>().Trigger());
                }

                if (Input.GetKeyDown(wipersWaterKey))
                {
                    FindObjectOfType<WindowWiperSprayLogicC>().StartCoroutine(FindObjectOfType<WindowWiperSprayLogicC>().Trigger());
                }

                if (GetToggleValue("AddTurnSignals") == false)
                    return;

                if (Input.GetKeyDown(leftTurnSignalKey))
                {
                    rightTurnSignalOn = false;
                    hazardsOn = false;
                    carLogic.hazardLightsOn = false;
                    leftTurnSignalOn = !leftTurnSignalOn;

                    if (leftTurnSignalOn)
                    {
                        turnSignalScript.currentPos = 0;
                        turnSignalScript.StartCoroutine(turnSignalScript.Trigger());
                    }
                    else
                    {
                        turnSignalScript.currentPos = 2;
                        turnSignalScript.StartCoroutine(turnSignalScript.Trigger());
                    }

                    hazardsScript.currentPos = 2;
                    hazardsScript.StartCoroutine(hazardsScript.Trigger());

                    StopAllFlashes();
                }

                if (Input.GetKeyDown(rightTurnSignalKey))
                {
                    leftTurnSignalOn = false;
                    hazardsOn = false;
                    carLogic.hazardLightsOn = false;
                    rightTurnSignalOn = !rightTurnSignalOn;

                    if (rightTurnSignalOn)
                    {
                        turnSignalScript.currentPos = 1;
                        turnSignalScript.StartCoroutine(turnSignalScript.Trigger());
                    }
                    else
                    {
                        turnSignalScript.currentPos = 2;
                        turnSignalScript.StartCoroutine(turnSignalScript.Trigger());
                    }

                    hazardsScript.currentPos = 2;
                    hazardsScript.StartCoroutine(hazardsScript.Trigger());

                    StopAllFlashes();
                }
            }

            if (GetToggleValue("AddTurnSignals") == false)
                return;

            // if none of the lights are active, set them all to default & return
            if (!hazardsOn && !leftTurnSignalOn && !rightTurnSignalOn)
            {
                StopAllFlashes();

                return;
            }

            // don't flash lights unless there is enough battery
            if (!EnoughBattery)
            {
                StopAllFlashes();

                return;
            }

            // flash hazards
            if (hazardsOn)
            {
                currentTime += Time.deltaTime;
                if (currentTime > 0.5f)
                {
                    currentTime = 0f;

                    foreach (var signal in signals)
                    {
                        var light = signal.GetComponent<Light>();
                        light.enabled = !light.enabled;
                        if (light.enabled)
                        {
                            source.PlayOneShot(onSound, 0.1f);
                            signal.GetComponent<MeshRenderer>().material = turnSignalGlowMaterial;
                        }
                        else
                        {
                            source.PlayOneShot(offSound, 0.1f);
                            signal.GetComponent<MeshRenderer>().material = commonDefaultMaterial;
                        }
                    }
                }
            }

            // don't flash individual turn signals unless the car is on && the hazards are off
            if (!carLogic.engineOn && !hazardsOn)
            {
                StopAllFlashes();

                return;
            }

            if (leftTurnSignalOn)
            {
                currentTime += Time.deltaTime;
                if (currentTime > 0.5f)
                {
                    currentTime = 0f;

                    for (int i = 2; i < 4; i++)
                    {
                        var light = signals[i].GetComponent<Light>();
                        light.enabled = !light.enabled;
                        if (light.enabled)
                        {
                            source.PlayOneShot(onSound, 0.1f);
                            signals[i].GetComponent<MeshRenderer>().material = turnSignalGlowMaterial;
                        }
                        else
                        {
                            source.PlayOneShot(offSound, 0.1f);
                            signals[i].GetComponent<MeshRenderer>().material = commonDefaultMaterial;
                        }
                    }   
                }
            }
            else if (rightTurnSignalOn)
            {
                currentTime += Time.deltaTime;
                if (currentTime > 0.5f)
                {
                    currentTime = 0f;

                    for (int i = 0; i < 2; i++)
                    {
                        var light = signals[i].GetComponent<Light>();
                        light.enabled = !light.enabled;
                        if (light.enabled)
                        {
                            source.PlayOneShot(onSound, 0.1f);
                            signals[i].GetComponent<MeshRenderer>().material = turnSignalGlowMaterial;
                        }
                        else
                        {
                            source.PlayOneShot(offSound, 0.1f);
                            signals[i].GetComponent<MeshRenderer>().material = commonDefaultMaterial;
                        }
                    }
                }
            }
        }

        public void StopAllFlashes()
        {
            foreach (var signal in signals)
            {
                var light = signal.GetComponent<Light>();
                light.enabled = false;
                signal.GetComponent<MeshRenderer>().material = commonDefaultMaterial;
            }
        }
    }

    public class LightsKnob : MonoBehaviour
    {
        public GameObject carLogic;

        public float timeToComplete = 0.3f;

        public int currentPos;

        public Vector3[] position;

        public AudioClip audioSample;

        public Material glowMaterial;

        public Material startMaterial;

        public string easeType = "easeout";

        public AudioClip errorAudio;

        private bool isGlowing;

        private bool errorActionIsPlaying;

        private void Start()
        {
            startMaterial = gameObject.GetComponent<Renderer>().material;

            position = new Vector3[2];

            position[0] = new Vector3(0, 0, 0);
            position[1] = new Vector3(0, 45, 0);
        }

        private void Update()
        {
            if (isGlowing)
            {
                float value = Mathf.PingPong(Time.time, 0.75f) + 1.25f;
                gameObject.GetComponent<Renderer>().material.SetFloat("_RimPower", value);
            }
        }

        public void ElectricsOff()
        {
            Stop(gameObject);
            currentPos = 0;
            RotateTo(gameObject, Hash("rotation", position[0], "islocal", true, "time", timeToComplete, "easetype", easeType));
            GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
            carLogic.GetComponent<CarLogicC>().StopCoroutine("HeadLightOn");
            carLogic.GetComponent<CarLogicC>().StopCoroutine("FlickerLight");
            carLogic.GetComponent<CarLogicC>().LightsOff();
        }

        public IEnumerator Trigger()
        {
            if (carLogic.GetComponent<CarPerformanceC>().installedBattery != null)
            {
                if ((carLogic.GetComponent<CarPerformanceC>().installedBattery.GetComponent<EngineComponentC>().charge <= 0.0 && !carLogic.GetComponent<CarLogicC>().engineOn) || ((double)carLogic.GetComponent<CarPerformanceC>().installedBattery.GetComponent<EngineComponentC>().Condition <= 0.0 && !carLogic.GetComponent<CarLogicC>().engineOn))
                {
                    GetComponent<AudioSource>().PlayOneShot(errorAudio, 0.5f);
                    Stop(gameObject, "RotateTo");
                    RotateTo(gameObject, Hash("rotation", position[1], "islocal", true, "time", timeToComplete, "easetype", easeType));
                    yield return new WaitForSeconds(timeToComplete);
                    RotateTo(gameObject.transform.parent.gameObject, Hash("rotation", position[0], "islocal", true, "time", timeToComplete, "easetype", easeType));
                }
                else if (currentPos == 0)
                {
                    Stop(gameObject);
                    currentPos = 1;
                    RotateTo(gameObject, Hash("rotation", position[1], "islocal", true, "time", timeToComplete, "easetype", easeType));
                    GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                    carLogic.GetComponent<CarLogicC>().HeadLightOn();
                }
                else if (currentPos == 1)
                {
                    Stop(gameObject);
                    currentPos = 0;
                    RotateTo(gameObject, Hash("rotation", position[0], "islocal", true, "time", timeToComplete, "easetype", easeType));
                    GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                    carLogic.GetComponent<CarLogicC>().StopCoroutine("HeadLightOn");
                    carLogic.GetComponent<CarLogicC>().StopCoroutine("FlickerLight");
                    carLogic.GetComponent<CarLogicC>().LightsOff();
                }
            }
            else if (!errorActionIsPlaying)
            {
                errorActionIsPlaying = true;
                Stop(gameObject, "RotateBy");
                RotateTo(gameObject, Hash("rotation", position[1], "islocal", true, "time", timeToComplete, "easetype", easeType));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                yield return new WaitForSeconds(timeToComplete);
                RotateTo(gameObject, Hash("rotation", position[0], "islocal", true, "time", timeToComplete, "easetype", easeType));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                yield return new WaitForSeconds(timeToComplete);
                errorActionIsPlaying = false;
            }
        }

        public void RaycastEnter()
        {
            isGlowing = true;
            gameObject.GetComponent<Renderer>().material = glowMaterial;
        }

        public void RaycastExit()
        {
            isGlowing = false;
            gameObject.GetComponent<Renderer>().material = startMaterial;
        }
    }

    public class TurnSignalLever : MonoBehaviour
    {
        public float timeToComplete = 0.3f;

        public int currentPos;

        public Vector3[] position;

        public AudioClip audioSample;

        public Material glowMaterial;

        public Material startMaterial;

        public string easeType = "easeoutelastic";

        private bool isGlowing;

        private void Update()
        {
            if (isGlowing)
            {
                float value = Mathf.PingPong(Time.time, 0.75f) + 1.25f;
                gameObject.transform.parent.gameObject.GetComponent<Renderer>().material.SetFloat("_RimPower", value);
            }
        }

        public void ElectricsOff()
        {
            FindObjectOfType<LaikaAddons>().leftTurnSignalOn = FindObjectOfType<LaikaAddons>().rightTurnSignalOn = false;
        }

        public IEnumerator Trigger()
        {
            if (currentPos == 0)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 1;
                RotateTo(gameObject.transform.parent.gameObject, Hash("rotation", position[1], "islocal", true, "time", timeToComplete, "easetype", easeType));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                FindObjectOfType<LaikaAddons>().rightTurnSignalOn = false;
                FindObjectOfType<LaikaAddons>().leftTurnSignalOn = true;
                FindObjectOfType<LaikaAddons>().StopAllFlashes();
            }
            else if (currentPos == 1)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 2;
                RotateTo(gameObject.transform.parent.gameObject, Hash("rotation", position[2], "islocal", true, "time", timeToComplete, "easetype", easeType));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                FindObjectOfType<LaikaAddons>().leftTurnSignalOn = false;
                FindObjectOfType<LaikaAddons>().rightTurnSignalOn = true;
                FindObjectOfType<LaikaAddons>().StopAllFlashes();
            }
            else if (currentPos == 2)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 0;
                RotateTo(gameObject.transform.parent.gameObject, Hash("rotation", position[0], "islocal", true, "time", timeToComplete, "easetype", easeType));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                FindObjectOfType<LaikaAddons>().leftTurnSignalOn = FindObjectOfType<LaikaAddons>().rightTurnSignalOn = false;
                FindObjectOfType<LaikaAddons>().StopAllFlashes();
            }
            else if(currentPos == 3)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 0;
                RotateTo(gameObject.transform.parent.gameObject, Hash("rotation", position[0], "islocal", true, "time", timeToComplete, "easetype", easeType));
                FindObjectOfType<LaikaAddons>().leftTurnSignalOn = FindObjectOfType<LaikaAddons>().rightTurnSignalOn = false;
                FindObjectOfType<LaikaAddons>().StopAllFlashes();
            }

            yield return null;
        }

        public void RaycastEnter()
        {
            isGlowing = true;
            gameObject.transform.parent.gameObject.GetComponent<Renderer>().material = glowMaterial;
        }

        public void RaycastExit()
        {
            isGlowing = false;
            gameObject.transform.parent.gameObject.GetComponent<Renderer>().material = startMaterial;
        }
    }

    public class HazardsButton : MonoBehaviour
    {
        public float timeToComplete = 0.1f;

        public int currentPos;

        public Vector3[] positionRotation;

        public AudioClip audioSample;

        public string easeType = "linear";

        private bool isGlowing;

        public Material glowMaterial;
        public Material startMaterial;

        private void Update()
        {
            if (isGlowing)
            {
                float value = Mathf.PingPong(Time.time, 0.75f) + 1.25f;
                gameObject.transform.parent.gameObject.GetComponent<Renderer>().material.SetFloat("_RimPower", value);
            }
        }

        public void ElectricsOff()
        {
            FindObjectOfType<LaikaAddons>().hazardsOn = false;
        }

        public IEnumerator Trigger()
        {
            if (currentPos == 0)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 1;
                RotateTo(gameObject.transform.parent.gameObject, Hash(new object[8]
                {
                    "rotation",
                    positionRotation[1],
                    "islocal",
                    true,
                    "time",
                    timeToComplete,
                    "easetype",
                    easeType
                }));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                FindObjectOfType<LaikaAddons>().hazardsOn = true;
            }
            else if (currentPos == 1)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 0;
                RotateTo(gameObject.transform.parent.gameObject, Hash(new object[8]
                {
                    "rotation",
                    positionRotation[0],
                    "islocal",
                    true,
                    "time",
                    timeToComplete,
                    "easetype",
                    easeType
                }));
                GetComponent<AudioSource>().PlayOneShot(audioSample, 0.7f);
                FindObjectOfType<LaikaAddons>().hazardsOn = false;
            }
            else if(currentPos == 2)
            {
                Stop(gameObject.transform.parent.gameObject);
                currentPos = 0;
                RotateTo(gameObject.transform.parent.gameObject, Hash(new object[8]
                {
                    "rotation",
                    positionRotation[0],
                    "islocal",
                    true,
                    "time",
                    timeToComplete,
                    "easetype",
                    easeType
                }));
                FindObjectOfType<LaikaAddons>().hazardsOn = false;
            }

            yield return null;
        }

        public void RaycastEnter()
        {
            isGlowing = true;
            gameObject.transform.parent.GetComponent<Renderer>().material = glowMaterial;
        }

        public void RaycastExit()
        {
            isGlowing = false;
            gameObject.transform.parent.GetComponent<Renderer>().material = startMaterial;
        }
    }
}
