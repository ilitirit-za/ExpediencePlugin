using System;

namespace Expedience.Db.Models
{
    public class LocalRecord
    {
        public int Id { get; set; }
        public int? TerritoryId { get; set; }
        public string? PlaceName { get; set; }
        public string ContentName { get; set; }
        public DateTime CompletionDate { get; set; }
		public string Duration { get; set; }
		public bool HasEcho { get; set; }
		public bool IsUnrestricted { get; set; }
		public bool IsMinILevel { get; set; }
		public bool HasNpcMembers { get; set; }
        public bool IsUploaded { get; set; }
        public string PluginVersion { get; set; }
        public string Payload { get; set; }
		public string User{ get; set; }
	}
}
