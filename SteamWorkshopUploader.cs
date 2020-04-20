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
*/

using System;
using System.IO;
using Steamworks;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

class Program
{	
	static T AllocCallback<T>(SteamAPICall_t handle, out IntPtr pCallback, int k_iCallback)
	{
		pCallback = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
		SteamUtils.GetAPICallResult(handle, pCallback, Marshal.SizeOf(typeof(T)), k_iCallback, out bool pbFailed);
		return (T)Marshal.PtrToStructure(pCallback, typeof(T));
	}

	static void ReleaseCallback(IntPtr pCallback)
	{
		Marshal.FreeHGlobal(pCallback);
	}

	static bool IsCompleted (SteamAPICall_t handle)
	{
		return SteamUtils.IsAPICallCompleted(handle, out bool result);
	}

	static void ClearLine (string param)
	{
		Console.Write("\r" + new string(' ', Console.WindowWidth-1) + "\r");
		Console.Write(param);
	}	

	static async void CreateItemAsync(string content, string icon)
	{
		Console.WriteLine ("Please Wait...");
		SteamAPICall_t createHandle = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);		
		await Task.Run(() => CreateItemTask(createHandle, "Title", "Description", content, new string[1] {"item"}, icon, false));
	}

	static void CreateItemTask(SteamAPICall_t handle, string title, string description, string content, string[] tags, string image, bool update = false)
	{
		while (!IsCompleted(handle)) { }
		UGCUpdateHandle_t updateHandle = UGCUpdateHandle_t.Invalid;
		CreateItemResult_t callback = AllocCallback<CreateItemResult_t>(handle, out IntPtr pCallback, CreateItemResult_t.k_iCallback);		
		if (callback.m_eResult == EResult.k_EResultOK)
		{
			PublishedFileId_t _PublishedFileID = callback.m_nPublishedFileId;
			updateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), _PublishedFileID);
			SteamUGC.SetItemTitle(updateHandle, title);
			SteamUGC.SetItemDescription(updateHandle, description);
			SteamUGC.SetItemContent(updateHandle, content);
			SteamUGC.SetItemTags(updateHandle, tags);
			SteamUGC.SetItemPreview(updateHandle, image);
			SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);
			SteamAPICall_t submitHandle = SteamUGC.SubmitItemUpdate(updateHandle, "Initial commit");
			SubmitItemAsync(submitHandle, updateHandle);
		}
		else
		{
			Console.WriteLine("Couldn't create a new item ! Press any key to continue...");
		}
		ReleaseCallback(pCallback);
	}

	static async void SubmitItemAsync(SteamAPICall_t submitHandle, UGCUpdateHandle_t updateHandle)
	{
		await Task.Run(() => SubmitItemTask(submitHandle, updateHandle));
	}

	static void SubmitItemTask(SteamAPICall_t handle, UGCUpdateHandle_t updateHandle)
	{
		while (!IsCompleted(handle)) 
		{
			if (updateHandle != UGCUpdateHandle_t.Invalid)
			{
				System.Threading.Thread.Sleep(1);
				EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(updateHandle, out ulong punBytesProcessed, out ulong punBytesTotal);
				float progress = (float)punBytesProcessed / (float)punBytesTotal * 100.0f;
				if (status == EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig) ClearLine("Processing configuration data...");
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusPreparingContent && !Single.IsNaN (progress)) ClearLine("Processing files: " + progress.ToString("F2") + " % ");
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusUploadingContent && !Single.IsNaN (progress)) ClearLine("Upload files: " + progress.ToString("F2") + " % ");
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusUploadingPreviewFile) ClearLine("Upload preview file...");				
				else if (status == EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges) ClearLine("Commiting changes...");
			}
		}	
		SubmitItemUpdateResult_t callback = AllocCallback<SubmitItemUpdateResult_t>(handle, out IntPtr pCallback, SubmitItemUpdateResult_t.k_iCallback);
		if (callback.m_eResult == EResult.k_EResultOK)
		{
			Console.WriteLine("\nSuccessfully submitted item to Steam ! Press any key to continue...");
		}
		else
		{
			Console.WriteLine("\nCouldn't submit the item to Steam (" + callback.m_eResult.ToString() + ") ! Press any key to continue...");
		}
		ReleaseCallback(pCallback);
	}

	static async void GetModsInfoFromUserAsync()
	{
		var query = SteamUGC.CreateQueryUserUGCRequest( SteamUser.GetSteamID().GetAccountID(), EUserUGCList.k_EUserUGCList_Published,
			EUGCMatchingUGCType.k_EUGCMatchingUGCType_UsableInGame, EUserUGCListSortOrder.k_EUserUGCListSortOrder_VoteScoreDesc, 
			SteamUtils.GetAppID(), SteamUtils.GetAppID(), 1 );
		SteamAPICall_t request = SteamUGC.SendQueryUGCRequest( query );		
		await Task.Run(() => GetModsInfoFromUserTask(request));
	}
		
	static void GetModsInfoFromUserTask(SteamAPICall_t handle)
	{
		while (!IsCompleted(handle)) { }
		List <string> _ModsReceivedResult = new List<string>();
		SteamUGCQueryCompleted_t result = AllocCallback<SteamUGCQueryCompleted_t>(handle, out IntPtr pCallback, SteamUGCQueryCompleted_t.k_iCallback);
		Console.WriteLine("*************************************");
		_ModsReceivedResult.Add("User mods: " + result.m_unNumResultsReturned.ToString());
		for (uint i = 0; i < result.m_unNumResultsReturned; i++)
		{
			if (SteamUGC.GetQueryUGCResult(result.m_handle, i, out var details))
			{
				DateTime FromUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(details.m_rtimeUpdated);
				_ModsReceivedResult.Add("- " + details.m_rgchTitle.ToString() + ", " + details.m_rgchDescription.ToString()+", "+ FromUnixTime.ToString() +", "+ details.m_nPublishedFileId.ToString());
			}
		}
		for (int i = 0; i < _ModsReceivedResult.Count; i++) Console.WriteLine(_ModsReceivedResult[i]);
		Console.WriteLine("*************************************");		
		ReleaseCallback(pCallback);
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
		GetModsInfoFromUserAsync();
		CreateItemAsync(content, icon);
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