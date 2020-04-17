/*
	Login to partner.steamgames.com and open your game's App Admin page.
	Under Technical Tools hit Edit Steamworks Settings.
	Go to Application->Steam Cloud and set your data quotas e.g. 1048576000 for data per user and 1000 for number of files.
	[optional] Tick Enable cloud support for developers only to hide your Workshop work until it is finished for public use.
	Go to Workshop->General and tick Enable ISteamUGC for file transfer.
	Go to Publish and apply your changes.
	Steam might need a few hours for the changes to be applied -> be patient if it doesn't start to work instantly.
*/

/*
	Download binaries: https://github.com/rlabrecque/Steamworks.NET/releases/download/13.0.0/Steamworks.NET-Standalone_13.0.0.zip
	Copy files "steam_api64.dll", "steam_appid.txt" and "Steamworks.NET.dll" from "Windows-x64" into
		directory with SteamWorkshopUploader.cs file.
	Create directory "\\Upload\\Content" and place some files, for example *png.
	Create preview file "\\Upload\\icon.png" (maximum size should be 512 x 512 pixels).
	Edit file "steam_appid.txt" to match AppId of your game.
	Visibility mode is set as "only visible to the creator". Changeable with "ERemoteStoragePublishedFileVisibility".
	Compile with Visual Studio C# command line: csc SteamWorkshopUploader.cs -reference:Steamworks.NET.dll
	Login to Steam client (Steam client must run in background to get application working properly).
	Run executable.
	After upload, subscribe to an item in the Steam Workshop, then it will be downloaded here:
	"SteamLibrary\steamapps\workshop\content\<AppID>"
	For update existing item, set proper fileID (line 51).
*/

