﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Rawr
{
    public delegate void OptimizeCharacterProgressChangedEventHandler(object sender, OptimizeCharacterProgressChangedEventArgs e);

    public class OptimizeCharacterProgressChangedEventArgs : ProgressChangedEventArgs
    {
        private float bestValue;

        public OptimizeCharacterProgressChangedEventArgs(int progressPercentage, float bestValue)
            : base(progressPercentage, null)
        {
            this.bestValue = bestValue;
        }

        public float BestValue
        {
            get
            {
                return bestValue;
            }
        }
    }

    public delegate void ComputeUpgradesProgressChangedEventHandler(object sender, ComputeUpgradesProgressChangedEventArgs e);

    public class ComputeUpgradesProgressChangedEventArgs : ProgressChangedEventArgs
    {
        private int itemProgressPercentage;
        private string currentItem;

        public ComputeUpgradesProgressChangedEventArgs(int progressPercentage, int itemProgressPercentage, string currentItem)
            : base(progressPercentage, null)
        {
            this.itemProgressPercentage = itemProgressPercentage;
            this.currentItem = currentItem;
        }

        public int ItemProgressPercentage
        {
            get
            {
                return itemProgressPercentage;
            }
        }

        public string CurrentItem
        {
            get
            {
                return currentItem;
            }
        }
    }

    public delegate void OptimizeCharacterCompletedEventHandler(object sender, OptimizeCharacterCompletedEventArgs e);

    public class OptimizeCharacterCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Character optimizedCharacter;
        private float optimizedCharacterValue;
        private Character currentCharacter;
        private float currentCharacterValue;
        private bool injected;

        public OptimizeCharacterCompletedEventArgs(Character optimizedCharacter, float optimizedCharacterValue, Character currentCharacter, float currentCharacterValue, bool injected, Exception error, bool cancelled)
            : base(error, cancelled, null)
        {
            this.optimizedCharacter = optimizedCharacter;
            this.optimizedCharacterValue = optimizedCharacterValue;
            this.currentCharacter = currentCharacter;
            this.currentCharacterValue = currentCharacterValue;
            this.injected = injected;
        }

        public Character OptimizedCharacter
        {
            get
            {
                RaiseExceptionIfNecessary();
                return optimizedCharacter;
            }
        }

        public Character CurrentCharacter
        {
            get
            {
                RaiseExceptionIfNecessary();
                return currentCharacter;
            }
        }

        public float OptimizedCharacterValue
        {
            get
            {
                RaiseExceptionIfNecessary();
                return optimizedCharacterValue;
            }
        }

        public float CurrentCharacterValue
        {
            get
            {
                RaiseExceptionIfNecessary();
                return currentCharacterValue;
            }
        }

        public bool CurrentCharacterInjected
        {
            get
            {
                RaiseExceptionIfNecessary();
                return injected;
            }
        }
    }

    public delegate void ComputeUpgradesCompletedEventHandler(object sender, ComputeUpgradesCompletedEventArgs e);

    public class ComputeUpgradesCompletedEventArgs : AsyncCompletedEventArgs
    {
        private Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> upgrades;

        public ComputeUpgradesCompletedEventArgs(Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> upgrades, Exception error, bool cancelled)
            : base(error, cancelled, null)
        {
            this.upgrades = upgrades;
        }

        public Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> Upgrades
        {
            get
            {
                RaiseExceptionIfNecessary();
                return upgrades;
            }
        }
    }

    public delegate void EvaluateUpgradeCompletedEventHandler(object sender, EvaluateUpgradeCompletedEventArgs e);

    public class EvaluateUpgradeCompletedEventArgs : AsyncCompletedEventArgs
    {
        private float upgradeValue;

        public EvaluateUpgradeCompletedEventArgs(float upgradeValue, Exception error, bool cancelled)
            : base(error, cancelled, null)
        {
            this.upgradeValue = upgradeValue;
        }

        public float UpgradeValue
        {
            get
            {
                RaiseExceptionIfNecessary();
                return upgradeValue;
            }
        }
    }

    [Serializable()]
    public class OptimizationRequirement
    {
        public string Calculation { get; set; }
        public bool LessThan { get; set; }
        public float Value { get; set; }
    }

    public enum OptimizationMethod
    {
        GeneticAlgorithm,
        SimulatedAnnealing
    }

    public class Optimizer
    {
        private Character _character;
        private string _calculationToOptimize;
        private OptimizationRequirement[] _requirements;
        private int _thoroughness;
        private CalculationsBase model;

        private ItemCacheInstance mainItemCache;
        private ItemCacheInstance optimizerItemCache;

        public static OptimizationMethod OptimizationMethod { get; set; }

        public Optimizer()
        {
            optimizeCharacterProgressChangedDelegate = new SendOrPostCallback(PrivateOptimizeCharacterProgressChanged);
            optimizeCharacterCompletedDelegate = new SendOrPostCallback(PrivateOptimizeCharacterCompleted);
            optimizeCharacterThreadStartDelegate = new OptimizeCharacterThreadStartDelegate(OptimizeCharacterThreadStart);
            computeUpgradesProgressChangedDelegate = new SendOrPostCallback(PrivateComputeUpgradesProgressChanged);
            computeUpgradesCompletedDelegate = new SendOrPostCallback(PrivateComputeUpgradesCompleted);
            computeUpgradesThreadStartDelegate = new ComputeUpgradesThreadStartDelegate(ComputeUpgradesThreadStart);
            evaluateUpgradeProgressChangedDelegate = new SendOrPostCallback(PrivateEvaluateUpgradeProgressChanged);
            evaluateUpgradeCompletedDelegate = new SendOrPostCallback(PrivateEvaluateUpgradeCompleted);
            evaluateUpgradeThreadStartDelegate = new EvaluateUpgradeThreadStartDelegate(EvaluateUpgradeThreadStart);
        }

        public void InitializeItemCache(List<string> availableItems, bool overrideRegem, bool overrideReenchant, CalculationsBase model)
        {
            mainItemCache = ItemCache.Instance;
            optimizerItemCache = new ItemCacheInstance(mainItemCache);
            this.model = model;

            try
            {
                ItemCache.Instance = optimizerItemCache;
                PopulateAvailableIds(availableItems, overrideRegem, overrideReenchant);
            }
            finally
            {
                ItemCache.Instance = mainItemCache;
            }
        }

        private enum OptimizationOperation
        {
            OptimizeCharacter,
            ComputeUpgrades,
            EvaluateUpgrade
        }

        private OptimizationOperation currentOperation;

        #region Asynchronous Pattern Implementation
        private void PrivateOptimizeCharacterProgressChanged(object state)
        {
            OnOptimizeCharacterProgressChanged(state as OptimizeCharacterProgressChangedEventArgs);
        }

        protected void OnOptimizeCharacterProgressChanged(OptimizeCharacterProgressChangedEventArgs e)
        {
            if (OptimizeCharacterProgressChanged != null)
            {
                OptimizeCharacterProgressChanged(this, e);
            }
        }

        private void PrivateOptimizeCharacterCompleted(object state)
        {
            isBusy = false;
            cancellationPending = false;
            OnOptimizeCharacterCompleted(state as OptimizeCharacterCompletedEventArgs);
        }

        protected void OnOptimizeCharacterCompleted(OptimizeCharacterCompletedEventArgs e)
        {
            if (OptimizeCharacterCompleted != null)
            {
                OptimizeCharacterCompleted(this, e);
            }
        }

        private void PrivateComputeUpgradesProgressChanged(object state)
        {
            OnComputeUpgradesProgressChanged(state as ComputeUpgradesProgressChangedEventArgs);
        }

        protected void OnComputeUpgradesProgressChanged(ComputeUpgradesProgressChangedEventArgs e)
        {
            if (ComputeUpgradesProgressChanged != null)
            {
                ComputeUpgradesProgressChanged(this, e);
            }
        }

        private void PrivateComputeUpgradesCompleted(object state)
        {
            isBusy = false;
            cancellationPending = false;
            OnComputeUpgradesCompleted(state as ComputeUpgradesCompletedEventArgs);
        }

        protected void OnComputeUpgradesCompleted(ComputeUpgradesCompletedEventArgs e)
        {
            if (ComputeUpgradesCompleted != null)
            {
                ComputeUpgradesCompleted(this, e);
            }
        }

        private void PrivateEvaluateUpgradeProgressChanged(object state)
        {
            OnEvaluateUpgradeProgressChanged(state as ProgressChangedEventArgs);
        }

        protected void OnEvaluateUpgradeProgressChanged(ProgressChangedEventArgs e)
        {
            if (EvaluateUpgradeProgressChanged != null)
            {
                EvaluateUpgradeProgressChanged(this, e);
            }
        }

        private void PrivateEvaluateUpgradeCompleted(object state)
        {
            isBusy = false;
            cancellationPending = false;
            OnEvaluateUpgradeCompleted(state as EvaluateUpgradeCompletedEventArgs);
        }

        protected void OnEvaluateUpgradeCompleted(EvaluateUpgradeCompletedEventArgs e)
        {
            if (EvaluateUpgradeCompleted != null)
            {
                EvaluateUpgradeCompleted(this, e);
            }
        }

        private bool isBusy;

        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
        }

        public void CancelAsync()
        {
            cancellationPending = true;
        }

        private bool cancellationPending;
        private AsyncOperation asyncOperation;
        private delegate void OptimizeCharacterThreadStartDelegate(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, bool injectCharacter);
        private delegate void ComputeUpgradesThreadStartDelegate(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness);
        private delegate void EvaluateUpgradeThreadStartDelegate(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, Item upgrade, Enchant upgradeEnchant);

        public event OptimizeCharacterCompletedEventHandler OptimizeCharacterCompleted;
        public event OptimizeCharacterProgressChangedEventHandler OptimizeCharacterProgressChanged;
        public event ComputeUpgradesProgressChangedEventHandler ComputeUpgradesProgressChanged;
        public event ComputeUpgradesCompletedEventHandler ComputeUpgradesCompleted;
        public event ProgressChangedEventHandler EvaluateUpgradeProgressChanged;
        public event EvaluateUpgradeCompletedEventHandler EvaluateUpgradeCompleted;

        private SendOrPostCallback optimizeCharacterProgressChangedDelegate;
        private SendOrPostCallback optimizeCharacterCompletedDelegate;
        private OptimizeCharacterThreadStartDelegate optimizeCharacterThreadStartDelegate;
        private SendOrPostCallback computeUpgradesProgressChangedDelegate;
        private SendOrPostCallback computeUpgradesCompletedDelegate;
        private ComputeUpgradesThreadStartDelegate computeUpgradesThreadStartDelegate;
        private SendOrPostCallback evaluateUpgradeProgressChangedDelegate;
        private SendOrPostCallback evaluateUpgradeCompletedDelegate;
        private EvaluateUpgradeThreadStartDelegate evaluateUpgradeThreadStartDelegate;

        public void OptimizeCharacterAsync(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, bool injectCharacter)
        {
            if (isBusy) throw new InvalidOperationException("Optimizer is working on another operation.");
            isBusy = true;
            cancellationPending = false;
            asyncOperation = AsyncOperationManager.CreateOperation(null);
            optimizeCharacterThreadStartDelegate.BeginInvoke(character, calculationToOptimize, requirements, thoroughness, injectCharacter, null, null);
        }

        private void OptimizeCharacterThreadStart(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, bool injectCharacter)
        {
            Exception error = null;
            Character optimizedCharacter = null;
            float optimizedCharacterValue = 0.0f;
            float currentCharacterValue = 0.0f;
            bool injected = false;
            try
            {
                optimizedCharacter = PrivateOptimizeCharacter(character, calculationToOptimize, requirements, thoroughness, injectCharacter, out injected, out error);
                optimizedCharacterValue = GetCalculationsValue(model.GetCharacterCalculations(optimizedCharacter));
                currentCharacterValue = GetCalculationsValue(model.GetCharacterCalculations(character));
            }
            catch (Exception ex)
            {
                error = ex;
            }
            asyncOperation.PostOperationCompleted(optimizeCharacterCompletedDelegate, new OptimizeCharacterCompletedEventArgs(optimizedCharacter, optimizedCharacterValue, character, currentCharacterValue, injected, error, cancellationPending));
        }

        public void ComputeUpgradesAsync(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness)
        {
            if (isBusy) throw new InvalidOperationException("Optimizer is working on another operation.");
            isBusy = true;
            cancellationPending = false;
            asyncOperation = AsyncOperationManager.CreateOperation(null);
            computeUpgradesThreadStartDelegate.BeginInvoke(character, calculationToOptimize, requirements, thoroughness, null, null);
        }

        private void ComputeUpgradesThreadStart(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness)
        {
            Exception error = null;
            Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> upgrades = null;
            try
            {
                upgrades = PrivateComputeUpgrades(character, calculationToOptimize, requirements, thoroughness, out error);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            asyncOperation.PostOperationCompleted(computeUpgradesCompletedDelegate, new ComputeUpgradesCompletedEventArgs(upgrades, error, cancellationPending));
        }

        public void EvaluateUpgradeAsync(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, Item upgrade, Enchant upgradeEnchant)
        {
            if (isBusy) throw new InvalidOperationException("Optimizer is working on another operation.");
            isBusy = true;
            cancellationPending = false;
            asyncOperation = AsyncOperationManager.CreateOperation(null);
            evaluateUpgradeThreadStartDelegate.BeginInvoke(character, calculationToOptimize, requirements, thoroughness, upgrade, upgradeEnchant, null, null);
        }

        private void EvaluateUpgradeThreadStart(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, Item upgrade, Enchant upgradeEnchant)
        {
            Exception error = null;
            float upgradeValue = 0f;
            try
            {
                upgradeValue = PrivateEvaluateUpgrade(character, calculationToOptimize, requirements, thoroughness, upgrade, upgradeEnchant, out error);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            asyncOperation.PostOperationCompleted(evaluateUpgradeCompletedDelegate, new EvaluateUpgradeCompletedEventArgs(upgradeValue, error, cancellationPending));
        }
        #endregion

        private void ReportProgress(int progressPercentage, float bestValue)
        {
            if (!cancellationPending && asyncOperation != null)
            {
                switch (currentOperation)
                {
                    case OptimizationOperation.OptimizeCharacter:
                        asyncOperation.Post(optimizeCharacterProgressChangedDelegate, new OptimizeCharacterProgressChangedEventArgs(progressPercentage, bestValue));
                        break;
                    case OptimizationOperation.ComputeUpgrades:
                        asyncOperation.Post(computeUpgradesProgressChangedDelegate, new ComputeUpgradesProgressChangedEventArgs(itemProgressPercentage, progressPercentage, currentItem));
                        break;
                    case OptimizationOperation.EvaluateUpgrade:
                        asyncOperation.Post(evaluateUpgradeProgressChangedDelegate, new ProgressChangedEventArgs(progressPercentage, null));
                        break;
                }
            }
        }

        public Character OptimizeCharacter(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, bool injectCharacter)
        {
            if (isBusy) throw new InvalidOperationException("Optimizer is working on another operation.");
            isBusy = true;
            cancellationPending = false;
            asyncOperation = null;
            Exception error;
            bool injected;
            Character optimizedCharacter = PrivateOptimizeCharacter(character, calculationToOptimize, requirements, thoroughness, injectCharacter, out injected, out error);
            if (error != null) throw error;
            isBusy = false;
            return optimizedCharacter;
        }

        private Character PrivateOptimizeCharacter(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, bool injectCharacter, out bool injected, out Exception error)
        {
            if (!itemCacheInitialized) throw new InvalidOperationException("Optimization item cache was not initialized.");
            error = null;
            _character = character;
            model = Calculations.GetModel(_character.CurrentModel);
            _calculationToOptimize = calculationToOptimize;
            _requirements = requirements;
            _thoroughness = thoroughness;

            currentOperation = OptimizationOperation.OptimizeCharacter;
            Character optimizedCharacter = null;
            float bestValue = 0.0f;
            injected = false;
            lockedSlot = Character.CharacterSlot.None;

            try
            {
                ItemCache.Instance = optimizerItemCache;
                if (injectCharacter)
                {
                    optimizedCharacter = Optimize(character, out bestValue, out injected);
                }
                else
                {
                    optimizedCharacter = Optimize(out bestValue);
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                ItemCache.Instance = mainItemCache;
            }

            ReportProgress(100, bestValue);
            return optimizedCharacter;
        }

        public Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> ComputeUpgrades(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness)
        {
            if (isBusy) throw new InvalidOperationException("Optimizer is working on another operation.");
            isBusy = true;
            cancellationPending = false;
            asyncOperation = null;
            Exception error;
            Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> upgrades = PrivateComputeUpgrades(character, calculationToOptimize, requirements, thoroughness, out error);
            if (error != null) throw error;
            isBusy = false;
            return upgrades;
        }

        private int itemProgressPercentage = 0;
        private string currentItem = "";

        private Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> PrivateComputeUpgrades(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, out Exception error)
        {
            if (!itemCacheInitialized) throw new InvalidOperationException("Optimization item cache was not initialized.");
            error = null;
            _character = character;
            model = Calculations.GetModel(_character.CurrentModel);
            _calculationToOptimize = calculationToOptimize;
            _requirements = requirements;
            _thoroughness = thoroughness;

            currentOperation = OptimizationOperation.ComputeUpgrades;
            Character saveCharacter = _character;
            Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>> upgrades = null;
            try
            {
                ItemCache.Instance = optimizerItemCache;

                // make equipped gear/enchant valid
                for (int i = 0; i < 19; i++)
                {
                    Item item = _character[(Character.CharacterSlot)i];
                    if (item != null)
                    {
                        Enchant itemEnchant = _character.GetEnchantBySlot((Character.CharacterSlot)i);
                        Dictionary<int, bool> dict;
                        List<Enchant> list;
                        if (!itemEnchantValid.TryGetValue(item.GemmedId, out dict))
                        {
                            dict = new Dictionary<int, bool>();
                            itemEnchantValid[item.GemmedId] = dict;
                        }
                        item.EnchantValid = dict;
                        if (!itemEnchantValidList.TryGetValue(item.GemmedId, out list))
                        {
                            list = new List<Enchant>();
                            itemEnchantValidList[item.GemmedId] = list;
                        }
                        item.EnchantValidList = list;
                        if (itemEnchant != null)
                        {
                            dict[itemEnchant.Id] = true;
                            if (!list.Contains(itemEnchant)) list.Add(itemEnchant);
                        }
                    }
                }

                upgrades = new Dictionary<Character.CharacterSlot, List<ComparisonCalculationBase>>();

                Item[] items = mainItemCache.GetRelevantItems(model);
                Character.CharacterSlot[] slots = new Character.CharacterSlot[] { Character.CharacterSlot.Back, Character.CharacterSlot.Chest, Character.CharacterSlot.Feet, Character.CharacterSlot.Finger1, Character.CharacterSlot.Hands, Character.CharacterSlot.Head, Character.CharacterSlot.Legs, Character.CharacterSlot.MainHand, Character.CharacterSlot.Neck, Character.CharacterSlot.OffHand, Character.CharacterSlot.Projectile, Character.CharacterSlot.ProjectileBag, Character.CharacterSlot.Ranged, Character.CharacterSlot.Shoulders, Character.CharacterSlot.Trinket1, Character.CharacterSlot.Waist, Character.CharacterSlot.Wrist };
                foreach (Character.CharacterSlot slot in slots)
                    upgrades[slot] = new List<ComparisonCalculationBase>();

                CharacterCalculationsBase baseCalculations = model.GetCharacterCalculations(_character);
                float baseValue = GetCalculationsValue(baseCalculations);
                Dictionary<int, Item> itemById = new Dictionary<int, Item>();
                foreach (Item item in items)
                {
                    itemById[item.Id] = item;
                }

                items = new List<Item>(itemById.Values).ToArray();

                for (int i = 0; i < items.Length; i++)
                {
                    Item item = items[i];
                    currentItem = item.Name;
                    itemProgressPercentage = (int)Math.Round((float)i / ((float)items.Length / 100f));
                    if (cancellationPending)
                    {
                        return null;
                    }
                    ReportProgress(0, 0);
                    foreach (Character.CharacterSlot slot in slots)
                    {
                        if (item.FitsInSlot(slot))
                        {
                            List<ComparisonCalculationBase> comparisons = upgrades[slot];
                            PopulateLockedItems(item);
                            lockedSlot = slot;
                            if (lockedSlot == Character.CharacterSlot.Finger1 && item.Unique && _character.Finger2 != null && _character.Finger2.Id == item.Id)
                            {
                                lockedSlot = Character.CharacterSlot.Finger2;
                            }
                            if (lockedSlot == Character.CharacterSlot.Trinket1 && item.Unique && _character.Trinket2 != null && _character.Trinket2.Id == item.Id)
                            {
                                lockedSlot = Character.CharacterSlot.Trinket2;
                            }
                            _character = BuildSingleItemSwapCharacter(_character, lockedSlot, lockedItems[0]);
                            float best;
                            CharacterCalculationsBase bestCalculations;
                            Character bestCharacter;
                            if (_thoroughness > 1)
                            {
                                int saveThoroughness = _thoroughness;
                                _thoroughness = 1;
                                float injectValue;
                                bool injected;
                                Character inject = Optimize(null, 0, out injectValue, out bestCalculations, out injected);
                                _thoroughness = saveThoroughness;
                                bestCharacter = Optimize(inject, injectValue, out best, out bestCalculations, out injected);
                            }
                            else
                            {
                                bool injected;
                                bestCharacter = Optimize(null, 0, out best, out bestCalculations, out injected);
                            }
                            if (best > baseValue)
                            {
                                item = bestCharacter[lockedSlot];
                                ComparisonCalculationBase itemCalc = Calculations.CreateNewComparisonCalculation();
                                itemCalc.Item = item;
                                itemCalc.Enchant = bestCharacter.GetEnchantBySlot(lockedSlot);
                                itemCalc.Character = bestCharacter;
                                itemCalc.Name = item.Name;
                                itemCalc.Equipped = false;
                                //itemCalc.OverallPoints = bestCalculations.OverallPoints - baseCalculations.OverallPoints;
                                //float[] subPoints = new float[bestCalculations.SubPoints.Length];
                                //for (int j = 0; j < bestCalculations.SubPoints.Length; j++)
                                //{
                                //    subPoints[j] = bestCalculations.SubPoints[j] - baseCalculations.SubPoints[j];
                                //}
                                //itemCalc.SubPoints = subPoints;
                                itemCalc.OverallPoints = best - baseValue;

                                comparisons.Add(itemCalc);
                            }
                            _character = saveCharacter;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _character = saveCharacter;
                ItemCache.Instance = mainItemCache;
            }

            ReportProgress(100, 0f);
            return upgrades;
        }

        public float EvaluateUpgrade(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, Item upgrade, Enchant upgradeEnchant)
        {
            if (isBusy) throw new InvalidOperationException("Optimizer is working on another operation.");
            isBusy = true;
            cancellationPending = false;
            asyncOperation = null;
            Exception error;
            float upgradeValue = PrivateEvaluateUpgrade(character, calculationToOptimize, requirements, thoroughness, upgrade, upgradeEnchant, out error);
            if (error != null) throw error;
            isBusy = false;
            return upgradeValue;
        }

        private float PrivateEvaluateUpgrade(Character character, string calculationToOptimize, OptimizationRequirement[] requirements, int thoroughness, Item upgrade, Enchant upgradeEnchant, out Exception error)
        {
            if (!itemCacheInitialized) throw new InvalidOperationException("Optimization item cache was not initialized.");
            error = null;
            _character = character;
            model = Calculations.GetModel(_character.CurrentModel);
            _calculationToOptimize = calculationToOptimize;
            _requirements = requirements;
            _thoroughness = thoroughness;

            currentOperation = OptimizationOperation.EvaluateUpgrade;
            Character saveCharacter = _character;
            float upgradeValue = 0f;
            try
            {
                ItemCache.Instance = optimizerItemCache;

                Character.CharacterSlot[] slots = new Character.CharacterSlot[] { Character.CharacterSlot.Back, Character.CharacterSlot.Chest, Character.CharacterSlot.Feet, Character.CharacterSlot.Finger1, Character.CharacterSlot.Hands, Character.CharacterSlot.Head, Character.CharacterSlot.Legs, Character.CharacterSlot.MainHand, Character.CharacterSlot.Neck, Character.CharacterSlot.OffHand, Character.CharacterSlot.Projectile, Character.CharacterSlot.ProjectileBag, Character.CharacterSlot.Ranged, Character.CharacterSlot.Shoulders, Character.CharacterSlot.Trinket1, Character.CharacterSlot.Waist, Character.CharacterSlot.Wrist };
                CharacterCalculationsBase baseCalculations = model.GetCharacterCalculations(_character);
                float baseValue = GetCalculationsValue(baseCalculations);

                Item item = upgrade;
                foreach (Character.CharacterSlot slot in slots)
                {
                    if (item.FitsInSlot(slot))
                    {
                        lockedItems = new Item[] { item };
                        if (upgradeEnchant == null)
                        {
                            lockedEnchants = null;
                        }
                        else
                        {
                            lockedEnchants = new Enchant[] { upgradeEnchant };
                        }
                        lockedSlot = slot;
                        if (lockedSlot == Character.CharacterSlot.Finger1 && item.Unique && _character.Finger2 != null && _character.Finger2.Id == item.Id)
                        {
                            lockedSlot = Character.CharacterSlot.Finger2;
                        }
                        if (lockedSlot == Character.CharacterSlot.Trinket1 && item.Unique && _character.Trinket2 != null && _character.Trinket2.Id == item.Id)
                        {
                            lockedSlot = Character.CharacterSlot.Trinket2;
                        }
                        _character = BuildSingleItemEnchantSwapCharacter(_character, lockedSlot, upgrade, upgradeEnchant);
                        float best;
                        CharacterCalculationsBase bestCalculations;
                        Character bestCharacter;
                        if (_thoroughness > 1)
                        {
                            int saveThoroughness = _thoroughness;
                            _thoroughness = 1;
                            float injectValue;
                            bool injected;
                            Character inject = Optimize(null, 0, out injectValue, out bestCalculations, out injected);
                            _thoroughness = saveThoroughness;
                            bestCharacter = Optimize(inject, injectValue, out best, out bestCalculations, out injected);
                        }
                        else
                        {
                            bool injected;
                            bestCharacter = Optimize(null, 0, out best, out bestCalculations, out injected);
                        }
                        if (bestCharacter[lockedSlot] == null || bestCharacter[lockedSlot].Id != item.Id || bestCharacter.GetEnchantBySlot(lockedSlot) != upgradeEnchant) throw new Exception("There was an internal error in Optimizer when evaluating upgrade.");
                        upgradeValue = best - baseValue;
                        if (upgradeValue < 0 && (saveCharacter[lockedSlot] == null || saveCharacter[lockedSlot].Id != item.Id)) upgradeValue = 0f;
                        _character = saveCharacter;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _character = saveCharacter;
                ItemCache.Instance = mainItemCache;
            }

            ReportProgress(100, 0f);
            return upgradeValue;
        }

        private bool itemCacheInitialized;

        Item[] metaGemItems;
        Item[] gemItems;
        const int slotCount = 19;
        int[] pairSlotList = new int[] { (int)Character.CharacterSlot.Finger1, (int)Character.CharacterSlot.MainHand, (int)Character.CharacterSlot.Trinket1 };
        int[] pairSlotMap;
        //SortedList<Item, bool> uniqueItems; not needed since we store the items themselves, we can just check Unique on the item
        Item[] headItems, neckItems, shouldersItems, backItems, chestItems, wristItems, handsItems, waistItems,
                    legsItems, feetItems, fingerItems, trinketItems, mainHandItems, offHandItems, rangedItems,
                    projectileItems, projectileBagItems;
        Enchant[] backEnchants, chestEnchants, feetEnchants, fingerEnchants, handsEnchants, headEnchants,
            legsEnchants, shouldersEnchants, mainHandEnchants, offHandEnchants, rangedEnchants, wristEnchants;
        Item[][] slotItems = new Item[slotCount][];
        Enchant[][] slotEnchants = new Enchant[slotCount][];
        Dictionary<int, bool>[] slotAvailableEnchants = new Dictionary<int, bool>[slotCount];
        Item[] lockedItems;
        Enchant[] lockedEnchants;
        Dictionary<string, Dictionary<int, bool>> itemEnchantValid;
        Dictionary<string, List<Enchant>> itemEnchantValidList;
        Character.CharacterSlot lockedSlot = Character.CharacterSlot.None;
        Random rand;

        private bool ItemEnchantValid(Character.CharacterSlot slot, Item item, Enchant enchant)
        {
            bool valid;
            if (slot == lockedSlot)
            {
                if (lockedEnchants == null)
                {
                    return slotAvailableEnchants[(int)slot].TryGetValue(enchant.Id, out valid) && valid;
                }
                else
                {
                    return Array.IndexOf<Enchant>(lockedEnchants, enchant) >= 0;
                }
            }
            (item.EnchantValid ?? itemEnchantValid[item.GemmedId]).TryGetValue(enchant.Id, out valid);
            return valid;
        }

        private void PopulateLockedItems(Item item)
        {
            lockedItems = GetPossibleGemmedItemsForItem(item, item.Id.ToString(), gemItems, metaGemItems);
            //foreach (Item possibleGemmedItem in lockedItems)
            //    uniqueItems[possibleGemmedItem] = item.Unique;
            lockedEnchants = null;
        }

        private void PopulateAvailableIds(List<string> availableItems, bool overrideRegem, bool overrideReenchant)
        {
            Dictionary<int, Item> relevantItemMap = new Dictionary<int, Item>();
            foreach (Item relevantItem in mainItemCache.GetRelevantItems(model))
            {
                relevantItemMap[relevantItem.Id] = relevantItem;
            }

            List<string> itemIds = new List<string>(availableItems);
            List<string> removeIds = new List<string>();
            List<Item> metaGemItemList = new List<Item>();
            List<Item> gemItemList = new List<Item>();
            foreach (string xid in availableItems)
            {
                int dot = xid.IndexOf('.');
                int id = int.Parse((dot >= 0) ? xid.Substring(0, dot) : xid);
                if (id > 0)
                {
                    Item availableItem;
                    relevantItemMap.TryGetValue(id, out availableItem);
                    if (availableItem != null)
                    {
                        switch (availableItem.Slot)
                        {
                            case Item.ItemSlot.Meta:
                                metaGemItemList.Add(availableItem);
                                removeIds.Add(xid);
                                break;
                            case Item.ItemSlot.Red:
                            case Item.ItemSlot.Orange:
                            case Item.ItemSlot.Yellow:
                            case Item.ItemSlot.Green:
                            case Item.ItemSlot.Blue:
                            case Item.ItemSlot.Purple:
                            case Item.ItemSlot.Prismatic:
                                gemItemList.Add(availableItem);
                                removeIds.Add(xid);
                                break;
                        }
                    }
                }
            }
            if (gemItemList.Count == 0) gemItemList.Add(null);
            if (metaGemItemList.Count == 0) metaGemItemList.Add(null);
            itemIds.RemoveAll(x => x.StartsWith("-") || removeIds.Contains(x));

            metaGemItems = metaGemItemList.ToArray();
            gemItems = FilterList(gemItemList);

            List<Item> headItemList = new List<Item>();
            List<Item> neckItemList = new List<Item>();
            List<Item> shouldersItemList = new List<Item>();
            List<Item> backItemList = new List<Item>();
            List<Item> chestItemList = new List<Item>();
            List<Item> wristItemList = new List<Item>();
            List<Item> handsItemList = new List<Item>();
            List<Item> waistItemList = new List<Item>();
            List<Item> legsItemList = new List<Item>();
            List<Item> feetItemList = new List<Item>();
            List<Item> fingerItemList = new List<Item>();
            List<Item> trinketItemList = new List<Item>();
            List<Item> mainHandItemList = new List<Item>();
            List<Item> offHandItemList = new List<Item>();
            List<Item> rangedItemList = new List<Item>();
            List<Item> projectileItemList = new List<Item>();
            List<Item> projectileBagItemList = new List<Item>();

            List<Enchant> backEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Back, availableItems, model))
                backEnchantList.Add(enchant);
            backEnchants = backEnchantList.ToArray();
            List<Enchant> chestEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Chest, availableItems, model))
                chestEnchantList.Add(enchant);
            chestEnchants = chestEnchantList.ToArray();
            List<Enchant> feetEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Feet, availableItems, model))
                feetEnchantList.Add(enchant);
            feetEnchants = feetEnchantList.ToArray();
            List<Enchant> fingerEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Finger, availableItems, model))
                fingerEnchantList.Add(enchant);
            fingerEnchants = fingerEnchantList.ToArray();
            List<Enchant> handsEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Hands, availableItems, model))
                handsEnchantList.Add(enchant);
            handsEnchants = handsEnchantList.ToArray();
            List<Enchant> headEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Head, availableItems, model))
                headEnchantList.Add(enchant);
            headEnchants = headEnchantList.ToArray();
            List<Enchant> legsEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Legs, availableItems, model))
                legsEnchantList.Add(enchant);
            legsEnchants = legsEnchantList.ToArray();
            List<Enchant> shouldersEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Shoulders, availableItems, model))
                shouldersEnchantList.Add(enchant);
            shouldersEnchants = shouldersEnchantList.ToArray();
            List<Enchant> mainHandEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.MainHand, availableItems, model))
                mainHandEnchantList.Add(enchant);
            mainHandEnchants = mainHandEnchantList.ToArray();
            List<Enchant> offHandEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.OffHand, availableItems, model))
                offHandEnchantList.Add(enchant);
            offHandEnchants = offHandEnchantList.ToArray();
            List<Enchant> rangedEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Ranged, availableItems, model))
                rangedEnchantList.Add(enchant);
            rangedEnchants = rangedEnchantList.ToArray();
            List<Enchant> wristEnchantList = new List<Enchant>();
            foreach (Enchant enchant in Enchant.FindEnchants(Item.ItemSlot.Wrist, availableItems, model))
                wristEnchantList.Add(enchant);
            wristEnchants = wristEnchantList.ToArray();

            //Dictionary<Item, bool> uniqueDict = new Dictionary<Item, bool>();
            Item item = null;
            Item[] possibleGemmedItems = null;
            List<string> gemmedIds = null;
            Dictionary<string, List<string>> gemmedIdMap = new Dictionary<string, List<string>>();
            foreach (string xid in itemIds)
            {
                int dot = xid.LastIndexOf('.');
                string gemmedId = (dot >= 0) ? xid.Substring(0, dot) : xid;
                List<string> restrictions;
                if (!gemmedIdMap.TryGetValue(gemmedId, out restrictions))
                {
                    restrictions = new List<string>();
                    gemmedIdMap[gemmedId] = restrictions;
                }
                string restriction = (dot >= 0) ? xid.Substring(dot + 1) : "*";
                if (overrideReenchant) restriction = "*";
                if (!restrictions.Contains(restriction)) restrictions.Add(restriction);

                if (overrideRegem)
                {
                    int dot2 = xid.IndexOf('.');
                    gemmedId = (dot2 >= 0) ? xid.Substring(0, dot2) : xid;
                }
                if (overrideReenchant) restriction = "*";
                if (overrideRegem || overrideReenchant)
                {
                    if (!gemmedIdMap.TryGetValue(gemmedId, out restrictions))
                    {
                        restrictions = new List<string>();
                        gemmedIdMap[gemmedId] = restrictions;
                    }
                    if (!restrictions.Contains(restriction)) restrictions.Add(restriction);
                }
            }
            gemmedIds = new List<string>(gemmedIdMap.Keys);
            itemEnchantValid = new Dictionary<string, Dictionary<int, bool>>();
            itemEnchantValidList = new Dictionary<string, List<Enchant>>();
            foreach (string gid in gemmedIds)
            {
                int dot = gid.IndexOf('.');
                int itemId = int.Parse((dot >= 0) ? gid.Substring(0, dot) : gid);
                relevantItemMap.TryGetValue(itemId, out item);

                if (item != null)
                {
                    possibleGemmedItems = GetPossibleGemmedItemsForItem(item, gid, gemItems, metaGemItems);
                    if (item.FitsInSlot(Character.CharacterSlot.Head)) foreach (Item gemmedItem in possibleGemmedItems) if (headItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) headItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Neck)) foreach (Item gemmedItem in possibleGemmedItems) if (neckItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) neckItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Shoulders)) foreach (Item gemmedItem in possibleGemmedItems) if (shouldersItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) shouldersItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Back)) foreach (Item gemmedItem in possibleGemmedItems) if (backItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) backItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Chest)) foreach (Item gemmedItem in possibleGemmedItems) if (chestItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) chestItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Wrist)) foreach (Item gemmedItem in possibleGemmedItems) if (wristItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) wristItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Hands)) foreach (Item gemmedItem in possibleGemmedItems) if (handsItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) handsItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Waist)) foreach (Item gemmedItem in possibleGemmedItems) if (waistItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) waistItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Legs)) foreach (Item gemmedItem in possibleGemmedItems) if (legsItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) legsItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Feet)) foreach (Item gemmedItem in possibleGemmedItems) if (feetItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) feetItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Finger1)) foreach (Item gemmedItem in possibleGemmedItems) if (fingerItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) fingerItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Trinket1)) foreach (Item gemmedItem in possibleGemmedItems) if (trinketItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) trinketItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.MainHand)) foreach (Item gemmedItem in possibleGemmedItems) if (mainHandItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) mainHandItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.OffHand)) foreach (Item gemmedItem in possibleGemmedItems) if (offHandItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) offHandItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Ranged)) foreach (Item gemmedItem in possibleGemmedItems) if (rangedItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) rangedItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.Projectile)) foreach (Item gemmedItem in possibleGemmedItems) if (projectileItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) projectileItemList.Add(gemmedItem);
                    if (item.FitsInSlot(Character.CharacterSlot.ProjectileBag)) foreach (Item gemmedItem in possibleGemmedItems) if (projectileBagItemList.FindIndex(x => x.GemmedId == gemmedItem.GemmedId) < 0) projectileBagItemList.Add(gemmedItem);

                    List<Enchant> validEnchants = new List<Enchant>();
                    foreach (string restriction in gemmedIdMap[gid])
                    {
                        if (restriction == "*")
                        {
                            List<Enchant> merge = null;
                            switch (item.Slot)
                            {
                                case Item.ItemSlot.Back:
                                    merge = backEnchantList;
                                    break;
                                case Item.ItemSlot.Chest:
                                    merge = chestEnchantList;
                                    break;
                                case Item.ItemSlot.Feet:
                                    merge = feetEnchantList;
                                    break;
                                case Item.ItemSlot.Finger:
                                    merge = fingerEnchantList;
                                    break;
                                case Item.ItemSlot.Hands:
                                    merge = handsEnchantList;
                                    break;
                                case Item.ItemSlot.Head:
                                    merge = headEnchantList;
                                    break;
                                case Item.ItemSlot.Legs:
                                    merge = legsEnchantList;
                                    break;
                                case Item.ItemSlot.MainHand:
                                case Item.ItemSlot.OneHand:
                                case Item.ItemSlot.TwoHand:
                                    merge = mainHandEnchantList;
                                    break;
                                case Item.ItemSlot.OffHand:
                                    merge = offHandEnchantList;
                                    break;
                                case Item.ItemSlot.Ranged:
                                    merge = rangedEnchantList;
                                    break;
                                case Item.ItemSlot.Shoulders:
                                    merge = shouldersEnchantList;
                                    break;
                                case Item.ItemSlot.Wrist:
                                    merge = wristEnchantList;
                                    break;
                            }
                            if (merge != null)
                            {
                                foreach (Enchant e in merge)
                                {
                                    if (!validEnchants.Contains(e)) validEnchants.Add(e);
                                }
                            }
                        }
                        else
                        {
                            Enchant e = Enchant.FindEnchant(int.Parse(restriction), item.Slot);
                            if (e != null && !validEnchants.Contains(e)) validEnchants.Add(e);
                        }
                    }
                    List<Enchant> allEnchants = Enchant.FindEnchants(item.Slot);
                    foreach (Item possibleGemmedItem in possibleGemmedItems)
                    {
                        Dictionary<int, bool> dict;
                        List<Enchant> list;
                        if (!itemEnchantValid.TryGetValue(possibleGemmedItem.GemmedId, out dict))
                        {
                            dict = new Dictionary<int, bool>();
                            itemEnchantValid[possibleGemmedItem.GemmedId] = dict;
                        }
                        possibleGemmedItem.EnchantValid = dict;
                        if (!itemEnchantValidList.TryGetValue(possibleGemmedItem.GemmedId, out list))
                        {
                            list = new List<Enchant>();
                            itemEnchantValidList[possibleGemmedItem.GemmedId] = list;
                        }
                        possibleGemmedItem.EnchantValidList = list;
                        foreach (Enchant enchant in validEnchants)
                        {
                            dict[enchant.Id] = true;
                            if (!list.Contains(enchant)) list.Add(enchant);
                        }
                    }
                }
            }
            // store dictionaries for available enchants for locked slots
            for (int slot = 0; slot < slotCount; slot++)
            {
                Enchant[] validEnchants = null;
                Item.ItemSlot itemSlot = Item.ItemSlot.None;
                switch ((Character.CharacterSlot)slot)
                {
                    case Character.CharacterSlot.Back:
                        itemSlot = Item.ItemSlot.Back;
                        validEnchants = backEnchants;
                        break;
                    case Character.CharacterSlot.Chest:
                        itemSlot = Item.ItemSlot.Chest;
                        validEnchants = chestEnchants;
                        break;
                    case Character.CharacterSlot.Feet:
                        itemSlot = Item.ItemSlot.Feet;
                        validEnchants = feetEnchants;
                        break;
                    case Character.CharacterSlot.Finger1:
                    case Character.CharacterSlot.Finger2:
                        itemSlot = Item.ItemSlot.Finger;
                        validEnchants = fingerEnchants;
                        break;
                    case Character.CharacterSlot.Hands:
                        itemSlot = Item.ItemSlot.Hands;
                        validEnchants = handsEnchants;
                        break;
                    case Character.CharacterSlot.Head:
                        itemSlot = Item.ItemSlot.Head;
                        validEnchants = headEnchants;
                        break;
                    case Character.CharacterSlot.Legs:
                        itemSlot = Item.ItemSlot.Legs;
                        validEnchants = legsEnchants;
                        break;
                    case Character.CharacterSlot.MainHand:
                        itemSlot = Item.ItemSlot.MainHand;
                        validEnchants = mainHandEnchants;
                        break;
                    case Character.CharacterSlot.OffHand:
                        itemSlot = Item.ItemSlot.OffHand;
                        validEnchants = offHandEnchants;
                        break;
                    case Character.CharacterSlot.Ranged:
                        itemSlot = Item.ItemSlot.Ranged;
                        validEnchants = rangedEnchants;
                        break;
                    case Character.CharacterSlot.Shoulders:
                        itemSlot = Item.ItemSlot.Shoulders;
                        validEnchants = shouldersEnchants;
                        break;
                    case Character.CharacterSlot.Wrist:
                        itemSlot = Item.ItemSlot.Wrist;
                        validEnchants = wristEnchants;
                        break;
                }
                if (validEnchants != null)
                {
                    List<Enchant> allEnchants = Enchant.FindEnchants(itemSlot, model);
                    Dictionary<int, bool> dict = slotAvailableEnchants[slot] = new Dictionary<int, bool>();
                    foreach (Enchant enchant in allEnchants)
                    {
                        bool valid;
                        if (!dict.TryGetValue(enchant.Id, out valid)) dict[enchant.Id] = false;
                    }
                    foreach (Enchant enchant in validEnchants)
                    {
                        dict[enchant.Id] = true;
                    }
                }
            }

            // set to all enchants, restrictions will be applied per item
            slotEnchants[(int)Character.CharacterSlot.Back] = backEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Back).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Chest] = chestEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Chest).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Feet] = feetEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Feet).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Finger1] = slotEnchants[(int)Character.CharacterSlot.Finger2] = fingerEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Finger).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Hands] = handsEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Hands).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Head] = headEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Head).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Legs] = legsEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Legs).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Shoulders] = shouldersEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Shoulders).ToArray();
            slotEnchants[(int)Character.CharacterSlot.MainHand] = mainHandEnchants = Enchant.FindAllEnchants(Item.ItemSlot.MainHand).ToArray();
            slotEnchants[(int)Character.CharacterSlot.OffHand] = offHandEnchants = Enchant.FindAllEnchants(Item.ItemSlot.OffHand).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Ranged] = rangedEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Ranged).ToArray();
            slotEnchants[(int)Character.CharacterSlot.Wrist] = wristEnchants = Enchant.FindAllEnchants(Item.ItemSlot.Wrist).ToArray();

            if (headItemList.Count == 0) headItemList.Add(null);
            if (neckItemList.Count == 0) neckItemList.Add(null);
            if (shouldersItemList.Count == 0) shouldersItemList.Add(null);
            if (backItemList.Count == 0) backItemList.Add(null);
            if (chestItemList.Count == 0) chestItemList.Add(null);
            if (wristItemList.Count == 0) wristItemList.Add(null);
            if (handsItemList.Count == 0) handsItemList.Add(null);
            if (waistItemList.Count == 0) waistItemList.Add(null);
            if (legsItemList.Count == 0) legsItemList.Add(null);
            if (feetItemList.Count == 0) feetItemList.Add(null);
            if (rangedItemList.Count == 0) rangedItemList.Add(null);
            if (projectileItemList.Count == 0) projectileItemList.Add(null);
            if (projectileBagItemList.Count == 0) projectileBagItemList.Add(null);
            fingerItemList.Add(null);
            trinketItemList.Add(null);
            mainHandItemList.Add(null);
            offHandItemList.Add(null);

            slotItems[(int)Character.CharacterSlot.Head] = headItems = FilterList(headItemList);
            slotItems[(int)Character.CharacterSlot.Neck] = neckItems = FilterList(neckItemList);
            slotItems[(int)Character.CharacterSlot.Shoulders] = shouldersItems = FilterList(shouldersItemList);
            slotItems[(int)Character.CharacterSlot.Back] = backItems = FilterList(backItemList);
            slotItems[(int)Character.CharacterSlot.Chest] = chestItems = FilterList(chestItemList);
            slotItems[(int)Character.CharacterSlot.Wrist] = wristItems = FilterList(wristItemList);
            slotItems[(int)Character.CharacterSlot.Hands] = handsItems = FilterList(handsItemList);
            slotItems[(int)Character.CharacterSlot.Waist] = waistItems = FilterList(waistItemList);
            slotItems[(int)Character.CharacterSlot.Legs] = legsItems = FilterList(legsItemList);
            slotItems[(int)Character.CharacterSlot.Feet] = feetItems = FilterList(feetItemList);
            slotItems[(int)Character.CharacterSlot.Finger1] = slotItems[(int)Character.CharacterSlot.Finger2] = fingerItems = fingerItemList.ToArray(); //When one ring/trinket is completely better than another
            slotItems[(int)Character.CharacterSlot.Trinket1] = slotItems[(int)Character.CharacterSlot.Trinket2] = trinketItems = trinketItemList.ToArray(); //you may still want to use both, so don't filter
            slotItems[(int)Character.CharacterSlot.MainHand] = mainHandItems = FilterList(mainHandItemList);
            slotItems[(int)Character.CharacterSlot.OffHand] = offHandItems = FilterList(offHandItemList);
            slotItems[(int)Character.CharacterSlot.Ranged] = rangedItems = FilterList(rangedItemList);
            slotItems[(int)Character.CharacterSlot.Projectile] = projectileItems = FilterList(projectileItemList);
            slotItems[(int)Character.CharacterSlot.ProjectileBag] = projectileBagItems = FilterList(projectileBagItemList);

            pairSlotMap = new int[slotCount];
            pairSlotMap[(int)Character.CharacterSlot.Back] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Chest] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Feet] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Finger1] = (int)Character.CharacterSlot.Finger2;
            pairSlotMap[(int)Character.CharacterSlot.Finger2] = (int)Character.CharacterSlot.Finger1;
            pairSlotMap[(int)Character.CharacterSlot.Hands] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Head] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Legs] = -1;
            pairSlotMap[(int)Character.CharacterSlot.MainHand] = (int)Character.CharacterSlot.OffHand;
            pairSlotMap[(int)Character.CharacterSlot.OffHand] = (int)Character.CharacterSlot.MainHand;
            pairSlotMap[(int)Character.CharacterSlot.Neck] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Projectile] = -1;
            pairSlotMap[(int)Character.CharacterSlot.ProjectileBag] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Ranged] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Shoulders] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Trinket1] = (int)Character.CharacterSlot.Trinket2;
            pairSlotMap[(int)Character.CharacterSlot.Trinket2] = (int)Character.CharacterSlot.Trinket1;
            pairSlotMap[(int)Character.CharacterSlot.Waist] = -1;
            pairSlotMap[(int)Character.CharacterSlot.Wrist] = -1;

            itemCacheInitialized = true;
        }

        public string GetWarningPromptIfNeeded()
        {
            int gemLimit = 8;
            int itemLimit = 512;
            int enchantLimit = 8;

            List<string> emptyList = new List<string>();
            List<string> tooManyList = new List<string>();

            CalculateWarnings(gemItems, "Gems", emptyList, tooManyList, gemLimit);
            CalculateWarnings(metaGemItems, "Meta Gems", emptyList, tooManyList, gemLimit);

            CalculateWarnings(headItems, "Head Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(neckItems, "Neck Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(shouldersItems, "Shoulder Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(backItems, "Back Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(chestItems, "Chest Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(wristItems, "Wrist Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(handsItems, "Hands Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(waistItems, "Waist Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(legsItems, "Legs Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(feetItems, "Feet Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(fingerItems, "Finger Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(trinketItems, "Trinket Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(mainHandItems, "Main Hand Items", emptyList, tooManyList, itemLimit);
            CalculateWarnings(offHandItems, "Offhand Items", null, tooManyList, itemLimit);
            CalculateWarnings(rangedItems, "Ranged Items", null, tooManyList, itemLimit);
            CalculateWarnings(projectileItems, "Projectile Items", null, tooManyList, itemLimit);
            CalculateWarnings(projectileBagItems, "Projectile Bag Items", null, tooManyList, itemLimit);

            //CalculateWarnings(backEnchants, "Back Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(chestEnchants, "Chest Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(feetEnchants, "Feet Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(fingerEnchants, "Finger Enchants", null, tooManyList, enchantLimit);
            //CalculateWarnings(handsEnchants, "Hands Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(headEnchants, "Head Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(legsEnchants, "Legs Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(shouldersEnchants, "Shoulder Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(mainHandEnchants, "Main Hand Enchants", emptyList, tooManyList, enchantLimit);
            //CalculateWarnings(offHandEnchants, "Offhand Enchants", null, tooManyList, enchantLimit);
            //CalculateWarnings(rangedEnchants, "Ranged Enchants", null, tooManyList, enchantLimit);
            //CalculateWarnings(wristEnchants, "Wrist Enchants", emptyList, tooManyList, enchantLimit);

            if (emptyList.Count + tooManyList.Count > 0)
            {
                if (emptyList.Count > 5)
                {
                    emptyList.RemoveRange(5, emptyList.Count - 5);
                    emptyList.Add("...");
                }
                if (tooManyList.Count > 5)
                {
                    tooManyList.RemoveRange(5, tooManyList.Count - 5);
                    tooManyList.Add("...");
                }
                if (tooManyList.Count == 0)
                {
                    // good sizes but some are empty
                    return "You have not selected any of the following:" + Environment.NewLine + Environment.NewLine + "\t" + string.Join(Environment.NewLine + "\t", emptyList.ToArray()) + Environment.NewLine + Environment.NewLine + "Do you want to continue with the optimization?";
                }
                else if (emptyList.Count == 0)
                {
                    return "The following slots have a very large number of items selected :" + Environment.NewLine + Environment.NewLine + "\t" + string.Join(Environment.NewLine + "\t", tooManyList.ToArray()) + Environment.NewLine + Environment.NewLine + "Do you want to continue with the optimization?";
                }
                else
                {
                    return "You have not selected any of the following:" + Environment.NewLine + Environment.NewLine + "\t" + string.Join(Environment.NewLine + "\t", emptyList.ToArray()) + Environment.NewLine + Environment.NewLine + "The following slots have a very large number of items selected :" + Environment.NewLine + Environment.NewLine + "\t" + string.Join(Environment.NewLine + "\t", tooManyList.ToArray()) + Environment.NewLine + Environment.NewLine + "Do you want to continue with the optimization?";
                }
            }
            return null;
        }

        private void CalculateWarnings(Array list, string group, List<string> emptyList, List<string> tooManyList, int tooManyLimit)
        {
            object el0 = (list.Length > 0) ? list.GetValue(0) : null;
            if (emptyList != null && (list.Length == 0 || (list.Length == 1 && (el0 == null || (el0 is Enchant && ((Enchant)el0).Id == 0))))) emptyList.Add(group);
            if (tooManyList != null && list.Length > tooManyLimit) tooManyList.Add(group);
        }

        private Character Optimize(out float bestValue)
        {
            CharacterCalculationsBase bestCalc;
            bool injected;
            return Optimize(null, 0, out bestValue, out bestCalc, out injected);
        }

        private Character Optimize(Character injectCharacter, out float bestValue, out bool injected)
        {
            CharacterCalculationsBase bestCalc;
            return Optimize(injectCharacter, GetOptimizationValue(injectCharacter, model), out bestValue, out bestCalc, out injected);
        }

        private Character Optimize(Character injectCharacter, float injectValue, out float best, out CharacterCalculationsBase bestCalculations, out bool injected)
        {
            switch (OptimizationMethod)
            {
                case OptimizationMethod.GeneticAlgorithm:
                    return OptimizeGA(injectCharacter, injectValue, out best, out bestCalculations, out injected);
                case OptimizationMethod.SimulatedAnnealing:
                    return OptimizeSA(injectCharacter, injectValue, out best, out bestCalculations, out injected);
            }
            best = 0.0f;
            bestCalculations = null;
            injected = false;
            return null;
        }

        /// <summary>
        /// Optimization function based on simulated annealing
        /// http://en.wikipedia.org/wiki/Simulated_annealing
        /// Author: ebo
        /// </summary>
        private Character OptimizeSA(Character injectCharacter, float injectValue, out float best, out CharacterCalculationsBase bestCalculations, out bool injected)
        {
            best = -10000000;
            bestCalculations = null;
            injected = false;

            rand = new Random();
            injected = false;

            Character currentChar = BuildRandomCharacter();
            double currentValue = GetCalculationsValue(model.GetCharacterCalculations(currentChar));

            Character bestChar = currentChar;
            double bestValue = currentValue;

            int maxCycles = _thoroughness * _thoroughness;

            //http://research.microsoft.com/constraint-reasoning/workshops/autonomous-cp07/papers/2.pdf

            double temp = 10;
            double acceptRate = 0.5;
            double lamRate = 0.5;

            for (int cycle = 0; cycle < maxCycles; cycle++)
            {
                if (cancellationPending) return null;
                ReportProgress((int)Math.Round((float)cycle / ((float)(maxCycles / 100f))), (float)bestValue);

                // Generate new character
                Character nextChar = GeneratorBuildSACharacter(currentChar);

                double nextValue = GetCalculationsValue(model.GetCharacterCalculations(nextChar));


                // Save best character
                if (nextValue > bestValue)
                {
                    bestChar = nextChar;
                    bestValue = nextValue;
                }

                if (nextValue > currentValue)
                {
                    //Better solution. Accept move
                    currentChar = nextChar;
                    currentValue = nextValue;
                    acceptRate = 1.0 / 500.0 * (499.0 * acceptRate + 1);
                }
                else
                {
                    if (SAAcceptance(currentValue, nextValue, temp, rand))
                    {
                        //accept move
                        currentChar = nextChar;
                        currentValue = nextValue;
                        acceptRate = 1.0 / 500.0 * (499.0 * acceptRate + 1);
                    }
                    else
                    {
                        //reject move
                        acceptRate = 1.0 / 500.0 * (499.0 * acceptRate);
                    }

                }


                // tune acceptRate
                double part = (double)cycle / maxCycles;
                if (part < 0.15)
                {
                    lamRate = 0.44 + 0.56 * Math.Pow(560, -cycle / (maxCycles * 0.15));
                }
                else if (part < 0.65)
                {
                    lamRate = 0.44;
                }
                else
                {
                    lamRate = 0.44 * Math.Pow(440, -((double)cycle / (double)maxCycles - 0.65) / 0.35);
                }

                if (acceptRate > lamRate)
                {
                    temp *= 0.999;
                }
                else
                {
                    temp /= 0.999;
                }


            }

            best = (float)bestValue;
            bestCalculations = model.GetCharacterCalculations(bestChar);

            return bestChar;
        }

        private Character OptimizeGA(Character injectCharacter, float injectValue, out float best, out CharacterCalculationsBase bestCalculations, out bool injected)
        {
            //Begin Genetic
            int noImprove, i1, i2;
            best = -10000000;
            bestCalculations = null;
            injected = false;

            int popSize = _thoroughness;
            int islandSize = 20;
            int islandStagnationLimit = 50;
            int islandCount = (popSize - 1) / islandSize + 1;
            int cycleLimit = _thoroughness;
            Character[] population = new Character[popSize];
            Character[] popCopy = new Character[popSize];
            float[] values = new float[popSize];
            float[] minIsland = new float[islandCount];
            float[] maxIsland = new float[islandCount];
            float[] bestIsland = new float[islandCount];
            Character[] characterIsland = new Character[islandCount];
            int[] islandNoImprove = new int[islandCount];
            for (int i = 0; i < islandCount; i++)
            {
                bestIsland[i] = -10000000;
            }
            float[] share = new float[popSize];
            float s, sum, minv, maxv;
            Character bestCharacter = null;
            rand = new Random();

            if (_thoroughness > 1)
            {
                for (int i = 0; i < popSize; i++)
                {
                    population[i] = BuildRandomCharacter();
                }
            }
            else
            {
                bestCharacter = _character;
                best = GetCalculationsValue(model.GetCharacterCalculations(_character));
            }

            noImprove = 0;
            while (noImprove < cycleLimit)
            {
                if (_thoroughness > 1)
                {
                    if (cancellationPending) return null;
                    ReportProgress((int)Math.Round((float)noImprove / ((float)cycleLimit / 100f)), best);

                    for (int i = 0; i < islandCount; i++)
                    {
                        minIsland[i] = 10000000;
                        maxIsland[i] = -10000000;
                        islandNoImprove[i]++;
                    }
                    minv = 10000000;
                    maxv = -10000000;
                    for (int i = 0; i < popSize; i++)
                    {
                        int island = i / islandSize;
                        CharacterCalculationsBase calculations;
                        values[i] = GetCalculationsValue(calculations = model.GetCharacterCalculations(population[i]));
                        if (values[i] < minIsland[island]) minIsland[island] = values[i];
                        if (values[i] > maxIsland[island]) maxIsland[island] = values[i];
                        if (values[i] > bestIsland[island])
                        {
                            bestIsland[island] = values[i];
                            characterIsland[island] = population[i];
                            islandNoImprove[island] = 0;
                        }
                        if (values[i] > best)
                        {
                            best = values[i];
                            bestCalculations = calculations;
                            bestCharacter = population[i];
                            noImprove = -1;
                            //if (population[i].Geneology != null) System.Diagnostics.Trace.WriteLine(best + " " + population[i].Geneology);
                        }
                    }
                    for (int island = 0; island < islandCount; island++)
                    {
                        sum = 0;
                        for (int i = island * islandSize; i < Math.Min(popSize, (island + 1) * islandSize); i++)
                            sum += values[i] - minIsland[island] + (maxIsland[island] - minIsland[island]) / 2;
                        for (int i = island * islandSize; i < Math.Min(popSize, (island + 1) * islandSize); i++)
                            share[i] = sum == 0 ? 1f / (Math.Min(popSize, (island + 1) * islandSize) - island * islandSize) : (values[i] - minIsland[island] + (maxIsland[island] - minIsland[island]) / 2) / sum;
                    }
                }

                noImprove++;

                if (_thoroughness > 1 && noImprove < cycleLimit)
                {
                    population.CopyTo(popCopy, 0);
                    for (int i = 0; i < popSize; i++)
                    {
                        int island = i / islandSize;
                        if (i % islandSize == 0)
                        {
                            if (characterIsland[island] == null)
                            {
                                population[i] = BuildRandomCharacter();
                                //population[i].Geneology = "Random";
                            }
                            else
                            {
                                population[i] = characterIsland[island];
                            }
                        }
                        else if (rand.NextDouble() < 0.05d)
                        {
                            //completely random
                            population[i] = BuildRandomCharacter();
                            //population[i].Geneology = "Random";
                        }
                        else if (rand.NextDouble() < 0.4d)
                        {
                            int transplant = island;
                            if (islandNoImprove[island] > islandStagnationLimit) transplant = rand.Next(islandCount);
                            //crossover
                            s = (float)rand.NextDouble();
                            sum = 0;
                            for (i1 = transplant * islandSize; i1 < Math.Min(popSize, (transplant + 1) * islandSize) - 1; i1++)
                            {
                                sum += share[i1];
                                if (sum >= s) break;
                            }
                            s = (float)rand.NextDouble();
                            sum = 0;
                            for (i2 = island * islandSize; i2 < Math.Min(popSize, (island + 1) * islandSize) - 1; i2++)
                            {
                                sum += share[i2];
                                if (sum >= s) break;
                            }
                            population[i] = BuildChildCharacter(popCopy[i1], popCopy[i2]);
                            //population[i].Geneology = "Crossover(" + values[i1] + ", " + values[i2] + ")";
                        }
                        else
                        {
                            int transplant = island;
                            if (islandNoImprove[island] > islandStagnationLimit && rand.NextDouble() < 1.0 / islandSize) transplant = rand.Next(islandCount);
                            //mutate
                            s = (float)rand.NextDouble();
                            sum = 0;
                            for (i1 = transplant * islandSize; i1 < Math.Min(popSize, (transplant + 1) * islandSize) - 1; i1++)
                            {
                                sum += share[i1];
                                if (sum >= s) break;
                            }
                            bool successful;
                            if (rand.NextDouble() < 0.9)
                            {
                                population[i] = BuildMutantCharacter(popCopy[i1]);
                                //population[i].Geneology = "Mutation(" + values[i1] + ")";
                            }
                            else if (rand.NextDouble() < 0.5)
                            {
                                population[i] = BuildReplaceGemMutantCharacter(popCopy[i1], out successful);
                                //population[i].Geneology = "MutationReplaceGem(" + values[i1] + ")";
                                if (!successful)
                                {
                                    population[i] = BuildMutantCharacter(popCopy[i1]);
                                    //population[i].Geneology = "Mutation(" + values[i1] + ")";
                                }
                            }
                            else
                            {
                                population[i] = BuildSwapGemMutantCharacter(popCopy[i1], out successful);
                                //population[i].Geneology = "MutationSwapGem(" + values[i1] + ")";
                                if (!successful)
                                {
                                    population[i] = BuildMutantCharacter(popCopy[i1]);
                                    //population[i].Geneology = "Mutation(" + values[i1] + ")";
                                }
                            }
                        }
                    }
                }
                else if (_thoroughness > 1 && injectCharacter != null && !injected && injectValue > best)
                {
                    population[popSize - 1] = injectCharacter;
                    noImprove = 0;
                    injected = true;
                }
                else
                {
                    //last try, look for single direct upgrades
                    KeyValuePair<float, Character> results;
                    CharacterCalculationsBase calculations;
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        results = LookForDirectItemUpgrades(slotItems[slot], (Character.CharacterSlot)slot, best, bestCharacter, out calculations);
                        if (results.Key > best)
                        {
                            best = results.Key;
                            bestCalculations = calculations;
                            bestCharacter = results.Value;
                            noImprove = 0;
                            population[0] = bestCharacter;
                            //population[0].Geneology = "DirectUpgrade";
                        }
                    }

                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        if (slotEnchants[slot] != null)
                        {
                            results = LookForDirectEnchantUpgrades(slotEnchants[slot], (Character.CharacterSlot)slot, best, bestCharacter, out calculations);
                            if (results.Key > best)
                            {
                                best = results.Key;
                                bestCalculations = calculations;
                                bestCharacter = results.Value;
                                noImprove = 0;
                                population[0] = bestCharacter;
                                //population[0].Geneology = "DirectUpgrade";
                            }
                        }
                    }
                }
            }

            if (best == 0)
            {
                bestCharacter = null;
                bestCalculations = null;
            }
            else
                ToString();
            return bestCharacter;

            //ulong msGenetic = (ulong)DateTime.Now.Subtract(startTime).TotalMilliseconds;
            //w.ToString();

            //startTime = DateTime.Now;

            //for (int i = 0; i < 10000; i++)
            //    Calculations.GetCharacterCalculations(_character);


            //ulong msManual10000 = (ulong)(DateTime.Now.Subtract(startTime).TotalMilliseconds);
            //ulong msManual = msManual10000 * (totalCombinations / (ulong)10000);

            //MessageBox.Show(string.Format("Genetic: {0} sec\r\nManual: {1} sec\r\nRelative Speed: {2}",
            //    msGenetic/1000, msManual/1000, msManual / msGenetic));
        }

        private KeyValuePair<float, Character> LookForDirectItemUpgrades(Item[] items, Character.CharacterSlot slot, float best, Character bestCharacter, out CharacterCalculationsBase bestCalculations)
        {
            Character charSwap;
            bestCalculations = null;
            float value;
            bool foundUpgrade = false;
            if (slot == lockedSlot) items = lockedItems;
            foreach (Item item in items)
            {
                int pairSlot = pairSlotMap[(int)slot];
                if (item != null && (bestCharacter[slot] == null || bestCharacter[slot].GemmedId != item.GemmedId) && !(pairSlot >= 0 && bestCharacter[(Character.CharacterSlot)pairSlot] != null && bestCharacter[(Character.CharacterSlot)pairSlot].Id == item.Id && item.Unique))
                {
                    Enchant enchant = null;
                    if (slotEnchants[(int)slot] != null) enchant = bestCharacter.GetEnchantBySlot(slot);
                    // replace enchant with a valid enchant
                    if (slot == lockedSlot && lockedEnchants != null && Array.IndexOf(lockedEnchants, enchant) == -1)
                    {
                        enchant = lockedEnchants[0];
                    }
                    if (slot != lockedSlot && enchant != null)
                    {
                        Dictionary<int, bool> dict = item.EnchantValid ?? itemEnchantValid[item.GemmedId];
                        bool valid;
                        dict.TryGetValue(enchant.Id, out valid);
                        if (!valid)
                        {
                            foreach (Enchant e in slotEnchants[(int)slot])
                            {
                                if (dict.TryGetValue(e.Id, out valid) && valid)
                                {
                                    enchant = e;
                                    break;
                                }
                            }
                        }
                    }
                    charSwap = BuildSingleItemEnchantSwapCharacter(bestCharacter, slot, item, enchant);
                    CharacterCalculationsBase calculations;
                    value = GetCalculationsValue(calculations = model.GetCharacterCalculations(charSwap));
                    if (value > best)
                    {
                        best = value;
                        bestCalculations = calculations;
                        bestCharacter = charSwap;
                        foundUpgrade = true;
                    }
                }
            }
            if (foundUpgrade)
                return new KeyValuePair<float, Character>(best, bestCharacter);
            return new KeyValuePair<float, Character>(float.NegativeInfinity, null);
        }

        private KeyValuePair<float, Character> LookForDirectEnchantUpgrades(Enchant[] enchants, Character.CharacterSlot slot, float best, Character bestCharacter, out CharacterCalculationsBase bestCalculations)
        {
            Character charSwap;
            bestCalculations = null;
            float value;
            float newBest = best;
            Character newBestCharacter = null;
            Enchant currentEnchant = bestCharacter.GetEnchantBySlot(slot);
            if (bestCharacter[slot] != null)
            {
                foreach (Enchant enchant in enchants)
                {
                    if (currentEnchant != enchant && ItemEnchantValid(slot, bestCharacter[slot], enchant))
                    {
                        charSwap = BuildSingleEnchantSwapCharacter(bestCharacter, slot, enchant);
                        CharacterCalculationsBase calculations;
                        value = GetCalculationsValue(calculations = model.GetCharacterCalculations(charSwap));
                        if (value > newBest)
                        {
                            newBest = value;
                            bestCalculations = calculations;
                            newBestCharacter = charSwap;
                        }
                    }
                }
            }
            if (newBest > best)
                return new KeyValuePair<float, Character>(newBest, newBestCharacter);
            return new KeyValuePair<float, Character>(float.NegativeInfinity, null);
        }

        public static float GetOptimizationValue(Character character, CalculationsBase model)
        {
            return GetCalculationsValue(model.GetCharacterCalculations(character), character.CalculationToOptimize, character.OptimizationRequirements.ToArray());
        }

        private float GetCalculationsValue(CharacterCalculationsBase calcs)
        {
            return Optimizer.GetCalculationsValue(calcs, _calculationToOptimize, _requirements);
        }

        private static float GetCalculationsValue(CharacterCalculationsBase calcs, string calculation, OptimizationRequirement[] requirements)
        {
            float ret = 0;
            foreach (OptimizationRequirement requirement in requirements)
            {
                float calcValue = GetCalculationValue(calcs, requirement.Calculation);
                if (requirement.LessThan)
                {
                    if (!(calcValue <= requirement.Value))
                        ret += requirement.Value - calcValue;
                }
                else
                {
                    if (!(calcValue >= requirement.Value))
                        ret += calcValue - requirement.Value;
                }
            }

            if (ret < 0) return ret;
            else return GetCalculationValue(calcs, calculation);
        }

        private static float GetCalculationValue(CharacterCalculationsBase calcs, string calculation)
        {
            if (calculation == null || calculation == "[Overall]")
                return calcs.OverallPoints;
            else if (calculation.StartsWith("[SubPoint "))
                return calcs.SubPoints[int.Parse(calculation.Substring(10).TrimEnd(']'))];
            else
                return calcs.GetOptimizableCalculationValue(calculation);
        }

        private delegate Item GeneratorItemSelector(int slot);
        private delegate Enchant GeneratorEnchantSelector(int slot, Item item);

        private void GeneratorFillSlot(int slot, Item[] item, Enchant[] enchant, GeneratorItemSelector itemSelector, GeneratorEnchantSelector enchantSelector)
        {
            Item i = item[slot] = itemSelector(slot);
            if (i != null && slotEnchants[slot] != null)
            {
                Enchant e = null;
                do
                {
                    e = enchantSelector(slot, i);
                } while (!ItemEnchantValid((Character.CharacterSlot)slot, i, e));
                enchant[slot] = e;
            }
        }

        private Character GeneratorBuildCharacter(GeneratorItemSelector itemSelector, GeneratorEnchantSelector enchantSelector)
        {
            Item[] item = new Item[slotCount];
            Enchant[] enchant = new Enchant[slotCount];
            for (int slot = 0; slot < slotCount; slot++)
            {
                GeneratorFillSlot(slot, item, enchant, itemSelector, enchantSelector);
            }
            foreach (int slot in pairSlotList)
            {
                int pairSlot = pairSlotMap[slot];
                while (item[slot] != null && item[pairSlot] != null && item[slot].Id == item[pairSlot].Id && item[slot].Unique)
                {
                    GeneratorFillSlot(slot, item, enchant, itemSelector, enchantSelector);
                    GeneratorFillSlot(pairSlot, item, enchant, itemSelector, enchantSelector);
                }
            }

            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race, item, enchant, _character.ActiveBuffs, false, _character.CurrentModel);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;
            return character;
        }

        private Character BuildRandomCharacter()
        {
            return GeneratorBuildCharacter(
                delegate (int slot)
                {
                    return (lockedSlot == (Character.CharacterSlot)slot) ? lockedItems[rand.Next(lockedItems.Length)] : slotItems[slot][rand.Next(slotItems[slot].Length)];
                },
                delegate (int slot, Item item)
                {
                    return (lockedSlot == (Character.CharacterSlot)slot && lockedEnchants != null) ? lockedEnchants[rand.Next(lockedEnchants.Length)] : (item.EnchantValidList != null ? item.EnchantValidList[rand.Next(item.EnchantValidList.Count)] : slotEnchants[slot][rand.Next(slotEnchants[slot].Length)]);
                });
        }

        private Character BuildChildCharacter(Character father, Character mother)
        {
            return GeneratorBuildCharacter(
                delegate (int slot)
                {
                    return rand.NextDouble() < 0.5d ? father[(Character.CharacterSlot)slot] : mother[(Character.CharacterSlot)slot];
                },
                delegate (int slot, Item item)
                {
                    return rand.NextDouble() < 0.5d ? father.GetEnchantBySlot((Character.CharacterSlot)slot) : mother.GetEnchantBySlot((Character.CharacterSlot)slot);
                });
        }

        /// <summary>
        /// This is funtions decides wether we take a new character or drop it
        /// Author: ebo
        /// </summary>
        private bool SAAcceptance(double e, double enew, double T, Random R)
        {
            // Always accept character if its better
            if (enew > e)
            {
                return true;
            }
            else
            {
                // Accept based on difference and temperature
                // higher temperature means bigger differences possible (or likely)
                // see http://en.wikipedia.org/wiki/Simulated_annealing
                double chance = Math.Exp((enew - e) / T);
                return chance > R.NextDouble();
            }

        }

        /// <summary>
        /// This is funtions clones a character and changes one item and based on a probability one enchant
        /// Author: ebo
        /// </summary>
        private Character GeneratorBuildSACharacter(Character parent)
        {
            Item[] item = new Item[slotCount];
            Enchant[] enchant = new Enchant[slotCount];

            for (int slot = 0; slot < slotCount; slot++)
            {
                item[slot] = parent[(Character.CharacterSlot)slot];
                enchant[slot] = parent.GetEnchantBySlot((Character.CharacterSlot)slot);
            }

            double r = rand.NextDouble();
            bool successfull = false;

            if (r < 0.1)
            {
                // Change one enchant?
                // There are better methods to make sure to change one item (shuffled list of all slots) but this works

                for (int i = 0; i < slotCount; i++)
                {
                    int slot = rand.Next(slotCount);
                    Item cItem = item[slot];
                    if (cItem == null || slotEnchants[slot] == null)
                    {
                        continue;
                    }

                    Enchant newenchant;
                    if (lockedSlot == (Character.CharacterSlot)slot && lockedEnchants != null)
                    {
                        newenchant = lockedEnchants[rand.Next(lockedEnchants.Length)];
                    }
                    else
                    {
                        if (cItem.EnchantValidList != null)
                        {
                            newenchant = cItem.EnchantValidList[rand.Next(cItem.EnchantValidList.Count)];
                        }
                        else
                        {
                            newenchant = slotEnchants[slot][rand.Next(slotEnchants[slot].Length)];
                        }
                    }
                    if (newenchant == null) continue;
                    if (enchant[slot] == null || (newenchant.Id != enchant[slot].Id && ItemEnchantValid((Character.CharacterSlot)slot, item[slot], newenchant)))
                    {
                        enchant[slot] = newenchant;
                        successfull = true;
                        break;
                    }
                }
            }

            r = rand.NextDouble();

            if (r < 0.5 && !successfull)
            {

                Character nextChar2 = rand.NextDouble() < 0.75f ? BuildReplaceGemMutantCharacter(parent, out successfull) : BuildSwapGemMutantCharacter(parent, out successfull);
                if (successfull)
                {
                    return nextChar2;
                }
            }

            if (!successfull)
            {
                // Make sure to change one item
                // There are better methods to make sure to change one item (shuffled list of all slots) but this works
                for (int i = 0; i < slotCount; i++)
                {
                    int slot = rand.Next(slotCount);
                    Item newitem = ((lockedSlot == (Character.CharacterSlot)slot) ? lockedItems[rand.Next(lockedItems.Length)] : slotItems[slot][rand.Next(slotItems[slot].Length)]);
                    if (newitem == null) continue;
                    int pairSlot = pairSlotMap[slot];
                    if (item[slot] == null || newitem.Id != item[slot].Id)
                    {
                        if (pairSlot != -1 && item[pairSlot] != null && item[pairSlot].Id == newitem.Id && newitem.Unique)
                        {
                            continue;
                        }
                        item[slot] = newitem;
                        break;
                    }
                }
            }


            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race, item, enchant, _character.ActiveBuffs, false, _character.CurrentModel);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;

            return character;

        }

        private Item ReplaceGem(Item item, int index, Item gem)
        {
            // alternatively construct gemmedid and retrieve from cache, trading memory footprint for dictionary access
            Item copy = new Item(item.Name, item.Quality, item.Type, item.Id, item.IconPath, item.Slot,
                item.SetName, item.Unique, item.Stats.Clone(), item.Sockets.Clone(), 0, 0, 0, item.MinDamage,
                item.MaxDamage, item.DamageType, item.Speed, item.RequiredClasses);
            copy.SetGemInternal(1, item.Gem1);
            copy.SetGemInternal(2, item.Gem2);
            copy.SetGemInternal(3, item.Gem3);
            copy.SetGemInternal(index, gem);
            return copy;
            //string gemmedId = string.Format("{0}.{1}.{2}.{3}", item.Id, (index == 1) ? gem.Id : item.Gem1Id, (index == 2) ? gem.Id : item.Gem2Id, (index == 3) ? gem.Id : item.Gem3Id);
            //return ItemCache.FindItemById(gemmedId, true, false);
        }

        private struct GemInformation
        {
            public Character.CharacterSlot Slot;
            public int Index;
            public Item Gem;
            public Item.ItemSlot Socket;
        }

        private Character BuildReplaceGemMutantCharacter(Character parent, out bool successful)
        {
            Item[] items = new Item[slotCount];
            items[(int)Character.CharacterSlot.Head] = parent[Character.CharacterSlot.Head];
            items[(int)Character.CharacterSlot.Neck] = parent[Character.CharacterSlot.Neck];
            items[(int)Character.CharacterSlot.Shoulders] = parent[Character.CharacterSlot.Shoulders];
            items[(int)Character.CharacterSlot.Back] = parent[Character.CharacterSlot.Back];
            items[(int)Character.CharacterSlot.Chest] = parent[Character.CharacterSlot.Chest];
            items[(int)Character.CharacterSlot.Wrist] = parent[Character.CharacterSlot.Wrist];
            items[(int)Character.CharacterSlot.Hands] = parent[Character.CharacterSlot.Hands];
            items[(int)Character.CharacterSlot.Waist] = parent[Character.CharacterSlot.Waist];
            items[(int)Character.CharacterSlot.Legs] = parent[Character.CharacterSlot.Legs];
            items[(int)Character.CharacterSlot.Feet] = parent[Character.CharacterSlot.Feet];
            items[(int)Character.CharacterSlot.Finger1] = parent[Character.CharacterSlot.Finger1];
            items[(int)Character.CharacterSlot.Finger2] = parent[Character.CharacterSlot.Finger2];
            items[(int)Character.CharacterSlot.Trinket1] = parent[Character.CharacterSlot.Trinket1];
            items[(int)Character.CharacterSlot.Trinket2] = parent[Character.CharacterSlot.Trinket2];
            items[(int)Character.CharacterSlot.MainHand] = parent[Character.CharacterSlot.MainHand];
            items[(int)Character.CharacterSlot.OffHand] = parent[Character.CharacterSlot.OffHand];
            items[(int)Character.CharacterSlot.Ranged] = parent[Character.CharacterSlot.Ranged];
            items[(int)Character.CharacterSlot.Projectile] = parent[Character.CharacterSlot.Projectile];
            items[(int)Character.CharacterSlot.ProjectileBag] = parent[Character.CharacterSlot.ProjectileBag];
            successful = false;

            // do the work

            // build a list of possible mutation points
            List<GemInformation> locationList = new List<GemInformation>();
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (items[slot] != null)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        Item gem = items[slot].GetGem(i);
                        if (gem != null) locationList.Add(new GemInformation() { Slot = (Character.CharacterSlot)slot, Index = i, Gem = gem, Socket = items[slot].Sockets.GetColor(i) });
                    }
                }
            }

            if (locationList.Count > 0)
            {
                int numberMutations = rand.Next(1, 2);
                for (int i = 0; i < numberMutations; i++)
                {
                    // randomly select mutation point
                    int mutationIndex = rand.Next(locationList.Count);

                    // mutate
                    GemInformation mutation = locationList[mutationIndex];
                    Item newGem;
                    if (mutation.Socket == Item.ItemSlot.Meta)
                    {
                        newGem = metaGemItems[rand.Next(metaGemItems.Length)];
                    }
                    else
                    {
                        newGem = gemItems[rand.Next(gemItems.Length)];
                    }
                    Item newItem = ReplaceGem(items[(int)mutation.Slot], mutation.Index, newGem);
                    Dictionary<int, bool> dict;
                    // make sure the item and item-enchant combo is allowed
                    Enchant enchant = parent.GetEnchantBySlot(mutation.Slot);
                    bool valid;
                    if ((lockedSlot != mutation.Slot || lockedItems == null || lockedItems.Length > 1) && itemEnchantValid.TryGetValue(newItem.GemmedId, out dict) && (enchant == null || (dict.TryGetValue(enchant.Id, out valid) && valid)))
                    {
                        items[(int)mutation.Slot] = newItem;
                        successful = true;
                    }
                }
            }

            // create character

            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race, items,
                parent.HeadEnchant,
                parent.ShouldersEnchant,
                parent.BackEnchant,
                parent.ChestEnchant,
                parent.WristEnchant,
                parent.HandsEnchant,
                parent.LegsEnchant,
                parent.FeetEnchant,
                parent.Finger1Enchant,
                parent.Finger2Enchant,
                parent.MainHandEnchant,
                parent.OffHandEnchant,
                parent.RangedEnchant,
                _character.ActiveBuffs, false, _character.CurrentModel);
            //foreach (KeyValuePair<string, string> kvp in _character.CalculationOptions)
            //	character.CalculationOptions.Add(kvp.Key, kvp.Value);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;
            //character.RecalculateSetBonuses();
            return character;
        }

        private Character BuildSwapGemMutantCharacter(Character parent, out bool successful)
        {
            Item[] items = new Item[slotCount];
            items[(int)Character.CharacterSlot.Head] = parent[Character.CharacterSlot.Head];
            items[(int)Character.CharacterSlot.Neck] = parent[Character.CharacterSlot.Neck];
            items[(int)Character.CharacterSlot.Shoulders] = parent[Character.CharacterSlot.Shoulders];
            items[(int)Character.CharacterSlot.Back] = parent[Character.CharacterSlot.Back];
            items[(int)Character.CharacterSlot.Chest] = parent[Character.CharacterSlot.Chest];
            items[(int)Character.CharacterSlot.Wrist] = parent[Character.CharacterSlot.Wrist];
            items[(int)Character.CharacterSlot.Hands] = parent[Character.CharacterSlot.Hands];
            items[(int)Character.CharacterSlot.Waist] = parent[Character.CharacterSlot.Waist];
            items[(int)Character.CharacterSlot.Legs] = parent[Character.CharacterSlot.Legs];
            items[(int)Character.CharacterSlot.Feet] = parent[Character.CharacterSlot.Feet];
            items[(int)Character.CharacterSlot.Finger1] = parent[Character.CharacterSlot.Finger1];
            items[(int)Character.CharacterSlot.Finger2] = parent[Character.CharacterSlot.Finger2];
            items[(int)Character.CharacterSlot.Trinket1] = parent[Character.CharacterSlot.Trinket1];
            items[(int)Character.CharacterSlot.Trinket2] = parent[Character.CharacterSlot.Trinket2];
            items[(int)Character.CharacterSlot.MainHand] = parent[Character.CharacterSlot.MainHand];
            items[(int)Character.CharacterSlot.OffHand] = parent[Character.CharacterSlot.OffHand];
            items[(int)Character.CharacterSlot.Ranged] = parent[Character.CharacterSlot.Ranged];
            items[(int)Character.CharacterSlot.Projectile] = parent[Character.CharacterSlot.Projectile];
            items[(int)Character.CharacterSlot.ProjectileBag] = parent[Character.CharacterSlot.ProjectileBag];
            successful = false;

            // do the work

            // build a list of possible mutation points
            // make sure not to do meta gem swaps
            List<GemInformation> locationList = new List<GemInformation>();
            for (int slot = 0; slot < slotCount; slot++)
            {
                if (items[slot] != null)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        Item gem = items[slot].GetGem(i);
                        if (gem != null && gem.Slot != Item.ItemSlot.Meta) locationList.Add(new GemInformation() { Slot = (Character.CharacterSlot)slot, Index = i, Gem = gem, Socket = items[slot].Sockets.GetColor(i) });
                    }
                }
            }

            if (locationList.Count > 1)
            {
                GemInformation mutation1;
                GemInformation mutation2;
                int tries = 0;
                // randomly select mutation point
                do
                {
                    int mutationIndex1 = rand.Next(locationList.Count);
                    int mutationIndex2 = rand.Next(locationList.Count);
                    mutation1 = locationList[mutationIndex1];
                    mutation2 = locationList[mutationIndex2];
                    tries++;
                } while (tries < 10 && mutation1.Gem == mutation2.Gem);

                // mutate
                Item item1 = ReplaceGem(items[(int)mutation1.Slot], mutation1.Index, mutation2.Gem);
                Item item2 = ReplaceGem(items[(int)mutation2.Slot], mutation2.Index, mutation1.Gem);
                Dictionary<int, bool> dict1, dict2;
                Enchant enchant1, enchant2;
                enchant1 = parent.GetEnchantBySlot(mutation1.Slot);
                enchant2 = parent.GetEnchantBySlot(mutation2.Slot);
                bool valid1, valid2;
                if ((lockedSlot != mutation1.Slot || lockedItems == null || lockedItems.Length > 1) && (lockedSlot != mutation2.Slot || lockedItems == null || lockedItems.Length > 1) && itemEnchantValid.TryGetValue(item1.GemmedId, out dict1) && (enchant1 == null || (dict1.TryGetValue(enchant1.Id, out valid1) && valid1)) && itemEnchantValid.TryGetValue(item2.GemmedId, out dict2) && (enchant2 == null || (dict2.TryGetValue(enchant2.Id, out valid2) && valid2)))
                {
                    successful = true;
                    items[(int)mutation1.Slot] = item1;
                    items[(int)mutation2.Slot] = item2;
                }
            }

            // create character

            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race, items,
                parent.HeadEnchant,
                parent.ShouldersEnchant,
                parent.BackEnchant,
                parent.ChestEnchant,
                parent.WristEnchant,
                parent.HandsEnchant,
                parent.LegsEnchant,
                parent.FeetEnchant,
                parent.Finger1Enchant,
                parent.Finger2Enchant,
                parent.MainHandEnchant,
                parent.OffHandEnchant,
                parent.RangedEnchant,
                _character.ActiveBuffs, false, _character.CurrentModel);
            //foreach (KeyValuePair<string, string> kvp in _character.CalculationOptions)
            //	character.CalculationOptions.Add(kvp.Key, kvp.Value);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;
            //character.RecalculateSetBonuses();
            return character;
        }

        private Character BuildMutantCharacter(Character parent)
        {
            int targetMutations = 2;
            while (targetMutations < 32 && rand.NextDouble() < 0.75d) targetMutations++;
            double mutationChance = (double)targetMutations / 32d;

            return GeneratorBuildCharacter(
                delegate (int slot)
                {
                    return rand.NextDouble() < mutationChance ? ((lockedSlot == (Character.CharacterSlot)slot) ? lockedItems[rand.Next(lockedItems.Length)] : slotItems[slot][rand.Next(slotItems[slot].Length)]) : parent[(Character.CharacterSlot)slot];
                },
                delegate (int slot, Item item)
                {
                    return rand.NextDouble() < mutationChance ? ((lockedSlot == (Character.CharacterSlot)slot && lockedEnchants != null) ? lockedEnchants[rand.Next(lockedEnchants.Length)] : (item.EnchantValidList != null ? item.EnchantValidList[rand.Next(item.EnchantValidList.Count)] : slotEnchants[slot][rand.Next(slotEnchants[slot].Length)])) : parent.GetEnchantBySlot((Character.CharacterSlot)slot);
                });
        }

        private Character BuildSingleItemEnchantSwapCharacter(Character baseCharacter, Character.CharacterSlot slot, Item item, Enchant enchant)
        {
            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race,
                slot == Character.CharacterSlot.Head ? item : baseCharacter.Head,
                slot == Character.CharacterSlot.Neck ? item : baseCharacter.Neck,
                slot == Character.CharacterSlot.Shoulders ? item : baseCharacter.Shoulders,
                slot == Character.CharacterSlot.Back ? item : baseCharacter.Back,
                slot == Character.CharacterSlot.Chest ? item : baseCharacter.Chest,
                null, null,
                slot == Character.CharacterSlot.Wrist ? item : baseCharacter.Wrist,
                slot == Character.CharacterSlot.Hands ? item : baseCharacter.Hands,
                slot == Character.CharacterSlot.Waist ? item : baseCharacter.Waist,
                slot == Character.CharacterSlot.Legs ? item : baseCharacter.Legs,
                slot == Character.CharacterSlot.Feet ? item : baseCharacter.Feet,
                slot == Character.CharacterSlot.Finger1 ? item : baseCharacter.Finger1,
                slot == Character.CharacterSlot.Finger2 ? item : baseCharacter.Finger2,
                slot == Character.CharacterSlot.Trinket1 ? item : baseCharacter.Trinket1,
                slot == Character.CharacterSlot.Trinket2 ? item : baseCharacter.Trinket2,
                slot == Character.CharacterSlot.MainHand ? item : baseCharacter.MainHand,
                slot == Character.CharacterSlot.OffHand ? item : baseCharacter.OffHand,
                slot == Character.CharacterSlot.Ranged ? item : baseCharacter.Ranged,
                slot == Character.CharacterSlot.Projectile ? item : baseCharacter.Projectile,
                slot == Character.CharacterSlot.ProjectileBag ? item : baseCharacter.ProjectileBag,
                slot == Character.CharacterSlot.Head ? enchant : baseCharacter.HeadEnchant,
                slot == Character.CharacterSlot.Shoulders ? enchant : baseCharacter.ShouldersEnchant,
                slot == Character.CharacterSlot.Back ? enchant : baseCharacter.BackEnchant,
                slot == Character.CharacterSlot.Chest ? enchant : baseCharacter.ChestEnchant,
                slot == Character.CharacterSlot.Wrist ? enchant : baseCharacter.WristEnchant,
                slot == Character.CharacterSlot.Hands ? enchant : baseCharacter.HandsEnchant,
                slot == Character.CharacterSlot.Legs ? enchant : baseCharacter.LegsEnchant,
                slot == Character.CharacterSlot.Feet ? enchant : baseCharacter.FeetEnchant,
                slot == Character.CharacterSlot.Finger1 ? enchant : baseCharacter.Finger1Enchant,
                slot == Character.CharacterSlot.Finger2 ? enchant : baseCharacter.Finger2Enchant,
                slot == Character.CharacterSlot.MainHand ? enchant : baseCharacter.MainHandEnchant,
                slot == Character.CharacterSlot.OffHand ? enchant : baseCharacter.OffHandEnchant,
                slot == Character.CharacterSlot.Ranged ? enchant : baseCharacter.RangedEnchant,
                _character.ActiveBuffs, false, _character.CurrentModel);
            //foreach (KeyValuePair<string, string> kvp in _character.CalculationOptions)
            //	character.CalculationOptions.Add(kvp.Key, kvp.Value);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;
            //character.RecalculateSetBonuses();
            return character;
        }

        private Character BuildSingleItemSwapCharacter(Character baseCharacter, Character.CharacterSlot slot, Item item)
        {
            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race,
                slot == Character.CharacterSlot.Head ? item : baseCharacter.Head,
                slot == Character.CharacterSlot.Neck ? item : baseCharacter.Neck,
                slot == Character.CharacterSlot.Shoulders ? item : baseCharacter.Shoulders,
                slot == Character.CharacterSlot.Back ? item : baseCharacter.Back,
                slot == Character.CharacterSlot.Chest ? item : baseCharacter.Chest,
                null, null,
                slot == Character.CharacterSlot.Wrist ? item : baseCharacter.Wrist,
                slot == Character.CharacterSlot.Hands ? item : baseCharacter.Hands,
                slot == Character.CharacterSlot.Waist ? item : baseCharacter.Waist,
                slot == Character.CharacterSlot.Legs ? item : baseCharacter.Legs,
                slot == Character.CharacterSlot.Feet ? item : baseCharacter.Feet,
                slot == Character.CharacterSlot.Finger1 ? item : baseCharacter.Finger1,
                slot == Character.CharacterSlot.Finger2 ? item : baseCharacter.Finger2,
                slot == Character.CharacterSlot.Trinket1 ? item : baseCharacter.Trinket1,
                slot == Character.CharacterSlot.Trinket2 ? item : baseCharacter.Trinket2,
                slot == Character.CharacterSlot.MainHand ? item : baseCharacter.MainHand,
                slot == Character.CharacterSlot.OffHand ? item : baseCharacter.OffHand,
                slot == Character.CharacterSlot.Ranged ? item : baseCharacter.Ranged,
                slot == Character.CharacterSlot.Projectile ? item : baseCharacter.Projectile,
                slot == Character.CharacterSlot.ProjectileBag ? item : baseCharacter.ProjectileBag,
                baseCharacter.HeadEnchant,
                baseCharacter.ShouldersEnchant,
                baseCharacter.BackEnchant,
                baseCharacter.ChestEnchant,
                baseCharacter.WristEnchant,
                baseCharacter.HandsEnchant,
                baseCharacter.LegsEnchant,
                baseCharacter.FeetEnchant,
                baseCharacter.Finger1Enchant,
                baseCharacter.Finger2Enchant,
                baseCharacter.MainHandEnchant,
                baseCharacter.OffHandEnchant,
                baseCharacter.RangedEnchant,
                _character.ActiveBuffs, false, _character.CurrentModel);
            //foreach (KeyValuePair<string, string> kvp in _character.CalculationOptions)
            //	character.CalculationOptions.Add(kvp.Key, kvp.Value);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;
            //character.RecalculateSetBonuses();
            return character;
        }

        private Character BuildSingleEnchantSwapCharacter(Character baseCharacter, Character.CharacterSlot slot, Enchant enchant)
        {
            Character character = new Character(_character.Name, _character.Realm, _character.Region, _character.Race,
                baseCharacter.Head,
                baseCharacter.Neck,
                baseCharacter.Shoulders,
                baseCharacter.Back,
                baseCharacter.Chest,
                null, null,
                baseCharacter.Wrist,
                baseCharacter.Hands,
                baseCharacter.Waist,
                baseCharacter.Legs,
                baseCharacter.Feet,
                baseCharacter.Finger1,
                baseCharacter.Finger2,
                baseCharacter.Trinket1,
                baseCharacter.Trinket2,
                baseCharacter.MainHand,
                baseCharacter.OffHand,
                baseCharacter.Ranged,
                baseCharacter.Projectile,
                baseCharacter.ProjectileBag,
                slot == Character.CharacterSlot.Head ? enchant : baseCharacter.HeadEnchant,
                slot == Character.CharacterSlot.Shoulders ? enchant : baseCharacter.ShouldersEnchant,
                slot == Character.CharacterSlot.Back ? enchant : baseCharacter.BackEnchant,
                slot == Character.CharacterSlot.Chest ? enchant : baseCharacter.ChestEnchant,
                slot == Character.CharacterSlot.Wrist ? enchant : baseCharacter.WristEnchant,
                slot == Character.CharacterSlot.Hands ? enchant : baseCharacter.HandsEnchant,
                slot == Character.CharacterSlot.Legs ? enchant : baseCharacter.LegsEnchant,
                slot == Character.CharacterSlot.Feet ? enchant : baseCharacter.FeetEnchant,
                slot == Character.CharacterSlot.Finger1 ? enchant : baseCharacter.Finger1Enchant,
                slot == Character.CharacterSlot.Finger2 ? enchant : baseCharacter.Finger2Enchant,
                slot == Character.CharacterSlot.MainHand ? enchant : baseCharacter.MainHandEnchant,
                slot == Character.CharacterSlot.OffHand ? enchant : baseCharacter.OffHandEnchant,
                slot == Character.CharacterSlot.Ranged ? enchant : baseCharacter.RangedEnchant,
                _character.ActiveBuffs, false, _character.CurrentModel);
            //foreach (KeyValuePair<string, string> kvp in _character.CalculationOptions)
            //	character.CalculationOptions.Add(kvp.Key, kvp.Value);
            character.CalculationOptions = _character.CalculationOptions;
            character.Class = _character.Class;
            character.Talents = _character.Talents;
            character.EnforceMetagemRequirements = _character.EnforceMetagemRequirements;
            //character.RecalculateSetBonuses();
            return character;
        }

        private Item[] GetPossibleGemmedItemsForItem(Item item, string gemmedId, Item[] gemItems, Item[] metaGemItems)
        {
            List<Item> possibleGemmedItems = new List<Item>();
            if (!gemmedId.EndsWith("*.*.*") && gemmedId.IndexOf('.') >= 0)
            {
                item = ItemCache.FindItemById(gemmedId, false, false);
                if (item != null) possibleGemmedItems.Add(item);
            }
            else
            {
                Item[] possibleGem1s, possibleGem2s, possibleGem3s = null;
                switch (item.Sockets.Color1)
                {
                    case Item.ItemSlot.Meta:
                        possibleGem1s = metaGemItems;
                        break;
                    case Item.ItemSlot.Red:
                    case Item.ItemSlot.Orange:
                    case Item.ItemSlot.Yellow:
                    case Item.ItemSlot.Green:
                    case Item.ItemSlot.Blue:
                    case Item.ItemSlot.Purple:
                    case Item.ItemSlot.Prismatic:
                        possibleGem1s = gemItems;
                        break;
                    default:
                        possibleGem1s = new Item[] { null };
                        break;
                }
                switch (item.Sockets.Color2)
                {
                    case Item.ItemSlot.Meta:
                        possibleGem2s = metaGemItems;
                        break;
                    case Item.ItemSlot.Red:
                    case Item.ItemSlot.Orange:
                    case Item.ItemSlot.Yellow:
                    case Item.ItemSlot.Green:
                    case Item.ItemSlot.Blue:
                    case Item.ItemSlot.Purple:
                    case Item.ItemSlot.Prismatic:
                        possibleGem2s = gemItems;
                        break;
                    default:
                        possibleGem2s = new Item[] { null };
                        break;
                }
                switch (item.Sockets.Color3)
                {
                    case Item.ItemSlot.Meta:
                        possibleGem3s = metaGemItems;
                        break;
                    case Item.ItemSlot.Red:
                    case Item.ItemSlot.Orange:
                    case Item.ItemSlot.Yellow:
                    case Item.ItemSlot.Green:
                    case Item.ItemSlot.Blue:
                    case Item.ItemSlot.Purple:
                    case Item.ItemSlot.Prismatic:
                        possibleGem3s = gemItems;
                        break;
                    default:
                        possibleGem3s = new Item[] { null };
                        break;
                }

                foreach (Item gem1 in possibleGem1s)
                    foreach (Item gem2 in possibleGem2s)
                        foreach (Item gem3 in possibleGem3s)
                        {
                            //possibleGemmedItems.Add(ItemCache.Instance.FindItemById(string.Format("{0}.{1}.{2}.{3}", id0, id1, id2, id3), true, false, true));
                            // skip item cache, since we're creating new gemmings most likely they don't exist
                            // it will search through all item to find the item we already have and then clone it
                            // so skip all do that and do cloning ourselves
                            Item copy = new Item(item.Name, item.Quality, item.Type, item.Id, item.IconPath, item.Slot,
                                item.SetName, item.Unique, item.Stats.Clone(), item.Sockets.Clone(), 0, 0, 0, item.MinDamage,
                                item.MaxDamage, item.DamageType, item.Speed, item.RequiredClasses);
                            copy.SetGemInternal(1, gem1);
                            copy.SetGemInternal(2, gem2);
                            copy.SetGemInternal(3, gem3);
                            possibleGemmedItems.Add(copy);
                            //ItemCache.AddItem(copy, true, false);
                        }
            }

            return possibleGemmedItems.ToArray();
        }

        private Item[] FilterList(List<Item> unfilteredList)
        {
            List<Item> filteredList = new List<Item>();
            List<StatsColors> filteredStatsColors = new List<StatsColors>();
            foreach (Item gemmedItem in unfilteredList)
            {
                if (gemmedItem == null)
                {
                    filteredList.Add(gemmedItem);
                    continue;
                }
                int meta = 0, red = 0, yellow = 0, blue = 0;
                foreach (Item gem in new Item[] { gemmedItem.Gem1, gemmedItem.Gem2, gemmedItem.Gem3 })
                    if (gem != null)
                        switch (gem.Slot)
                        {
                            case Item.ItemSlot.Meta: meta++; break;
                            case Item.ItemSlot.Red: red++; break;
                            case Item.ItemSlot.Orange: red++; yellow++; break;
                            case Item.ItemSlot.Yellow: yellow++; break;
                            case Item.ItemSlot.Green: yellow++; blue++; break;
                            case Item.ItemSlot.Blue: blue++; break;
                            case Item.ItemSlot.Purple: blue++; red++; break;
                            case Item.ItemSlot.Prismatic: red++; yellow++; blue++; break;
                        }

                StatsColors statsColorsA = new StatsColors()
                {
                    GemmedItem = gemmedItem,
                    SetName = gemmedItem.SetName,
                    Stats = gemmedItem.GetTotalStats(),
                    Meta = meta,
                    Red = red,
                    Yellow = yellow,
                    Blue = blue
                };
                bool addItem = true;
                List<StatsColors> removeItems = new List<StatsColors>();
                foreach (StatsColors statsColorsB in filteredStatsColors)
                {
                    ArrayUtils.CompareResult compare = statsColorsA.CompareTo(statsColorsB);
                    if (compare == ArrayUtils.CompareResult.GreaterThan) //A>B
                    {
                        removeItems.Add(statsColorsB);
                    }
                    else if (compare == ArrayUtils.CompareResult.Equal || compare == ArrayUtils.CompareResult.LessThan)
                    {
                        addItem = false;
                        break;
                    }
                }
                foreach (StatsColors removeItem in removeItems)
                    filteredStatsColors.Remove(removeItem);
                if (addItem) filteredStatsColors.Add(statsColorsA);
            }
            foreach (StatsColors statsColors in filteredStatsColors)
            {
                filteredList.Add(statsColors.GemmedItem);
            }
            return filteredList.ToArray();
        }

        private class StatsColors
        {
            public Item GemmedItem;
            public Stats Stats;
            public int Meta;
            public int Red;
            public int Yellow;
            public int Blue;
            public string SetName;

            public ArrayUtils.CompareResult CompareTo(StatsColors other)
            {
                if (this.SetName != other.SetName) return ArrayUtils.CompareResult.Unequal;

                int compare = Meta.CompareTo(other.Meta);
                bool haveLessThan = compare < 0;
                bool haveGreaterThan = compare > 0;
                if (haveGreaterThan && haveLessThan) return ArrayUtils.CompareResult.Unequal;

                compare = Red.CompareTo(other.Red);
                haveLessThan |= compare < 0;
                haveGreaterThan |= compare > 0;
                if (haveGreaterThan && haveLessThan) return ArrayUtils.CompareResult.Unequal;

                compare = Yellow.CompareTo(other.Yellow);
                haveLessThan |= compare < 0;
                haveGreaterThan |= compare > 0;
                if (haveGreaterThan && haveLessThan) return ArrayUtils.CompareResult.Unequal;

                compare = Blue.CompareTo(other.Blue);
                haveLessThan |= compare < 0;
                haveGreaterThan |= compare > 0;
                if (haveGreaterThan && haveLessThan) return ArrayUtils.CompareResult.Unequal;

                ArrayUtils.CompareResult compareResult = Stats.CompareTo(other.Stats);
                if (compareResult == ArrayUtils.CompareResult.Unequal) return ArrayUtils.CompareResult.Unequal;
                haveLessThan |= compareResult == ArrayUtils.CompareResult.LessThan;
                haveGreaterThan |= compareResult == ArrayUtils.CompareResult.GreaterThan;
                if (haveGreaterThan && haveLessThan) return ArrayUtils.CompareResult.Unequal;
                else if (haveGreaterThan) return ArrayUtils.CompareResult.GreaterThan;
                else if (haveLessThan) return ArrayUtils.CompareResult.LessThan;
                else return ArrayUtils.CompareResult.Equal;
            }
            public static bool operator ==(StatsColors x, StatsColors y)
            {
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;
                return x.Meta == y.Meta && x.Red == y.Red && x.Yellow == y.Yellow
                    && x.Blue == y.Blue && x.Stats == y.Stats;
            }
            public override int GetHashCode()
            {
                return Stats.GetHashCode() ^ GemmedItem.GetHashCode();
            }
            public override bool Equals(object obj)
            {

                if (obj != null && obj.GetType() == this.GetType())
                {
                    return this == (obj as StatsColors);
                }
                return base.Equals(obj);
            }
            public static bool operator !=(StatsColors x, StatsColors y)
            {
                return !(x == y);
            }
        }
    }
}
