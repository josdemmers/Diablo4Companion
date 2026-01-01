using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using D4Companion.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;

namespace D4Companion.ViewModels.Dialogs
{
    public class SetAffixTypeColorViewModel : ObservableObject
    {
        private ObservableCollection<KeyValuePair<string, Color>> _colors = new();

        private Color _currentColor = System.Windows.Media.Colors.Green;
        private KeyValuePair<string, Color> _selectedColor = new KeyValuePair<string, Color>("Green", System.Windows.Media.Colors.Green);

        // Start of Constructors region

        #region Constructors

        public SetAffixTypeColorViewModel(Action<SetAffixTypeColorViewModel?> closeHandler, Color color)
        {
            // Init view commands
            CloseCommand = new RelayCommand<SetAffixTypeColorViewModel>(closeHandler);
            SetAffixColorDoneCommand = new RelayCommand(SetAffixColorDoneExecute);

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

        public ICommand CloseCommand { get; }
        public ICommand SetAffixColorDoneCommand { get; }

        public KeyValuePair<string, Color> SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                OnPropertyChanged();

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
                {
                    var value = prop.GetValue(null) as System.Windows.Media.Color?;
                    return new KeyValuePair<string, System.Windows.Media.Color>(prop.Name, value ?? default);
                });
        }


        #endregion
    }
}
