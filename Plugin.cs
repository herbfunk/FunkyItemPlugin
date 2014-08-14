using System;
using System.IO;
using System.Linq;
using System.Windows;
using fBaseXtensions;
using fBaseXtensions.Behaviors;
using fBaseXtensions.Game;
using fBaseXtensions.Game.Hero;
using fBaseXtensions.Helpers;
using fBaseXtensions.Items;
using fBaseXtensions.Items.Enums;
using fItemPlugin.ItemRules;
using fItemPlugin.Townrun;
using Zeta.Bot;
using Zeta.Bot.Logic;
using Zeta.Common;
using Zeta.Common.Plugins;
using Zeta.Game;
using Zeta.Game.Internals.Actors;
using Zeta.TreeSharp;
using Action = Zeta.TreeSharp.Action;
using Logger = fBaseXtensions.Helpers.Logger;

namespace fItemPlugin
{
	public partial class FunkyTownRunPlugin : IPlugin
	{
		public string Name { get { return "fItemPlugin"; } }
		public string Author { get { return "HerbFunk"; } }
		public string Description
		{
			get { return "An Item Plugin - with vendor behavior replacement!"; }
		}
		formSettings settingsWindow;
		public Window DisplayWindow
		{
			get
			{
				Settings.LoadSettings();

				settingsWindow = new formSettings();

				Window fakeWindow = new Window
				{
					Width = 0,
					Height = 0,
					WindowStartupLocation = WindowStartupLocation.Manual,
				};
				fakeWindow.Initialized += (sender, args) =>
				{
					settingsWindow.ShowDialog();
				};
				fakeWindow.Loaded += (sender, args) =>
				{
					fakeWindow.Close();
				};

				return fakeWindow;
			}


		}

		public void OnDisabled()
		{
			BotMain.OnStart -= FunkyBotStart;
			BotMain.OnStop -= FunkyBotStop;
		}

		public void OnEnabled()
		{
			//var basePlugin = PluginManager.Plugins.First(p => p.Plugin.Name == "fBaseXtensions");
			//if (basePlugin != null)
			//{
			//	if (!basePlugin.Enabled)
			//	{
			//		DBLog.Warn("FunkyTownRun requires fBaseXtensions to be enabled! -- Enabling it automatically.");
			//		basePlugin.Enabled = true;
			//	}
			//}

			BotMain.OnStart += FunkyBotStart;
			BotMain.OnStop += FunkyBotStop;
			if (BotMain.IsRunning) FunkyBotStart(null);
		}

		public void OnInitialize()
		{
			ItemSnoCache.LoadItemIds();
		}

		public void OnPulse()
		{
			if (initTreeHooks && !trinityCheck)
			{
				trinityCheck = true;

				if (RunningTrinity)
				{
					var vendorDecorator = TreeHooks.Instance.Hooks["VendorRun"][0] as Decorator;
					var replacementvendorDecorator = new Decorator(VendorCanRunDelegate, VendorPrioritySelector);
					TreeHooks.Instance.ReplaceHook("VendorRun", replacementvendorDecorator);
					DBLog.DebugFormat("Replaced Trinity Townrun Replacement!?");
				}


				//if (vendorDecorator.DecoratedChild
			}
		}

		public void OnShutdown()
		{

		}

		public bool Equals(IPlugin other) { return (other.Name == Name) && (other.Version == Version); }


		public static Settings PluginSettings = new Settings();
		public static Interpreter ItemRulesEval;

		internal static readonly log4net.ILog DBLog = Zeta.Common.Logger.GetLoggerInstanceForType();

