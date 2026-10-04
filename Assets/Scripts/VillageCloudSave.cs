using System;
using System.Collections;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace LittleColony
{
    /// <summary>
    /// Synchronizes an authenticated village through same-origin HTTP requests while retaining local recovery data.
    /// </summary>
    public sealed class VillageCloudSave : MonoBehaviour
    {
        /// <summary>Describes the server-issued browser session without exposing provider credentials.</summary>
        [Serializable]
        private sealed class SessionResponse
        {
            public bool authenticated;
            public bool facebookEnabled;
            public bool discordEnabled;
            public string provider;
            public string playerId;
            public string displayName;
            public string csrfToken;
        }

        /// <summary>Preserves the game's existing serialized state inside its cloud concurrency envelope.</summary>
        [Serializable]
        private sealed class Snapshot
        {
            public int schemaVersion;
            public long revision;
            public long expectedRevision;
            public string requestId;
            public VillageState state;
        }

        private VillageGame game;
        private SessionResponse session;
        private string baseUrl;
        private long revision;
        private int generation;
        private bool dirty;
        private bool saving;
        private float nextSaveAt;
        private string pendingWrite;
        private string statusKey = "cloud.saved";

        /// <summary>Gets whether session discovery completed successfully.</summary>
        public bool SessionKnown => session != null;

        /// <summary>Gets whether the server has Facebook credentials configured.</summary>
        public bool FacebookEnabled => session != null && session.facebookEnabled;

        /// <summary>Gets whether the server has Discord credentials configured.</summary>
        public bool DiscordEnabled => session != null && session.discordEnabled;

        /// <summary>Gets whether this browser has an authenticated account.</summary>
        public bool IsAuthenticated => session != null && session.authenticated;

        /// <summary>Gets the name returned by the verified account provider.</summary>
        public string DisplayName => session == null ? "" : session.displayName;

        /// <summary>Gets whether the active village belongs to the authenticated account.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Gets whether a village transition temporarily blocks gameplay input.</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>Gets whether a concurrent cloud update requires an explicit player choice.</summary>
        public bool HasConflict { get; private set; }

        /// <summary>Gets whether the welcome screen should expose a failed account-loading attempt.</summary>
        public bool HasLoadError { get; private set; }

        /// <summary>Gets the localized synchronization status shown in the welcome card and account settings.</summary>
        public string Status => I18n.Text(statusKey);

        /// <summary>Gets whether account actions can start without overlapping a save or village transition.</summary>
        public bool CanManageAccount => IsActive && !saving && !IsTransitioning;

        /// <summary>Gets whether the player must sign in again before cloud synchronization can resume.</summary>
        public bool NeedsSignIn => statusKey == "cloud.session_expired";

        /// <summary>Gets the private local cache key for the current internal player ID.</summary>
        public string CacheKey => "little-colony.cloud." + session.playerId;

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>Navigates the current browser tab to a same-origin authentication route.</summary>
        [DllImport("__Internal")]
        private static extern void LittleColony_AuthNavigate(string path);
#endif

        /// <summary>Discovers the backend session without activating cloud writes for guest play.</summary>
        public void Initialize(VillageGame villageGame)
        {
            game = villageGame;
            baseUrl = Application.platform == RuntimePlatform.WebGLPlayer
                ? new Uri(Application.absoluteURL).GetLeftPart(UriPartial.Authority)
                : "http://localhost:5100";
            if (Application.platform == RuntimePlatform.WebGLPlayer
                && new Uri(Application.absoluteURL).Query.Contains("login=failed"))
            {
                statusKey = "cloud.login_failed";
                HasLoadError = true;
            }

            StartCoroutine(DiscoverSession());
        }

        /// <summary>Reads the current cookie-backed account and a CSRF token from the game's origin.</summary>
        private IEnumerator DiscoverSession()
        {
            using (UnityWebRequest request = CreateRequest("/api/session", "GET"))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    session = Parse<SessionResponse>(request.downloadHandler.text);
                }
            }
        }

        /// <summary>Starts Facebook OAuth, or loads the already-authenticated player's village.</summary>
        public bool SignInWithFacebook()
        {
            return SignIn(FacebookEnabled, "/auth/facebook");
        }

        /// <summary>Starts Discord OAuth, or loads the already-authenticated player's village.</summary>
        public bool SignInWithDiscord()
        {
            return SignIn(DiscordEnabled, "/auth/discord");
        }

        /// <summary>Preserves the local village before starting an available provider or entering the current account.</summary>
        private bool SignIn(bool enabled, string path)
        {
            if (IsTransitioning)
            {
                return true;
            }

            if (IsAuthenticated)
            {
                StartCoroutine(LoadAccount(false));
                return true;
            }

            if (!enabled)
            {
                StartCoroutine(DiscoverSession());
                return false;
            }

            game.Save();
            Navigate(path);
            return true;
        }

        /// <summary>Marks the isolated account cache as newer than the last acknowledged server save.</summary>
        public void MarkDirty()
        {
            if (!IsActive)
            {
                return;
            }

            dirty = true;
            generation++;
            PlayerPrefs.SetInt(CacheKey + ".dirty", 1);
            PlayerPrefs.SetString(CacheKey + ".revision", revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Saves modified cloud villages periodically without sending a request every frame.</summary>
        private void Update()
        {
            if (IsActive && dirty && !saving && !HasConflict && !IsTransitioning && Time.unscaledTime >= nextSaveAt)
            {
                StartCoroutine(SaveCloud());
            }
        }

        /// <summary>Loads cloud data or reconciles the same account's pending local write before allowing play.</summary>
        private IEnumerator LoadAccount(bool chooseCloud)
        {
            IsTransitioning = true;
            HasLoadError = false;
            statusKey = "cloud.loading";
            bool cachedDirty = !chooseCloud && PlayerPrefs.GetInt(CacheKey + ".dirty", 0) == 1;
            long.TryParse(PlayerPrefs.GetString(CacheKey + ".revision", "0"), out long cachedRevision);
            VillageState cachedState = cachedDirty ? Parse<VillageState>(PlayerPrefs.GetString(CacheKey, "")) : null;
            string retry = cachedDirty ? PlayerPrefs.GetString(CacheKey + ".pending", "") : "";
            bool retryConflict = false;

            if (!string.IsNullOrEmpty(retry))
            {
                using (UnityWebRequest request = CreateRequest("/api/save", "PUT", retry))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Snapshot committed = Parse<Snapshot>(request.downloadHandler.text);
                        if (committed == null)
                        {
                            FailTransition();
                            yield break;
                        }

                        cachedRevision = committed.revision;
                        PlayerPrefs.SetString(CacheKey + ".revision", cachedRevision.ToString());
                        PlayerPrefs.DeleteKey(CacheKey + ".pending");
                    }
                    else if (request.responseCode == 409)
                    {
                        retryConflict = true;
                    }
                    else
                    {
                        FailTransition();
                        yield break;
                    }
                }
            }

            using (UnityWebRequest request = CreateRequest("/api/save", "GET"))
            {
                yield return request.SendWebRequest();
                Snapshot cloud = null;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    cloud = Parse<Snapshot>(request.downloadHandler.text);
                    if (cloud == null || cloud.state == null)
                    {
                        FailTransition();
                        yield break;
                    }
                }
                else if (request.responseCode != 404)
                {
                    FailTransition();
                    yield break;
                }

                revision = cloud == null ? 0 : cloud.revision;
                HasConflict = cachedDirty && cachedState != null && (retryConflict || cachedRevision != revision);
                VillageState chosen = cachedDirty && cachedState != null
                    ? cachedState
                    : cloud == null ? Parse<VillageState>(JsonUtility.ToJson(game.State)) : cloud.state;
                if (HasConflict)
                {
                    revision = cachedRevision;
                }

                if (chooseCloud && PlayerPrefs.HasKey(CacheKey))
                {
                    PlayerPrefs.SetString(CacheKey + ".recovery", PlayerPrefs.GetString(CacheKey));
                }

                if (!game.EnterCloudVillage(chosen, CacheKey))
                {
                    statusKey = "cloud.invalid";
                    HasLoadError = true;
                    IsTransitioning = false;
                    yield break;
                }

                pendingWrite = "";
                PlayerPrefs.DeleteKey(CacheKey + ".pending");
                IsActive = true;
                MarkDirty();
                PlayerPrefs.Save();
                statusKey = HasConflict ? "cloud.conflict" : "cloud.saving";
                nextSaveAt = Time.unscaledTime;
                IsTransitioning = false;
                game.CloseWelcomeScreen();
            }
        }

        /// <summary>Retains the current village and allows retry when account loading fails.</summary>
        private void FailTransition()
        {
            statusKey = "cloud.connection_error";
            HasLoadError = true;
            IsTransitioning = false;
        }

        /// <summary>Writes one immutable snapshot and retains its exact body for safe network retries.</summary>
        private IEnumerator SaveCloud()
        {
            saving = true;
            statusKey = "cloud.saving";
            game.Save();
            int savedGeneration = generation;
            bool retrying = !string.IsNullOrEmpty(pendingWrite);
            if (!retrying)
            {
                pendingWrite = JsonUtility.ToJson(new Snapshot
                {
                    schemaVersion = game.State.version,
                    expectedRevision = revision,
                    requestId = Guid.NewGuid().ToString(),
                    state = game.State
                });
                PlayerPrefs.SetString(CacheKey + ".pending", pendingWrite);
                PlayerPrefs.Save();
            }

            using (UnityWebRequest request = CreateRequest("/api/save", "PUT", pendingWrite))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Snapshot saved = Parse<Snapshot>(request.downloadHandler.text);
                    if (saved != null)
                    {
                        revision = saved.revision;
                        pendingWrite = "";
                        dirty = retrying || generation != savedGeneration;
                        PlayerPrefs.DeleteKey(CacheKey + ".pending");
                        PlayerPrefs.SetString(CacheKey + ".revision", revision.ToString());
                        PlayerPrefs.SetInt(CacheKey + ".dirty", dirty ? 1 : 0);
                        PlayerPrefs.Save();
                        statusKey = "cloud.saved";
                    }
                    else
                    {
                        statusKey = "cloud.connection_error";
                    }
                }
                else if (request.responseCode == 409)
                {
                    HasConflict = true;
                    statusKey = "cloud.conflict";
                }
                else
                {
                    statusKey = request.responseCode == 401 ? "cloud.session_expired" : "cloud.connection_error";
                }
            }

            nextSaveAt = Time.unscaledTime + 30;
            saving = false;
        }

        /// <summary>Flushes the current account before expiring its session and returning to the welcome screen.</summary>
        private IEnumerator Logout()
        {
            IsTransitioning = true;
            if (saving)
            {
                IsTransitioning = false;
                yield break;
            }

            if (!HasConflict)
            {
                yield return SaveCloud();
            }

            if (dirty || HasConflict)
            {
                if (!NeedsSignIn)
                {
                    statusKey = "cloud.logout_pending";
                }
                IsTransitioning = false;
                yield break;
            }

            using (UnityWebRequest request = CreateRequest("/auth/logout", "POST"))
            {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    IsActive = false;
                    Navigate("/");
                }
                else
                {
                    statusKey = "cloud.connection_error";
                }
            }

            IsTransitioning = false;
        }

        /// <summary>Creates a bounded same-origin request; the browser manages the HttpOnly session cookie.</summary>
        private UnityWebRequest CreateRequest(string path, string method, string body = null)
        {
            var request = new UnityWebRequest(baseUrl + path, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 12
            };
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (session != null && !string.IsNullOrEmpty(session.csrfToken) && method != "GET")
            {
                request.SetRequestHeader("X-CSRF-TOKEN", session.csrfToken);
            }

            return request;
        }

        /// <summary>Parses network or cache JSON without discarding the player's current village on malformed data.</summary>
        private static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Navigates to local authentication routes without placing tokens in the URL.</summary>
        private void Navigate(string path)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            LittleColony_AuthNavigate(path);
#else
            Application.OpenURL(baseUrl + path);
#endif
        }

        /// <summary>Starts a protected sign-out from settings after flushing pending village progress.</summary>
        public void RequestLogout()
        {
            if (CanManageAccount)
            {
                StartCoroutine(Logout());
            }
        }

        /// <summary>Restarts the verified account's provider flow after its session expires.</summary>
        public void RequestSignInAgain()
        {
            if (CanManageAccount && NeedsSignIn)
            {
                game.Save();
                Navigate(session.provider == "discord" ? "/auth/discord" : "/auth/facebook");
            }
        }

        /// <summary>Loads the server village after preserving the conflicting local cache for recovery.</summary>
        public void RequestCloudRecovery()
        {
            if (CanManageAccount && HasConflict)
            {
                StartCoroutine(LoadAccount(true));
            }
        }
    }
}
