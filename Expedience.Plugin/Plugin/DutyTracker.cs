using System;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Expedience.Models;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Game.Text;
using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using Dalamud.Plugin.Services;

namespace Expedience.Services
{
	public class DutyTracker
	{
		private readonly ChatManager chatManager;

		//private readonly IDalamudPluginInterface pluginInterface;
		private readonly DataManager _dataManager;
		private readonly LocalDbManager _localDbManager;
		private readonly UserManager _userManager;
		private CurrentDutyInfo _currentDuty;
		private bool _currentDutyIsUnsynced;

		private readonly List<uint> _echoBuffList = new List<uint> { 42, 239, 2734 };

		public DutyTracker(ChatManager chatManager, 
			DataManager dataManager, 
			LocalDbManager localDbManager, 
			UserManager userManager)
		{
			this.chatManager = chatManager;
			_dataManager = dataManager;
			_localDbManager = localDbManager;
			_userManager = userManager;
		}

		public void OnDutyStarted(object sender, ushort territoryType)
		{
			try
			{
				var stopwatch = new System.Diagnostics.Stopwatch();
				stopwatch.Start();

				_currentDuty = new CurrentDutyInfo(stopwatch);
				var startTime = DateTime.UtcNow;
				var localPlayer = Service.ClientState.LocalPlayer;

				var territory = _dataManager.GetTerritory(territoryType);
				var contentName = DataManager.FormatContentName(territory.ContentFinderCondition);
				var hasEcho = CheckForEchoStatus(localPlayer);

				chatManager.PrintMessage($"Expedience: {contentName} started at {DateTime.UtcNow:HH:mm:ss}.");

				_currentDuty.Player = CreatePlayerInfo(localPlayer);
				_currentDuty.TerritoryId = territoryType;
				_currentDuty.PlaceName = territory.PlaceName.Value?.Name ?? string.Empty;
				_currentDuty.ContentName = contentName;
				_currentDuty.HasEcho = hasEcho;
				_currentDuty.IsMinILevel = IsMinILevel();
				_currentDuty.DataCenter = localPlayer.CurrentWorld.GameData.DataCenter.Value?.Name ?? string.Empty;

				_currentDuty.PartyMembers.AddRange(GetAllPartyMembers(localPlayer));
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, "Error in OnDutyStarted");
			}
		}

		private List<GroupMemberInfo> GetAllPartyMembers(IPlayerCharacter localPlayer)
		{
			var groupMemberInfos = new List<GroupMemberInfo>();

			var allianceMembers = GetAllianceMembers(localPlayer.EntityId);
			var partyMembers = GetPartyMembers(localPlayer.EntityId);
			var buddyMembers = GetBuddyMembers();

			groupMemberInfos.AddRange(allianceMembers);
			groupMemberInfos.AddRange(partyMembers);
			groupMemberInfos.AddRange(buddyMembers);

			if (groupMemberInfos.Count == 0)
			{
				// Not sure why TRUST NPCs aren't picked up as buddy members
				// While this could be solo, let's check the object table...
				var trustMembers = GetBattleNpcs(localPlayer.GameObjectId);
				groupMemberInfos.AddRange(trustMembers);
			}

			return groupMemberInfos;
		}

		private PlayerInfo CreatePlayerInfo(IPlayerCharacter localPlayer)
		{
			var player = new PlayerInfo
			{
				ClassJob = localPlayer.ClassJob.GameData.Abbreviation,
				Level = localPlayer.Level
			};

			return player;
		}

		private bool CheckForEchoStatus(IPlayerCharacter localPlayer) =>
			localPlayer.StatusList.Any(s => _echoBuffList.Contains(s.StatusId));

		public async void OnDutyCompleted(object sender, ushort territoryType)
		{
			try
			{
				if (_currentDuty == null || _currentDuty.TerritoryId != territoryType) return;

				_currentDuty.EndDuty();
				_currentDuty.IsUnrestricted = _currentDutyIsUnsynced;

				chatManager.PrintDutyCompletionTime(_currentDuty.ContentName, _currentDuty.GetDuration());

				var result = new DutyCompletionResult(_currentDuty, _dataManager.ClientInfo, _userManager.GetUserInfo());
				await _localDbManager.SaveResultToLocalDb(result);
			}
			catch (Exception ex)
			{
				Service.PluginLog.Error(ex, $"Error occurred in OnDutyCompleted: {ex.Message}");
			}
			finally
			{
				_currentDuty = null;
			}
		}