		internal static void LogGoodItems(CacheACDItem thisgooditem, PluginBaseItemTypes thisPluginBaseItemTypes, PluginItemTypes thisPluginItemType)
		{

			try
			{
				//Update this item
				using (ZetaDia.Memory.AcquireFrame())
				{
					thisgooditem = new CacheACDItem(thisgooditem.ACDItem);
				}
			}
			catch
			{
				DBLog.DebugFormat("Failure to update CacheACDItem during Logging");
			}
			//double iThisItemValue = ItemFunc.ValueThisItem(thisgooditem, thisPluginItemType);

			FileStream LogStream = null;
			try
			{
				string outputPath = FolderPaths.LoggingFolderPath + @"\StashLog.log";

				LogStream = File.Open(outputPath, FileMode.Append, FileAccess.Write, FileShare.Read);
				using (StreamWriter LogWriter = new StreamWriter(LogStream))
				{
					if (!TownRunManager.bLoggedAnythingThisStash)
					{
						TownRunManager.bLoggedAnythingThisStash = true;
						LogWriter.WriteLine(DateTime.Now.ToString() + ":");
						LogWriter.WriteLine("====================");
					}
					string sLegendaryString = "";
					if (thisgooditem.ThisQuality >= ItemQuality.Legendary)
					{
						if (!thisgooditem.IsUnidentified)
						{
							//Prowl.AddNotificationToQueue(thisgooditem.ThisRealName + " [" + thisPluginItemType.ToString() + "] (Score=" + iThisItemValue.ToString() + ". " + TownRunManager.sValueItemStatString + ")", ZetaDia.Service.Hero.Name + " new legendary!", Prowl.ProwlNotificationPriority.Emergency);
							sLegendaryString = " {legendary item}";
							// Change made by bombastic
							DBLog.Info("+=+=+=+=+=+=+=+=+ LEGENDARY FOUND +=+=+=+=+=+=+=+=+");
							DBLog.Info("+  Name:       " + thisgooditem.ThisRealName + " (" + thisPluginItemType.ToString() + ")");
							//DBLog.Info("+  Score:       " + Math.Round(iThisItemValue).ToString());
							DBLog.Info("+  Attributes: " + thisgooditem.ItemStatString);
							DBLog.Info("+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+");
						}
						else
						{
							DBLog.Info("+=+=+=+=+=+=+=+=+ LEGENDARY FOUND +=+=+=+=+=+=+=+=+");
							DBLog.Info("+  Unid:       " + thisPluginItemType.ToString());
							DBLog.Info("+  Level:       " + thisgooditem.ThisLevel.ToString());
							DBLog.Info("+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+");
						}


					}
					else
					{
						// Check for non-legendary notifications
						bool bShouldNotify = false;
						switch (thisPluginBaseItemTypes)
						{
							case PluginBaseItemTypes.WeaponOneHand:
							case PluginBaseItemTypes.WeaponRange:
							case PluginBaseItemTypes.WeaponTwoHand:
								//if (ithisitemvalue >= settings.iNeedPointsToNotifyWeapon)
								//  bShouldNotify = true;
								break;
							case PluginBaseItemTypes.Armor:
							case PluginBaseItemTypes.Offhand:
								//if (ithisitemvalue >= settings.iNeedPointsToNotifyArmor)
								//bShouldNotify = true;
								break;
							case PluginBaseItemTypes.Jewelry:
								//if (ithisitemvalue >= settings.iNeedPointsToNotifyJewelry)
								//bShouldNotify = true;
								break;
						}
						//if (bShouldNotify)
						//Prowl.AddNotificationToQueue(thisgooditem.ThisRealName + " [" + thisPluginItemType.ToString() + "] (Score=" + iThisItemValue.ToString() + ". " + TownRunManager.sValueItemStatString + ")", ZetaDia.Service.Hero.Name + " new item!", Prowl.ProwlNotificationPriority.Emergency);
					}
					if (!thisgooditem.IsUnidentified)
					{

						LogWriter.WriteLine(thisgooditem.ThisQuality.ToString() + "  " + thisPluginItemType.ToString() + " '" + thisgooditem.ThisRealName + sLegendaryString);
						LogWriter.WriteLine("  " + thisgooditem.ItemStatString);
						LogWriter.WriteLine("");
					}
					else
					{
						LogWriter.WriteLine(thisgooditem.ThisQuality.ToString() + "  " + thisPluginItemType.ToString() + " '" + sLegendaryString);
						LogWriter.WriteLine("iLevel " + thisgooditem.ThisLevel.ToString());
						LogWriter.WriteLine("");
					}
				}

			}
			catch (IOException)
			{
				DBLog.Info("Fatal Error: File access error for stash log file.");
			}
		}

		internal static void LogJunkItems(CacheACDItem thisgooditem, PluginBaseItemTypes thisPluginBaseItemTypes, PluginItemTypes thisPluginItemType)
		{
			FileStream LogStream = null;
			string outputPath = FolderPaths.LoggingFolderPath + @"\JunkLog.log";

			try
			{
				LogStream = File.Open(outputPath, FileMode.Append, FileAccess.Write, FileShare.Read);
				using (StreamWriter LogWriter = new StreamWriter(LogStream))
				{
					if (!TownRunManager.bLoggedJunkThisStash)
					{
						TownRunManager.bLoggedJunkThisStash = true;
						LogWriter.WriteLine(DateTime.Now.ToString() + ":");
						LogWriter.WriteLine("====================");
					}
					string sLegendaryString = "";
					if (thisgooditem.ThisQuality >= ItemQuality.Legendary)
						sLegendaryString = " {legendary item}";
					LogWriter.WriteLine(thisgooditem.ThisQuality.ToString() + " " + thisPluginItemType.ToString() + " '" + thisgooditem.ThisRealName + sLegendaryString);
					LogWriter.Write(thisgooditem.ItemStatProperties.ReturnPrimaryStatString());
					LogWriter.WriteLine("");
				}

			}
			catch (IOException)
			{
				DBLog.Info("Fatal Error: File access error for junk log file.");
			}
		}

