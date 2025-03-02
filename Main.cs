using HarmonyLib;
using Il2Cpp;
using JesusHack.LiteConfig;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ChillBlacklist.Main), "ChillBlacklist", "1.2.0", "MasterHell", null)]
[assembly: MelonGame("ZeoWorks", "Slendytubbies 3")]
[assembly: MelonColor(1, 161, 253, 0)]

namespace ChillBlacklist
{
    public class Main : MelonMod
    {
        public static string BlackList;
        private JesusHackConfig _config;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            _config = ConfigManager.Load();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu" || sceneName == "Updater")
                SetupConfigAsync().Wait();
        }

        private async Task SetupConfigAsync()
        {
            if (!File.Exists(ConfigManager.BlacklistPath))
                File.Create(ConfigManager.BlacklistPath).Dispose();
            else
                BlackList = File.ReadAllText(ConfigManager.BlacklistPath);

            if (_config.OnlineBlacklist)
                BlackList = await DownloadFile(_config.LinkToOnlineBlacklist);
        }

        private static async Task<string> DownloadFile(string uri)
        {
            using (HttpClient httpClient = new())
            {
                HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(uri);

                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadAsStringAsync();
            }

            return null;
        }

        public static bool CheckPlayer(string playerName)
        {
            if (string.IsNullOrEmpty(BlackList))
                return false;

            string[] blackList = BlackList.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

            foreach (string player in blackList)
            {
                string trimmedPlayer = player.Trim();

                if (trimmedPlayer.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void Kick(int photonPlayerID)
        {
            if (!IsPlayerExist(photonPlayerID))
                return;

            RaiseEventOptions raiseEventOptions = new() { TargetActors = new int[] { photonPlayerID } };
            PhotonNetwork.networkingPeer.OpRaiseEvent(203, null, true, raiseEventOptions);

            Il2CppSystem.Int32 @int = default;
            @int.m_value = photonPlayerID;
            Il2CppSystem.Object eventContent = @int.BoxIl2CppObject();
            PhotonNetwork.RaiseEvent(1, eventContent, true, new RaiseEventOptions { Receivers = ReceiverGroup.All });
        }

        public static void Crash(PhotonPlayer photonPlayer)
        {
            if (!IsPlayerExist(photonPlayer))
                return;

            Il2CppSystem.Single single = default;
            single.m_value = 1E+09f;

            Il2CppSystem.Object[] parameters = ["syncShotGun", single.BoxIl2CppObject()];
            PhotonView photonView = GetPlayer(photonPlayer);
            photonView?.RPC("BCPFIMDIMJE", photonPlayer, parameters);
        }

        public static void SetFakeMasterClient()
        {
            if (PhotonNetwork.player.IsMasterClient)
                return;

            PhotonNetwork.room.masterClientId = PhotonNetwork.player.ID;
            PhotonNetwork.room.MasterClientId = PhotonNetwork.player.ID;
            PhotonNetwork.room.masterClientIdField = PhotonNetwork.player.ID;
            PhotonNetwork.networkingPeer.mMasterClientId = PhotonNetwork.player.ID;
        }

        public static bool IsPlayerExist(PhotonPlayer photonPlayer)
        {
            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                if (player.NickName.Contains(photonPlayer.NickName))
                    return true;
            }

            return false;
        }

        public static bool IsPlayerExist(int photonPlayerID)
        {
            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                if (player.ID == photonPlayerID)
                    return true;
            }

            return false;
        }

        public static PhotonView GetPlayer(PhotonPlayer photonPlayer)
        {
            GameObject gameObject = GameObject.Find(photonPlayer.name.Split('|')[0]);
            return gameObject?.GetComponent<PhotonView>();
        }
    }

    [HarmonyPatch(typeof(WhoKilledWho), "OnPhotonPlayerConnected")]
    public static class OnPlayerJoined
    {
        [HarmonyPrefix]
        private static void Prefix(ref PhotonPlayer otherPlayer, WhoKilledWho __instance)
        {
            string nickName = otherPlayer.name.Split(['|'])[0];

            if (Main.CheckPlayer(nickName) && PhotonNetwork.isMasterClient)
            {
                PhotonNetwork.DestroyPlayerObjects(otherPlayer);
                Main.Kick(otherPlayer.ID);
                PhotonNetwork.CloseConnection(otherPlayer);
                Main.Crash(otherPlayer);
            }
            else if (Main.CheckPlayer(nickName) && !PhotonNetwork.isMasterClient)
            {
                Main.SetFakeMasterClient();
                PhotonNetwork.DestroyPlayerObjects(otherPlayer);
                otherPlayer.actorID = 999;
                Main.Crash(otherPlayer);
            }
        }
    }
}