		public void OnContentsFinderPop(ContentFinderCondition obj)
		{
			_currentDutyIsUnsynced = false;
		}

		public void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled) 
		{
			if (timestamp == 0 && (int)type == 2105)
			{
				if (_dataManager.IsUnrestrictedMessage(message.TextValue))
				{
					_currentDutyIsUnsynced = true;
				}
			}
		}

		private readonly Dictionary<string, DalamudLinkPayload> _dutyLinkMap = new Dictionary<string, DalamudLinkPayload>();
		private readonly Dictionary<uint, string> _reverseDutyLinkMap = new Dictionary<uint, string>();
		private uint _nextCommandId = 57300;

		

		public static unsafe bool IsMinILevel()
		{
			// Shamelessly copied from:
			// https://github.com/Kouzukii/ffxiv-characterstatus-refined/blob/master/CharacterPanelRefined/IlvlSync.cs
			if (EventFramework.Instance() != null && EventFramework.Instance()->GetInstanceContentDirector() != null)
			{
				var icd = (IntPtr)EventFramework.Instance()->GetInstanceContentDirector();
				if (*(byte*)(icd + 3284) != 8 && (*(byte*)(icd + 828) & 1) == 0)
				{
					if (*(byte*)(icd + 7324) >= 0x80 && *(ushort*)(icd + 1316) > 0)
					{
						return true;
					}
				}
			}

			return false;
		}

		private unsafe List<GroupMemberInfo> GetAllianceMembers(uint objectId)
		{
			var partyMembers = new List<GroupMemberInfo>();

			for (var j = 0; j < 2; ++j)
			{
				for (var i = 0; i < 8; ++i)
				{
					var pGroupMember = GroupManager.Instance()->MainGroup.GetAllianceMemberByGroupAndIndex(j, i);
					if ((IntPtr)pGroupMember != IntPtr.Zero)
					{
						var partyMember = Service.PartyList.CreateAllianceMemberReference((IntPtr)pGroupMember);
						if (partyMember.Name.TextValue.Length == 0)
							continue;

						partyMembers.Add(new GroupMemberInfo
						{
							GroupNumber = j + 1,
							ClassJob = partyMember.ClassJob.GameData.Abbreviation,
							Level = partyMember.Level,
							IsPlayer = partyMember.ObjectId == objectId,
						});
					}
				}
			}

			return partyMembers;
		}

		private List<GroupMemberInfo> GetPartyMembers(uint objectId)
		{
			var partyMembers = new List<GroupMemberInfo>();
			Service.PluginLog.Info($"{Service.PartyList.Count} party members found");
			foreach (var member in Service.PartyList)
			{
				partyMembers.Add(new GroupMemberInfo
				{
					GroupNumber = 0,
					ClassJob = member.ClassJob.GameData.Abbreviation,
					Level = member.Level,
					IsPlayer = member.ObjectId == objectId,
				});
			}

			return partyMembers;
		}

		private List<GroupMemberInfo> GetBuddyMembers()
		{
			var partyMembers = new List<GroupMemberInfo>();

			Service.PluginLog.Info($"{Service.BuddyList.Count} buddies found");
			foreach (var member in Service.BuddyList)
			{
				if (Service.ObjectTable.FirstOrDefault(o => o.EntityId == member.ObjectId) is not IBattleNpc buddy)
					continue;

				partyMembers.Add(new GroupMemberInfo
				{
					GroupNumber = 0,
					ClassJob = buddy.ClassJob.GameData.Abbreviation,
					Level = buddy.Level,
					IsNpc = true,
				});
			}

			return partyMembers;
		}

		private List<GroupMemberInfo> GetBattleNpcs(ulong playerId)
		{
			var partyMembers = new List<GroupMemberInfo>();

			var battleNpcs = Service.ObjectTable.OfType<IBattleNpc>().ToList();

			Service.PluginLog.Info($"{battleNpcs.Count} Battle NPCs found");

			foreach (var member in battleNpcs)
			{
				if (member.OwnerId != playerId)
					continue;

				var buddy = member;

				Service.PluginLog.Info($"Adding Battle NPC {buddy.Name} with Owner Id {buddy.OwnerId} and job {buddy.ClassJob.GameData.Abbreviation}");

				partyMembers.Add(new GroupMemberInfo
				{
					GroupNumber = 0,
					ClassJob = buddy.ClassJob.GameData.Abbreviation,
					Level = buddy.Level,
					IsNpc = true,
				});
			}

			return partyMembers;
		}
	}
}