using System;
namespace Expedience.Models
{
    public class DutyCompletionResult
    {
        public DutyCompletionResult()
        {

        }

        public DutyCompletionResult(CurrentDutyInfo currentDuty, ClientInfo clientInfo, UserInfo userInfo)
        {
            DutyInfo = new DutyInfo
            {
                TerritoryId = currentDuty.TerritoryId,
                PlaceName = currentDuty.PlaceName,
                ContentName = currentDuty.ContentName,
                IsUnrestricted = currentDuty.IsUnrestricted,
                IsMinILevel = currentDuty.IsMinILevel,
                IsNpcSupported = currentDuty.PartyMembers.Any(p => p.IsNpc),
                ContentFinderConditionId = (int?)currentDuty.ContentFinderConditionId,
                HasEcho = currentDuty.HasEcho,
            };

            PlayerInfo = new PlayerInfo
            {
                ClassJob = currentDuty.Player.ClassJob,
                Level = currentDuty.Player.Level,
            };
            
            GroupMembers = currentDuty.PartyMembers.Select(p => new GroupMemberInfo
            {
                ClassJob = p.ClassJob,
                Level = p.Level,
                GroupNumber = p.GroupNumber,
                IsNpc = p.IsNpc,
                IsPlayer = p.IsPlayer,
            }).ToList();

            ClientInfo = new ClientInfo
            {
                GameVersion = clientInfo.GameVersion,
                GameLanguage = clientInfo.GameLanguage,
                PluginVersion = clientInfo.PluginVersion,
            };

            UserInfo = new UserInfo
            {
                UserId = userInfo.UserId,
                WorldId = userInfo.WorldId,
                WorldName = userInfo.WorldName,
            };

            CompletionTimeInfo = new CompletionTimeInfo(currentDuty.GetDuration());
            DutyStartDateUtc = currentDuty.StartTime;
            DutyCompletionDateUtc = currentDuty.EndTime;
            DataCenter = currentDuty.DataCenter;
        }

        public Guid UploadId { get; set; }
        public DutyInfo DutyInfo { get; set; }
        public CompletionTimeInfo CompletionTimeInfo { get; set; }
        public PlayerInfo PlayerInfo { get; set; }
        public List<GroupMemberInfo> GroupMembers { get; set; }
        public DateTime DutyStartDateUtc { get; set; }
        public DateTime DutyCompletionDateUtc { get; set; }
        public ClientInfo ClientInfo { get; set; }
        public UserInfo UserInfo{ get; set; }
        public DateTime UploadDateUtc { get; set; }
        public string DataCenter { get; set; }
    }
}
