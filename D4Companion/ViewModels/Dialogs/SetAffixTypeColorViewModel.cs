using D4Companion.Entities;
using D4Companion.Interfaces;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetAffixTypeColorViewModel : BindableBase
    {
        private ObservableCollection<KeyValuePair<string, Color>> _colors = new();

        private Color _currentColor = System.Windows.Media.Colors.Green;
        private KeyValuePair<string, Color> _selectedColor = new KeyValuePair<string, Color>("Green", System.Windows.Media.Colors.Green);

        // Start of Constructors region

        #region Constructors

        public SetAffixTypeColorViewModel(Action<SetAffixTypeColorViewModel> closeHandler, Color color)
        {
            // Init View commands
            CloseCommand = new DelegateCommand<SetAffixTypeColorViewModel>(closeHandler);
            SetAffixColorDoneCommand = new DelegateCommand(SetAffixColorDoneExecute);

            _currentColor = color;

            // Init colors
            InitColors();
        }

        #endregion

        // Start of Events region

        #region Events

        #endregion

        // Start of Properties region

        #region Properties

        public ObservableCollection<KeyValuePair<string, Color>> Colors { get => _colors; set => _colors = value; }

        public DelegateCommand<SetAffixTypeColorViewModel> CloseCommand { get; }
        public DelegateCommand SetAffixColorDoneCommand { get; }

        public KeyValuePair<string, Color> SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                RaisePropertyChanged();

                _currentColor = _selectedColor.Value;
            }
        }

        #endregion

        // Start of Event handlers region

        #region Event handlers

        private void SetAffixColorDoneExecute()
        {
            CloseCommand.Execute(this);
        }

        #endregion

        // Start of Methods region

        #region Methods

        private void InitColors()
        {
            Colors.Clear();
            Colors.AddRange(GetColors());

            string colorName = _colors.FirstOrDefault(c => c.Value == _currentColor).Key ?? string.Empty;
            SelectedColor = new KeyValuePair<string, Color>(colorName, _currentColor);
        }

        private IEnumerable<KeyValuePair<string, Color>> GetColors()
        {
            return typeof(Colors)
                .GetProperties()
                .Where(prop =>
                    typeof(Color).IsAssignableFrom(prop.PropertyType))
                .Select(prop =>
                    new KeyValuePair<string, Color>(prop.Name, (Color)prop.GetValue(null)));
        }


        #endregion
    }
}
