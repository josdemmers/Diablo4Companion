using CommunityToolkit.Mvvm.ComponentModel;

namespace D4Companion.ViewModels.Entities
{
    public class ControllerImageVM : ObservableObject
    {
        private string _fileName = string.Empty;
        private string _folder = string.Empty;

        // Start of Constructors region

        #region Constructors

        public ControllerImageVM(string folder, string fileName)
        {
            _fileName = fileName;
            _folder = folder;
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
