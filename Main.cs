using System.Collections;
using Il2Cpp;
using JesusHack.LiteConfig;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ChillBlacklist.Main), "ChillBlacklist", "1.1.0", "MasterHell", null)]
[assembly: MelonGame("ZeoWorks", "Slendytubbies 3")]
[assembly: MelonColor(1, 161, 253, 0)]

namespace ChillBlacklist
{
    public class Main : MelonMod
    {
        private string[] _blackList;
        private bool _isOnMainMenu;
        private JesusHackConfig _config;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
            _config = ConfigManager.Load();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu" || sceneName == "Updater")
            {
                SetupConfig();
                _isOnMainMenu = true;

                return;
            }

            _isOnMainMenu = false;
        }

        public override void OnUpdate()
        {
            if (_isOnMainMenu)
                return;

            CheckPlayers();
        }

        private void SetupConfig()
        {
            if (!File.Exists(ConfigManager.BlacklistPath))
                File.Create(ConfigManager.BlacklistPath).Dispose();

            if (_config.OnlineBlacklist)
                MelonCoroutines.Start(FetchBlacklist());
        }

        private void CheckPlayers()
        {
            foreach (PhotonPlayer photonPlayer in PhotonNetwork.playerList)
            {
                if (!CheckPlayer(photonPlayer.name))
                    continue;

                if (PhotonNetwork.player.IsMasterClient)
                {
                    PhotonNetwork.CloseConnection(photonPlayer);
                    Kick(photonPlayer.ID);
                }

                SetFakeMasterClient();
                photonPlayer.actorID = 999;
                Crash(photonPlayer);
            }
        }

        private bool CheckPlayer(string playerName)
        {
            string[] blacklist = GetBlacklist();

            if (blacklist.Length == 0)
                return false;

            foreach (string player in blacklist)
            {
                if (playerName.Split('|')[0].Contains(player))
                    return true;
            }

            return false;
        }

        private string[] GetBlacklist()
        {
            if (_config.OnlineBlacklist && _blackList != null)
                return _blackList;

            if (File.Exists(ConfigManager.BlacklistPath))
                return File.ReadAllLines(ConfigManager.BlacklistPath);

            return [];
        }

        private IEnumerator FetchBlacklist()
        {
            WWW www = new(_config.LinkToOnlineBlacklist);
            yield return www;

            if (www.error == null)
                _blackList = www.text.Split(['\n']);
        }

        private void Kick(int photonPlayerID)
        {
            if (!IsPlayerExist(photonPlayerID))
                return;

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { TargetActors = new int[] { photonPlayerID } };
            PhotonNetwork.networkingPeer.OpRaiseEvent(203, null, true, raiseEventOptions);

            Il2CppSystem.Int32 @int = default;
            @int.m_value = photonPlayerID;
            Il2CppSystem.Object eventContent = @int.BoxIl2CppObject();

            PhotonNetwork.RaiseEvent(1, eventContent, true, new RaiseEventOptions { Receivers = ReceiverGroup.All });
        }

        private void Crash(PhotonPlayer photonPlayer)
        {
            if (!IsPlayerExist(photonPlayer))
                return;

            Il2CppSystem.Single single = default;
            single.m_value = 1E+09f;

            Il2CppSystem.Object[] parameters = ["syncShotGun", single.BoxIl2CppObject()];
            PhotonView photonView = GetPlayer(photonPlayer);
            photonView?.RPC("BCPFIMDIMJE", photonPlayer, parameters);
        }

        private void SetFakeMasterClient()
        {
            if (PhotonNetwork.player.IsMasterClient)
                return;

            PhotonNetwork.room.masterClientId = PhotonNetwork.player.ID;
            PhotonNetwork.room.MasterClientId = PhotonNetwork.player.ID;
            PhotonNetwork.room.masterClientIdField = PhotonNetwork.player.ID;
            PhotonNetwork.networkingPeer.mMasterClientId = PhotonNetwork.player.ID;
        }

        private bool IsPlayerExist(PhotonPlayer photonPlayer)
        {
            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                if (player.NickName.Contains(photonPlayer.NickName))
                    return true;
            }

            return false;
        }

        private bool IsPlayerExist(int photonPlayerID)
        {
            foreach (PhotonPlayer player in PhotonNetwork.playerList)
            {
                if (player.ID == photonPlayerID)
                    return true;
            }

            return false;
        }

        private PhotonView GetPlayer(PhotonPlayer photonPlayer)
        {
            GameObject gameObject = GameObject.Find(photonPlayer.name.Split('|')[0]);
            return gameObject?.GetComponent<PhotonView>();
        }
    }
}