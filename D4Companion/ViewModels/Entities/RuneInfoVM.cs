using CommunityToolkit.Mvvm.ComponentModel;
using D4Companion.Constants;
using D4Companion.Entities;
using D4Companion.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace D4Companion.ViewModels.Entities
{
    public class RuneInfoBase : ObservableObject
    {

    }

    public class RuneInfoConfig : RuneInfoBase
    {

    }

    public class RuneInfoWanted : RuneInfoBase
    {
        private readonly ISystemPresetManager _systemPresetManager;

        private RuneInfo _runeInfo = new RuneInfo();

        // Start of Constructors region

        #region Constructors

        public RuneInfoWanted(RuneInfo runeInfo)
        {
            _runeInfo = runeInfo;

            // Init services
            _systemPresetManager = App.Current.Services.GetRequiredService<ISystemPresetManager>();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public string Description
        {
            get => _runeInfo.Description;
        }

        public string IdName
        {
            get => _runeInfo.IdName;
        }

        public RuneInfo Model
        {
            get => _runeInfo;
        }

        public string Name 
        {
            get => _runeInfo.Name;
        }

        public bool IsCondition
        {
            get => _runeInfo.RuneType.Equals(RuneTypeConstants.Condition);
        }

        public bool IsEffect
        {
            get => _runeInfo.RuneType.Equals(RuneTypeConstants.Effect);
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }

    public class RuneInfoCustomSort : IComparer
    {
        public int Compare(object? x, object? y)
        {
            int result = -1;

            if (x == null) return -1;
            if (y == null) return 1;
            if ((x.GetType() == typeof(RuneInfoConfig)) && !(y.GetType() == typeof(RuneInfoConfig))) return -1;
            if ((y.GetType() == typeof(RuneInfoConfig)) && !(x.GetType() == typeof(RuneInfoConfig))) return 1;

            if ((x.GetType() == typeof(RuneInfoWanted)) && (y.GetType() == typeof(RuneInfoWanted)))
            {
                var itemX = (RuneInfoWanted)x;
                var itemY = (RuneInfoWanted)y;

                result = itemX.Name.CompareTo(itemY.Name);
            }

            return result;
        }
    }
}