using System;
using System.IO;
using Steamworks;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class Program
{
	struct SteamWorkshopItem
	{
		public string Title;
		public string Description;
		public string ContentFolderPath;
		public string[] Tags;
		public string PreviewImagePath;
	}

	static SteamWorkshopItem _CurrentSteamWorkshopItem;
	static UGCUpdateHandle_t _UpdateHandle = UGCUpdateHandle_t.Invalid;
	static List <string> _ModsReceivedResult = new List<string>();

	static void UploadContent(string title, string description, string content, string[] tags, string image, bool update = false)
	{
		_CurrentSteamWorkshopItem = new SteamWorkshopItem { Title = title, Description = description, ContentFolderPath = content, Tags = tags, PreviewImagePath = image };
		if (update)
		{
			UpdateExistingItem(123456789, "Update 2");
		}
		else
		{
			SteamAPICall_t createHandle = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
			CallResult<CreateItemResult_t> OnCreateItemResultCallResult = CallResult<CreateItemResult_t>.Create();
			OnCreateItemResultCallResult.Set(createHandle, OnCreateItemResult);
		}
	}

	static void OnCreateItemResult(CreateItemResult_t pCallback, bool bIOFailure)
	{
		if (pCallback.m_eResult == EResult.k_EResultOK)
		{
			PublishedFileId_t _PublishedFileID = pCallback.m_nPublishedFileId;
			_UpdateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), _PublishedFileID);
			SteamUGC.SetItemTitle(_UpdateHandle, _CurrentSteamWorkshopItem.Title);
			SteamUGC.SetItemDescription(_UpdateHandle, _CurrentSteamWorkshopItem.Description);
			SteamUGC.SetItemContent(_UpdateHandle, _CurrentSteamWorkshopItem.ContentFolderPath);
			SteamUGC.SetItemTags(_UpdateHandle, _CurrentSteamWorkshopItem.Tags);
			SteamUGC.SetItemPreview(_UpdateHandle, _CurrentSteamWorkshopItem.PreviewImagePath);
			SteamUGC.SetItemVisibility(_UpdateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);
			SteamAPICall_t submitHandle = SteamUGC.SubmitItemUpdate(_UpdateHandle, "Initial commit");
			SubmitAsync(submitHandle);
		}
		else
		{
			Console.WriteLine("Couldn't create a new item ! Press any key to continue...");
		}
	}

	static void UpdateExistingItem(ulong fileID, string changelog)
	{
		PublishedFileId_t _PublishedFileID = new PublishedFileId_t(fileID);
		_UpdateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), _PublishedFileID);
		SteamUGC.SetItemTitle(_UpdateHandle, _CurrentSteamWorkshopItem.Title);
		SteamUGC.SetItemDescription(_UpdateHandle, _CurrentSteamWorkshopItem.Description);
		SteamUGC.SetItemContent(_UpdateHandle, _CurrentSteamWorkshopItem.ContentFolderPath);
		SteamUGC.SetItemTags(_UpdateHandle, _CurrentSteamWorkshopItem.Tags);
		SteamUGC.SetItemPreview(_UpdateHandle, _CurrentSteamWorkshopItem.PreviewImagePath);
		SteamUGC.SetItemVisibility(_UpdateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);
		SteamAPICall_t submitHandle = SteamUGC.SubmitItemUpdate(_UpdateHandle, changelog);
		SubmitAsync(submitHandle);
	}

	static bool IsCompleted (SteamAPICall_t handle)
	{
		return SteamUtils.IsAPICallCompleted(handle, out bool result);
	}

	static async void SubmitAsync(SteamAPICall_t submitHandle)
	{
		await Task.Run(() => SubmitTask(submitHandle));
	}

	static void SubmitTask(SteamAPICall_t submitHandle)
	{
		while (!IsCompleted(submitHandle)) 
		{
			if (_UpdateHandle != UGCUpdateHandle_t.Invalid)
			{
				System.Threading.Thread.Sleep(1);
				EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(_UpdateHandle, out ulong punBytesProcessed, out ulong punBytesTotal);
				float progress = (float)punBytesProcessed / (float)punBytesTotal * 100.0f;
				if (status == EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig) ClearLine("Processing configuration data...");
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusPreparingContent && !Single.IsNaN (progress)) ClearLine("Processing files: " + progress.ToString("F2") + " % ");
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusUploadingContent && !Single.IsNaN (progress)) ClearLine("Upload files: " + progress.ToString("F2") + " % ");
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusUploadingPreviewFile) ClearLine("Upload preview file...");				
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges) ClearLine("Commiting changes...");
			}
		}
		IntPtr pCallback = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SubmitItemUpdateResult_t)));
		SteamUtils.GetAPICallResult(submitHandle, pCallback, Marshal.SizeOf(typeof(SubmitItemUpdateResult_t)), SubmitItemUpdateResult_t.k_iCallback, out bool pbFailed);
		SubmitItemUpdateResult_t callback = (SubmitItemUpdateResult_t)Marshal.PtrToStructure(pCallback, typeof(SubmitItemUpdateResult_t));		
		if (callback.m_eResult == EResult.k_EResultOK)
		{
			Console.WriteLine("\nSuccessfully submitted item to Steam ! Press any key to continue...");
		}
		else
		{
			Console.WriteLine("\nCouldn't submit the item to Steam (" + callback.m_eResult.ToString() + ") ! Press any key to continue...");
		}
		Marshal.FreeHGlobal(pCallback);
	}

	static void ClearLine (string param)
	{
		Console.Write("\r" + new string(' ', Console.WindowWidth-1) + "\r");
		Console.Write(param);
	}

	static async void UploadAsync(string content, string icon)
	{
		await Task.Run(() => UploadContent("Title", "Description", content, new string[1] {"item"}, icon, false));
		Console.WriteLine ("Please Wait...");
	}

	static void GetModsInfoFromUser()
	{
		var query = SteamUGC.CreateQueryUserUGCRequest( SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published,
			EUGCMatchingUGCType.k_EUGCMatchingUGCType_UsableInGame, EUserUGCListSortOrder.k_EUserUGCListSortOrder_VoteScoreDesc, 
			SteamUtils.GetAppID(), SteamUtils.GetAppID(), 1 );
		SteamAPICall_t request = SteamUGC.SendQueryUGCRequest( query );
		CallResult<SteamUGCQueryCompleted_t> _userModsCallResult = CallResult<SteamUGCQueryCompleted_t>.Create(OnUserModsReceivedResult);
		_userModsCallResult.Set( request );
	}

	static DateTime FromUnixTime(uint unixTime)
	{
		DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		return epoch.AddSeconds(unixTime);
	}

	static void OnUserModsReceivedResult( SteamUGCQueryCompleted_t result, bool failure )
	{
		Console.WriteLine("*************************************");
		_ModsReceivedResult.Add("User mods: " + result.m_unNumResultsReturned.ToString());
		for (uint i = 0; i < result.m_unNumResultsReturned; i++)
		{
			if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out var details))
			{
				_ModsReceivedResult.Add("- " + details.m_rgchTitle.ToString() + ", " + details.m_rgchDescription.ToString()+", "+ FromUnixTime(details.m_rtimeUpdated).ToString() +", "+ details.m_nPublishedFileId.ToString());
			}
		}
		SteamUGC.ReleaseQueryUGCRequest( result.m_handle );
		for (int i = 0; i < _ModsReceivedResult.Count; i++) Console.WriteLine(_ModsReceivedResult[i]);
		Console.WriteLine("*************************************");
	}
	
	static void Start()
	{
		if (!SteamAPI.Init())
		{
			Console.WriteLine("SteamAPI.Init() failed!"); Environment.Exit(0);
		}
		string content = Directory.GetCurrentDirectory() + "\\Upload\\Content";
		string icon = Directory.GetCurrentDirectory() + "\\Upload\\icon.png";
		if (!Directory.Exists(content) || !File.Exists(icon))
		{
			Console.WriteLine("Upload path or preview file not found !"); Environment.Exit(0);
		}
		SteamAPI.RunCallbacks();
		GetModsInfoFromUser();
		UploadAsync(content, icon);
	}

	static void Update()
	{
		while (!Console.KeyAvailable) SteamAPI.RunCallbacks();
	}

	static void Exit()
	{
		SteamAPI.Shutdown();
		Environment.Exit(0);
	}

	static void Main(string[] args)
	{
		Start();
		Update();
		Exit();
	}
}