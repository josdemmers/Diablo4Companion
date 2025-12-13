using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace D4Companion.ViewModels.Entities
{
    public class AspectInfoBase : BindableBase
    {

    }

    public class AspectInfoConfig : AspectInfoBase
    {

    }

    public class AspectInfoWanted : AspectInfoBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private AspectInfo _aspectInfo = new AspectInfo();

        // Start of Constructors region

        #region Constructors

        public AspectInfoWanted(AspectInfo aspectInfo)
        {
            _aspectInfo = aspectInfo;

            // Init IEventAggregator
            _eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            _systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public List<int> AllowedForPlayerClass
        {
            get => _aspectInfo.AllowedForPlayerClass;
        }

        public string Description
        {
            get => _aspectInfo.Description;
        }

        public string Dungeon
        {
            get => _aspectInfo.Dungeon;
        }

        public string IdName
        {
            get => _aspectInfo.IdName;
        }

        public bool IsClassBarb
        {
            get => _aspectInfo.AllowedForPlayerClass[2] == 1;
        }

        public bool IsClassDruid
        {
            get => _aspectInfo.AllowedForPlayerClass[1] == 1;
        }

        public bool IsClassNecro
        {
            get => _aspectInfo.AllowedForPlayerClass[4] == 1;
        }

        public bool IsClassPaladin
        {
            get => _aspectInfo.AllowedForPlayerClass[6] == 1;
        }

        public bool IsClassRogue
        {
            get => _aspectInfo.AllowedForPlayerClass[3] == 1;
        }

        public bool IsClassSorc
        {
            get => _aspectInfo.AllowedForPlayerClass[0] == 1;
        }

        public bool IsClassSpiritborn
        {
            get => _aspectInfo.AllowedForPlayerClass[5] == 1;
        }

        public bool IsCodex
        {
            get => _aspectInfo.IsCodex && !string.IsNullOrWhiteSpace(_aspectInfo.Dungeon);
        }

        public bool IsDropOnly
        {
            get => string.IsNullOrWhiteSpace(_aspectInfo.Dungeon);
        }

        public bool IsSeasonal
        {
            get => _aspectInfo.IsSeasonal;
        }

        public AspectInfo Model
        {
            get => _aspectInfo;
        }

        public string Name 
        {
            get => _aspectInfo.Name;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }

    public class AspectInfoCustomSort : IComparer
    {
        public int Compare(object? x, object? y)
        {
            int result = -1;

            if ((x.GetType() == typeof(AspectInfoConfig)) && !(y.GetType() == typeof(AspectInfoConfig))) return -1;
            if ((y.GetType() == typeof(AspectInfoConfig)) && !(x.GetType() == typeof(AspectInfoConfig))) return 1;

            if ((x.GetType() == typeof(AspectInfoWanted)) && (y.GetType() == typeof(AspectInfoWanted)))
            {
                var itemX = (AspectInfoWanted)x;
                var itemY = (AspectInfoWanted)y;

                result = itemX.Name.CompareTo(itemY.Name);
            }

            return result;
        }
    }
}
