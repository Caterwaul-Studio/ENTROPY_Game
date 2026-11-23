using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapSectors : MonoBehaviour
{
    [Serializable]
    public class Sector
    {
        public string name = "";

        [Tooltip("Set by the power system. Untick in Play mode to test")]
        public bool powered = true;
        public Renderer[] parts = new Renderer[0];
        public TMP_Text[] labels = new TMP_Text[0];
        [NonSerialized] public float shown = -1f;
        [NonSerialized] public float progress = 1f;
        [NonSerialized] public bool wasPowered = true;
        [NonSerialized] public float noiseSeed;
    }

    public Sector[] sectors = new Sector[0];

    [Header("Unpowered Look")]
    [Range(0f, 1f)] public float unpoweredBrightness = 0.25f;
    [Range(0f, 1f)] public float unpoweredSaturation = 0.2f;

    [Header("Switching (s)")]
    public float switchOnTime = 0.8f;
    public float switchOffTime = 0.3f;

    [Header("Flicker")]
    [Range(0f, 1f)] public float flickerStrength = 0.8f;
    public float flickerSpeed = 14f;

    // Shared by every map, cleared each scene
    private static readonly Dictionary<string, bool> powerStates = new Dictionary<string, bool>();
    private static readonly List<MapSectors> maps = new List<MapSectors>();
    private MapFaceCamera labelGroup;

    // Snap on the first draw so a level starting unpowered doesn't flicker
    private bool started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        powerStates.Clear();
        maps.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) powerStates.Clear();
    }

    public static void SetPowered(string sector, bool powered)
    {
        if (string.IsNullOrEmpty(sector)) return;
        powerStates[sector] = powered;
        foreach (MapSectors map in maps)
        {
            Sector found = map.Find(sector);
            if (found != null) found.powered = found.wasPowered = powered;
        }
    }

    public static bool IsPowered(string sector)
    {
        return !powerStates.TryGetValue(sector, out bool powered) || powered;
    }

    public Sector Find(string sector)
    {
        return Array.Find(sectors, item => item.name == sector);
    }

    private void OnEnable()
    {
        maps.Add(this);
        labelGroup = GetComponentInChildren<MapFaceCamera>(true);
        foreach (Sector sector in sectors)
        {
            if (powerStates.TryGetValue(sector.name, out bool powered)) sector.powered = powered;
            sector.wasPowered = sector.powered;
            sector.noiseSeed = UnityEngine.Random.value * 100f;
        }
        started = false;
    }

    private void OnDisable()
    {
        maps.Remove(this);
    }

    private void Update()
    {
        Step(Time.deltaTime);
    }

    public void Step(float deltaTime)
    {
        foreach (Sector sector in sectors)
        {
            if (sector.powered != sector.wasPowered) SetPowered(sector.name, sector.powered);

            float target = sector.powered ? 1f : 0f;
            if (!started)
            {
                sector.progress = target;
            }
            else
            {
                float time = sector.powered ? switchOnTime : switchOffTime;
                sector.progress = Mathf.MoveTowards(sector.progress, target, time > 0f ? deltaTime / time : 1f);
            }

            float p = sector.progress;
            float eased = p * p * (3f - 2f * p);
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, sector.noiseSeed);
            float dip = flickerStrength * Mathf.Sin(p * Mathf.PI) * noise * noise * 1.6f;
            Show(sector, Mathf.Clamp01(eased * (1f - dip)));
        }
        started = true;
    }

    private void Show(Sector sector, float power)
    {
        if (Mathf.Approximately(power, sector.shown)) return;
        sector.shown = power;

        foreach (Renderer part in sector.parts)
        {
            if (part != null) MapPartValues.SetPower(part, power, unpoweredBrightness, unpoweredSaturation);
        }
        if (labelGroup == null) return;
        foreach (TMP_Text label in sector.labels)
        {
            if (label != null) labelGroup.SetPower(label, power, unpoweredBrightness, unpoweredSaturation);
        }
    }
}
