using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using Expedience.Db.Models;

namespace Expedience.UI
{
	public class MainWindow : Window, IDisposable
	{
		private List<LocalRecord> _localRecords = new();

		private string _dutySearchText = "";
		private bool _exactmatch = false;
		private readonly LocalDbManager _localDbManager;
		private readonly TriStateCheckbox _echoCheckbox = new("Echo", TriStateCheckbox.CheckboxState.PartiallyChecked);
		private readonly TriStateCheckbox _unsyncedCheckbox = new("Unsynced", TriStateCheckbox.CheckboxState.PartiallyChecked);
		private readonly TriStateCheckbox _mineCheckbox = new("Min iLevel", TriStateCheckbox.CheckboxState.PartiallyChecked);
		private readonly TriStateCheckbox _npcCheckbox = new("NPC", TriStateCheckbox.CheckboxState.PartiallyChecked);

		public MainWindow(LocalDbManager localDbManager)  :
			base($"Expedience v{Service.Plugin.GetVersion()}", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
		{
			_localDbManager = localDbManager;

			SizeConstraints = new WindowSizeConstraints
			{
				MinimumSize = new Vector2(430, 330),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
			
			GetLocalRecords();
		}

		private async void GetLocalRecords(string filter = null, bool exactMatch = false)
		{
			var query = await _localDbManager.GetLocalRecordsQuery();

			// Filter by content name
			if (!string.IsNullOrWhiteSpace(filter))
			{
				query = exactMatch
					? query.Where(lr => lr.ContentName.ToLower() == filter.ToLower())
					: query.Where(lr => lr.ContentName.ToLower().Contains(filter.ToLower()));
			}

			// Apply Echo filter
			switch (_echoCheckbox.GetState())
			{
				case TriStateCheckbox.CheckboxState.Checked:
					query = query.Where(lr => lr.HasEcho != null && lr.HasEcho);
					break;
				case TriStateCheckbox.CheckboxState.PartiallyChecked:
					// Do nothing, include both Echo and non-Echo
					break;
				case TriStateCheckbox.CheckboxState.Unchecked:
					query = query.Where(lr => !lr.HasEcho);
					break;
			}

			// Apply Unsynced filter
			switch (_unsyncedCheckbox.GetState())
			{
				case TriStateCheckbox.CheckboxState.Checked:
					query = query.Where(lr => lr.IsUnrestricted);
					break;
				case TriStateCheckbox.CheckboxState.PartiallyChecked:
					// Do nothing, include both Unsynced and Synced
					break;
				case TriStateCheckbox.CheckboxState.Unchecked:
					query = query.Where(lr => !lr.IsUnrestricted);
					break;
			}

			// Apply Min iLevel filter
			switch (_mineCheckbox.GetState())
			{
				case TriStateCheckbox.CheckboxState.Checked:
					query = query.Where(lr => lr.IsMinILevel);
					break;
				case TriStateCheckbox.CheckboxState.PartiallyChecked:
					// Do nothing, include both Min iLevel and non-Min iLevel
					break;
				case TriStateCheckbox.CheckboxState.Unchecked:
					query = query.Where(lr => !lr.IsMinILevel);
					break;
			}

			// Apply NPC filter
			switch (_npcCheckbox.GetState())
			{
				case TriStateCheckbox.CheckboxState.Checked:
					query = query.Where(lr => lr.HasNpcMembers);
					break;
				case TriStateCheckbox.CheckboxState.PartiallyChecked:
					// Do nothing, include both NPC and non-NPC
					break;
				case TriStateCheckbox.CheckboxState.Unchecked:
					query = query.Where(lr => !lr.HasNpcMembers);
					break;
			}

			await _localDbManager.WaitForSemaphore();
			// Apply ordering and take the top 10 results
			_localRecords = query
				.OrderByDescending(lr => lr.CompletionDate)
				.Take(10)
				.ToList();

			_localDbManager.ReleaseSemaphore();
		}

		public override void Draw()
		{
			var tableStyle = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.Sortable | ImGuiTableFlags.SortMulti | ImGuiTableFlags.Resizable;

			// Input text for custom input
			ImGui.SetNextItemWidth(200); // Set the width of the input text box
			ImGui.InputText("##customInput", ref _dutySearchText, 100);

			ImGui.SameLine();
						
			ImGui.Checkbox("Exact Match", ref _exactmatch);
			ImGui.SameLine();
			_echoCheckbox.Draw();
			ImGui.SameLine();
			_unsyncedCheckbox.Draw();
			ImGui.SameLine();
			_mineCheckbox.Draw();
			ImGui.SameLine();
			_npcCheckbox.Draw();
			ImGui.SameLine();

			if (ImGui.Button("Search"))
			{
				GetLocalRecords(_dutySearchText, _exactmatch);
			}

			ImGui.Spacing(); // Add some space between the combo box and the table

			if (ImGui.BeginTable("DutyTable", 8, tableStyle, new Vector2(0.0f, 300.0f)))
			{
				// Set up the headers for the table
				ImGui.TableSetupColumn("Duty", ImGuiTableColumnFlags.WidthStretch, 250);
				ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthStretch, 80);
				ImGui.TableSetupColumn("Completed On", ImGuiTableColumnFlags.WidthStretch, 130);
				ImGui.TableSetupColumn("Echo", ImGuiTableColumnFlags.WidthStretch, 60);
				ImGui.TableSetupColumn("Unsynced", ImGuiTableColumnFlags.WidthStretch, 60);
				ImGui.TableSetupColumn("MINE", ImGuiTableColumnFlags.WidthStretch, 60);
				ImGui.TableSetupColumn("NPC", ImGuiTableColumnFlags.WidthStretch, 60);
				ImGui.TableSetupColumn("Uploaded", ImGuiTableColumnFlags.WidthStretch, 40);
				ImGui.TableHeadersRow();

				// Check if sorting is needed
				var sortSpecs = ImGui.TableGetSortSpecs();
				if (sortSpecs.SpecsDirty)
				{
					SortRecords(sortSpecs);
					sortSpecs.SpecsDirty = false;
				}

				foreach (var record in _localRecords)
				{
					var isUploaded = record.IsUploaded;
					var echo = record.HasEcho;
					var unsynced = record.IsUnrestricted;
					var minILevel = record.IsMinILevel;
					var npc = record.HasNpcMembers;

					ImGui.TableNextRow();

					ImGui.TableSetColumnIndex(0);
					ImGui.Text(record.ContentName);

					ImGui.TableSetColumnIndex(1);
					ImGui.Text(record.Duration);

					ImGui.TableSetColumnIndex(2);
					ImGui.Text(record.CompletionDate.ToString("yyyy-MM-dd hh:MM:ss"));

					// Center checkboxes in columns 2-5 and 7
					for (int i = 3; i <= 6; i++)
					{
						ImGui.TableSetColumnIndex(i);
						float columnWidth = ImGui.GetColumnWidth();
						float checkboxWidth = ImGui.GetFrameHeight(); // Checkbox is usually square, so height = width
						ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (columnWidth - checkboxWidth) * 0.5f);

						switch (i)
						{
							case 2:
								ImGui.Checkbox($"##Echo{record.Id}", ref echo);
								break;
							case 3:
								ImGui.Checkbox($"##Unsynced{record.Id}", ref unsynced);
								break;
							case 4:
								ImGui.Checkbox($"##MINE{record.Id}", ref minILevel);
								break;
							case 5:
								ImGui.Checkbox($"##NPC{record.Id}", ref npc);
								break;
						}
					}

					ImGui.TableSetColumnIndex(7);
					float lastColumnWidth = ImGui.GetColumnWidth();
					float lastCheckboxWidth = ImGui.GetFrameHeight();
					ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (lastColumnWidth - lastCheckboxWidth) * 0.5f);
					ImGui.Checkbox($"##Uploaded{record.Id}", ref isUploaded);
				}

				ImGui.EndTable();
			}
		}

