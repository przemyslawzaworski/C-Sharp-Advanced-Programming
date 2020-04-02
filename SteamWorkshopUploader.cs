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
			CallResult<SubmitItemUpdateResult_t> OnSubmitItemUpdateResultCallResult = CallResult<SubmitItemUpdateResult_t>.Create();
			OnSubmitItemUpdateResultCallResult.Set(submitHandle, OnSubmitItemUpdateResult);
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
		CallResult<SubmitItemUpdateResult_t> OnSubmitItemUpdateResultCallResult = CallResult<SubmitItemUpdateResult_t>.Create();
		OnSubmitItemUpdateResultCallResult.Set(submitHandle, OnSubmitItemUpdateResult);
	}
	
	static void OnSubmitItemUpdateResult(SubmitItemUpdateResult_t param, bool bIOFailure)
	{
		if (param.m_eResult == EResult.k_EResultOK)
		{
			_UpdateHandle = UGCUpdateHandle_t.Invalid;
			Console.WriteLine("\nSuccessfully submitted item to Steam ! Press any key to continue...");
		}
		else
		{
			_UpdateHandle = UGCUpdateHandle_t.Invalid;
			Console.WriteLine("\nCouldn't submit the item to Steam (" + param.m_eResult.ToString() + ") ! Press any key to continue...");
		}
	}
		
	static void ClearLine (string param)
	{
		Console.Write("\r" + new string(' ', Console.WindowWidth-1) + "\r");
		Console.Write(param);
	}

	static async void ProgressBarAsync()
	{
		System.Threading.Thread.Sleep(2);
		await Task.Run(() => ProgressBarTask());
	}
	
	static void ProgressBarTask()
	{
		if (_UpdateHandle != UGCUpdateHandle_t.Invalid)
		{
			EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(_UpdateHandle, out ulong punBytesProcessed, out ulong punBytesTotal);
			float progress = (float)punBytesProcessed / (float)punBytesTotal * 100.0f;
			if (status == EItemUpdateStatus.k_EItemUpdateStatusPreparingContent && !Single.IsNaN (progress)) ClearLine("Processing files: " + progress.ToString("F2") + " % ");
			if (status == EItemUpdateStatus.k_EItemUpdateStatusUploadingContent && !Single.IsNaN (progress)) ClearLine("Upload files: " + progress.ToString("F2") + " % ");
			if (status == EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges) ClearLine("Commiting changes...");
		}
	}

	static async void UploadAsync(string content, string icon)
	{
		await Task.Run(() => UploadContent("Title", "Description", content, new string[1] {"item"}, icon, false));
		Console.WriteLine ("Please Wait...");
	}
		
	static void Main(string[] args)
	{
		if (!SteamAPI.Init())
		{
			Console.WriteLine("SteamAPI.Init() failed!"); return;
		}
		bool upload = true;
		string contentpath = Directory.GetCurrentDirectory() + "\\Upload\\Content";
		string iconpath = Directory.GetCurrentDirectory() + "\\Upload\\icon.png";
		if (!Directory.Exists(contentpath) || !File.Exists(iconpath))
		{
			Console.WriteLine("Upload path or preview file not found !"); return;
		}
		while (Console.KeyAvailable == false)
		{
			SteamAPI.RunCallbacks();
			if (upload)
			{
				UploadAsync(contentpath, iconpath);
			}
			ProgressBarAsync();
			upload = false;
		}
		SteamAPI.Shutdown();
	}
}