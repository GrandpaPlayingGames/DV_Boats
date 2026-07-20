using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DV_Boats
{
    internal enum BoatMode
    {
        Passive,
        Driving,
        Sinking,
        Sunk
    }

    internal class BoatController : MonoBehaviour
    {

        public string BoatTypeId { get; private set; }
        private BoatHullProfile hullProfile;
        private BoatCameraProfile[] cameraProfiles;
    
        public float maxThrottle = 10.0f;
        bool lastRealisticSpeed;
        public float maxSteerSpeed = 8f;

        public float throttleStep = 2.5f;     // per second
        public float thrustForce = 18000f;

        public float rudderTorque = 6f;

        private float throttle;
        private Rigidbody rb;
        private RigidbodyConstraints originalConstraints;

        private bool blockedByCollision;
        private float collisionBlockTimer;
        private BoatProbes probes;

        [Header("Horn Sounds")]
        public AudioSource hornSource;
        public AudioSource foghornSource;

        [SerializeField] private float sinkDuration = 10f;   
        [SerializeField] private float sinkDepth = 3.5f;     
        [SerializeField] private float maxSinkRoll = 30f; 
        [SerializeField] private float engineFadeTime = 1.5f;   
        
        private float engineFadeTimer;
        private float sinkTargetRoll;
        private float sinkStartRoll;
        private float sinkTimer;
        private float sinkStartY;

        //==========================
        //          AUDIO
        //==========================
        [Header("Engine Audio")]
        public AudioSource engineSource;
        public float engineMaxPitch = 1.6f;
        public float engineMinPitch = 0.8f;
        public float engineMaxVolume = 0.7f;
        public float engineSpeedForMax = 12f;
        public float engineIdleVolume = 0.25f;

        public float engineVolumeMultiplier = 30f;
        public float enginePitchOffset = 0.3f;

        //===========================
        //        GLOBAL LOGIC
        //===========================
        public BoatMode Mode { get; private set; } = BoatMode.Passive;

        private bool isHovered;
        public bool IsDriving => Mode == BoatMode.Driving;
        private Collider interactionCollider;
        private int waterGateBlockedDirection = 0;

        private const string LOG = "[BoatController]";

        public static BoatController Instance;

        private BoatProbes _probes;

        //==========================
        //   GHOST BOAT
        //==========================
        public bool GhostBoatOn { get; private set; }
        private bool ghostBoatApplied;

        private readonly List<(Collider col, bool wasEnabled)> ghostCachedColliders
            = new List<(Collider, bool)>();
        

        //==========================
        //   NAV AND DECK LIGHTS
        //==========================
        public bool DeckLightOn { get; private set; }
        public bool NavLightOn { get; private set; }

        public float CurrentSpeedKmh { get; private set; }
        public float CurrentHeading { get; private set; }

        public Light deckLight;

        public Light navLightPort;      
        public Light navLightStarboard; 
        public Light navLightMast;
        public GameObject navLightMastObj;

        public MeshRenderer navBulbPort;
        public MeshRenderer navBulbStarboard;


        //==========================
        //   SPOTLIGHT
        //==========================
        public Light spotLight;
        public GameObject spotLightHousing;
        public GameObject spotLightLensOn;
        public GameObject spotLightLensOff;
        
        public GameObject spotLightRoot;
        
        private float spotLightYaw = 0f;
        private const float SpotLightMaxYaw = 45f;   
        private const float SpotLightSwivelSpeed = 20f;
        public Transform spotLightSwivelPivot;

        
        private static void ___________SYSTEM___________()
        {
        }

        void Awake()
        {            
            Instance = this;           
            rb = GetComponent<Rigidbody>();
            originalConstraints = rb.constraints;
            probes = GetComponent<BoatProbes>();
            SetupInteractionCollider();
            SetPassive();
            BoatWorldShiftManager.WOSDeltaAdjustment += ApplyWorldShift;
            _probes = GetComponentInChildren<BoatProbes>(true);
        }
        void Update()
        {
            if (BoatDriveManager.ActiveBoat != this)
                return;

            if (!Main.Enabled || rb == null)
                return;

            if (Mode == BoatMode.Sunk)
            {
                if (engineSource != null && engineSource.isPlaying)
                    engineSource.Stop();

                return;
            }

            //messy dynamic adjustment of 2 vars that donesn't warrant 200 lines of elegant code
            if (Main.Settings.realisticSpeed != lastRealisticSpeed)
            {
                lastRealisticSpeed = Main.Settings.realisticSpeed;

                if (lastRealisticSpeed)
                {
                    maxThrottle = 4.0f;
                    throttleStep = 0.5f;
                }
                else
                {
                    maxThrottle = 10.0f;
                    throttleStep = 2.5f;
                }
            }

            if (Mode == BoatMode.Sinking)
            {
                sinkTimer += Time.deltaTime;

                float t = Mathf.Clamp01(sinkTimer / sinkDuration);

                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(sinkStartY, sinkStartY - sinkDepth, t);
                transform.position = pos;
 
                Vector3 rot = transform.localEulerAngles;
                float roll = Mathf.LerpAngle(sinkStartRoll, sinkTargetRoll, t);
                rot.z = roll;
                transform.localEulerAngles = rot;

                if (sinkTimer >= sinkDuration)
                {
                    Mode = BoatMode.Sunk;
                    BoatDriveManager.ExitDriving();
                }
            }

            if (Mode == BoatMode.Driving)
            {
                deckLight.enabled = BoatUIController.DeckLightOn;
                spotLight.enabled = BoatUIController.SpotLightOn;
            }
            else
            {
                deckLight.enabled = false;
                spotLight.enabled = false;
            }

            if (Mode == BoatMode.Sinking)
            {
                if (engineSource != null && engineSource.isPlaying)
                {
                    engineFadeTimer += Time.deltaTime;

                    float t = Mathf.Clamp01(engineFadeTimer / engineFadeTime);

                    
                    engineSource.volume = Mathf.Lerp(
                        engineSource.volume,
                        0f,
                        t
                    );

                   
                    engineSource.pitch = Mathf.Lerp(
                        engineSource.pitch,
                        engineMinPitch,
                        t
                    );

                    if (engineFadeTimer >= engineFadeTime)
                    {
                        engineSource.Stop();
                    }
                }

                return; 
            }


            if (!IsDriving)
                return;

            if (deckLight == null)
                return;

            if (Mode == BoatMode.Driving)
            {
                bool on = BoatUIController.SpotLightOn;

                if (spotLight != null)
                    spotLight.enabled = on;

                if (spotLightLensOn != null)
                    spotLightLensOn.SetActive(on);

                if (spotLightLensOff != null)
                    spotLightLensOff.SetActive(!on);
            }
            else
            {
                if (spotLight != null)
                    spotLight.enabled = false;

                if (spotLightLensOn != null)
                    spotLightLensOn.SetActive(false);

                if (spotLightLensOff != null)
                    spotLightLensOff.SetActive(true);
            }
            
  
           
            if (Mode == BoatMode.Driving)
            {
               SyncGhostBoatState();

                bool navOn = BoatUIController.NavLightOn;

                if (navLightPort != null)
                    navLightPort.enabled = navOn;

                if (navLightStarboard != null)
                    navLightStarboard.enabled = navOn;

                if (navLightMast != null)
                    navLightMast.enabled = navOn;

                if (navLightMastObj != null)
                    navLightMastObj.SetActive(navOn);

                if (navBulbPort != null)
                {
                    navBulbPort.enabled = navOn;
                    navBulbPort.gameObject.SetActive(navOn);

                    if (navOn)
                    {
                        var mat = navBulbPort.material;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.red * 1.2f);
                    }
                }
               
                if (navBulbStarboard != null)
                {
                    navBulbStarboard.enabled = navOn;
                    navBulbStarboard.gameObject.SetActive(navOn);

                    if (navOn)
                    {
                        var mat = navBulbStarboard.material;
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.green * 1.2f);
                    }
                }
            }
            else
            {
                if (navLightPort != null)
                    navLightPort.enabled = false;

                if (navLightStarboard != null)
                    navLightStarboard.enabled = false;

                if (navLightMast != null)
                    navLightMast.enabled = false;

                if (navLightMastObj != null)
                    navLightMastObj.SetActive(false);

                if (navBulbPort != null)
                {
                    navBulbPort.enabled = false;
                    navBulbPort.gameObject.SetActive(false);
                }

                if (navBulbStarboard != null)
                {
                    navBulbStarboard.enabled = false;
                    navBulbStarboard.gameObject.SetActive(false);
                }
            }

            var s = Main.Settings;

            bool forward =
                IsKeyWithModifiersPressed(
                    s.forwardKey,
                    s.forwardCtrl,
                    s.forwardAlt,
                    s.forwardShift,
                    held: true
                )
                ||
                (BoatUIController.Instance != null && BoatUIController.MoveForwardHeld);


            if (forward)
                throttle += throttleStep * Time.deltaTime;

            bool backward =
                IsKeyWithModifiersPressed(
                    s.backwardKey,
                    s.backwardCtrl,
                    s.backwardAlt,
                    s.backwardShift,
                    held: true
                )
                ||
                (BoatUIController.Instance != null && BoatUIController.MoveBackwardHeld);


            if (backward)
                throttle -= throttleStep * Time.deltaTime;


            throttle = Mathf.Clamp(throttle, -maxThrottle, maxThrottle);

            BoatUIController.SetMoveForwardFromKey(
                IsKeyWithModifiersPressed(
                    s.forwardKey,
                    s.forwardCtrl,
                    s.forwardAlt,
                    s.forwardShift,
                    held: true
                )
            );

            BoatUIController.SetMoveBackwardFromKey(
                IsKeyWithModifiersPressed(
                    s.backwardKey,
                    s.backwardCtrl,
                    s.backwardAlt,
                    s.backwardShift,
                    held: true
                )
            );

            if (IsKeyWithModifiersPressed(
                Main.Settings.hornKey,
                Main.Settings.hornCtrl,
                Main.Settings.hornAlt,
                Main.Settings.hornShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerHornFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.foghornKey,
                Main.Settings.foghornCtrl,
                Main.Settings.foghornAlt,
                Main.Settings.foghornShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerFoghornFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.DeckLightKey,
                Main.Settings.DeckLightCtrl,
                Main.Settings.DeckLightAlt,
                Main.Settings.DeckLightShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerDeckLightFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.NavLightKey,
                Main.Settings.NavLightCtrl,
                Main.Settings.NavLightAlt,
                Main.Settings.NavLightShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerNavLightFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.SpotLightKey,
                Main.Settings.SpotLightCtrl,
                Main.Settings.SpotLightAlt,
                Main.Settings.SpotLightShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerSpotLightFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.SpotLightSwivelCenterKey,
                Main.Settings.SpotLightSwivelCenterCtrl,
                Main.Settings.SpotLightSwivelCenterAlt,
                Main.Settings.SpotLightSwivelCenterShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerSpotLightSwivelCenterFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.ghostBoatToggleKey,
                Main.Settings.ghostBoatToggleCtrl,
                Main.Settings.ghostBoatToggleAlt,
                Main.Settings.ghostBoatToggleShift,
                held: false
            ))
            {
                BoatUIController.Instance?.TriggerGhostBoatFromInput();
            }

            if (IsKeyWithModifiersPressed(
                Main.Settings.cameraPrevKey,
                Main.Settings.cameraPrevCtrl,
                Main.Settings.cameraPrevAlt,
                Main.Settings.cameraPrevShift,
                held: false
            ))
            {
                BoatUIController.Instance?.OnPrevCameraClicked();
            }


            if (IsKeyWithModifiersPressed(
                Main.Settings.cameraNextKey,
                Main.Settings.cameraNextCtrl,
                Main.Settings.cameraNextAlt,
                Main.Settings.cameraNextShift,
                held: false
            ))
            {
                BoatUIController.Instance?.OnNextCameraClicked(); 
            }


            bool swivelLeft;
            bool swivelRight;

            swivelLeft =
                IsKeyWithModifiersPressed(
                    s.SpotLightSwivelLeftKey,
                    s.SpotLightSwivelLeftCtrl,
                    s.SpotLightSwivelLeftAlt,
                    s.SpotLightSwivelLeftShift,
                    held: true
                )
                ||
                (BoatUIController.Instance != null && BoatUIController.SpotLightSwivelLeftHeld);

            swivelRight =
                IsKeyWithModifiersPressed(
                    s.SpotLightSwivelRightKey,
                    s.SpotLightSwivelRightCtrl,
                    s.SpotLightSwivelRightAlt,
                    s.SpotLightSwivelRightShift,
                    held: true
                )
                ||
                (BoatUIController.Instance != null && BoatUIController.SpotLightSwivelRightHeld);

            BoatUIController.SetSpotLightSwivelLeftFromKey(
                IsKeyWithModifiersPressed(
                    s.SpotLightSwivelLeftKey,
                    s.SpotLightSwivelLeftCtrl,
                    s.SpotLightSwivelLeftAlt,
                    s.SpotLightSwivelLeftShift,
                    held: true
                )
            );

            BoatUIController.SetSpotLightSwivelRightFromKey(
                IsKeyWithModifiersPressed(
                    s.SpotLightSwivelRightKey,
                    s.SpotLightSwivelRightCtrl,
                    s.SpotLightSwivelRightAlt,
                    s.SpotLightSwivelRightShift,
                    held: true
                )
            );

            UpdateSpotLightSwivel();
        }

        void FixedUpdate()
        {
            if (BoatDriveManager.ActiveBoat != this)
                return;

            if (!Main.Enabled || rb == null)
                return;

            if (!IsDriving)
            {
                throttle = 0f;
                UpdateEngineAudio();
                return;
            }

            if (blockedByCollision)
            {
                collisionBlockTimer -= Time.fixedDeltaTime;
                if (collisionBlockTimer <= 0f)
                    blockedByCollision = false;

                return;
            }

            bool overWater = BoatSpawner.IsOverWaterForBoat(gameObject);

            if (!overWater)
            {
                Main.Log("WaterGate fired");

                if (waterGateBlockedDirection == 0)
                {
                    if (throttle > 0f)
                        waterGateBlockedDirection = +1;  
                    else if (throttle < 0f)
                        waterGateBlockedDirection = -1;  
                }

                if (waterGateBlockedDirection == +1 && throttle > 0f)
                {
                    throttle = 0f;
                    UpdateEngineAudio();
                    return;
                }

                if (waterGateBlockedDirection == -1 && throttle < 0f)
                {
                    throttle = 0f;
                    UpdateEngineAudio();
                    return;
                }

            }
            else
            {
                 waterGateBlockedDirection = 0;
            }

            float speed = rb.velocity.magnitude;
            float speedFactor = Mathf.Clamp01(speed / 6f);

            // calculate for display
            CurrentSpeedKmh = speed * 3.6f;

            Vector3 fwd = transform.forward;
            fwd.y = 0f;

            if (fwd.sqrMagnitude > 0.001f)
            {
                fwd.Normalize();
                float h = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
                if (h < 0f) h += 360f;
                CurrentHeading = h;
            }

            if (probes != null)
            {
                if (throttle > 0f && probes.BowBlocked)
                    return;

                if (throttle < 0f && probes.SternBlocked)
                    return;
            }

            rb.AddForce(
                transform.forward * (thrustForce * throttle),
                ForceMode.Force
            );
           
            float steer = 0f;

            var s = Main.Settings;

            bool left =
                IsKeyWithModifiersPressed(
                    s.leftKey,
                    s.leftCtrl,
                    s.leftAlt,
                    s.leftShift,
                    held: true
                )
                ||
                (BoatUIController.Instance != null && BoatUIController.MoveLeftHeld);

            bool right =
                IsKeyWithModifiersPressed(
                    s.rightKey,
                    s.rightCtrl,
                    s.rightAlt,
                    s.rightShift,
                    held: true
                )
                ||
                (BoatUIController.Instance != null && BoatUIController.MoveRightHeld);

            if (left)
                steer = -0.4f;

            if (right)
                steer = 0.4f;

            BoatUIController.SetMoveLeftFromKey(
                IsKeyWithModifiersPressed(
                    s.leftKey,
                    s.leftCtrl,
                    s.leftAlt,
                    s.leftShift,
                    held: true
                )
            );

            BoatUIController.SetMoveRightFromKey(
                IsKeyWithModifiersPressed(
                    s.rightKey,
                    s.rightCtrl,
                    s.rightAlt,
                    s.rightShift,
                    held: true
                )
            );

            if (steer != 0f && speedFactor > 0.01f)
            {
                rb.AddRelativeTorque(                 
                    Vector3.up * steer * rudderTorque * speedFactor * Main.Settings.turnStrengthMultiplier,
                    ForceMode.Acceleration
                );
            }
   
            if (engineSource == null || engineSource.clip == null || rb == null)
                return;

            float t = Mathf.Clamp01((speed * 2f) / engineSpeedForMax);

            engineSource.pitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, t) + enginePitchOffset;

            float volume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, t);
           
            engineSource.volume =
                Mathf.Clamp01(volume * engineVolumeMultiplier)
                * Main.Settings.masterVolume
                * Main.Settings.engineVolume;
        }
  
        private void Start()
        {
            StartCoroutine(FixRenderersAndLOD());
        }

        private void OnDestroy()
        {
            BoatWorldShiftManager.WOSDeltaAdjustment -= ApplyWorldShift;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider != null &&
                collision.collider.gameObject != null &&
                collision.collider.gameObject.name.StartsWith("DV_BEACHBALL_"))
            {
                return;
            }

            if (collision.collider.GetComponent<ItemBuoyancyEnabler>() == null)
            {
                throttle = 0f;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                blockedByCollision = true;
                collisionBlockTimer = 0.5f; 

                Main.Log("[BoatController] 💥 Collision → blocking movement");
            }
        }

        private static void ___________DRIVING___________()
        {
        }

        public void ForceIdle()
        {
            throttle = 0f;
        }

        public void SetPassive()
        {
            Mode = BoatMode.Passive;
            throttle = 0f;

            if (engineSource != null)
            {
  
                engineSource.volume = 0f;

                if (engineSource.isPlaying)
                    engineSource.Stop();
  
                _probes?.SetEnabled(false);
            }
        }


        public void SetDriving()
        {
            Mode = BoatMode.Driving;
            if (engineSource != null && !engineSource.isPlaying)
                engineSource.Play();
 
            _probes?.SetEnabled(true);           
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
        }

        private void UpdateEngineAudio()
        {
            if (engineSource == null || engineSource.clip == null || rb == null)
                return;

            float speed = rb.velocity.magnitude;

            float t = Mathf.Clamp01((speed * 2f) / engineSpeedForMax);

            engineSource.pitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, t) + enginePitchOffset;

            float volume = Mathf.Lerp(engineIdleVolume, engineMaxVolume, t);

            engineSource.volume =
                Mathf.Clamp01(volume * engineVolumeMultiplier)
                * Main.Settings.masterVolume
                * Main.Settings.engineVolume;
        }

        private static void ___________GHOSTBOAT___________()
        {
        }
        private void SyncGhostBoatState()
        {
            bool desired = BoatUIController.GhostBoatOn;

            if (desired == ghostBoatApplied)
                return;

            if (desired)
            {
                EnableGhostBoat();
              
            }
            else
            {
                DisableGhostBoat();           
            }
        }


        private void EnableGhostBoat()
        {
            ghostBoatApplied = true;
            ghostCachedColliders.Clear();

            Main.Log("[GhostBoat] ENABLE");

            if (probes != null)
            {
                probes.ClearProbeBlockState();
                probes.enabled = false;
            }

            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                ghostCachedColliders.Add((col, col.enabled));
                col.enabled = false;
            }
        }

        private void DisableGhostBoat()
        {
            ghostBoatApplied = false;

            foreach (var entry in ghostCachedColliders)
            {
                if (entry.col != null)
                    entry.col.enabled = entry.wasEnabled;
            }

            ghostCachedColliders.Clear();

            if (probes != null)
            {
                probes.enabled = true;
                probes.ClearProbeBlockState();
            }
        }

        public void ToggleGhostBoat()
        {
            GhostBoatOn = !GhostBoatOn;
            
        }

        public void SetGhostBoat(bool on)
        {
            GhostBoatOn = on;
        }
        private static void ___________HORNS___________()
        {
        }

        public void PlayHorn()
        {
            if (hornSource == null || hornSource.clip == null)
                return;

            hornSource.PlayOneShot(
                hornSource.clip,
                Main.Settings.masterVolume * Main.Settings.effectsVolume
            );
        }

        public void PlayFoghorn()
        {
            if (foghornSource == null || foghornSource.clip == null)
                return;

            foghornSource.PlayOneShot(
                foghornSource.clip,
                Main.Settings.masterVolume * Main.Settings.effectsVolume
            );
        }

        private static void ___________LIGHTS__________()
        {
        }

        public void ToggleDeckLight()
        {
            DeckLightOn = !DeckLightOn;
        }

        public void ToggleNavLight()
        {
            NavLightOn = !NavLightOn;
        }

        public void SetDeckLight(bool on)
        {
            DeckLightOn = on;
        }

        public void SetNavLight(bool on)
        {
            NavLightOn = on;
        }

        public void SetSpotLight(Light light)
        {
            spotLight = light;
        }

        private void UpdateSpotLightSwivel()
        {
            if (Mode != BoatMode.Driving)
                return;

            if (spotLightRoot == null)
                return;

            bool left = BoatUIController.SpotLightSwivelLeftHeld;
            bool right = BoatUIController.SpotLightSwivelRightHeld;

            float delta = 0f;

            if (left)
                delta -= SpotLightSwivelSpeed * Time.deltaTime;

            if (right)
                delta += SpotLightSwivelSpeed * Time.deltaTime;


            if (Mathf.Abs(delta) > 0.0001f)
            {
                spotLightYaw = Mathf.Clamp(
                    spotLightYaw + delta,
                    -SpotLightMaxYaw,
                    SpotLightMaxYaw
                );

                ApplySpotLightYaw();
            }
        }

        private void ApplySpotLightYaw()
        {
            if (Mode != BoatMode.Driving)
                return;

            if (spotLightRoot == null)
                return;

            bool left =
                Input.GetKey(Main.Settings.SpotLightSwivelLeftKey) ||
                BoatUIController.SpotLightSwivelLeftHeld;

            bool right =
                Input.GetKey(Main.Settings.SpotLightSwivelRightKey) ||
                BoatUIController.SpotLightSwivelRightHeld;

            const float SpotLightSwivelSpeed = 90f; 
            const float SpotLightMaxYaw = 45f;      

            if (left)
                spotLightYaw -= SpotLightSwivelSpeed * Time.deltaTime;

            if (right)
                spotLightYaw += SpotLightSwivelSpeed * Time.deltaTime;

            spotLightYaw = Mathf.Clamp(spotLightYaw, -SpotLightMaxYaw, SpotLightMaxYaw);

            spotLightSwivelPivot.localRotation = Quaternion.Euler(0f, spotLightYaw, 0f);
        }
        
        public void ResetSpotLightPose()
        {
            spotLightYaw = 0f;

            if (spotLightSwivelPivot != null)
            {
                 spotLightSwivelPivot.localRotation = Quaternion.identity;
            }
        }

        private static void ___________WOS__________()
        {
        }
        public void ApplyWorldShift(Vector3 delta)
        {
            transform.position += delta;
        }

        private static void ___________HELPERS__________()
        {
        }

        public static bool IsKeyWithModifiersPressed(
                KeyCode key,
                bool requireCtrl,
                bool requireAlt,
                bool requireShift,
                bool held = true
            )
        {
            // Main key
            bool keyDown = held ? Input.GetKey(key) : Input.GetKeyDown(key);
            if (!keyDown)
                return false;

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (requireCtrl != ctrl) return false;
            if (requireAlt != alt) return false;
            if (requireShift != shift) return false;

            return true;
        }


        private IEnumerator FixRenderersAndLOD()
        {
            yield return new WaitForEndOfFrame();

            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
                r.enabled = true;

            var lodGroups = GetComponentsInChildren<LODGroup>(true);
            foreach (var lod in lodGroups)
            {
                lod.enabled = false;
                lod.enabled = true;
                lod.ForceLOD(0);
                lod.RecalculateBounds();
            }
        }

        public void TryEnterSinking(float impactSpeedKmh)
        {

            if (Mode == BoatMode.Sinking || Mode == BoatMode.Sunk)
                return;

            Mode = BoatMode.Sinking;
            sinkTimer = 0f;
            Main.Log("[SINKING] glub glub glub");
 
            sinkStartY = transform.position.y;

            if (rb != null)
            {
                rb.constraints =
                    RigidbodyConstraints.FreezeRotationX |
                    RigidbodyConstraints.FreezeRotationZ;
            }
            sinkStartRoll = transform.localEulerAngles.z;

            sinkTargetRoll = Random.value < 0.5f
                ? maxSinkRoll
                : -maxSinkRoll;

            engineFadeTimer = 0f;

        }

        private void SetupInteractionCollider()
        {

            var go = new GameObject("Boat_InteractionCollider");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;

            box.size = new Vector3(8f, 6f, 14f);
            box.center = new Vector3(0f, 3f, 0f);

            interactionCollider = box;
        }

        public void SetBoatTypeId(string boatTypeId)
        {
            BoatTypeId = boatTypeId;

        }
      
    }
}

