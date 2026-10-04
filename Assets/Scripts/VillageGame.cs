using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LittleColony
{
    /// <summary>
    /// Coordinates gameplay input, village state, persistence, audio, and world updates.
    /// </summary>
    public sealed class VillageGame : MonoBehaviour
    {
        public VillageState State { get; private set; }
        public VillageWorld World { get; private set; }
        public BuildingKind? Placing { get; private set; }
        public int EditingId { get; private set; }
        public Vector3 DraftPosition { get; private set; }
        public int DraftRotation { get; private set; }
        public bool DraftLocked { get; private set; }
        public bool DraftVisible { get; private set; }
        public int DraftTier => EditingId == 0 ? 1 : State.buildings.Find(b => b.id == EditingId).tier;
        public string DraftError => Placing.HasValue
            ? State.PlacementError(Placing.Value, (int)DraftPosition.x, (int)DraftPosition.z, EditingId, DraftRotation)
            : null;
        public Building Selected => State.buildings.Find(b => b.id == selectedId);
        private string notice = I18n.Source("notice.welcome");

        /// <summary>
        /// Gets the last gameplay notice in the selected language while retaining its canonical source.
        /// </summary>
        public string Notice
        {
            get => I18n.Translate(notice);
            private set => notice = value;
        }
        public bool VisitorActive { get; private set; }
        public bool AntLionActive { get; private set; }

        private float antLionClock = 30;
        private float antLionWait;
        private const string VolumeKey = "little-colony.audio.volume";
        private const string LanguageKey = "little-colony.language";

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// Synchronizes the WebGL page labels and the next loading screen with the selected language.
        /// </summary>
        [DllImport("__Internal")]
        private static extern void LittleColony_SetLanguage(int language);
#endif

        /// <summary>
        /// Gets the normalized master volume shared by all game audio layers.
        /// </summary>
        public float MasterVolume { get; private set; } = 1f;

        private bool suppressTouchUntilReleased;
        private int firstPinchFinger = -1;
        private int secondPinchFinger = -1;
        private float previousPinchDistance;
        private const string SaveKey = "little-colony.save.v1";
        private string activeSaveKey = SaveKey;

        /// <summary>Gets the account session and cloud synchronization coordinator.</summary>
        public VillageCloudSave Cloud { get; private set; }
        private int selectedId;
        private double lastClock;
        private float saveClock;
        private float visitorClock = 30;
        private float visitorWait;
        private Vector2 pointerDown;
        private Vector2 lastDrag;
        private bool pointerOnUI;
        private int pressedBuildingId;
        private bool draggingBuilding;
        private float pointerDownTime;
        private VillageUI ui;
        private VillageLoginScreen loginScreen;

        /// <summary>
        /// Gets whether the welcome screen is currently consuming all gameplay input.
        /// </summary>
        public bool IsWelcomeScreenOpen => loginScreen != null && loginScreen.IsVisible;
        private AudioSource backgroundMusic;
        private AudioSource nightAmbience;
        private AudioSource rainAmbience;
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Loads the village and initializes its world, welcome screen, interface, audio, and frame clock.
        /// </summary>
        void Awake()
        {
            Application.targetFrameRate = 60;
            SetLanguage((GameLanguage)PlayerPrefs.GetInt(LanguageKey, (int)GameLanguage.English));
            Load();
            SetMasterVolume(PlayerPrefs.GetFloat(VolumeKey, 1f));
            World = gameObject.AddComponent<VillageWorld>();
            World.Create();
            World.Sync(State);
            ui = gameObject.AddComponent<VillageUI>();
            ui.Game = this;
            StartBackgroundMusic();
            nightAmbience = CreateAmbience("Audio/NightAmbience");
            rainAmbience = CreateAmbience("Audio/RainAmbience");
            lastClock = Time.realtimeSinceStartupAsDouble;
            loginScreen = gameObject.AddComponent<VillageLoginScreen>();
            loginScreen.Initialize(this);
            Cloud = gameObject.AddComponent<VillageCloudSave>();
            Cloud.Initialize(this);
        }

        /// <summary>
        /// Loads and starts the looping backyard music.
        /// </summary>
        void StartBackgroundMusic()
        {
            var clip = Resources.Load<AudioClip>("Audio/BackyardMusic");
            if (clip == null)
            {
                Debug.LogWarning(I18n.Text("audio.music_load_failed"));
                return;
            }

            backgroundMusic = gameObject.AddComponent<AudioSource>();
            backgroundMusic.clip = clip;
            backgroundMusic.loop = true;
            backgroundMusic.playOnAwake = false;
            backgroundMusic.spatialBlend = 0;
            backgroundMusic.volume = .4f;
            backgroundMusic.Play();
        }

        /// <summary>
        /// Creates an initially silent looping source for the requested ambience clip.
        /// </summary>
        AudioSource CreateAmbience(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning(I18n.Format("message.audio_load_failed", path));
                return null;
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = 0;
            return source;
        }

        /// <summary>
        /// Fades an ambience layer in or out as its condition changes.
        /// </summary>
        static void UpdateAmbience(AudioSource source, bool active, float targetVolume)
        {
            if (source == null)
            {
                return;
            }

            if (active && !source.isPlaying)
            {
                source.Play();
            }

            source.volume = Mathf.MoveTowards(source.volume, active ? targetVolume : 0, Time.unscaledDeltaTime * .5f);
            if (!active && source.isPlaying && source.volume <= .001f)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// Restores and migrates the saved village, applies offline time, or creates a new village.
        /// </summary>
        void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(SaveKey, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonUtility.FromJson<VillageState>(json);
                    if (loaded != null && loaded.version == 1 && !PlayerPrefs.HasKey(SaveKey + ".v1-backup"))
                    {
                        PlayerPrefs.SetString(SaveKey + ".v1-backup", json);
                    }

                    if (loaded != null && loaded.Migrate())
                    {
                        State = loaded;
                        State.Advance(Math.Max(0, Now - State.savedAt));
                    }
                    else
                    {
                        Notice = I18n.Source("save.invalid");
                    }

                    if (State == null)
                    {
                        PlayerPrefs.SetString(SaveKey + ".recovery", json);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save recovery: " + e.Message);
                Notice = I18n.Source("save.unreadable");
            }

            if (State == null)
            {
                State = VillageState.NewGame(Now);
            }
        }

        /// <summary>
        /// Writes the village locally, preserving the guest key and isolating authenticated account caches.
        /// </summary>
        public void Save()
        {
            State.savedAt = Now;
            PlayerPrefs.SetString(activeSaveKey, JsonUtility.ToJson(State));
            Cloud?.MarkDirty();
            PlayerPrefs.Save();
            saveClock = 0;
        }

        /// <summary>
        /// Advances gameplay and presentation, then processes input after the welcome screen closes.
        /// </summary>
        void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            bool hadFood = State.ActiveFood != null;
            bool worldChanged = State.Advance(now - lastClock);
            lastClock = now;
            if (worldChanged)
            {
                World.Sync(State);
                Changed(hadFood
                    && State.ActiveFood == null
                    ? I18n.Source("notice.meal_finished")
                    : I18n.Source("notice.upgrade_finished"));
            }

            saveClock += Time.unscaledDeltaTime;
            if (saveClock > 5)
            {
                Save();
            }

            if (!VisitorActive && !World.VisitorLeaving)
            {
                visitorClock -= Time.deltaTime;
                visitorWait += Time.deltaTime;
                if (visitorClock <= 0)
                {
                    visitorClock = 30;
                    // Like rain, roll once per time slot. Bound the wait to preserve the zero-money rescue.
                    if ((UnityEngine.Random.value < .2f || visitorWait >= 300) && World.TryShowVisitor(State))
                    {
                        VisitorActive = true;
                        visitorWait = 0;
                    }
                }
            }

            if (!AntLionActive && !World.AntLionLeaving)
            {
                antLionClock -= Time.deltaTime;
                antLionWait += Time.deltaTime;
                if (antLionClock <= 0)
                {
                    antLionClock = 30;
                    if ((UnityEngine.Random.value < .2f || antLionWait >= 300) && World.TryShowAntLion(State))
                    {
                        AntLionActive = true;
                        antLionWait = 0;
                    }
                }
            }

            World.ApplyClimate(State, Time.time);
            UpdateAmbience(nightAmbience, State.IsNight, .3f);
            UpdateAmbience(rainAmbience, State.IsRaining, .4f);
            World.Animate(State, Time.time, selectedId);
            if (IsWelcomeScreenOpen || (Cloud != null && Cloud.IsTransitioning))
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && !ui.CloseSettings())
            {
                Cancel();
            }

            if (HandlePinchZoom() || ui.IsSettingsOpen)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.R) && Placing.HasValue)
            {
                RotateDraft();
            }

            if (Input.GetKeyDown(KeyCode.Return) && Placing.HasValue && DraftLocked)
            {
                ConfirmDraft();
            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                World.ResetCamera();
            }

            var pan = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            World.Navigate(ui.BlocksPointer(Input.mousePosition) ? 0 : Input.mouseScrollDelta.y * .45f, pan * Time.deltaTime * 5);
            if (Placing.HasValue)
            {
                if (!DraftLocked && !ui.BlocksPointer(Input.mousePosition) && World.Ground(Input.mousePosition, out var p))
                {
                    DraftPosition = new Vector3(Mathf.Round(p.x), .17f, Mathf.Round(p.z));
                    DraftVisible = true;
                }

                World.ShowDraft(DraftVisible, Placing.Value, DraftTier, DraftPosition, DraftRotation, DraftError == null, EditingId);
            }
            else
            {
                World.ShowDraft(false, BuildingKind.AntHome, 1, Vector3.zero, 0, false);
            }

            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = lastDrag = Input.mousePosition;
                pointerOnUI = ui.BlocksPointer(pointerDown);
                pressedBuildingId = 0;
                draggingBuilding = false;
                pointerDownTime = Time.unscaledTime;
                if (!pointerOnUI
                    && !Placing.HasValue
                    && Physics.Raycast(World.ViewCamera.ScreenPointToRay(pointerDown), out var pressedHit))
                {
                    var view = pressedHit.collider.GetComponentInParent<BuildingView>();
                    if (view != null)
                    {
                        pressedBuildingId = view.id;
                    }
                }
            }

            if (Input.GetMouseButton(0) && pressedBuildingId != 0 && !draggingBuilding && Time.unscaledTime - pointerDownTime >= .38f)
            {
                Select(pressedBuildingId);
                BeginMove();
                draggingBuilding = Placing.HasValue;
                if (draggingBuilding)
                {
                    Notice = I18n.Source("notice.drag_building");
                }
            }

            if (Input.GetMouseButton(0) && draggingBuilding && World.Ground(Input.mousePosition, out var draggedPoint))
            {
                DraftPosition = new Vector3(Mathf.Round(draggedPoint.x), .17f, Mathf.Round(draggedPoint.z));
                DraftVisible = true;
            }

            if (Input.GetMouseButton(0)
                && !pointerOnUI
                && pressedBuildingId == 0
                && !Placing.HasValue
                && Vector2.Distance(pointerDown, Input.mousePosition) > 8)
            {
                Vector2 delta = (Vector2)Input.mousePosition - lastDrag;
                World.Navigate(0, -delta * World.ViewCamera.orthographicSize / Screen.height * 2);
                lastDrag = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (draggingBuilding)
                {
                    DraftLocked = true;
                    draggingBuilding = false;
                    pressedBuildingId = 0;
                }
                else if (!pointerOnUI && !ui.BlocksPointer(Input.mousePosition) && Vector2.Distance(pointerDown, Input.mousePosition) < 8)
                {
                    ClickWorld(Input.mousePosition);
                }

                pressedBuildingId = 0;
            }
        }

        /// <summary>
        /// Changes the interface language and stores it independently of village progress.
        /// </summary>
        /// <param name="language">The requested language; unsupported values fall back to English.</param>
        public void SetLanguage(GameLanguage language)
        {
            I18n.Language = language;
            PlayerPrefs.SetInt(LanguageKey, (int)I18n.Language);
#if UNITY_WEBGL && !UNITY_EDITOR
            LittleColony_SetLanguage((int)I18n.Language);
#endif
        }

        /// <summary>
        /// Applies a normalized volume to every game audio source and stores the preference separately from village progress.
        /// </summary>
        /// <param name="volume">The master volume from zero (silent) to one (full volume).</param>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            AudioListener.volume = MasterVolume;
            PlayerPrefs.SetFloat(VolumeKey, MasterVolume);
        }

        /// <summary>
        /// Handles two-finger pinch zoom and suppresses emulated mouse actions until all gesture fingers are released.
        /// </summary>
        /// <returns>Whether this frame belongs to a pinch gesture and should skip world pointer actions.</returns>
        private bool HandlePinchZoom()
        {
            if (Input.touchCount < 2)
            {
                firstPinchFinger = -1;
                secondPinchFinger = -1;
                previousPinchDistance = 0;
                if (!suppressTouchUntilReleased)
                {
                    return false;
                }

                if (Input.touchCount == 0)
                {
                    suppressTouchUntilReleased = false;
                }

                return true;
            }

            suppressTouchUntilReleased = true;
            pressedBuildingId = 0;
            pointerOnUI = true;
            if (draggingBuilding)
            {
                draggingBuilding = false;
                DraftLocked = true;
            }

            var firstTouch = Input.GetTouch(0);
            var secondTouch = Input.GetTouch(1);
            float distance = Vector2.Distance(firstTouch.position, secondTouch.position);
            bool sameFingers = firstTouch.fingerId == firstPinchFinger && secondTouch.fingerId == secondPinchFinger;
            bool fingersActive = firstTouch.phase != TouchPhase.Ended && firstTouch.phase != TouchPhase.Canceled
                && secondTouch.phase != TouchPhase.Ended && secondTouch.phase != TouchPhase.Canceled;
            if (sameFingers && fingersActive && previousPinchDistance > 0 && distance > 0
                && !ui.BlocksPointer(firstTouch.position) && !ui.BlocksPointer(secondTouch.position))
            {
                float zoom = World.ViewCamera.orthographicSize * (1f - previousPinchDistance / distance);
                World.Navigate(zoom, Vector2.zero);
            }

            firstPinchFinger = firstTouch.fingerId;
            secondPinchFinger = secondTouch.fingerId;
            previousPinchDistance = distance;
            return true;
        }

        /// <summary>
        /// Handles placement, visitor interactions, and building selection at a screen position.
        /// </summary>
        void ClickWorld(Vector2 screen)
        {
            if (Placing.HasValue)
            {
                if (World.Ground(screen, out var p))
                {
                    DraftPosition = new Vector3(Mathf.Round(p.x), .17f, Mathf.Round(p.z));
                    DraftLocked = true;
                    DraftVisible = true;
                    Notice = DraftError ?? I18n.Source("notice.position_selected");
                }

                return;
            }

            if (Physics.Raycast(World.ViewCamera.ScreenPointToRay(screen), out var hit))
            {
                if (hit.collider.GetComponentInParent<AntLionView>() != null)
                {
                    RepelAntLion();
                    return;
                }

                if (hit.collider.GetComponentInParent<VisitorView>() != null)
                {
                    HelpVisitor();
                    return;
                }

                var view = hit.collider.GetComponentInParent<BuildingView>();
                selectedId = view != null ? view.id : 0;
            }
            else
            {
                selectedId = 0;
            }
        }

        /// <summary>
        /// Selects a building and clears the active placement mode.
        /// </summary>
        public void Select(int id)
        {
            selectedId = id;
            Placing = null;
            EditingId = 0;
        }

        /// <summary>
        /// Starts a placement preview after checking unlock, cost, and food restrictions.
        /// </summary>
        public void Build(BuildingKind kind)
        {
            if (!State.IsUnlocked(kind))
            {
                Notice = I18n.Source("notice.unlock_prefix") + VillageState.UnlockLevel(kind) + ".";
                return;
            }

            if (State.acorns < VillageState.Cost(kind))
            {
                Notice = I18n.Source("notice.need_acorns");
                return;
            }

            if (VillageState.IsFood(kind) && State.ActiveFood != null)
            {
                Notice = I18n.Source("error.food_exists");
                return;
            }

            if (VillageState.IsFood(kind) && State.energy >= VillageState.MaxEnergy)
            {
                Notice = I18n.Source("error.full_energy");
                return;
            }

            Placing = kind;
            selectedId = 0;
            EditingId = 0;
            DraftRotation = 0;
            DraftLocked = false;
            DraftVisible = false;
            Notice = VillageState.IsFood(kind)
                ? I18n.Source("notice.place_food")
                : I18n.Source("notice.place_building");
        }

        /// <summary>
        /// Starts a move or rotation preview for the selected building.
        /// </summary>
        public void BeginMove(bool rotate = false)
        {
            var b = Selected;
            if (b == null)
            {
                return;
            }

            if (VillageState.IsFood(b.kind))
            {
                Notice = I18n.Source("notice.food_in_use");
                return;
            }

            if (b.upgradeRemaining > 0)
            {
                Notice = I18n.Source("notice.wait_for_beetle");
                return;
            }

            Placing = b.kind;
            EditingId = b.id;
            DraftPosition = new Vector3(b.x, .17f, b.z);
            DraftRotation = b.rotation;
            DraftLocked = rotate;
            DraftVisible = true;
            if (rotate)
            {
                RotateDraft();
            }

            Notice = rotate
                ? I18n.Source("notice.preview_rotation")
                : I18n.Source("notice.move_building");
        }

        /// <summary>
        /// Rotates the placement preview by one quarter turn.
        /// </summary>
        public void RotateDraft()
        {
            DraftRotation = (DraftRotation + 1) % 4;
        }

        /// <summary>
        /// Commits a valid locked placement preview and saves the resulting state.
        /// </summary>
        public void ConfirmDraft()
        {
            if (!Placing.HasValue || !DraftVisible || !DraftLocked)
            {
                return;
            }

            string error;
            bool moving = EditingId != 0;
            bool success = moving
                ? State.TryMove(EditingId, (int)DraftPosition.x, (int)DraftPosition.z, DraftRotation, out error)
                : State.TryBuild(Placing.Value, (int)DraftPosition.x, (int)DraftPosition.z, out error, DraftRotation);
            if (!success)
            {
                Notice = error;
                return;
            }

            selectedId = moving ? EditingId : State.buildings[State.buildings.Count - 1].id;
            Placing = null;
            EditingId = 0;
            DraftVisible = false;
            World.Sync(State);
            Changed(moving
                ? I18n.Source("notice.move_saved")
                : VillageState.IsFood(State.buildings[State.buildings.Count - 1].kind)
                    ? I18n.Source("notice.food_placed")
                    : I18n.Source("notice.building_ready"));
        }

        /// <summary>
        /// Requests an upgrade for the selected building and reports the result.
        /// </summary>
        public void Upgrade()
        {
            if (State.Upgrade(selectedId, out var error))
            {
                World.Sync(State);
                Changed(I18n.Source("notice.upgrade_started"));
            }
            else
            {
                Notice = error;
            }
        }

        /// <summary>
        /// Cancels placement or clears the current selection.
        /// </summary>
        public void Cancel()
        {
            bool draft = Placing.HasValue;
            Placing = null;
            EditingId = 0;
            DraftVisible = false;
            if (!draft)
            {
                selectedId = 0;
            }

            Notice = draft ? I18n.Source("notice.move_cancelled") : I18n.Source("notice.select_building");
        }

        /// <summary>
        /// Assigns the selected job option and reports any domain validation error.
        /// </summary>
        public void StartJob(int option)
        {
            if (State.StartJob(selectedId, option, out var error))
            {
                Changed(I18n.Source("notice.job_started"));
            }
            else
            {
                Notice = error;
            }
        }

        /// <summary>
        /// Collects a completed job and saves its stored reward.
        /// </summary>
        public void Collect(int id)
        {
            var b = State.buildings.Find(item => item.id == id);
            int reward = b != null ? b.reward : 0;
            if (State.Collect(id))
            {
                Changed(I18n.Source("action.harvest_prefix") + reward + I18n.Source("notice.harvest_suffix"));
            }
        }

        /// <summary>
        /// Pays to replenish the selected empty worksite.
        /// </summary>
        public void Refill()
        {
            if (State.Refill(selectedId))
            {
                Changed(I18n.Source("notice.refilled"));
            }
            else
            {
                Notice = I18n.Source("notice.need_prefix") + VillageState.RefillCost + I18n.Source("notice.refill_suffix");
            }
        }

        /// <summary>
        /// Claims the current tutorial reward when its completion condition is met.
        /// </summary>
        public void Quest()
        {
            if (State.ClaimQuest())
            {
                Changed(I18n.Source("notice.quest_prefix") + VillageState.QuestAcorns + I18n.Source("notice.reward_separator") + VillageState.QuestXp + " XP.");
            }
        }

        /// <summary>
        /// Rewards an active ant lion interaction once and starts its retreat animation.
        /// </summary>
        public void RepelAntLion()
        {
            if (!AntLionActive)
            {
                return;
            }

            AntLionActive = false;
            antLionClock = 30;
            antLionWait = 0;
            State.RewardAntLion();
            World.RepelAntLion();
            Changed(I18n.Source("notice.ant_lion_prefix") + VillageState.AntLionAcorns + I18n.Source("notice.reward_separator") + VillageState.AntLionXp + " XP.");
        }

        /// <summary>
        /// Rewards an active ladybug interaction once and starts its rescue animation.
        /// </summary>
        public void HelpVisitor()
        {
            if (!VisitorActive)
            {
                return;
            }

            State.HelpVisitor();
            VisitorActive = false;
            visitorClock = 30;
            visitorWait = 0;
            World.RescueVisitor();
            Changed(I18n.Source("notice.ladybug_prefix") + VillageState.VisitorAcorns + I18n.Source("notice.reward_separator") + VillageState.VisitorXp + " XP.");
        }

        /// <summary>
        /// Purchases an outer plot, refreshes its appearance, and persists ownership immediately.
        /// </summary>
        public bool BuyExpansion(LandPlot plot, out string error)
        {
            if (!State.TryBuyExpansion(plot, out error))
            {
                return false;
            }

            World.Sync(State);
            Changed(I18n.Source("expansion.purchase_success"));
            return true;
        }

        /// <summary>
        /// Updates the notification and immediately saves the changed village.
        /// </summary>
        private void Changed(string message)
        {
            Notice = message;
            Save();
        }

        /// <summary>Loads a validated account village and retains the existing local guest save independently.</summary>
        /// <returns>Whether migration succeeded and the cloud village could be activated.</returns>
        public bool EnterCloudVillage(VillageState loaded, string cacheKey)
        {
            if (loaded == null || loaded.buildings == null || !loaded.Migrate())
            {
                return false;
            }

            State = loaded;
            State.Advance(Math.Max(0, Now - State.savedAt));
            activeSaveKey = cacheKey;
            selectedId = 0;
            Placing = null;
            EditingId = 0;
            DraftVisible = false;
            lastClock = Time.realtimeSinceStartupAsDouble;
            World.ReplaceVillage(State);
            Save();
            return true;
        }

        /// <summary>Dismisses the welcome screen after a cloud village is ready.</summary>
        public void CloseWelcomeScreen()
        {
            loginScreen.ContinueAsGuest();
        }

        /// <summary>
        /// Saves the village when the application is paused.
        /// </summary>
        void OnApplicationPause(bool paused)
        {
            if (paused && State != null)
            {
                Save();
            }
        }

        /// <summary>
        /// Saves the village when the application loses focus.
        /// </summary>
        void OnApplicationFocus(bool focus)
        {
            if (!focus && State != null)
            {
                Save();
            }
        }

        /// <summary>
        /// Saves the village before the application exits.
        /// </summary>
        void OnApplicationQuit()
        {
            if (State != null)
            {
                Save();
            }
        }
    }
}