		internal static void LogTownRunStats()
		{
			FileStream LogStream = null;
			string outputPath = FolderPaths.LoggingFolderPath + @"\" + FunkyGame.CurrentHeroName + " -- TownRunStats.log";
			try
			{
				LogStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
				using (StreamWriter LogWriter = new StreamWriter(LogStream))
				{
					LogWriter.Write(TownRunStats.GenerateOutputString());
				}
			}
			catch (IOException)
			{
				DBLog.Info("Fatal Error: File access error for town run stats log file.");
			}
		}

		internal static Stats TownRunStats = new Stats();

		internal static bool initTreeHooks;
		internal static bool trinityCheck;
		internal static PrioritySelector VendorPrioritySelector;
		internal static CanRunDecoratorDelegate VendorCanRunDelegate;
		internal static bool RunningTrinity = false;

		internal static void HookBehaviorTree()
		{
			HookHandler.StoreHook(HookHandler.HookType.VendorRun);

			DBLog.InfoFormat("[FunkyTownRun] Treehooks..");
			#region VendorRun

			// Wipe the vendorrun and loot behavior trees, since we no longer want them

			DBLog.DebugFormat("[FunkyTownRun] VendorRun...");

			Decorator GilesDecorator = HookHandler.ReturnHookValue(HookHandler.HookType.VendorRun)[0] as Decorator;
			//PrioritySelector GilesReplacement = GilesDecorator.Children[0] as PrioritySelector;
			PrioritySelector GilesReplacement = GilesDecorator.Children[0] as PrioritySelector;
			//HookHandler.PrintChildrenTypes(GilesReplacement.Children);

			CanRunDecoratorDelegate canRunDelegateReturnToTown = TownPortalBehavior.FunkyTPOverlord;
			ActionDelegate actionDelegateReturnTown = TownPortalBehavior.FunkyTPBehavior;
			ActionDelegate actionDelegateTownPortalFinish = TownPortalBehavior.FunkyTownPortalTownRun;
			Sequence sequenceReturnTown = new Sequence(
				new Zeta.TreeSharp.Action(actionDelegateReturnTown),
				new Zeta.TreeSharp.Action(actionDelegateTownPortalFinish)
				);
			GilesReplacement.Children[1] = new Decorator(canRunDelegateReturnToTown, sequenceReturnTown);
			Logger.DBLog.DebugFormat("[FunkyTownRun] Town Portal - hooked...");


			ActionDelegate actionDelegatePrePause = TownRunManager.GilesStashPrePause;
			ActionDelegate actionDelegatePause = TownRunManager.GilesStashPause;

			#region Idenify



			CanRunDecoratorDelegate canRunDelegateFunkyIDManual = TownRunManager.IdenifyItemManualOverlord;
			ActionDelegate actionDelegateIDManual = TownRunManager.IdenifyItemManualBehavior;
			ActionDelegate actionDelegateIDFinish = TownRunManager.IdenifyItemManualFinishBehavior;
			Sequence sequenceIDManual = new Sequence(
				new Action(actionDelegateIDManual),
				new Action(actionDelegateIDFinish)
			);

			CanRunDecoratorDelegate canRunDelegateFunkyIDBookOfCain = TownRunManager.IdenifyItemBookOfCainOverlord;
			ActionDelegate actionDelegateIDBookOfCainMovement = TownRunManager.IdenifyItemBookOfCainMovementBehavior;
			ActionDelegate actionDelegateIDBookOfCainInteraction = TownRunManager.IdenifyItemBookOfCainInteractionBehavior;
			Sequence sequenceIDBookOfCain = new Sequence(
				new Action(actionDelegateIDBookOfCainMovement),
				new Action(actionDelegateIDBookOfCainInteraction),
				new Action(actionDelegateIDFinish)
			);


			PrioritySelector priorityIDItems = new PrioritySelector(
				new Decorator(canRunDelegateFunkyIDManual, sequenceIDManual),
				new Decorator(canRunDelegateFunkyIDBookOfCain, sequenceIDBookOfCain)
			);

			CanRunDecoratorDelegate canRunDelegateFunkyIDOverlord = TownRunManager.IdenifyItemOverlord;
			GilesReplacement.Children[2] = new Decorator(canRunDelegateFunkyIDOverlord, priorityIDItems);

			DBLog.DebugFormat("[FunkyTownRun] Idenify Items - hooked...");



			#endregion

			// Replace the pause just after identify stuff to ensure we wait before trying to run to vendor etc.
			CanRunDecoratorDelegate canRunDelegateEvaluateAction = TownRunManager.ActionsEvaluatedOverlord;
			ActionDelegate actionDelegateEvaluateAction = TownRunManager.ActionsEvaluatedBehavior;

			Sequence sequenceEvaluate = new Sequence(
						new Action(actionDelegatePrePause),
						new Action(actionDelegatePause),
						new Action(actionDelegateEvaluateAction)
					);

			GilesReplacement.Children[3] = new Decorator(canRunDelegateEvaluateAction, sequenceEvaluate);



			#region Salvage

			// Replace DB salvaging behavior tree with my optimized & "one-at-a-time" version
			CanRunDecoratorDelegate canRunDelegateSalvageGilesOverlord = TownRunManager.GilesSalvageOverlord;
			ActionDelegate actionDelegatePreSalvage = TownRunManager.GilesOptimisedPreSalvage;
			ActionDelegate actionDelegateSalvage = TownRunManager.GilesOptimisedSalvage;
			ActionDelegate actionDelegatePostSalvage = TownRunManager.GilesOptimisedPostSalvage;
			Sequence sequenceSalvage = new Sequence(
					new Action(actionDelegatePreSalvage),
					new Action(actionDelegateSalvage),
					new Action(actionDelegatePostSalvage),
					new Sequence(
					new Action(actionDelegatePrePause),
					new Action(actionDelegatePause)
					)
					);
			GilesReplacement.Children[4] = new Decorator(canRunDelegateSalvageGilesOverlord, sequenceSalvage);
			DBLog.DebugFormat("[FunkyTownRun] Salvage - hooked...");

			#endregion

			#region Stash

			// Replace DB stashing behavior tree with my optimized version with loot rule replacement
			CanRunDecoratorDelegate canRunDelegateStashGilesOverlord = TownRunManager.StashOverlord;
			ActionDelegate actionDelegatePreStash = TownRunManager.PreStash;
			ActionDelegate actionDelegatePostStash = TownRunManager.PostStash;

			ActionDelegate actionDelegateStashMovement = TownRunManager.StashMovement;
			ActionDelegate actionDelegateStashUpdate = TownRunManager.StashUpdate;
			ActionDelegate actionDelegateStashItems = TownRunManager.StashItems;

			Sequence sequencestash = new Sequence(
					new Action(actionDelegatePreStash),
					new Action(actionDelegateStashMovement),
					new Action(actionDelegateStashUpdate),
					new Action(actionDelegateStashItems),
					new Action(actionDelegatePostStash),
					new Sequence(
					new Action(actionDelegatePrePause),
					new Action(actionDelegatePause)
					)
					);
			GilesReplacement.Children[5] = new Decorator(canRunDelegateStashGilesOverlord, sequencestash);
			DBLog.DebugFormat("[FunkyTownRun] Stash - hooked...");

			#endregion

			#region Vendor

			// Replace DB vendoring behavior tree with my optimized & "one-at-a-time" version
			CanRunDecoratorDelegate canRunDelegateSellGilesOverlord = TownRunManager.GilesSellOverlord;
			ActionDelegate actionDelegatePreSell = TownRunManager.GilesOptimisedPreSell;
			ActionDelegate actionDelegateMovement = TownRunManager.VendorMovement;
			ActionDelegate actionDelegateSell = TownRunManager.GilesOptimisedSell;
			ActionDelegate actionDelegatePostSell = TownRunManager.GilesOptimisedPostSell;
			Sequence sequenceSell = new Sequence(
					new Action(actionDelegatePreSell),
					new Action(actionDelegateMovement),
					new Action(actionDelegateSell),
					new Action(actionDelegatePostSell),
					new Sequence(
					new Action(actionDelegatePrePause),
					new Action(actionDelegatePause)
					)
					);
			GilesReplacement.Children[6] = new Decorator(canRunDelegateSellGilesOverlord, sequenceSell);
			DBLog.DebugFormat("[FunkyTownRun] Vendor - hooked...");


			#endregion

			#region Interaction Behavior
			CanRunDecoratorDelegate canRunDelegateInteraction = TownRunManager.InteractionOverlord;
			ActionDelegate actionDelegateInteractionMovementhBehavior = TownRunManager.InteractionMovement;
			ActionDelegate actionDelegateInteractionClickBehaviorBehavior = TownRunManager.InteractionClickBehavior;
			ActionDelegate actionDelegateInteractionLootingBehaviorBehavior = TownRunManager.InteractionLootingBehavior;
			ActionDelegate actionDelegateInteractionFinishBehaviorBehavior = TownRunManager.InteractionFinishBehavior;

			Sequence sequenceInteraction = new Sequence(
					new Zeta.TreeSharp.Action(actionDelegateInteractionFinishBehaviorBehavior),
					new Zeta.TreeSharp.Action(actionDelegateInteractionMovementhBehavior),
					new Zeta.TreeSharp.Action(actionDelegateInteractionClickBehaviorBehavior),
					new Zeta.TreeSharp.Action(actionDelegateInteractionLootingBehaviorBehavior),
					new Zeta.TreeSharp.Action(actionDelegateInteractionFinishBehaviorBehavior)
				);
			GilesReplacement.InsertChild(7, new Decorator(canRunDelegateInteraction, sequenceInteraction));
			Logger.DBLog.DebugFormat("[FunkyTownRun] Interaction Behavior - Inserted...");

			#endregion

			#region Gambling Behavior

			CanRunDecoratorDelegate canRunDelegateGambling = TownRunManager.GamblingRunOverlord;
			ActionDelegate actionDelegateGamblingMovementBehavior = TownRunManager.GamblingMovement;
			ActionDelegate actionDelegateGamblingInteractionBehavior = TownRunManager.GamblingInteraction;
			ActionDelegate actionDelegateGamblingStartBehavior = TownRunManager.GamblingStart;
			ActionDelegate actionDelegateGamblingFinishBehavior = TownRunManager.GamblingFinish;

			Sequence sequenceGambling = new Sequence(
					new Action(actionDelegateGamblingStartBehavior),
					new Action(actionDelegateGamblingMovementBehavior),
					new Action(actionDelegateGamblingInteractionBehavior),
					new Action(actionDelegateGamblingFinishBehavior)
				);
			GilesReplacement.InsertChild(8, new Decorator(canRunDelegateGambling, sequenceGambling));
			DBLog.DebugFormat("[FunkyTownRun] Gambling Behavior - Inserted...");

			#endregion

			#region Finish Behavior

			int childrenCount = GilesReplacement.Children.Count;
			ActionDelegate actionFinishReset = TownRunManager.ActionsEvaluatedEndingBehavior;
			ActionDelegate actionDelegateFinishBehavior = TownRunManager.FinishBehavior;
			var finish = GilesReplacement.Children[childrenCount - 1];

			Sequence FinishSequence = new Sequence
			(
				finish,
				new Zeta.TreeSharp.Action(actionFinishReset),
				new Zeta.TreeSharp.Action(actionDelegateFinishBehavior)
			);
			GilesReplacement.Children[childrenCount - 1] = FinishSequence;
			DBLog.DebugFormat("[FunkyTownRun] Created Sequence Finish Behavior.");

			#endregion


			CanRunDecoratorDelegate canRunDelegateGilesTownRunCheck = TownRunManager.TownRunCheckOverlord;
			VendorCanRunDelegate = canRunDelegateGilesTownRunCheck;
			VendorPrioritySelector = GilesReplacement;
			var VendorComposite = new Decorator(canRunDelegateGilesTownRunCheck, GilesReplacement);
			HookHandler.SetHookValue(HookHandler.HookType.VendorRun, 0, VendorComposite);
			DBLog.DebugFormat("[FunkyTownRun] Vendor Run tree hooking finished.");
			#endregion


			initTreeHooks = true;
		}



		private void FunkyBotStart(IBot bot)
		{
			DBLog.Info("===================");
			DBLog.Info("Funky Town Run Plugin");
			DBLog.InfoFormat("Version {0}", Version.ToString());
			DBLog.Info("===================");

			if (!initTreeHooks)
				HookBehaviorTree();

			RunningTrinity = RoutineManager.Current.Name == "Trinity";

			Settings.LoadSettings();

			if (PluginSettings.UseItemRules)
				ItemRulesEval = new Interpreter();
		}
		private void FunkyBotStop(IBot bot)
		{
			RunningTrinity = false;
			initTreeHooks = false;
			trinityCheck = false;
			HookHandler.RestoreHook(HookHandler.HookType.VendorRun);
		}
	}
}