namespace DV_Boats
{
    internal static class BoatDriveManager
    {
        public static BoatController ActiveBoat { get; private set; }

        public static bool IsDriving => ActiveBoat != null;

        public static void EnterDriving(BoatController target)
        {
            if (target == null)
                return;

            if (ActiveBoat == target)
                return;

            if (ActiveBoat != null)
            {
                ActiveBoat.SetPassive();
            }

            ActiveBoat = target;
            ActiveBoat.SetDriving();
            BoatUIController.Show();

        }

        public static void ExitDriving()
        {
            if (ActiveBoat == null)
                return;

            if (BoatUIController.GhostBoatOn)
                BoatUIController.Instance?.TriggerGhostBoatFromInput();

            if (BoatUIController.Instance != null &&
                BoatUIController.Instance.hasEnteredBoatCameraThisSession)
            {
                BoatUIController.Instance.ForceExitToFreeRoamAndPlacePlayerOnBoat(ActiveBoat);
            }

            if (BoatUIController.Instance != null)
            {
                BoatUIController.Instance.ResetCameraSelectionToFreeRoam();
            }
            if (BoatUIController.spotLightOn)
            {
                ActiveBoat.spotLight.enabled = false;
            }
            BoatUIController.ResetlightState();

            BoatUIController.Instance.hasEnteredBoatCameraThisSession = false;

            
            BoatUIController.ghostBoatOn = false;
           
            
            BoatUIController.deckLightOn = false;
            BoatUIController.navLightOn = false;

           
            if (ActiveBoat.deckLight != null)
                ActiveBoat.deckLight.enabled = false;

            
            if (ActiveBoat.navLightPort != null)
                ActiveBoat.navLightPort.enabled = false;

            if (ActiveBoat.navLightStarboard != null)
                ActiveBoat.navLightStarboard.enabled = false;

            if (ActiveBoat.navLightMast != null)
                ActiveBoat.navLightMast.enabled = false;

            if (ActiveBoat.navLightMastObj != null)
                ActiveBoat.navLightMastObj.SetActive(false);

      
            if (ActiveBoat.navBulbPort != null)
            {
                ActiveBoat.navBulbPort.enabled = false;
                ActiveBoat.navBulbPort.gameObject.SetActive(false);
            }

            if (ActiveBoat.navBulbStarboard != null)
            {
                ActiveBoat.navBulbStarboard.enabled = false;
                ActiveBoat.navBulbStarboard.gameObject.SetActive(false);
            }

            if (ActiveBoat.Mode != BoatMode.Sunk)
                ActiveBoat.SetPassive();

            ActiveBoat = null;

            BoatUIController.Hide();
        }

    }
}