		internal void RetrieveDutyRecords(string dutyName, bool changeSearchText)
		{
			if (changeSearchText)
			{
				_dutySearchText = dutyName;
				_exactmatch = true;
			}

			GetLocalRecords(_dutySearchText, exactMatch: true);
		}

		internal void SortRecords(ImGuiTableSortSpecsPtr sortSpecs)
		{
			if (_localRecords == null || _localRecords.Any() == false) return;

			if (sortSpecs.SpecsCount > 0)
			{
				var spec = sortSpecs.Specs;
				var columnIndex = spec.ColumnIndex;
				var ascending = spec.SortDirection == ImGuiSortDirection.Ascending;

				_localRecords = columnIndex switch
				{
					0 => _localRecords.OrderBy(record => record.ContentName).ToList(),
					1 => _localRecords.OrderBy(record => record.Duration).ToList(),
					2 => _localRecords.OrderBy(record => record.HasEcho).ToList(),
					3 => _localRecords.OrderBy(record => record.IsUnrestricted).ToList(),
					4 => _localRecords.OrderBy(record => record.IsMinILevel).ToList(),
					5 => _localRecords.OrderBy(record => record.HasNpcMembers).ToList(),
					6 => _localRecords.OrderBy(record => record.CompletionDate).ToList(),
					7 => _localRecords.OrderBy(record => record.IsUploaded).ToList(),
					_ => _localRecords
				};

				if (!ascending)
				{
					_localRecords.Reverse();
				}
			}
		}


		private class TriStateCheckbox
		{
			public enum CheckboxState
			{
				Unchecked = 0,
				PartiallyChecked = 1,
				Checked = 2
			}

			private CheckboxState _state;
			private string _label;
			private int _flags;

			public TriStateCheckbox(string label, CheckboxState initialState = CheckboxState.Unchecked)
			{
				_label = label;
				_state = initialState;
				DetermineFlags();
			}

			public bool Draw()
			{
				bool changed = false;

				if (ImGui.CheckboxFlags(_label, ref _flags, 3))
				{
					changed = true;

					_state = _state switch
					{
						CheckboxState.Unchecked => CheckboxState.PartiallyChecked,
						CheckboxState.PartiallyChecked => CheckboxState.Checked,
						CheckboxState.Checked => CheckboxState.Unchecked,
						_ => CheckboxState.Unchecked
					};
					
					DetermineFlags();
				}

				return changed;
			}

			private void DetermineFlags()
			{
				_flags = _state switch
				{
					CheckboxState.Unchecked => 0,
					CheckboxState.PartiallyChecked => 1,
					CheckboxState.Checked => -1,
					_ => 0
				};
			}

			public CheckboxState GetState() => _state;
		}

		public void Dispose() { }
	}
}

