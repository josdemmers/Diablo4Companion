using D4Companion.Events;
using D4Companion.Interfaces;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D4Companion.ViewModels.Entities
{
    public class ControllerImageVM : BindableBase
    {
        //private readonly IEventAggregator _eventAggregator;
        //private readonly ISystemPresetManager _systemPresetManager;

        private string _fileName = string.Empty;
        private string _folder = string.Empty;

        // Start of Constructors region

        #region Constructors

        public ControllerImageVM(string folder, string fileName)
        {
            _fileName = fileName;
            _folder = folder;

            // Init IEventAggregator
            //_eventAggregator = (IEventAggregator)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(IEventAggregator));

            // Init services
            //_systemPresetManager = (ISystemPresetManager)Prism.Ioc.ContainerLocator.Container.Resolve(typeof(ISystemPresetManager));
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public string FileName
        {
            get => _fileName;
        }

        public string Folder
        {
            get => _folder;
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
