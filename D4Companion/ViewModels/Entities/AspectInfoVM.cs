﻿using D4Companion.Entities;
using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;

namespace D4Companion.ViewModels.Entities
{
    public class AspectInfoVM : BindableBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ISystemPresetManager _systemPresetManager;

        private AspectInfo _aspectInfo = new AspectInfo();

        // Start of Constructors region

        #region Constructors

        public AspectInfoVM(AspectInfo aspectInfo)
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
}
