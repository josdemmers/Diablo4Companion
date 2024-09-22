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
    public class UniqueInfoBase : BindableBase
    {

    }

    public class UniqueInfoConfig : UniqueInfoBase
    {

    }

    public class UniqueInfoWanted : UniqueInfoBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private UniqueInfo _uniqueInfo = new UniqueInfo();

        // Start of Constructors region

        #region Constructors

        public UniqueInfoWanted(UniqueInfo uniqueInfo)
        {
            _uniqueInfo = uniqueInfo;

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
            get => _uniqueInfo.AllowedForPlayerClass;
        }

        public string Description
        {
            get => _uniqueInfo.Description;
        }

        public string IdName
        {
            get => _uniqueInfo.IdName;
        }

        public bool IsClassBarb
        {
            get => _uniqueInfo.AllowedForPlayerClass[2] == 1;
        }

        public bool IsClassDruid
        {
            get => _uniqueInfo.AllowedForPlayerClass[1] == 1;
        }

        public bool IsClassNecro
        {
            get => _uniqueInfo.AllowedForPlayerClass[4] == 1;
        }

        public bool IsClassRogue
        {
            get => _uniqueInfo.AllowedForPlayerClass[3] == 1;
        }

        public bool IsClassSorc
        {
            get => _uniqueInfo.AllowedForPlayerClass[0] == 1;
        }

        public bool IsClassSpiritborn
        {
            get => _uniqueInfo.AllowedForPlayerClass[5] == 1;
        }

        public UniqueInfo Model
        {
            get => _uniqueInfo;
        }

        public string Name 
        {
            get => _uniqueInfo.Name;
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        #endregion

        // Start of Methods region

        #region Methods

        #endregion
    }

    public class UniqueInfoCustomSort : IComparer
    {
        public int Compare(object? x, object? y)
        {
            int result = -1;

            if ((x.GetType() == typeof(UniqueInfoConfig)) && !(y.GetType() == typeof(UniqueInfoConfig))) return -1;
            if ((y.GetType() == typeof(UniqueInfoConfig)) && !(x.GetType() == typeof(UniqueInfoConfig))) return 1;

            if ((x.GetType() == typeof(UniqueInfoWanted)) && (y.GetType() == typeof(UniqueInfoWanted)))
            {
                var itemX = (UniqueInfoWanted)x;
                var itemY = (UniqueInfoWanted)y;

                result = itemX.Name.CompareTo(itemY.Name);
            }

            return result;
        }
    }
}
